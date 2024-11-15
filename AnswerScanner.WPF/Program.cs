using AnswerScanner.WPF.Services;
using AnswerScanner.WPF.Services.Interfaces;
using AnswerScanner.WPF.ViewModels;
using AnswerScanner.WPF.Views.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.IO;
using System.Windows;

namespace AnswerScanner.WPF;

internal class Program
{
    private static bool IsInDebug { get; set; }

    private static Mutex? _mutex;

    [STAThread]
    public static void Main(string[] args)
    {

#if DEBUG
        IsInDebug = true;
#endif

        Log.Logger = GetLoggerConfiguration().CreateLogger();

        Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", IsInDebug ? "Development" : "Production");

        _mutex = new Mutex(true, App.Title, out var createdNew);
        if (!createdNew)
        {
            MessageBox.Show("Приложение уже запущено.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            App app = new();
            // app.InitializeComponent();
            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Возникло исключение во время работы приложения.");
        }
    }

    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host
            .CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((appConfig, _) =>
            {
                appConfig.HostingEnvironment.ApplicationName = App.Title;
                appConfig.HostingEnvironment.ContentRootPath = Environment.CurrentDirectory;
            })
            .UseSerilog()
            .ConfigureServices(ConfigureServices);
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
        collection.AddScoped<QuestionnairesUploadViewModel>();
    }

    private static LoggerConfiguration GetLoggerConfiguration()
    {
        var loggerConfiguration = new LoggerConfiguration();
        if (IsInDebug)
        {
            loggerConfiguration.MinimumLevel.Debug();
            loggerConfiguration.WriteTo.Debug();
            return loggerConfiguration;
        }

        var logsDirectory = Path.Combine(Environment.CurrentDirectory, "logs");
        if (!Directory.Exists(logsDirectory))
        {
            Directory.CreateDirectory(logsDirectory);
        }

        var logFileFullPath = Path.Combine(logsDirectory, "log.txt");

        loggerConfiguration.MinimumLevel.Information();
        loggerConfiguration.WriteTo.File(logFileFullPath, rollingInterval: RollingInterval.Day);
        return loggerConfiguration;
    }
}
