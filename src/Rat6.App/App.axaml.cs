using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Rat6.App.ViewModels;
using Rat6.App.Views;
using Rat6.Device;
using Rat6.Transport.Linux;

namespace Rat6.App;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var (device, error) = TryOpenDevice();
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(device, error),
            };

            if (device is not null)
            {
                desktop.Exit += (_, _) => device.Dispose();
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static (IRat6Device? Device, string? Error) TryOpenDevice()
    {
        try
        {
            var info = HidrawDiscovery.Find();
            if (info is null)
            {
                return (null, "R.A.T. 6+ (12cf:0c04) not found.");
            }

            return (new Rat6Device(Rat6TransportFactory.Open(info.Path)), null);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or Rat6Exception or InvalidOperationException)
        {
            return (null, exception.Message);
        }
    }
}
