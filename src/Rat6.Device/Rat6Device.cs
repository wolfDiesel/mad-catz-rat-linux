using System.Text.Json;
using Rat6.Protocol;
using Rat6.Transport;

namespace Rat6.Device;

public sealed class Rat6Device : IRat6Device
{
    private const int ProfileSlots = 4;
    private readonly IHidTransport _transport;
    private readonly int _profileAttempts;
    private readonly int _profileDelayMs;

    public Rat6Device(IHidTransport transport, int profileAttempts = 20, int profileDelayMs = 100)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _profileAttempts = profileAttempts;
        _profileDelayMs = profileDelayMs;
    }

    public int GetFirmware()
    {
        _transport.SetFeature(CommandReport.Build(CommandCodes.Firmware));
        var reply = PollCommand(
            reply => CommandReport.TryParseFirmware(reply, out _),
            attempts: 40,
            delayMs: 50,
            "reading firmware");

        return CommandReport.TryParseFirmware(reply, out var version)
            ? version
            : throw new Rat6Exception("Could not parse the firmware reply.");
    }

    public SelectedSlot GetSelectedSlot()
    {
        _transport.SetFeature(CommandReport.Build(CommandCodes.SelectedSlot));
        var reply = PollCommand(
            reply => CommandReport.TryParseSelectedSlot(reply, out _, out _),
            attempts: 40,
            delayMs: 50,
            "reading active slot");

        if (!CommandReport.TryParseSelectedSlot(reply, out var slot, out var count))
        {
            throw new Rat6Exception("Could not parse the slot reply.");
        }

        if (slot is < 0 or >= ProfileSlots)
        {
            throw new Rat6Exception($"Device returned slot {slot} outside 0..3.");
        }

        return new SelectedSlot(slot, count);
    }

    public Profile ReadProfile(int slot)
    {
        ValidateSlot(slot);
        _transport.SetFeature(CommandReport.Build(CommandCodes.ReadProfile, (byte)slot));

        for (var attempt = 0; attempt < _profileAttempts; attempt++)
        {
            Thread.Sleep(_profileDelayMs);
            try
            {
                var frame = _transport.GetFeature(ProfileFrame.ReportId, ProfileFrame.Length);
                if (ProfileFrame.TryParse(frame, out var frameSlot, out var checksum, out var payload) && frameSlot == slot)
                {
                    return new Profile(payload, checksum);
                }
            }
            catch (IOException)
            {
            }
        }

        throw new Rat6Exception($"Slot {slot}: no valid GET_FEATURE 0x05 reply.");
    }

    public IReadOnlyList<Profile> ReadAllProfiles()
    {
        var profiles = new Profile[ProfileSlots];
        for (var slot = 0; slot < ProfileSlots; slot++)
        {
            profiles[slot] = ReadProfile(slot);
        }

        return profiles;
    }

    public void WriteProfile(int slot, Profile profile)
    {
        ValidateSlot(slot);
        _transport.SetFeature(ProfileFrame.BuildWriteFrame(slot, profile.Payload));
        PollCommand(
            reply => CommandReport.IsWriteAck(reply, slot),
            attempts: 5,
            delayMs: 300,
            $"writing slot {slot}");
    }

    public void Reload()
    {
        _transport.SetFeature(CommandReport.Build(CommandCodes.Reload));
        Thread.Sleep(150);
    }

    public void SelectSlot(int slot)
    {
        ValidateSlot(slot);
        _transport.SetFeature(CommandReport.Build(CommandCodes.SelectSlot, (byte)slot));
        Thread.Sleep(250);

        var selected = GetSelectedSlot();
        if (selected.Slot != slot)
        {
            throw new Rat6Exception(
                $"Slot select was not confirmed: requested {slot}, active {selected.Slot}.");
        }
    }

    public string BackupAll(string? root = null)
    {
        var baseDirectory = root
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "rat6",
                "backups");

        var directory = Path.Combine(baseDirectory, DateTime.Now.ToString("yyyyMMdd-HHmmss"));
        Directory.CreateDirectory(directory);

        var profiles = ReadAllProfiles();
        for (var slot = 0; slot < profiles.Count; slot++)
        {
            File.WriteAllBytes(Path.Combine(directory, $"slot{slot}.bin"), profiles[slot].ToArray());
        }

        var metadata = new
        {
            generatedAt = DateTimeOffset.UtcNow,
            firmware = TryGetFirmware(),
            selectedSlot = TryGetSelectedSlot(),
            profiles = profiles.Select((profile, slot) => new
            {
                slot,
                checksum = $"0x{profile.Checksum:X2}",
                size = ProfileLayout.PayloadSize,
            }),
        };

        File.WriteAllText(
            Path.Combine(directory, "backup.json"),
            JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true }));

        return directory;
    }

    private byte[] PollCommand(Func<byte[], bool> matcher, int attempts, int delayMs, string operation)
    {
        for (var attempt = 0; attempt < attempts; attempt++)
        {
            Thread.Sleep(delayMs);
            try
            {
                var reply = _transport.GetFeature(CommandReport.ReportId, CommandReport.Length);
                if (matcher(reply))
                {
                    return reply;
                }
            }
            catch (IOException)
            {
            }
        }

        throw new Rat6Exception($"{operation}: no reply from device.");
    }

    private int? TryGetFirmware()
    {
        try
        {
            return GetFirmware();
        }
        catch (Exception exception) when (exception is Rat6Exception or IOException)
        {
            return null;
        }
    }

    private SelectedSlot? TryGetSelectedSlot()
    {
        try
        {
            return GetSelectedSlot();
        }
        catch (Exception exception) when (exception is Rat6Exception or IOException)
        {
            return null;
        }
    }

    private static void ValidateSlot(int slot)
    {
        if (slot is < 0 or >= ProfileSlots)
        {
            throw new ArgumentOutOfRangeException(nameof(slot), slot, "Slot must be 0..3.");
        }
    }

    public void Dispose() => _transport.Dispose();
}
