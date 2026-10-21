using Rat6.Transport;

namespace Rat6.Transport.Linux;

public static class HidrawDiscovery
{
    private const string HidId = "000012CF:00000C04";
    private const string HidrawRoot = "/sys/class/hidraw";
    private static readonly byte[] CommandReportMarker = [0x85, 0x0C];

    public static HidDeviceInfo? Find() => FindAll().FirstOrDefault();

    public static IReadOnlyList<HidDeviceInfo> FindAll()
    {
        var devices = new List<HidDeviceInfo>();
        if (!Directory.Exists(HidrawRoot))
        {
            return devices;
        }

        foreach (var entry in Directory.EnumerateFileSystemEntries(HidrawRoot, "hidraw*"))
        {
            if (!Directory.Exists(entry))
            {
                continue;
            }

            var info = TryRead(entry);
            if (info is not null)
            {
                devices.Add(info);
            }
        }

        return devices;
    }

    private static HidDeviceInfo? TryRead(string directory)
    {
        try
        {
            var uevent = File.ReadAllText(Path.Combine(directory, "device", "uevent"));
            if (!uevent.Contains(HidId, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var descriptor = ReadDescriptor(Path.Combine(directory, "device", "report_descriptor"));
            if (!Contains(descriptor, CommandReportMarker))
            {
                return null;
            }

            return new HidDeviceInfo(
                "/dev/" + Path.GetFileName(directory),
                0x12CF,
                0x0C04,
                ReadValue(uevent, "HID_NAME="),
                ReadValue(uevent, "HID_PHYS="));
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static byte[] ReadDescriptor(string path)
    {
        using var stream = File.OpenRead(path);
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    private static string ReadValue(string text, string prefix)
    {
        foreach (var line in text.Split('\n'))
        {
            if (line.StartsWith(prefix, StringComparison.Ordinal))
            {
                return line[prefix.Length..].Trim();
            }
        }

        return string.Empty;
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
