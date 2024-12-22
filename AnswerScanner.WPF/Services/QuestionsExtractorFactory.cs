using AnswerScanner.WPF.Services.Interfaces;
using AnswerScanner.WPF.Services.Responses;
using Microsoft.Extensions.DependencyInjection;

namespace AnswerScanner.WPF.Services;

internal class QuestionsExtractorFactory : IQuestionsExtractorFactory
{
    public IQuestionsExtractor CreateExtractor(QuestionnaireType questionnaireType)
    {
        return questionnaireType switch
        {
            QuestionnaireType.YesNoAnswerOptions => App.Services.GetRequiredService<YesNoAnswerOptionsQuestionsExtractor>(),
            QuestionnaireType.FiveAnswerOptions => App.Services.GetRequiredService<FiveAnswerOptionsQuestionsExtractor>(),
            _ => throw new ArgumentException(questionnaireType.ToString()),
        };
    }
}