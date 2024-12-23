using Serilog;
using System.IO;
using System.Windows;

namespace AnswerScanner.WPF;

internal static class Program
{
    private static bool IsInDebug { get; set; }

    private static Mutex? _mutex;

    [STAThread]
    public static void Main(string[] args)
    {
        
#if DEBUG
        IsInDebug = true;
#endif
        
        _mutex = new Mutex(true, App.Name, out var createdNew);
        if (!createdNew)
        {
            MessageBox.Show("Приложение уже запущено.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", IsInDebug ? "Development" : "Production");
        Log.Logger = GetLoggerConfiguration().CreateLogger();

        try
        {
            App app = new();
            app.InitializeComponent();
            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Возникло исключение во время работы приложения.");
        }
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
