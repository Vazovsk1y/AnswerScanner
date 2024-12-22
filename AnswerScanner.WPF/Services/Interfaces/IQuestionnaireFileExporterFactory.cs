using System.ComponentModel.DataAnnotations;

namespace AnswerScanner.WPF.Services.Interfaces;

public interface IQuestionnaireFileExporterFactory
{
    IQuestionnaireFileExporter CreateExporter(QuestionnaireFileExporterType exporterType);
}

public enum QuestionnaireFileExporterType
{
    [Display(Name = ".xlsx (Excel)")]
    Xlsx,
}