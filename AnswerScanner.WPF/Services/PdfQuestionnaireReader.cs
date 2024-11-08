using System.Globalization;
using AnswerScanner.WPF.Services.Interfaces;
using System.IO;
using AnswerScanner.WPF.Services.Responses;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Graphics.Colors;
using UglyToad.PdfPig.Rendering.Skia;

namespace AnswerScanner.WPF.Services;

internal class PdfQuestionnaireReader : IQuestionnaireReader
{
    public Questionnaire ReadFromFile(string filePath, QuestionnaireType questionnaireType)
    {
        using var pdfDocument = PdfDocument.Open(filePath);
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

            var (pageText, pageMeanConfidence, pageQuestions) = ImageQuestionnaireReader.ReadFromPdfPageImage(pageAsPng, questionnaireType, additionalInformation);
            result.AddRange(pageQuestions);
            ocrPagesCount++;

            additionalInformation[$"Текст страницы номер {page.Number}"] = pageText;
            additionalInformation[$"Значение \"Mean Confidence\" для страницы номер {page.Number}"] = pageMeanConfidence.ToString(CultureInfo.InvariantCulture);
        }

        additionalInformation["Количество non-searchable страниц"] = ocrPagesCount.ToString();

        return new Questionnaire(filePath, questionnaireType, additionalInformation, result);
    }

    private static bool IsNonSearchablePage(Page page)
    {
        return string.IsNullOrWhiteSpace(page.Text) && page.NumberOfImages == 1;
    }
}