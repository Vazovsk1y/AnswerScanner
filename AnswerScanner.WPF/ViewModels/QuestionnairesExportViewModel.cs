using System.Windows;
using AnswerScanner.WPF.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace AnswerScanner.WPF.ViewModels;

internal partial class QuestionnairesExportViewModel(MainWindowViewModel mainWindowViewModel) : ObservableObject
{
    public static readonly IEnumerable<EnumViewModel<QuestionnaireFileExporterType>> AvailableQuestionnaireFileExporterTypes = Enum
        .GetValues<QuestionnaireFileExporterType>()
        .Select(e => new EnumViewModel<QuestionnaireFileExporterType>(e))
        .ToList();
    
    public IReadOnlyCollection<QuestionnaireViewModel> Questionnaires { get; } = mainWindowViewModel.UploadedQuestionnaires?.ToList() ?? [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private string? _saveAs;
    
    [ObservableProperty]
    private EnumViewModel<QuestionnaireFileExporterType> _selectedQuestionnaireFileExporterType = AvailableQuestionnaireFileExporterTypes.First();

    [RelayCommand]
    private void SelectFile()
    {
        const string filter = "Excel Files (*.xlsx)|*.xlsx";
        const string title = "Выберите файл:";
        
        var fileDialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = filter,
            Title = title,
            RestoreDirectory = true,
        };

        fileDialog.ShowDialog();
        SaveAs = fileDialog.FileName;
    }

    [RelayCommand(CanExecute = nameof(CanConfirm))]
    private void Confirm()
    {
        using var scope = App.Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IQuestionnaireFileExporterFactory>();
        
        var exporter = factory.CreateExporter(SelectedQuestionnaireFileExporterType.Value);
        var data = Questionnaires
            .OrderBy(e => e.Name)
            .Select(e => new QuestionnaireExportModel(e.Name, e.FilePath,
                e.Questions
                    .OrderBy(o => o.Number)
                    .DistinctBy(o => o.Number)
                    .Select(o => new QuestionExportModel(o.Number, o.Answer.DisplayName))
                    .ToArray()))
            .ToList();

        exporter.Export(data, SaveAs!);
        MessageBox.Show("Опросники были успешно экспортированы.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
    }
    
    private bool CanConfirm() => !string.IsNullOrWhiteSpace(SaveAs) &&
                                 Questionnaires.Count > 0;
}