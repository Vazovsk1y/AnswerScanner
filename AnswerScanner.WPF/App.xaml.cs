using AnswerScanner.WPF.ViewModels;
using AnswerScanner.WPF.Views.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;

namespace AnswerScanner.WPF;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public const string Title = "AnswerScanner";

    private static readonly IHost _host;

    public static IServiceProvider Services { get; }

    static App()
    {
        _host = Program.CreateHostBuilder(Environment.GetCommandLineArgs()).Build();
        Services = _host.Services;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var vm = Services.GetRequiredService<MainWindowViewModel>();
        var window = Services.GetRequiredService<MainWindow>();
        window.DataContext = vm;
        window.Show();
    }
}
