using AnswerScanner.WPF.Services.Interfaces;
using AnswerScanner.WPF.Services.Responses;
using Microsoft.Extensions.DependencyInjection;

namespace AnswerScanner.WPF.Services;

internal class SimpleImageQuestionnaireParser : IQuestionnaireParser
{
    public Questionnaire ParseFromFile(byte[] fileBytes, QuestionnaireType questionnaireType)
    {
        var questionsExtractorFactory = App.Services.GetRequiredService<IQuestionsExtractorFactory>();
        var questionsExtractor = questionsExtractorFactory.CreateExtractor(questionnaireType);

        var questionsExtractionResult = questionsExtractor.ExtractFromImage(fileBytes);
        
        return new Questionnaire(
            questionnaireType, 
            questionsExtractionResult.AdditionalInformation, 
            questionsExtractionResult.Questions);
    }
}