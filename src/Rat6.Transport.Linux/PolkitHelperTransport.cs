using System.Diagnostics;
using System.Text.Json;
using Rat6.Helper.Contract;
using Rat6.Transport;

namespace Rat6.Transport.Linux;

public sealed class PolkitHelperTransport : IHidTransport
{
    public const string DefaultPkexecPath = "/usr/bin/pkexec";

    private const int OpenTimeoutMs = 120_000;
    private const int OperationTimeoutMs = 10_000;
    private const int MaxReportLength = 4096;

    private readonly Func<HelperLaunch> _launchFactory;
    private readonly object _gate = new();
    private Process? _process;
    private StreamWriter? _input;
    private StreamReader? _output;
    private Task<string>? _stderr;
    private int _nextId;

    public PolkitHelperTransport()
        : this(CreateDefaultLaunch)
    {
    }

    public PolkitHelperTransport(Func<HelperLaunch> launchFactory)
    {
        _launchFactory = launchFactory ?? throw new ArgumentNullException(nameof(launchFactory));
    }

    public static PolkitHelperTransport Open(string path)
    {
        var transport = new PolkitHelperTransport();
        transport.OpenDevice(path);
        return transport;
    }

    public void OpenDevice(string path)
    {
        lock (_gate)
        {
            if (_process is not null)
            {
                throw new InvalidOperationException("polkit-helper is already running.");
            }

            var launch = _launchFactory();
            var startInfo = new ProcessStartInfo
            {
                FileName = launch.FileName,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            foreach (var argument in launch.Arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            try
            {
                _process = Process.Start(startInfo);
            }
            catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException)
            {
                throw new IOException($"Could not start polkit-helper: {exception.Message}", exception);
            }

            if (_process is null)
            {
                throw new IOException("Could not start polkit-helper.");
            }

            _input = _process.StandardInput;
            _output = _process.StandardOutput;
            _stderr = _process.StandardError.ReadToEndAsync();

            try
            {
                Send(new HelperRequest(NextId(), "open", Path: path), OpenTimeoutMs);
            }
            catch (IOException exception) when (IsAuthorizationError(exception.Message))
            {
                Dispose();
                throw new UnauthorizedAccessException(
                    "Administrator rights are required (pkexec).",
                    exception);
            }
            catch
            {
                Dispose();
                throw;
            }
        }
    }

    public byte[] GetFeature(byte reportId, int length)
    {
        if (length is < 1 or > MaxReportLength)
        {
            throw new ArgumentOutOfRangeException(nameof(length), length, "Report length must be 1..4096.");
        }

        lock (_gate)
        {
            var response = Send(
                new HelperRequest(NextId(), "get", ReportId: reportId, Length: length),
                OperationTimeoutMs);
            if (string.IsNullOrEmpty(response.Data))
            {
                return [];
            }

            byte[] data;
            try
            {
                data = Convert.FromBase64String(response.Data);
            }
            catch (FormatException exception)
            {
                throw new IOException("polkit-helper returned invalid data.", exception);
            }

            return data.Length <= length
                ? data
                : throw new IOException("polkit-helper returned a report longer than requested.");
        }
    }

    public void SetFeature(ReadOnlySpan<byte> report)
    {
        if (report.Length is < 1 or > MaxReportLength)
        {
            throw new ArgumentOutOfRangeException(nameof(report), report.Length, "Report length must be 1..4096.");
        }

        lock (_gate)
        {
            Send(
                new HelperRequest(NextId(), "set", Data: Convert.ToBase64String(report)),
                OperationTimeoutMs);
        }
    }

    public byte[]? ReadReport(int timeoutMs) =>
        throw new NotSupportedException(
            "polkit-helper does not support streaming reads in this build.");

    public void Dispose()
    {
        lock (_gate)
        {
            if (_process is null)
            {
                return;
            }

            try
            {
                if (_input is not null)
                {
                    _input.WriteLine(HelperProtocol.Serialize(new HelperRequest(NextId(), "close")));
                    _input.Flush();
                }
            }
            catch (Exception exception) when (exception is IOException or ObjectDisposedException)
            {
            }

            try
            {
                _input?.Close();
            }
            catch (Exception exception) when (exception is IOException or ObjectDisposedException)
            {
            }

            try
            {
                if (!_process.WaitForExit(2000))
                {
                    Kill();
                }
            }
            catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
            {
            }

            _process.Dispose();
            _process = null;
            _input = null;
            _output = null;
            _stderr = null;
        }
    }

    private HelperResponse Send(HelperRequest request, int timeoutMs)
    {
        if (_process is null || _input is null || _output is null)
        {
            throw new IOException("polkit-helper is not running.");
        }

        try
        {
            _input.WriteLine(HelperProtocol.Serialize(request));
            _input.Flush();
        }
        catch (Exception exception) when (exception is IOException or ObjectDisposedException)
        {
            throw new IOException($"Could not send a request to polkit-helper: {exception.Message}", exception);
        }

        Task<string?> read;
        try
        {
            read = _output.ReadLineAsync();
        }
        catch (Exception exception) when (exception is IOException or ObjectDisposedException)
        {
            throw new IOException($"Could not read the polkit-helper reply: {exception.Message}", exception);
        }

        try
        {
            if (!read.Wait(timeoutMs))
            {
                Kill();
                throw new IOException($"polkit-helper did not reply within {timeoutMs} ms.");
            }
        }
        catch (AggregateException exception)
        {
            Kill();
            throw new IOException(
                $"polkit-helper exited: {exception.InnerException?.Message ?? exception.Message}",
                exception);
        }

        var line = read.Result;
        if (line is null)
        {
            var error = ReadStderr();
            throw new IOException(
                string.IsNullOrWhiteSpace(error)
                    ? "polkit-helper exited without a reply."
                    : $"polkit-helper: {error}");
        }

        HelperResponse response;
        try
        {
            response = HelperProtocol.DeserializeResponse(line);
        }
        catch (Exception exception) when (exception is JsonException or FormatException)
        {
            throw new IOException($"polkit-helper returned an invalid reply: {exception.Message}", exception);
        }

        if (response.Id != request.Id)
        {
            throw new IOException($"polkit-helper returned a reply with a mismatched id: {response.Id}.");
        }

        if (!response.Ok)
        {
            throw new IOException(response.Error ?? "polkit-helper returned an error.");
        }

        return response;
    }

    private void Kill()
    {
        try
        {
            if (_process is { HasExited: false })
            {
                _process.Kill(entireProcessTree: true);
            }
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
        }
    }

    private string ReadStderr()
    {
        if (_stderr is null)
        {
            return string.Empty;
        }

        try
        {
            return _stderr.Wait(1000) ? _stderr.Result.Trim() : string.Empty;
        }
        catch (AggregateException)
        {
            return string.Empty;
        }
    }

    private int NextId() => Interlocked.Increment(ref _nextId);

    private static bool IsAuthorizationError(string message) =>
        message.Contains("Not authorized", StringComparison.OrdinalIgnoreCase)
        || message.Contains("dismissed", StringComparison.OrdinalIgnoreCase)
        || message.Contains("Authentication failure", StringComparison.OrdinalIgnoreCase)
        || message.Contains("not allowed", StringComparison.OrdinalIgnoreCase);

    private static HelperLaunch CreateDefaultLaunch()
    {
        if (!File.Exists(DefaultPkexecPath))
        {
            throw new IOException($"pkexec not found: {DefaultPkexecPath}");
        }

        var helper = FindHelper()
            ?? throw new IOException("Rat6.Helper was not found next to the app; build the solution.");

        var wrapper = WriteWrapper(StageHelper(helper));
        return new HelperLaunch(DefaultPkexecPath, ["/bin/bash", wrapper]);
    }

    private static HelperCommand? FindHelper()
    {
        var baseDirectory = AppContext.BaseDirectory;
        var fromBase = FindInDirectory(baseDirectory);
        if (fromBase is not null)
        {
            return fromBase;
        }

        var repository = FindRepositoryRoot();
        if (repository is null)
        {
            return null;
        }

        foreach (var configuration in new[] { "Debug", "Release" })
        {
            var directory = Path.Combine(
                repository,
                "src",
                "Rat6.Helper",
                "bin",
                configuration,
                "net10.0");
            var helper = FindInDirectory(directory);
            if (helper is not null)
            {
                return helper;
            }
        }

        return null;
    }

    private static HelperCommand? FindInDirectory(string directory)
    {
        var appHost = Path.Combine(directory, "Rat6.Helper");
        var assembly = Path.Combine(directory, "Rat6.Helper.dll");
        var runtimeConfig = Path.Combine(directory, "Rat6.Helper.runtimeconfig.json");

        if (File.Exists(appHost))
        {
            return new HelperCommand(appHost, []);
        }

        if (File.Exists(assembly) && File.Exists(runtimeConfig) && File.Exists("/usr/bin/dotnet"))
        {
            return new HelperCommand("/usr/bin/dotnet", [assembly]);
        }

        return null;
    }

    private static HelperCommand StageHelper(HelperCommand helper)
    {
        if (helper.Arguments.Count != 0
            || !string.Equals(Path.GetFileName(helper.FileName), "Rat6.Helper", StringComparison.Ordinal))
        {
            return helper;
        }

        var sourceDirectory = Path.GetDirectoryName(helper.FileName);
        if (sourceDirectory is null || File.Exists(Path.Combine(sourceDirectory, "Rat6.Helper.dll")))
        {
            return helper;
        }

        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".local",
            "share",
            "rat6");
        Directory.CreateDirectory(directory);

        var target = Path.Combine(directory, "Rat6.Helper");
        try
        {
            var source = new FileInfo(helper.FileName);
            var staged = new FileInfo(target);
            if (!staged.Exists
                || staged.Length != source.Length
                || staged.LastWriteTimeUtc != source.LastWriteTimeUtc)
            {
                var temporary = Path.Combine(
                    directory,
                    $".Rat6.Helper.{Environment.ProcessId}.{Guid.NewGuid():N}.tmp");
                File.Copy(helper.FileName, temporary, overwrite: true);
                SetExecutable(temporary);
                File.Move(temporary, target, overwrite: true);
            }

            SetExecutable(target);
            return new HelperCommand(target, []);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new IOException($"Could not stage Rat6.Helper for pkexec: {exception.Message}", exception);
        }
    }

    private static void SetExecutable(string path)
    {
        if (OperatingSystem.IsLinux())
        {
            File.SetUnixFileMode(
                path,
                UnixFileMode.UserRead
                | UnixFileMode.UserWrite
                | UnixFileMode.UserExecute
                | UnixFileMode.GroupRead
                | UnixFileMode.GroupExecute
                | UnixFileMode.OtherRead
                | UnixFileMode.OtherExecute);
        }
    }

    private static string WriteWrapper(HelperCommand helper)
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".local",
            "share",
            "rat6");
        Directory.CreateDirectory(directory);

        var wrapper = Path.Combine(directory, "privileged-helper.sh");
        var command = string.Join(
            " ",
            new[] { helper.FileName }.Concat(helper.Arguments).Select(ShellQuote));

        File.WriteAllText(wrapper, $"#!/bin/bash\nexec {command} \"$@\"\n");
        if (OperatingSystem.IsLinux())
        {
            File.SetUnixFileMode(
                wrapper,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }

        return wrapper;
    }

    private static string? FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Rat6.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName;
    }

    private static string ShellQuote(string value) =>
        "'" + value.Replace("'", "'\\''") + "'";

    private sealed record HelperCommand(string FileName, IReadOnlyList<string> Arguments);
}
