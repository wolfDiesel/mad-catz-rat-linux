using CommunityToolkit.Mvvm.ComponentModel;
using Rat6.App.Localization;

namespace Rat6.App.ViewModels;

public partial class DpiStageViewModel : ObservableObject
{
    public DpiStageViewModel(int index)
    {
        Index = index;
    }

    public int Index { get; }

    public string Title => AppStrings.Instance.DpiStage(Index + 1);

    [ObservableProperty]
    private double _dpi = 800;

    public string Display => $"{Dpi:0}";

    public void RefreshLanguage() => OnPropertyChanged(nameof(Title));

    partial void OnDpiChanged(double value) => OnPropertyChanged(nameof(Display));
}
