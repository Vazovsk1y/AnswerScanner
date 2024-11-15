using AnswerScanner.WPF.Extensions;
using AnswerScanner.WPF.Infrastructure;
using AnswerScanner.WPF.Services.Interfaces;
using AnswerScanner.WPF.Services.Responses;
using AnswerScanner.WPF.Views.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace AnswerScanner.WPF.ViewModels;

public partial class QuestionnairesUploadViewModel : ObservableRecipient
{
    public static readonly IEnumerable<EnumViewModel<QuestionnaireType>> AvailableQuestionnaireTypes = Enum
        .GetValues<QuestionnaireType>()
        .Select(e => new EnumViewModel<QuestionnaireType>(e));
    
    [ObservableProperty]
    private IEnumerable<string> _selectedFiles = [];

    [ObservableProperty]
    private EnumViewModel<QuestionnaireType> _selectedQuestionnaireType = AvailableQuestionnaireTypes.First();

    [RelayCommand]
    private void SelectFiles()
    {
        const string title = "Выберите файлы:";
        const string filter = "Files|*.pdf;*.jpg;*.jpeg";

        var fileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = title,
            RestoreDirectory = true,
            Multiselect = true,
            Filter = filter,
        };

        fileDialog.ShowDialog();
        SelectedFiles = fileDialog.FileNames;
    }

    [RelayCommand]
    private void Confirm(QuestionnairesUploadWindow window) // TODO: Migrate to MVVM.
    {
        if (!SelectedFiles.Any())
        {
            return;
        }
        
        using var scope = App.Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IQuestionnaireReaderFactory>();

        var results = SelectedFiles
            .Select(e =>
            {
                var reader = factory.CreateReader(e);
                return reader.ReadFromFile(e, SelectedQuestionnaireType.Value);
            });

        Messenger.Send(new QuestionnairesReadMessage(results.Select(e => e.ToViewModel())));
        window.Close();
    }
}