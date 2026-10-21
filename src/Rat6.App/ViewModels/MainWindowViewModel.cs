using System.Collections.ObjectModel;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Rat6.App.Localization;
using Rat6.App.Services;
using Rat6.Device;
using Rat6.Protocol;

namespace Rat6.App.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private const int ProfileCount = 4;
    private readonly IRat6Device? _device;
    private readonly DeviceOperations? _operations;
    private Profile? _profile;
    private bool _suppressSlotChange;
    private int _profileLoadVersion;
    private int? _firmwareVersion;
    private int? _activeSlot;
    private int? _activeSlotCount;
    private string? _statusKey;
    private object?[] _statusArgs = [];

    public MainWindowViewModel(IRat6Device? device, string? startupError)
    {
        _device = device;
        _operations = device is null ? null : new DeviceOperations(device);
        Zones =
        [
            new ZoneOption(0, () => L.AllZones),
            new ZoneOption(1, () => L.ZoneName(1)),
            new ZoneOption(2, () => L.ZoneName(2)),
            new ZoneOption(3, () => L.ZoneName(3)),
        ];
        _selectedZone = Zones[0];
        _selectedLedMode = LedModes[0];

        for (var index = 0; index < ProfileLayout.DpiStages; index++)
        {
            DpiStages.Add(new DpiStageViewModel(index));
        }

        if (device is null)
        {
            SetStatusText(startupError ?? L.DeviceNotFound, true);
        }
        else
        {
            SetStatus("status_reading_profiles", false);
            _ = LoadAsync();
        }
    }

    public IReadOnlyList<ZoneOption> Zones { get; }

    public IReadOnlyList<LedModeOption> LedModes { get; } =
    [
        new(LedZone.StaticMode, true, () => AppStrings.Instance.ModeSolid),
        new(LedZone.BreathingMode, true, () => AppStrings.Instance.ModeBreathing),
        new(LedZone.RainbowMode, false, () => AppStrings.Instance.ModeRainbow),
    ];

    public AppStrings L => AppStrings.Instance;

    public IReadOnlyList<LanguageOption> Languages => AppStrings.AvailableLanguages;

    [ObservableProperty]
    private LanguageOption _selectedLanguage = AppStrings.AvailableLanguages[0];

    partial void OnSelectedLanguageChanged(LanguageOption value)
    {
        if (value is null)
        {
            return;
        }

        L.Language = value.Code;
        OnPropertyChanged(nameof(L));
        RefreshLocalized();
    }

    private void RefreshLocalized()
    {
        FirmwareText = _firmwareVersion is { } firmware ? L.Firmware(firmware) : L.FirmwareUnknown;
        ActiveSlotText = _activeSlot is { } slot && _activeSlotCount is { } count
            ? L.ActiveSlot(slot, count)
            : L.ActiveSlotUnknown;

        if (_statusKey is not null)
        {
            StatusText = L.Format(_statusKey, _statusArgs);
        }

        foreach (var zone in Zones)
        {
            zone.RefreshLanguage();
        }

        foreach (var mode in LedModes)
        {
            mode.RefreshLanguage();
        }

        foreach (var stage in DpiStages)
        {
            stage.RefreshLanguage();
        }

        OnPropertyChanged(nameof(BrightnessText));
        OnPropertyChanged(nameof(BreathingPeriodText));
    }

    public ObservableCollection<DpiStageViewModel> DpiStages { get; } = new();

    public bool CanApply => _device is not null && AllowWrite && !IsBusy;

    public bool CanActivate => _device is not null && !IsBusy;

    [ObservableProperty]
    private int _selectedSlot;

    public double ProfilePosition
    {
        get => SelectedSlot;
        set
        {
            if (!double.IsFinite(value))
            {
                OnPropertyChanged(nameof(ProfilePosition));
                return;
            }

            var slot = (int)Math.Clamp(Math.Round(value), 0, ProfileCount - 1);
            Console.WriteLine($"[rat6] ProfilePosition {value:0.##} -> slot {slot} (current {SelectedSlot})");
            if (SelectedSlot != slot)
            {
                SelectedSlot = slot;
            }
        }
    }

    [ObservableProperty]
    private ZoneOption _selectedZone;

    [ObservableProperty]
    private LedModeOption _selectedLedMode;

    public bool ShowColorPicker => SelectedLedMode.SupportsColor;

    public bool ShowBreathingPeriod => SelectedLedMode.Mode == LedZone.BreathingMode;

    [ObservableProperty]
    private string _firmwareText = AppStrings.Instance.FirmwareUnknown;

    [ObservableProperty]
    private string _activeSlotText = AppStrings.Instance.ActiveSlotUnknown;

    [ObservableProperty]
    private string _statusText = "";

    [ObservableProperty]
    private bool _isError;

    public IBrush StatusBrush => IsError ? Brushes.OrangeRed : Brushes.Gray;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _allowWrite;

    [ObservableProperty]
    private double _brightness = 255;

    [ObservableProperty]
    private double _breathingPeriod = 4;

    [ObservableProperty]
    private Color _pickerColor = Color.FromRgb(64, 64, 64);

    public string BrightnessText => $"{Brightness:0}";

    public string BreathingPeriodText => L.PeriodValue((int)Math.Round(BreathingPeriod));

    partial void OnAllowWriteChanged(bool value) => OnPropertyChanged(nameof(CanApply));

    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(CanApply));
        OnPropertyChanged(nameof(CanActivate));
    }

    partial void OnIsErrorChanged(bool value) => OnPropertyChanged(nameof(StatusBrush));

    partial void OnSelectedSlotChanged(int value)
    {
        Console.WriteLine($"[rat6] SelectedSlot={value} suppress={_suppressSlotChange}");
        OnPropertyChanged(nameof(ProfilePosition));

        if (_suppressSlotChange)
        {
            return;
        }

        _ = LoadProfileAsync();
    }

    partial void OnSelectedZoneChanged(ZoneOption value) => LoadZoneIntoEditor();

    partial void OnSelectedLedModeChanged(LedModeOption value)
    {
        OnPropertyChanged(nameof(ShowColorPicker));
        OnPropertyChanged(nameof(ShowBreathingPeriod));
    }

    partial void OnBrightnessChanged(double value) => OnPropertyChanged(nameof(BrightnessText));

    partial void OnBreathingPeriodChanged(double value) => OnPropertyChanged(nameof(BreathingPeriodText));

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    [RelayCommand]
    private void SelectProfile(int slot)
    {
        Console.WriteLine($"[rat6] SelectProfile({slot}) current={SelectedSlot}");

        if (slot < 0 || slot >= ProfileCount || SelectedSlot == slot)
        {
            return;
        }

        SelectedSlot = slot;
    }

    [RelayCommand]
    private async Task ActivateSlotAsync()
    {
        if (_device is null)
        {
            SetStatus("status_device_not_found", true);
            return;
        }

        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            var slot = SelectedSlot;
            var backup = await Task.Run(() => _operations!.EnsureBackup());
            var selected = await Task.Run(() => _operations!.Activate(slot));

            _activeSlot = selected.Slot;
            _activeSlotCount = selected.Count;
            ActiveSlotText = L.ActiveSlot(selected.Slot, selected.Count);
            SetStatus("status_slot_activated", false, selected.Slot, backup);
        }
        catch (Exception exception) when (IsDeviceError(exception))
        {
            SetStatus("status_write_failed", true, exception.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ApplyDpiAsync()
    {
        if (!TryBeginWrite(out var error))
        {
            SetStatusText(error, true);
            return;
        }

        try
        {
            IsBusy = true;
            var slot = SelectedSlot;
            var values = DpiStages
                .Select(stage => (int)Math.Round(stage.Dpi / 100.0) * 100)
                .ToArray();
            var backup = await Task.Run(() => _operations!.EnsureBackup());
            var written = await Task.Run(() => _operations!.WriteDpi(slot, values));

            ApplyProfileToEditor(written);
            SetStatus("status_dpi_written", false, slot, backup);
        }
        catch (Exception exception) when (IsDeviceError(exception))
        {
            SetStatus("status_write_failed", true, exception.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ApplyLedAsync()
    {
        if (!TryBeginWrite(out var error))
        {
            SetStatusText(error, true);
            return;
        }

        try
        {
            IsBusy = true;
            var slot = SelectedSlot;
            var color = new RgbColor(
                PickerColor.R,
                PickerColor.G,
                PickerColor.B);
            var mode = SelectedLedMode.Mode;
            var level = (byte)Math.Clamp(Brightness, 0, 255);
            ushort? period = mode == LedZone.BreathingMode
                ? (ushort)Math.Clamp((int)Math.Round(BreathingPeriod), 1, 20)
                : null;
            var zones = SelectedZone.Index == 0
                ? Enumerable.Range(0, ProfileLayout.LedZones).ToArray()
                : [SelectedZone.Index - 1];
            var backup = await Task.Run(() => _operations!.EnsureBackup());
            var written = await Task.Run(() => _operations!.WriteLed(slot, zones, mode, color, level, period));

            ApplyProfileToEditor(written);
            SetStatus("status_led_written", false, SelectedLedMode.Name, slot, backup);
        }
        catch (Exception exception) when (IsDeviceError(exception))
        {
            SetStatus("status_write_failed", true, exception.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadAsync()
    {
        if (_device is null)
        {
            return;
        }

        var version = ++_profileLoadVersion;

        try
        {
            IsBusy = true;
            var data = await Task.Run(() =>
            {
                var firmware = _device.GetFirmware();
                var selected = _device.GetSelectedSlot();
                var profiles = _device.ReadAllProfiles();
                return (Firmware: firmware, Selected: selected, Profiles: profiles);
            });

            if (version != _profileLoadVersion)
            {
                return;
            }

            FirmwareText = L.Firmware(data.Firmware);
            _firmwareVersion = data.Firmware;
            _activeSlot = data.Selected.Slot;
            _activeSlotCount = data.Selected.Count;
            ActiveSlotText = L.ActiveSlot(data.Selected.Slot, data.Selected.Count);

            _suppressSlotChange = true;
            SelectedSlot = data.Selected.Slot;
            _suppressSlotChange = false;

            ApplyProfileToEditor(data.Profiles[data.Selected.Slot]);
            SetStatus("status_profiles_loaded", false, data.Selected.Slot);
        }
        catch (Exception exception) when (IsDeviceError(exception))
        {
            if (version == _profileLoadVersion)
            {
                SetStatus("status_read_failed", true, exception.Message);
            }
        }
        finally
        {
            if (version == _profileLoadVersion)
            {
                IsBusy = false;
            }
        }
    }

    private async Task LoadProfileAsync()
    {
        if (_device is null)
        {
            return;
        }

        var version = ++_profileLoadVersion;

        try
        {
            IsBusy = true;
            var slot = SelectedSlot;
            var profile = await Task.Run(() => _device.ReadProfile(slot));

            if (version != _profileLoadVersion)
            {
                return;
            }

            ApplyProfileToEditor(profile);
            SetStatus("status_profile_read", false, slot);
        }
        catch (Exception exception) when (IsDeviceError(exception))
        {
            if (version == _profileLoadVersion)
            {
                SetStatus("status_read_failed", true, exception.Message);
            }
        }
        finally
        {
            if (version == _profileLoadVersion)
            {
                IsBusy = false;
            }
        }
    }

    private void ApplyProfileToEditor(Profile profile)
    {
        _profile = profile;
        for (var index = 0; index < DpiStages.Count; index++)
        {
            DpiStages[index].Dpi = profile.GetDpiStage(index).X;
        }

        LoadZoneIntoEditor();
    }

    private void LoadZoneIntoEditor()
    {
        if (_profile is null)
        {
            return;
        }

        var index = SelectedZone.Index == 0 ? 0 : SelectedZone.Index - 1;
        var zone = _profile.GetLedZone(index);
        PickerColor = Color.FromRgb(zone.Color.R, zone.Color.G, zone.Color.B);
        Brightness = zone.Brightness;
        BreathingPeriod = zone.Period == 0 ? 4 : zone.Period;
        SelectedLedMode = LedModes.FirstOrDefault(mode => mode.Mode == zone.Mode) ?? LedModes[0];
    }

    private bool TryBeginWrite(out string error)
    {
        if (_device is null)
        {
            error = L.DeviceNotFound;
            return false;
        }

        if (!AllowWrite)
        {
            error = L.EnableWrite;
            return false;
        }

        error = string.Empty;
        return true;
    }

    private void SetStatus(string key, bool error, params object?[] args)
    {
        _statusKey = key;
        _statusArgs = args;
        StatusText = L.Format(key, args);
        IsError = error;
    }

    private void SetStatusText(string text, bool error)
    {
        _statusKey = null;
        _statusArgs = [];
        StatusText = text;
        IsError = error;
    }

    private static bool IsDeviceError(Exception exception) =>
        exception is Rat6Exception or IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException;
}
