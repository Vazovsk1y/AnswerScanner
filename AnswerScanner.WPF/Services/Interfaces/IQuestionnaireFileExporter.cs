namespace AnswerScanner.WPF.Services.Interfaces;

public interface IQuestionnaireFileExporter
{
    void Export(IReadOnlyCollection<QuestionnaireExportModel> questionnaires, string filePath);
}

public record QuestionnaireExportModel(
    string Name,
    string? FilePath,
    IReadOnlyCollection<QuestionExportModel> Questions);
    
public record QuestionExportModel(
    int Number,
    string Answer);