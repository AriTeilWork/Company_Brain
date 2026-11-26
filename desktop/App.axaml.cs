using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CompanyBrain.Client.Api;
using CompanyBrain.Client.ViewModels;
using CompanyBrain.Client.Views;

namespace CompanyBrain.Client;

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
            var httpClient = ApiSettings.FromEnvironment().CreateHttpClient();
            var api = new ApiClient(httpClient);
            desktop.Exit += (_, _) => httpClient.Dispose();
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel(api),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
