using AnswerScanner.WPF.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.IO;

namespace AnswerScanner.WPF.Services;

internal class QuestionnaireReaderFactory : IQuestionnaireReaderFactory
{
    private static readonly Dictionary<string, Type> ExtToReaderType = new()
    {
        { ".pdf" , typeof(PdfQuestionnaireReader) },
        { ".jpg", typeof(ImageQuestionnaireReader) },
        { ".jpeg", typeof(ImageQuestionnaireReader) },
    };

    public IQuestionnaireReader CreateReader(FileInfo file)
    {
        var ext = Path.GetExtension(file.FullName).ToLower();
        var readerType = ExtToReaderType[ext];
        return (IQuestionnaireReader)App.Services.GetRequiredService(readerType);
    }
}