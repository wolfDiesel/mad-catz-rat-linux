using System.Globalization;
using System.Text.Json;
using Rat6.Device;
using Rat6.Protocol;
using Rat6.Transport;
using Rat6.Transport.Linux;

namespace Rat6.Cli;

internal static class Program
{
    private const int ProfileSlots = 4;

    private static int Main(string[] args)
    {
        try
        {
            return Run(args);
        }
        catch (Rat6Exception exception)
        {
            Console.Error.WriteLine($"error: {exception.Message}");
            return 1;
        }
        catch (IOException exception)
        {
            Console.Error.WriteLine($"error: {exception.Message}");
            if (exception.Message.Contains("errno 13", StringComparison.Ordinal))
            {
                Console.Error.WriteLine($"run: sudo {Environment.ProcessPath ?? "rat6"} <command>");
                Console.Error.WriteLine("or run without sudo — the app will request privileges via pkexec.");
            }

            return 2;
        }
        catch (UnauthorizedAccessException exception)
        {
            Console.Error.WriteLine($"error: {exception.Message}");
            Console.Error.WriteLine("run: sudo <rat6> <command> or run without sudo — pkexec will prompt.");
            return 2;
        }
        catch (FormatException exception)
        {
            Console.Error.WriteLine($"error: {exception.Message}");
            return 1;
        }
        catch (ArgumentOutOfRangeException exception)
        {
            Console.Error.WriteLine($"error: {exception.Message}");
            return 1;
        }
        catch (ArgumentException exception)
        {
            Console.Error.WriteLine($"error: {exception.Message}");
            return 1;
        }
    }

    private static int Run(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help")
        {
            PrintHelp();
            return 0;
        }

        var command = args[0];
        var deviceOption = Option(args, "--device");

        using var device = OpenDevice(deviceOption);

        return command switch
        {
            "info" => Info(device),
            "dump" => Dump(device, Option(args, "--out")),
            "backup" => Backup(device, Option(args, "--out")),
            "dpi" => Dpi(device, args),
            "led" => Led(device, args),
            "restore" => Restore(device, args),
            "select" => Select(device, args),
            _ => Unknown(command),
        };
    }

    private static Rat6Device OpenDevice(string? path)
    {
        if (path is null)
        {
            var info = HidrawDiscovery.Find()
                ?? throw new Rat6Exception("R.A.T. 6+ (12cf:0c04) not found.");
            path = info.Path;
        }

        Console.WriteLine($"device: {path}");
        return new Rat6Device(Rat6TransportFactory.Open(path));
    }

    private static int Info(IRat6Device device)
    {
        Console.WriteLine($"firmware: {device.GetFirmware()}");
        var selected = device.GetSelectedSlot();
        Console.WriteLine($"selected slot: {selected.Slot} (profiles: {selected.Count})");

        for (var slot = 0; slot < ProfileSlots; slot++)
        {
            var profile = device.ReadProfile(slot);
            var dpi = string.Join("/", profile.DpiStages.Select(stage => stage.X.ToString(CultureInfo.InvariantCulture)));
            var zones = string.Join(
                ", ",
                Enumerable.Range(0, ProfileLayout.LedZones)
                    .Select(index => profile.GetLedZone(index))
                    .Select(zone => $"{zone.Index + 1}:{ModeName(zone.Mode)} {zone.Color.ToHex()}"));

            Console.WriteLine($"slot {slot}: DPI {dpi}; LED {zones}; checksum 0x{profile.Checksum:X2}");
        }

        return 0;
    }

    private static int Dump(IRat6Device device, string? output)
    {
        var directory = output ?? Path.Combine(FindRepositoryRoot(), "dumps");
        Directory.CreateDirectory(directory);

        var profiles = new List<object>();
        foreach (var slot in Enumerable.Range(0, ProfileSlots))
        {
            var profile = device.ReadProfile(slot);
            File.WriteAllBytes(Path.Combine(directory, $"slot{slot}.bin"), profile.ToArray());
            profiles.Add(new
            {
                slot,
                checksum = $"0x{profile.Checksum:X2}",
                dpi = profile.DpiStages.Select(stage => new { x = stage.X, y = stage.Y }),
                led = Enumerable.Range(0, ProfileLayout.LedZones).Select(index =>
                {
                    var zone = profile.GetLedZone(index);
                    return new { zone = index + 1, mode = zone.Mode, color = zone.Color.ToHex() };
                }),
            });
            Console.WriteLine($"slot {slot}: saved");
        }

        var metadataPath = Path.Combine(directory, "cli-probe.json");
        File.WriteAllText(
            metadataPath,
            JsonSerializer.Serialize(
                new { generatedAt = DateTimeOffset.UtcNow, profiles },
                new JsonSerializerOptions { WriteIndented = true }));

        Console.WriteLine($"metadata: {metadataPath}");
        return 0;
    }

    private static int Backup(IRat6Device device, string? output)
    {
        var directory = device.BackupAll(output);
        Console.WriteLine($"backup: {directory}");
        return 0;
    }

    private static int Dpi(IRat6Device device, string[] args)
    {
        var slot = ParseSlot(args);
        var values = ParseDpiValues(Require(args, "--values"));
        var apply = Flag(args, "--apply");

        var profile = device.ReadProfile(slot);
        PrintDpi("current", profile);
        for (var index = 0; index < values.Count; index++)
        {
            profile.SetDpiStage(index, new DpiStage(values[index], values[index]));
        }

        PrintDpi("planned", profile);
        if (!apply)
        {
            Console.WriteLine("dry run: add --apply to write to the mouse.");
            return 0;
        }

        var backup = device.BackupAll();
        Console.WriteLine($"backup: {backup}");
        device.WriteProfile(slot, profile);
        device.Reload();

        var written = device.ReadProfile(slot);
        for (var index = 0; index < values.Count; index++)
        {
            var stage = written.GetDpiStage(index);
            if (stage.X != values[index] || stage.Y != values[index])
            {
                throw new Rat6Exception($"Post-write verification failed: stage {index + 1} = {stage.X}/{stage.Y}.");
            }
        }

        Console.WriteLine("written and verified.");
        return 0;
    }

    private static int Led(IRat6Device device, string[] args)
    {
        var slot = ParseSlot(args);
        var zone = Require(args, "--zone");
        var mode = ParseLedMode(Option(args, "--mode") ?? "solid");
        var color = RgbColor.FromHex(Require(args, "--color"));
        var brightness = ParseByte(Option(args, "--brightness") ?? "255");
        var period = ParsePeriod(Option(args, "--period"));
        var apply = Flag(args, "--apply");

        var profile = device.ReadProfile(slot);
        var zones = zone.Equals("all", StringComparison.OrdinalIgnoreCase)
            ? Enumerable.Range(0, ProfileLayout.LedZones).ToArray()
            : [ParseZone(zone)];

        foreach (var index in zones)
        {
            profile.SetLed(index, mode, color, brightness, period);
        }

        Console.WriteLine(
            $"slot {slot}: zone {zone}, {ModeName(mode)} {color.ToHex()}, brightness {brightness}"
            + (period is { } p ? $", period {p}" : string.Empty));
        if (!apply)
        {
            Console.WriteLine("dry run: add --apply to write to the mouse.");
            return 0;
        }

        var backup = device.BackupAll();
        Console.WriteLine($"backup: {backup}");
        device.WriteProfile(slot, profile);
        device.Reload();

        var written = device.ReadProfile(slot);
        foreach (var index in zones)
        {
            var writtenZone = written.GetLedZone(index);
            if (writtenZone.Mode != mode)
            {
                throw new Rat6Exception($"Post-write LED mode verification failed: zone {index + 1}.");
            }

            if (mode == LedZone.BreathingMode && period is { } expectedPeriod && writtenZone.Period != expectedPeriod)
            {
                throw new Rat6Exception($"Post-write breathing period verification failed: zone {index + 1}.");
            }

            if (mode != LedZone.RainbowMode
                && (writtenZone.Color != color || writtenZone.Brightness != brightness))
            {
                throw new Rat6Exception($"Post-write LED verification failed: zone {index + 1}.");
            }
        }

        Console.WriteLine("written and verified.");
        return 0;
    }

    private static int Restore(IRat6Device device, string[] args)
    {
        var directory = Require(args, "--dir");
        var slots = Option(args, "--slot") is null
            ? Enumerable.Range(0, ProfileSlots).ToArray()
            : [ParseSlot(args)];

        if (!Flag(args, "--yes"))
        {
            Console.WriteLine($"restore: {string.Join(", ", slots)} from {directory}; add --yes.");
            return 0;
        }

        var backup = device.BackupAll();
        Console.WriteLine($"backup: {backup}");

        foreach (var slot in slots)
        {
            var path = Path.Combine(directory, $"slot{slot}.bin");
            var data = File.ReadAllBytes(path);
            var profile = new Profile(data);
            device.WriteProfile(slot, profile);
        }

        device.Reload();

        foreach (var slot in slots)
        {
            var path = Path.Combine(directory, $"slot{slot}.bin");
            var expected = File.ReadAllBytes(path);
            var actual = device.ReadProfile(slot).ToArray();
            if (!expected.SequenceEqual(actual))
            {
                throw new Rat6Exception($"Restore verification failed: slot {slot}.");
            }
        }

        Console.WriteLine($"restored and verified: {string.Join(", ", slots)}");
        return 0;
    }

    private static int Select(IRat6Device device, string[] args)
    {
        var slot = ParseSlot(args);
        if (!Flag(args, "--yes"))
        {
            Console.WriteLine($"slot {slot}: command 0x07 is unverified on 6+; add --yes.");
            return 0;
        }

        device.SelectSlot(slot);
        Console.WriteLine($"slot {slot}: selected and verified");
        return 0;
    }

    private static void PrintDpi(string label, Profile profile)
    {
        var stages = string.Join("/", profile.DpiStages.Select(stage => $"{stage.X}"));
        Console.WriteLine($"{label}: {stages}");
    }

    private static int ParseSlot(string[] args)
    {
        var value = Option(args, "--slot") ?? throw new FormatException("missing --slot 0..3");
        var slot = int.Parse(value, CultureInfo.InvariantCulture);
        if (slot is < 0 or >= ProfileSlots)
        {
            throw new ArgumentOutOfRangeException(nameof(slot), slot, "Slot must be 0..3.");
        }

        return slot;
    }

    private static IReadOnlyList<int> ParseDpiValues(string text)
    {
        var parts = text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != ProfileLayout.DpiStages)
        {
            throw new FormatException($"need {ProfileLayout.DpiStages} comma-separated DPI values");
        }

        var values = new int[parts.Length];
        for (var index = 0; index < parts.Length; index++)
        {
            var dpi = int.Parse(parts[index], CultureInfo.InvariantCulture);
            if (dpi < ProfileLayout.DpiMin || dpi > 10000 || dpi % 100 != 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(text),
                    dpi,
                    $"DPI must be a multiple of 100 and within {ProfileLayout.DpiMin}..10000.");
            }

            values[index] = dpi;
        }

        return values;
    }

    private static int ParseZone(string text)
    {
        var zone = int.Parse(text, CultureInfo.InvariantCulture);
        if (zone is < 1 or > ProfileLayout.LedZones)
        {
            throw new ArgumentOutOfRangeException(nameof(text), zone, "Zone must be all or 1..3.");
        }

        return zone - 1;
    }

    private static byte ParseLedMode(string text) => text.ToLowerInvariant() switch
    {
        "solid" or "static" => LedZone.StaticMode,
        "breathing" or "breath" => LedZone.BreathingMode,
        "rainbow" => LedZone.RainbowMode,
        _ => throw new FormatException("--mode: solid|breathing|rainbow"),
    };

    private static ushort? ParsePeriod(string? text)
    {
        if (text is null)
        {
            return null;
        }

        if (!ushort.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            || value is < 1 or > 20)
        {
            throw new ArgumentOutOfRangeException(nameof(text), text, "Breathing period must be 1..20.");
        }

        return value;
    }

    private static byte ParseByte(string text) =>
        byte.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : throw new FormatException($"could not parse byte: {text}");

    private static string ModeName(byte mode) => mode switch
    {
        0 => "off",
        1 => "static",
        2 => "breathing",
        3 => "rainbow",
        _ => $"0x{mode:X2}",
    };

    private static string? Option(string[] args, string name)
    {
        var index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }

    private static string Require(string[] args, string name) =>
        Option(args, name) ?? throw new FormatException($"missing {name}");

    private static bool Flag(string[] args, string name) => args.Contains(name);

    private static int Unknown(string command)
    {
        Console.Error.WriteLine($"error: unknown command {command}");
        PrintHelp();
        return 1;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Rat6.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? Directory.GetCurrentDirectory();
    }

    private static void PrintHelp()
    {
        Console.WriteLine(
            """
            rat6 — read-only/controlled tool for Mad Catz R.A.T. 6+ (12cf:0c04)

              rat6 info
              rat6 dump [--out DIR]
              rat6 backup [--out DIR]
              rat6 dpi --slot 0 --values 800,1600,3200,10000 [--apply]
              rat6 led --slot 0 --zone all|1|2|3 --mode solid|breathing|rainbow --color RRGGBB [--brightness 255] [--period 1..20] [--apply]
              rat6 restore --dir DIR [--slot 0..3] --yes
              rat6 select --slot 0..3 --yes

            General options:
              --device /dev/hidrawN   explicit device node (autodetect by default)

            Writing (`--apply`, `--yes`) first creates a backup in
            ~/.local/share/rat6/backups/<timestamp>/.
            """);
    }
}
