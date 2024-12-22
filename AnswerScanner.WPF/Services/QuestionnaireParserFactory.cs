using AnswerScanner.WPF.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.IO;

namespace AnswerScanner.WPF.Services;

internal class QuestionnaireParserFactory : IQuestionnaireParserFactory
{
    private static readonly Dictionary<string, Type> ExtToReaderType = new()
    {
        { ".pdf" , typeof(PdfQuestionnaireParser) },
        { ".jpg", typeof(SimpleImageQuestionnaireParser) },
        { ".jpeg", typeof(SimpleImageQuestionnaireParser) },
    };

    public IQuestionnaireParser CreateParser(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLower();
        var readerType = ExtToReaderType[ext];
        return (IQuestionnaireParser)App.Services.GetRequiredService(readerType);
    }
}