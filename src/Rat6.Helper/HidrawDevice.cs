using System.Runtime.InteropServices;
using Rat6.Transport.Linux;

namespace Rat6.Helper;

public sealed class HidrawDevice : IHidrawDevice
{
    private const int MaxReportLength = 4096;
    private static readonly byte[] CommandReportMarker = [0x85, 0x0C];
    private int _fd = -1;

    public static bool IsSupportedPath(string path)
    {
        var fullPath = Path.GetFullPath(path);
        if (!fullPath.StartsWith("/dev/hidraw", StringComparison.Ordinal))
        {
            return false;
        }

        var name = Path.GetFileName(fullPath);
        if (name.Length <= 6 || !name[6..].All(char.IsDigit))
        {
            return false;
        }

        var ueventPath = Path.Combine("/sys/class/hidraw", name, "device", "uevent");
        if (!File.Exists(ueventPath))
        {
            return false;
        }

        var uevent = File.ReadAllText(ueventPath);
        if (!uevent.Contains("HID_ID=0003:000012CF:00000C04", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var descriptorPath = Path.Combine("/sys/class/hidraw", name, "device", "report_descriptor");
        if (!File.Exists(descriptorPath))
        {
            return false;
        }

        var descriptor = ReadDescriptor(descriptorPath);
        return Contains(descriptor, CommandReportMarker);
    }

    public void Open(string path)
    {
        if (!IsSupportedPath(path))
        {
            throw new UnauthorizedAccessException(
                $"helper: {path} is not a R.A.T. 6+ (12cf:0c04).");
        }

        if (_fd >= 0)
        {
            HidrawNative.close(_fd);
            _fd = -1;
        }

        _fd = HidrawNative.open(path, HidrawNative.ORdwr | HidrawNative.ONonblock);
        if (_fd < 0)
        {
            throw new IOException($"helper: open({path}) failed, errno {Marshal.GetLastPInvokeError()}.");
        }
    }

    public byte[] GetFeature(byte reportId, int length)
    {
        EnsureOpen();
        if (length is < 1 or > MaxReportLength)
        {
            throw new ArgumentOutOfRangeException(nameof(length), length, "Report length must be 1..4096.");
        }

        var buffer = new byte[length];
        buffer[0] = reportId;
        var result = HidrawNative.GetFeature(_fd, buffer);
        if (result < 0)
        {
            throw new IOException(
                $"helper: GET_FEATURE 0x{reportId:X2} failed, errno {Marshal.GetLastPInvokeError()}.");
        }

        return buffer[..Math.Min(result, length)];
    }

    public void SetFeature(ReadOnlySpan<byte> report)
    {
        EnsureOpen();
        if (report.Length is < 1 or > MaxReportLength)
        {
            throw new ArgumentOutOfRangeException(nameof(report), report.Length, "Report length must be 1..4096.");
        }

        var result = HidrawNative.SetFeature(_fd, report.ToArray());
        if (result < 0)
        {
            throw new IOException(
                $"helper: SET_FEATURE failed, errno {Marshal.GetLastPInvokeError()}.");
        }
    }

    public void Dispose()
    {
        if (_fd >= 0)
        {
            HidrawNative.close(_fd);
            _fd = -1;
        }
    }

    private void EnsureOpen()
    {
        if (_fd < 0)
        {
            throw new InvalidOperationException("helper: device is not open.");
        }
    }

    private static byte[] ReadDescriptor(string path)
    {
        using var stream = File.OpenRead(path);
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    private static bool Contains(ReadOnlySpan<byte> data, ReadOnlySpan<byte> marker)
    {
        for (var index = 0; index <= data.Length - marker.Length; index++)
        {
            if (data.Slice(index, marker.Length).SequenceEqual(marker))
            {
                return true;
            }
        }

        return false;
    }
}
