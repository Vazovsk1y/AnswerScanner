using System.IO;

namespace AnswerScanner.WPF.Services.Interfaces;

public interface IQuestionnaireReaderFactory
{
    IQuestionnaireReader CreateReader(FileInfo fileInfo);
}