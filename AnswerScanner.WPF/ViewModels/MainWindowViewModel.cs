using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using AnswerScanner.WPF.Infrastructure;
using AnswerScanner.WPF.Views.Windows;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Path = System.IO.Path;

namespace AnswerScanner.WPF.ViewModels;

internal partial class MainWindowViewModel : 
    ObservableRecipient,
    IRecipient<QuestionnairesReadMessage>
{
    public static readonly IReadOnlyCollection<string> AllowedExtensions = [".pdf", ".jpg", ".jpeg"];
    
    [ObservableProperty]
    private IReadOnlyCollection<QuestionnaireViewModel>? _uploadedQuestionnaires;

    [ObservableProperty]
    private QuestionnaireViewModel? _selectedQuestionnaire;

    public MainWindowViewModel()
    {
        IsActive = true;
    }

    [RelayCommand]
    private static void UploadQuestionnaires()
    {
        using var scope = App.Services.CreateScope(); 
        var window = scope.ServiceProvider.GetRequiredService<QuestionnairesUploadWindow>();
        var viewModel = scope.ServiceProvider.GetRequiredService<QuestionnairesUploadViewModel>();

        // TODO: Migrate to MVVM.
        
        window.DataContext = viewModel;
        window.ShowDialog();
    }
    
    [RelayCommand]
    private void ExportQuestionnaires()
    {
        if (UploadedQuestionnaires is null || UploadedQuestionnaires.Count == 0)
        {
            return;
        }
        
        using var scope = App.Services.CreateScope(); 
        var window = scope.ServiceProvider.GetRequiredService<QuestionnairesExportWindow>();
        var viewModel = new QuestionnairesExportViewModel
        {
            Questionnaires = UploadedQuestionnaires,
        };

        window.DataContext = viewModel;
        window.ShowDialog();
    }

    [RelayCommand]
    private static void OnDrop(object parameter)
    {
        if (parameter is not DragEventArgs args)
        {
            return;
        }

        var files = args.Data.GetData(DataFormats.FileDrop) as string[];
        if (files is null or { Length: 0 } || !files.Any(e => AllowedExtensions.Contains(Path.GetExtension(e), StringComparer.InvariantCultureIgnoreCase)))
        {
            return;
        }
        
        using var scope = App.Services.CreateScope(); 
        var questionnairesUploadWindow = scope.ServiceProvider.GetRequiredService<QuestionnairesUploadWindow>();
        var viewModel = scope.ServiceProvider.GetRequiredService<QuestionnairesUploadViewModel>();
        
        foreach (var file in files.Where(e => AllowedExtensions.Contains(Path.GetExtension(e), StringComparer.InvariantCultureIgnoreCase)))
        {
            viewModel.SelectedFiles.Add(new SelectedFileViewModel
            {
                FilePath = file, 
                SelectedQuestionnaireType = viewModel.SelectedQuestionnaireType, 
                IsProcessed = false
            });
        }

        // TODO: Migrate to MVVM.
        
        var mainWindow = scope.ServiceProvider.GetRequiredService<MainWindow>();
        mainWindow.IsEnabled = false;
        questionnairesUploadWindow.Closed += OnWidowClosed;
        
        args.Handled = true;
        
        questionnairesUploadWindow.DataContext = viewModel;
        questionnairesUploadWindow.Owner = mainWindow;
        questionnairesUploadWindow.Show();
        return;

        void OnWidowClosed(object? sender, EventArgs e)
        {
            mainWindow.IsEnabled = true;
            questionnairesUploadWindow.Closed -= OnWidowClosed;
        }
    }
    
    public void Receive(QuestionnairesReadMessage message)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            UploadedQuestionnaires = message.Questionnaires;
        });
    }
}