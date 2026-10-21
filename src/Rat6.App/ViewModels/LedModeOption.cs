using CommunityToolkit.Mvvm.ComponentModel;

namespace Rat6.App.ViewModels;

public sealed partial class LedModeOption : ObservableObject
{
    private readonly Func<string> _name;

    public LedModeOption(byte mode, bool supportsColor, Func<string> name)
    {
        Mode = mode;
        SupportsColor = supportsColor;
        _name = name;
    }

    public byte Mode { get; }

    public bool SupportsColor { get; }

    public string Name => _name();

    public void RefreshLanguage() => OnPropertyChanged(nameof(Name));
}
