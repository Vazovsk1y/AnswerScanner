using AnswerScanner.WPF.Services.Interfaces;
using System.IO;
using AnswerScanner.WPF.Services.Responses;
using Microsoft.Extensions.DependencyInjection;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Graphics.Colors;
using UglyToad.PdfPig.Rendering.Skia;

namespace AnswerScanner.WPF.Services;

internal class PdfQuestionnaireParser : IQuestionnaireParser
{
    public Questionnaire ParseFromFile(byte[] fileBytes, QuestionnaireType questionnaireType)
    {
        using var pdfDocument = PdfDocument.Open(fileBytes);
        pdfDocument.AddSkiaPageFactory();

        var result = new List<Question>();

        var additionalInformation = new Dictionary<string, string>
        {
            ["Количество страниц"] = pdfDocument.NumberOfPages.ToString(),
        };

        var ocrPagesCount = 0;
        foreach (var page in pdfDocument.GetPages())
        {
            if (!IsNonSearchablePage(page))
            {
                continue;
            }
            
            // Perform OCR for non-searchable page.

            const int scale = 3;
            const int quality = 300;
            
            using var pageAsPng = pdfDocument.GetPageAsPng(page.Number, scale, RGBColor.White, quality);
            pageAsPng.Seek(0, SeekOrigin.Begin);
            
            var questionsExtractorFactory = App.Services.GetRequiredService<IQuestionsExtractorFactory>();
            var questionsExtractor = questionsExtractorFactory.CreateExtractor(questionnaireType);

            var questionsExtractionResult = questionsExtractor.ExtractFromImage(GetBytesFromStream(pageAsPng));

            // TODO: Refactor.
            additionalInformation[$"Страница {page.Number}"] = string.Join($"{new string('-', 80)}\n",
                questionsExtractionResult.AdditionalInformation.Select(e => $"{e.Key}:\n{e.Value}\n"));
            
            result.AddRange(questionsExtractionResult.Questions);
            ocrPagesCount++;
        }

        additionalInformation["Количество non-searchable страниц"] = ocrPagesCount.ToString();

        return new Questionnaire(questionnaireType, additionalInformation, result);
    }

    private static bool IsNonSearchablePage(Page page)
    {
        return string.IsNullOrWhiteSpace(page.Text);
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
}