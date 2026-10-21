using Rat6.Device;
using Rat6.Protocol;

namespace Rat6.App.Services;

public sealed class DeviceOperations
{
    private readonly IRat6Device _device;
    private string? _backupPath;

    public DeviceOperations(IRat6Device device)
    {
        _device = device;
    }

    public string EnsureBackup()
    {
        _backupPath ??= _device.BackupAll();
        return _backupPath;
    }

    public SelectedSlot Activate(int slot)
    {
        _device.SelectSlot(slot);
        return _device.GetSelectedSlot();
    }

    public Profile WriteDpi(int slot, IReadOnlyList<int> values)
    {
        var profile = _device.ReadProfile(slot);
        for (var index = 0; index < values.Count; index++)
        {
            profile.SetDpiStage(index, new DpiStage(values[index], values[index]));
        }

        _device.WriteProfile(slot, profile);
        _device.Reload();

        var readBack = _device.ReadProfile(slot);
        for (var index = 0; index < values.Count; index++)
        {
            var stage = readBack.GetDpiStage(index);
            if (stage.X != values[index] || stage.Y != values[index])
            {
                throw new Rat6Exception($"DPI verification failed at stage {index + 1}.");
            }
        }

        return readBack;
    }

    public Profile WriteLed(int slot, IReadOnlyList<int> zones, byte mode, RgbColor color, byte brightness, ushort? period = null)
    {
        var profile = _device.ReadProfile(slot);
        foreach (var index in zones)
        {
            profile.SetLed(index, mode, color, brightness, period);
        }

        _device.WriteProfile(slot, profile);
        _device.Reload();

        var readBack = _device.ReadProfile(slot);
        foreach (var index in zones)
        {
            var zone = readBack.GetLedZone(index);
            if (zone.Mode != mode)
            {
                throw new Rat6Exception($"LED mode verification failed at zone {index + 1}.");
            }

            if (mode == LedZone.BreathingMode && period is { } expectedPeriod && zone.Period != expectedPeriod)
            {
                throw new Rat6Exception($"Breathing period verification failed at zone {index + 1}.");
            }

            if (mode != LedZone.RainbowMode
                && (zone.Color != color || zone.Brightness != brightness))
            {
                throw new Rat6Exception($"LED verification failed at zone {index + 1}.");
            }
        }

        return readBack;
    }
}
