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
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DropCommand))]
    private bool _isDropCommandEnabled = true;
    
    public MainWindowViewModel()
    {
        IsActive = true;
    }

    [RelayCommand]
    private void UploadQuestionnaires()
    {
        using var scope = App.Services.CreateScope(); 
        var window = scope.ServiceProvider.GetRequiredService<QuestionnairesUploadWindow>();
        var viewModel = scope.ServiceProvider.GetRequiredService<QuestionnairesUploadViewModel>();

        IsDropCommandEnabled = false;
        window.DataContext = viewModel;
        window.ShowDialog();
        IsDropCommandEnabled = true;
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

    [RelayCommand(CanExecute = nameof(CanDrop))]
    private void OnDrop(object parameter)
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

        var mainWindow = scope.ServiceProvider.GetRequiredService<MainWindow>();
        mainWindow.IsEnabled = false;
        questionnairesUploadWindow.Closed += OnWidowClosed;
        
        args.Handled = true;
        
        questionnairesUploadWindow.DataContext = viewModel;
        questionnairesUploadWindow.Owner = mainWindow;
        questionnairesUploadWindow.Show();
        IsDropCommandEnabled = false;
        return;

        void OnWidowClosed(object? sender, EventArgs e)
        {
            mainWindow.IsEnabled = true;
            questionnairesUploadWindow.Closed -= OnWidowClosed;
            IsDropCommandEnabled = true;
        }
    }

    private bool CanDrop() => IsDropCommandEnabled;

    [RelayCommand]
    private void SelectFiles()
    {
        var fileNames = DialogsHelper.ShowSelectAvailableToUploadFilesDialog();
        if (fileNames.Length == 0)
        {
            return;
        }
        
        using var scope = App.Services.CreateScope(); 
        var vm = scope.ServiceProvider.GetRequiredService<QuestionnairesUploadViewModel>();
        var questionnairesUploadWindow = scope.ServiceProvider.GetRequiredService<QuestionnairesUploadWindow>();
        
        foreach (var file in fileNames)
        {
            vm.SelectedFiles.Add(new SelectedFileViewModel
            {
                FilePath = file, 
                SelectedQuestionnaireType = vm.SelectedQuestionnaireType, 
                IsProcessed = false
            });
        }
        
        IsDropCommandEnabled = false;
        questionnairesUploadWindow.DataContext = vm;
        questionnairesUploadWindow.ShowDialog();
        IsDropCommandEnabled = true;
    }

    [RelayCommand]
    private void OpenQuestionAddBeforeDialog(QuestionViewModel? question)
    {
        if (question is null || 
            SelectedQuestionnaire is null || 
            question.Number <= 1 ||
            question.Number - 1 is var newNumber && SelectedQuestionnaire.Questions.Any(q => q.Number == newNumber) ||
            SelectedQuestionnaire.Questions.IndexOf(question) is var questionIndex && questionIndex == -1)
        {
            return;
        }
        
        var vm = new QuestionAddViewModel
        {
            Questionnaire = SelectedQuestionnaire,
            QuestionNumber = newNumber,
            QuestionIndex = questionIndex,
        };

        using var scope = App.Services.CreateScope();
        var window = scope.ServiceProvider.GetRequiredService<QuestionAddWindow>();
        IsDropCommandEnabled = false;
        window.DataContext = vm;
        window.ShowDialog();
        IsDropCommandEnabled = true;
    }
    
    [RelayCommand]
    private void OpenQuestionAddAfterDialog(QuestionViewModel? question)
    {
        if (question is null || 
            SelectedQuestionnaire is null ||
            question.Number < 1 ||
            question.Number + 1 is var newNumber && SelectedQuestionnaire.Questions.Any(q => q.Number == newNumber) ||
            SelectedQuestionnaire.Questions.IndexOf(question) is var questionIndex && questionIndex == -1)
        {
            return;
        }
        
        var vm = new QuestionAddViewModel
        {
            Questionnaire = SelectedQuestionnaire,
            QuestionNumber = newNumber,
            QuestionIndex = questionIndex + 1,
        };

        IsDropCommandEnabled = false;
        using var scope = App.Services.CreateScope();
        var window = scope.ServiceProvider.GetRequiredService<QuestionAddWindow>();
        window.DataContext = vm;
        window.ShowDialog();
        IsDropCommandEnabled = true;
    }
    
    public void Receive(QuestionnairesReadMessage message)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            UploadedQuestionnaires = message.Questionnaires;
        });
    }
}