using System.Windows;
using AnswerScanner.WPF.Extensions;
using AnswerScanner.WPF.Services.Interfaces;
using AnswerScanner.WPF.Services.Responses;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace AnswerScanner.WPF.ViewModels;

internal partial class QuestionnairesExportViewModel : ObservableObject
{
    public static readonly IEnumerable<EnumViewModel<QuestionnaireFileExporterType>> AvailableQuestionnaireFileExporterTypes = Enum
        .GetValues<QuestionnaireFileExporterType>()
        .Select(e => e.ToViewModel())
        .ToList();

    private static readonly Dictionary<AnswerType, int> AnswerTypeToNumber = new()
    {
        { AnswerType.Undefined, 0 },
        { AnswerType.Yes, 1 },
        { AnswerType.No, 2 },
        { AnswerType.AbsolutelyNot, 1 },
        { AnswerType.Slightly, 2 },
        { AnswerType.Moderately, 3 },
        { AnswerType.Strongly, 4 },
        { AnswerType.VeryStrongly, 5 },
    };
    
    public required IReadOnlyCollection<QuestionnaireViewModel> Questionnaires { get; init; }

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
    private void Confirm(Window window)
    {
        using var scope = App.Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IQuestionnaireFileExporterFactory>();
        
        var exporter = factory.CreateExporter(SelectedQuestionnaireFileExporterType.Value);
        var data = Questionnaires
            .OrderBy(e => e.Name)
            .Select(e => new QuestionnaireExportModel(e.Name, e.FilePath,
                e.Questions
                    .OrderBy(o => o.Number)
                    .Select(o => new QuestionExportModel((int)o.Number, AnswerTypeToNumber[o.Answer.Value]))
                    .ToList()))
            .ToList();

        exporter.Export(data, SaveAs!);
        MessageBox.Show("Опросники были успешно экспортированы.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        window.Close();
    }
    
    private bool CanConfirm() => !string.IsNullOrWhiteSpace(SaveAs) &&
                                 Questionnaires.Count > 0;
}