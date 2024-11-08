using System.Globalization;
using AnswerScanner.WPF.Services.Interfaces;
using System.IO;
using AnswerScanner.WPF.Services.Responses;
using Microsoft.Extensions.DependencyInjection;
using Tesseract;

namespace AnswerScanner.WPF.Services;

internal class ImageQuestionnaireReader : IQuestionnaireReader
{
    public Questionnaire ReadFromFile(string filePath, QuestionnaireType questionnaireType)
    {
        var imageBytes = File.ReadAllBytes(filePath);
        
        using var engine = CreateTesseractEngine();
        using var pixImg = Pix.LoadFromMemory(imageBytes);
        using var ocrAppliedPage = engine.Process(pixImg, GetPageSeqMode(questionnaireType));

        var additionalInformation = new Dictionary<string, string>
        {
            ["Текст"] = ocrAppliedPage.GetText(),
            ["Значение \"Main Confidence\""] = ocrAppliedPage.GetMeanConfidence().ToString(CultureInfo.InvariantCulture)
        };
        
        var questionsExtractorFactory = App.Services.GetRequiredService<IQuestionsExtractorFactory>();
        var questionsExtractor = questionsExtractorFactory.CreateExtractor(questionnaireType);
        
        return new Questionnaire(
            filePath, 
            questionnaireType, 
            additionalInformation, 
            questionsExtractor.Extract(ocrAppliedPage, imageBytes, new AnswerRegionOccupancyDetectionSettings
            {
                YOffset = 27,
                SizeScaleCoefficient = 1.7,
            }, additionalInformation));
    }

    internal static (string pageText, float pageMeanConfidence, IReadOnlyCollection<Question> pageQuestions) ReadFromPdfPageImage(MemoryStream pdfPageImage, QuestionnaireType questionnaireType, Dictionary<string, string> additionalInformation)
    {
        using var engine = CreateTesseractEngine();
        
        var imageBytes = GetBytesFromStream(pdfPageImage);
        using var pixImg = Pix.LoadFromMemory(imageBytes);
        
        using var ocrAppliedPage = engine.Process(pixImg, GetPageSeqMode(questionnaireType));

        var questionsExtractorFactory = App.Services.GetRequiredService<IQuestionsExtractorFactory>();
        var questionsExtractor = questionsExtractorFactory.CreateExtractor(questionnaireType);

        return (ocrAppliedPage.GetText(), ocrAppliedPage.GetMeanConfidence(), questionsExtractor.Extract(ocrAppliedPage, imageBytes, new AnswerRegionOccupancyDetectionSettings
        {
            YOffset = 20,
        }, additionalInformation));
    }

    private static byte[] GetBytesFromStream(Stream stream)
    {
        if (stream is MemoryStream ms)
        {
            return ms.ToArray();
        }

        using var memoryStream = new MemoryStream(); 
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }

    private static PageSegMode? GetPageSeqMode(QuestionnaireType questionnaireType)
    {
        return questionnaireType switch
        {
            QuestionnaireType.YesNoPossibleAnswers => PageSegMode.SingleBlock,
            QuestionnaireType.FivePossibleAnswers => null,
            _ => null,
        };
    }

    private static TesseractEngine CreateTesseractEngine()
    {
        return new TesseractEngine("tessdata", "rus", EngineMode.LstmOnly);
    }
}