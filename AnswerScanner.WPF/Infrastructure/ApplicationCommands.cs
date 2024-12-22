using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace AnswerScanner.WPF.Infrastructure;

public static class ApplicationCommands
{
    public static readonly ICommand CloseWindowCommand = new RelayCommand<Window>(window => window?.Close());
    
    public static readonly ICommand MaximizeWindowCommand = new RelayCommand<Window>(window =>
    {
        if (window is null)
        {
            return;
        }
        
        window.WindowState = window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    });
    
    public static readonly ICommand MinimizeWindowCommand = new RelayCommand<Window>(window =>
    {
        if (window is null)
        {
            return;
        }
        
        window.WindowState = WindowState.Minimized;
    });
}