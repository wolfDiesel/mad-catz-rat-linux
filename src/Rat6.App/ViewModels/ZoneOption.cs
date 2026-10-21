using CommunityToolkit.Mvvm.ComponentModel;

namespace Rat6.App.ViewModels;

public sealed partial class ZoneOption : ObservableObject
{
    private readonly Func<string> _name;

    public ZoneOption(int index, Func<string> name)
    {
        Index = index;
        _name = name;
    }

    public int Index { get; }

    public string Name => _name();

    public void RefreshLanguage() => OnPropertyChanged(nameof(Name));
}
