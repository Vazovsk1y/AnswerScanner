﻿using AnswerScanner.WPF.ViewModels;
using AnswerScanner.WPF.Views.Windows;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace AnswerScanner.WPF;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public const string Title = "AnswerScanner";

    public static IServiceProvider Services { get; }

    static App()
    {
        var host = Program.CreateHostBuilder(Environment.GetCommandLineArgs()).Build();
        Services = host.Services;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var window = Services.GetRequiredService<MainWindow>();
        var vm = Services.GetRequiredService<MainWindowViewModel>();
        
        window.DataContext = vm;
        window.Show();
    }
}
