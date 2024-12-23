using System.Reflection;
using AnswerScanner.WPF.ViewModels;
using AnswerScanner.WPF.Views.Windows;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using AnswerScanner.WPF.Services;
using AnswerScanner.WPF.Services.Interfaces;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace AnswerScanner.WPF;

public partial class App : Application
{
    public const string Name = "AnswerScanner";

    public static string Version { get; } 
    
    public static IServiceProvider Services { get; }

    static App()
    {
        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        Version = $"v{version}";
        
        var host = Host
            .CreateDefaultBuilder(Environment.GetCommandLineArgs())
            .ConfigureAppConfiguration((appConfig, _) =>
            {
                appConfig.HostingEnvironment.ApplicationName = Name;
            })
            .UseSerilog()
            .ConfigureServices(ConfigureServices)
            .Build();

        Services = host.Services;
    }
    
    private static void ConfigureServices(HostBuilderContext context, IServiceCollection collection)
    {
        collection.AddSingleton<MainWindow>();
        collection.AddSingleton<MainWindowViewModel>();

        collection.AddSingleton<IQuestionnaireParserFactory, QuestionnaireParserFactory>();
        collection.AddTransient<PdfQuestionnaireParser>();
        collection.AddTransient<SimpleImageQuestionnaireParser>();

        collection.AddSingleton<IQuestionsExtractorFactory, QuestionsExtractorFactory>();
        collection.AddTransient<YesNoAnswerOptionsQuestionsExtractor>();
        collection.AddTransient<FiveAnswerOptionsQuestionsExtractor>();

        collection.AddTransient<QuestionnairesUploadWindow>();
        collection.AddTransient<QuestionnairesUploadViewModel>();
        
        collection.AddSingleton<IQuestionnaireFileExporterFactory, QuestionnaireFileExporterFactory>();
        collection.AddTransient<QuestionnaireXlsxFileExporter>();

        collection.AddTransient<QuestionnairesExportWindow>();
        
        collection.AddTransient<QuestionAddWindow>();
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
