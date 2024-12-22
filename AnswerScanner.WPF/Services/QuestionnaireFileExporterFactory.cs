using AnswerScanner.WPF.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace AnswerScanner.WPF.Services;

internal class QuestionnaireFileExporterFactory : IQuestionnaireFileExporterFactory
{
    private static readonly Dictionary<QuestionnaireFileExporterType, Type> ExporterTypes = new()
    {
        { QuestionnaireFileExporterType.Xlsx, typeof(QuestionnaireXlsxFileExporter) }
    };
    
    public IQuestionnaireFileExporter CreateExporter(QuestionnaireFileExporterType exporterType)
    {
        return (IQuestionnaireFileExporter)App.Services.GetRequiredService(ExporterTypes[exporterType]);
    }
}