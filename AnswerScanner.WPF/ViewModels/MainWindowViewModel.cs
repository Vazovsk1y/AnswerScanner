using AnswerScanner.WPF.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Windows;
using AnswerScanner.WPF.Extensions;
using AnswerScanner.WPF.Services.Responses;

namespace AnswerScanner.WPF.ViewModels;

internal partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private IEnumerable<QuestionnaireViewModel>? _uploadedQuestionnaires;

    [ObservableProperty]
    private QuestionnaireViewModel? _selectedQuestionnaire;

    [RelayCommand]
    private void UploadFiles()
    {
        const string title = "Выберите файлы:";
        UploadedQuestionnaires = null;

        var fileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = title,
            RestoreDirectory = true,
            Multiselect = true,
        };

        fileDialog.ShowDialog();

        if (fileDialog.FileNames.Length == 0)
        {
            return;
        }

        using var scope = App.Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IQuestionnaireReaderFactory>();

        var results = fileDialog.FileNames
            .Select(e =>
            {
                var reader = factory.CreateReader(new FileInfo(e));
                return reader.ReadFromFile(e, QuestionnaireType.YesNoPossibleAnswers);
            });
        
        UploadedQuestionnaires = results.Select(e => e.ToViewModel());
        
        MessageBox.Show("Выбранные файлы успешно загружены.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}