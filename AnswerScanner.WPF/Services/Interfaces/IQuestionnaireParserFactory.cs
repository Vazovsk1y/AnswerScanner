namespace AnswerScanner.WPF.Services.Interfaces;

public interface IQuestionnaireParserFactory
{
    IQuestionnaireParser CreateParser(string filePath);
}