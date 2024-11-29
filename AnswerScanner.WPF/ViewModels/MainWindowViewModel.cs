using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using AnswerScanner.WPF.Infrastructure;
using AnswerScanner.WPF.Views.Windows;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace AnswerScanner.WPF.ViewModels;

internal partial class MainWindowViewModel : 
    ObservableRecipient,
    IRecipient<QuestionnairesReadMessage>
{
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
    private static void ExportQuestionnaires()
    {
        using var scope = App.Services.CreateScope(); 
        var window = scope.ServiceProvider.GetRequiredService<QuestionnairesExportWindow>();
        var viewModel = scope.ServiceProvider.GetRequiredService<QuestionnairesExportViewModel>();

        // TODO: Migrate to MVVM.
        
        window.DataContext = viewModel;
        window.ShowDialog();
    }

    public void Receive(QuestionnairesReadMessage message)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            UploadedQuestionnaires = message.Questionnaires;
        });
    }
}