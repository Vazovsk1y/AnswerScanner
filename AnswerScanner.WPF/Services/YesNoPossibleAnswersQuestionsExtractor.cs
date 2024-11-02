using System.Text.RegularExpressions;
using AnswerScanner.WPF.Services.Interfaces;
using AnswerScanner.WPF.Services.Responses;
using OpenCvSharp;
using Tesseract;
using OpenCvRect = OpenCvSharp.Rect;
using TesseractRect = Tesseract.Rect;

namespace AnswerScanner.WPF.Services;

internal partial class YesNoPossibleAnswersQuestionsExtractor : IQuestionsExtractor
{
    private const char QuestionEndingSign = ';';
    private static readonly IEnumerable<string> YesKeywordPossibleVariants = ["да", "де"];
    
    private class QuestionPrivate
    {
        public required TesseractRect QuestionRegion { get; init; }
        
        public required int QuestionNumber { get; init; }
        
        public required string QuestionText { get; init; }
        
        public required TesseractRect YesKeywordRegion { get; init; }
        
        public required TesseractRect NoKeywordRegion { get; init; }
        
        public double? YesFillRatio { get; set; }
        
        public double? NoFillRatio { get; set; }
    }
    
    public IReadOnlyCollection<Question> Extract(Page ocrAppliedImage, byte[] sourceImageBytes, AnswerRegionOccupancyDetectionSettings settings)
    {
        using var iterator = ocrAppliedImage.GetIterator();
        iterator.Begin();

        var segmentedRegions = ocrAppliedImage.GetSegmentedRegions(PageIteratorLevel.TextLine);
        var questions = new List<QuestionPrivate>();

        do  
        {
            var lineText = iterator.GetText(PageIteratorLevel.TextLine).Trim();
            if (string.IsNullOrWhiteSpace(lineText))
            {
                continue;
            }

            var match = ContainsQuestion().Match(lineText);
            if (!match.Success)
            {
                continue;
            }

            if (!iterator.TryGetBoundingBox(PageIteratorLevel.TextLine, out var questionBoundingBox))
            {
                throw new InvalidOperationException();
            }

            var questionNumber = int.Parse(match.Groups[1].Value);
            var questionText = match.Groups[2].Value;

            var (yesKeywordBoundingBox, noKeywordBoundingBox) = FindYesNoKeywordRegions(iterator);

            if (yesKeywordBoundingBox is { } yr && noKeywordBoundingBox is { } nr)
            {
                questions.Add(new QuestionPrivate
                {
                    QuestionRegion = questionBoundingBox,
                    QuestionNumber = questionNumber,
                    QuestionText = questionText[..(questionText.IndexOf(QuestionEndingSign) + 1)],
                    YesKeywordRegion = yr,
                    NoKeywordRegion = nr
                });
            }
        }
        while (iterator.Next(PageIteratorLevel.TextLine));

        var pageEngine = ocrAppliedImage.Engine;
        var pageImage = ocrAppliedImage.Image;

        ocrAppliedImage.Dispose();

        var missingQuestionRegions = segmentedRegions
            .Where(e => !questions.Select(o => o.QuestionRegion).Any(qb => qb.X1 == e.X && qb.Y1 == e.Y && qb.Width == e.Width && qb.Height == e.Height))
            .Select(e => new TesseractRect(e.X, e.Y, e.Width, e.Height));

        questions.AddRange(FindMissingQuestions(pageEngine, pageImage, missingQuestionRegions));

        var total = 0d;
        var validPairsCounter = 0;
        var checkBoxSize = (int)((questions.Select(e => e.YesKeywordRegion.Height).Average() + questions.Select(e => e.NoKeywordRegion.Width).Average()) / 2);

        foreach (var item in questions)
        {
            var (yesCheckboxRegion, noCheckboxRegion) = FindAnswerCheckboxRegions(item.YesKeywordRegion, item.NoKeywordRegion, checkBoxSize, settings);
            var yesFillRatio = CalculateFillRatio(yesCheckboxRegion, sourceImageBytes, settings);
            var noFillRatio = CalculateFillRatio(noCheckboxRegion, sourceImageBytes, settings);
            if (yesFillRatio is not { } yfr || noFillRatio is not { } nfr)
            {
                continue;
            }

            item.YesFillRatio = yfr;
            item.NoFillRatio = nfr;
            total += yfr;
            total += nfr;
            validPairsCounter++;
        }

        var occupancyRate = total / (validPairsCounter * 2);
        return questions.Select(e =>
        {
            var answer = e is not { YesFillRatio: { } yfr, NoFillRatio: { } nfr } ? AnswerType.Undefined : GetAnswer(yfr, nfr, occupancyRate);
            return new Question(e.QuestionNumber, e.QuestionText, answer);
        }).ToList();
    }
    
    [GeneratedRegex(@"^(\d+)(?:\.\s+|\s+)(.+)$")]
    private static partial Regex ContainsQuestion();

    
    private static IEnumerable<QuestionPrivate> FindMissingQuestions(
        TesseractEngine ocrEngine,
        Pix pixSourceImage,
        IEnumerable<TesseractRect> missingQuestionRegions)
    {
        foreach (var questionRegion in missingQuestionRegions)
        {
            using var missedQuestionPage = ocrEngine.Process(pixSourceImage, questionRegion, PageSegMode.RawLine);
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
                yield return new QuestionPrivate
                {
                    QuestionRegion = questionRegion,
                    QuestionNumber = questionNumber,
                    QuestionText = questionText[..(questionText.IndexOf(QuestionEndingSign) + 1)],
                    YesKeywordRegion = yr,
                    NoKeywordRegion = nr
                };
            }
        }
    }

    private static (OpenCvRect yesCheckboxRegion, OpenCvRect noCheckboxRegion) FindAnswerCheckboxRegions(
        TesseractRect yesKeywordRegion, 
        TesseractRect noKeywordRegion, 
        int checkBoxSize,
        AnswerRegionOccupancyDetectionSettings settings)
    {
        var yesX = yesKeywordRegion.X2 + settings.XOffset;
        var noX = noKeywordRegion.X2 + settings.XOffset;

        int y;
        const int tolerance = 8;
        if (Math.Abs(yesKeywordRegion.Y1 - noKeywordRegion.Y1) <= tolerance)
        {
            y = noKeywordRegion.Y1 - settings.YOffset;
        }
        else
        {
            const double offsetPercent = 0.4;
            y = Math.Min(yesKeywordRegion.Y1, noKeywordRegion.Y1) - (int)(settings.YOffset * offsetPercent);
        }

        checkBoxSize = (int)(checkBoxSize * settings.SizeScaleCoefficient);

        OpenCvRect yesCheckboxRect = new(yesX, y, checkBoxSize, checkBoxSize);
        OpenCvRect noCheckboxRect = new(noX, y, checkBoxSize, checkBoxSize);

        return (yesCheckboxRect, noCheckboxRect);
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
    
    private static AnswerType GetAnswer(double yesFillRatio, double noFillRatio, double occupancyRate)
    {
        var isYesChecked = IsFilled(yesFillRatio, occupancyRate);
        var isNoChecked = IsFilled(noFillRatio, occupancyRate);

        if (isYesChecked && isNoChecked || !isYesChecked && !isNoChecked)
        {
            return AnswerType.Undefined;
        }

        return isYesChecked ? AnswerType.Yes : AnswerType.No;
        
        static bool IsFilled(double fillRatio, double occupancyRate)
        {
            return fillRatio > occupancyRate;
        }
    }

    private static double? CalculateFillRatio(
        OpenCvRect answerRegion, 
        byte[] sourceImageBytes, 
        AnswerRegionOccupancyDetectionSettings settings)
    {
        try
        {
            using var sourceImage = Cv2.ImDecode(sourceImageBytes, ImreadModes.Color);

            using var matImg = new Mat(sourceImage, answerRegion);
            using var binaryImage = new Mat();
        
            Cv2.CvtColor(matImg, binaryImage, ColorConversionCodes.BGR2GRAY);
            Cv2.Threshold(binaryImage, binaryImage, settings.ThresholdValue, 255, ThresholdTypes.BinaryInv);
        
            var filledPixels = Cv2.CountNonZero(binaryImage);
            return (double)filledPixels / (binaryImage.Height * binaryImage.Width);
        }
        catch (OpenCVException)
        {
            return null;
        }
    }
}