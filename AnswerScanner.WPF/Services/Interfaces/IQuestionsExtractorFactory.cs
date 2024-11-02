using AnswerScanner.WPF.Services.Responses;

namespace AnswerScanner.WPF.Services.Interfaces;

public interface IQuestionsExtractorFactory
{
    IQuestionsExtractor CreateExtractor(QuestionnaireType questionnaireType);
}