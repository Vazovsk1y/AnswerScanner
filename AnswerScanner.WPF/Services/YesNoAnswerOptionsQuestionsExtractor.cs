using System.Drawing;
using System.Globalization;
using AnswerScanner.WPF.Services.Interfaces;
using AnswerScanner.WPF.Services.Responses;
using OpenCvSharp;
using Tesseract;
using OpenCvRect = OpenCvSharp.Rect;
using TesseractRect = Tesseract.Rect;

namespace AnswerScanner.WPF.Services;

internal class YesNoAnswerOptionsQuestionsExtractor : QuestionsExtractorBase, IQuestionsExtractor
{
    private static readonly IEnumerable<string> YesKeywordPossibleVariants = ["да", "де"];
    
    public QuestionsExtractionResult ExtractFromImage(byte[] imageBytes)
    {
        using var ocrEngine = CreateTesseractEngine();
        using var pix = Pix.LoadFromMemory(imageBytes);
        using var ocrAppliedPage = ocrEngine.Process(pix, PageSegMode.SingleBlock);
        
        using var iterator = ocrAppliedPage.GetIterator();
        iterator.Begin();

        var segmentedRegions = ocrAppliedPage.GetSegmentedRegions(PageIteratorLevel.TextLine);
        var additionalInformation = new Dictionary<string, string>
        {
            ["Текст"] = ocrAppliedPage.GetText(),
            ["Значение \"Main Confidence\""] = ocrAppliedPage.GetMeanConfidence().ToString(CultureInfo.InvariantCulture)
        };

        var questions = new List<Question>();
        do  
        {
            var lineText = iterator.GetText(PageIteratorLevel.TextLine)?.Trim();
            if (string.IsNullOrWhiteSpace(lineText))
            {
                continue;
            }

            var match = ContainsQuestion().Match(lineText);
            if (!match.Success)
            {
                continue;
            }

            _ = iterator.TryGetBoundingBox(PageIteratorLevel.TextLine, out var questionBoundingBox);

            var questionNumber = int.Parse(match.Groups[1].Value);
            var questionText = match.Groups[2].Value;

            var (yesKeywordBoundingBox, noKeywordBoundingBox) = FindYesNoKeywordRegions(iterator);

            if (yesKeywordBoundingBox is not { } yr || noKeywordBoundingBox is not { } nr)
            {
                continue;
            }
            
            questions.Add(new Question(questionNumber, TrimQuestionText(questionText), DetectAnswer(questionBoundingBox, yr, nr, imageBytes)));
                
            var processedQuestionRegion = new Rectangle(questionBoundingBox.X1, questionBoundingBox.Y1, questionBoundingBox.Width, questionBoundingBox.Height);
            segmentedRegions.Remove(processedQuestionRegion);
        }
        while (iterator.Next(PageIteratorLevel.TextLine));

        ocrAppliedPage.Dispose();

        questions.AddRange(ExtractMissingQuestions(ocrEngine, pix, imageBytes, segmentedRegions.Select(e => new TesseractRect(e.X, e.Y, e.Width, e.Height))));
        
        return new QuestionsExtractionResult(questions, additionalInformation);
    }
    
    private static IEnumerable<Question> ExtractMissingQuestions(
        TesseractEngine ocrEngine,
        Pix sourceImagePix,
        byte[] sourceImageBytes,
        IEnumerable<TesseractRect> missingQuestionRegions)
    {
        foreach (var questionRegion in missingQuestionRegions)
        {
            using var missedQuestionPage = ocrEngine.Process(sourceImagePix, questionRegion, PageSegMode.RawLine);
            using var iterator = missedQuestionPage.GetIterator();
            iterator.Begin();

            var text = missedQuestionPage.GetText();

            var match = ContainsQuestion().Match(text);

            if (!match.Success)
            {
                continue;
            }

            var questionNumber = int.Parse(match.Groups[1].Value);
            var questionText = match.Groups[2].Value;

            var (yesKeywordBoundingBox, noKeywordBoundingBox) = FindYesNoKeywordRegions(iterator);

            if (yesKeywordBoundingBox is { } yr && noKeywordBoundingBox is { } nr)
            {
                yield return new Question(questionNumber, TrimQuestionText(questionText),
                    DetectAnswer(questionRegion, yr, nr, sourceImageBytes));
            }
        }
    }
    
    private static AnswerType DetectAnswer(
        TesseractRect questionRegion,
        TesseractRect yesKeywordRegion,
        TesseractRect noKeywordRegion,
        byte[] sourceImageBytes)
    {
        var yesX = yesKeywordRegion.X2;
        var noX = noKeywordRegion.X2;

        var y = (questionRegion.Y2 + questionRegion.Y1) / 2 - noKeywordRegion.Height;
        var size = noKeywordRegion.Width;

        OpenCvRect yesCheckboxRect = new(yesX, y, size, size);
        OpenCvRect noCheckboxRect = new(noX, y, size, size);

        var yesFillRatio = CalculateFillRatio(yesCheckboxRect, sourceImageBytes);
        var noFillRatio = CalculateFillRatio(noCheckboxRect, sourceImageBytes);

        if (yesFillRatio is not {} yfr || noFillRatio is not {} nfr)
        {
            return AnswerType.Undefined;
        }

        const double tolerance = 0.01;
        const int roundingDigits = 2;
        if (Math.Abs(Math.Round(yfr, roundingDigits) -  Math.Round(nfr, roundingDigits)) <= tolerance)
        {
            return AnswerType.Undefined;
        }

        if (yfr > nfr)
        {
            return AnswerType.Yes;
        }

        return nfr > yfr ? AnswerType.No : AnswerType.Undefined;
    }
    
    private static (TesseractRect? yesKeywordRegion, TesseractRect? noKeywordRegion) FindYesNoKeywordRegions(ResultIterator iterator)
    {
        const string noKeyword = "нет";

        TesseractRect? yes = null;
        TesseractRect? no = null;

        var questionEndingSignGone = false;
        while (iterator.Next(PageIteratorLevel.Word))
        {
            var word = iterator.GetText(PageIteratorLevel.Word).Trim();

            if (word.EndsWith(QuestionEndingSign))
            {
                questionEndingSignGone = true;
            }

            if (questionEndingSignGone && iterator.TryGetBoundingBox(PageIteratorLevel.Word, out var keywordBoundingBox))
            {
                if (YesKeywordPossibleVariants.Any(e => word.Contains(e, StringComparison.InvariantCultureIgnoreCase)))
                {
                    yes = keywordBoundingBox;
                }
                else if (word.Contains(noKeyword, StringComparison.InvariantCultureIgnoreCase))
                {
                    no = keywordBoundingBox;
                }

                if (yes is not null && no is not null && iterator.IsAtFinalOf(PageIteratorLevel.TextLine, PageIteratorLevel.Word))
                {
                    break;
                }
            }

            if (iterator.IsAtFinalOf(PageIteratorLevel.TextLine, PageIteratorLevel.Symbol))
            {
                break;
            }
        }

        return (yes, no);
    }
    
    private static double? CalculateFillRatio(OpenCvRect answerRegion, byte[] sourceImageBytes)
    {
        try
        {
            using var sourceImage = Cv2.ImDecode(sourceImageBytes, ImreadModes.Color);

            using var matImg = new Mat(sourceImage, answerRegion);
            using var binaryImage = new Mat();

            Cv2.CvtColor(matImg, binaryImage, ColorConversionCodes.BGR2GRAY);
            Cv2.Threshold(binaryImage, binaryImage, ThresholdValue, 255, ThresholdTypes.BinaryInv);

            var filledPixels = Cv2.CountNonZero(binaryImage);
            return (double)filledPixels / (binaryImage.Height * binaryImage.Width);
        }
        catch (OpenCVException)
        {
            return null;
        }
    }

    private static string TrimQuestionText(string text)
    {
        return text[..(text.IndexOf(QuestionEndingSign) + 1)];
    }
}