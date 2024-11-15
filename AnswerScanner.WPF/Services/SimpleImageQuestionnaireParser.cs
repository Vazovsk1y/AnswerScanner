using AnswerScanner.WPF.Services.Interfaces;
using System.IO;
using AnswerScanner.WPF.Services.Responses;
using Microsoft.Extensions.DependencyInjection;

namespace AnswerScanner.WPF.Services;

internal class SimpleImageQuestionnaireParser : IQuestionnaireParser
{
    public Questionnaire ParseFromFile(string filePath, QuestionnaireType questionnaireType)
    {
        var imageBytes = File.ReadAllBytes(filePath);
        
        var questionsExtractorFactory = App.Services.GetRequiredService<IQuestionsExtractorFactory>();
        var questionsExtractor = questionsExtractorFactory.CreateExtractor(questionnaireType);

        var questionsExtractionResult = questionsExtractor.ExtractFromImage(imageBytes);
        
        return new Questionnaire(
            filePath, 
            questionnaireType, 
            questionsExtractionResult.AdditionalInformation, 
            questionsExtractionResult.Questions);
    }
}