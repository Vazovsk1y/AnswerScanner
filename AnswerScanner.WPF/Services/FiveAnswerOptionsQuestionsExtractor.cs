using System.Globalization;
using AnswerScanner.WPF.Services.Interfaces;
using AnswerScanner.WPF.Services.Responses;
using OpenCvSharp;
using Tesseract;
using TesseractRect = Tesseract.Rect;
using OpenCvRect = OpenCvSharp.Rect;

namespace AnswerScanner.WPF.Services;

public class FiveAnswerOptionsQuestionsExtractor : QuestionsExtractorBase, IQuestionsExtractor
{
    private static readonly string[] AnswerOptionsExpectedKeyWords = ["Совсем", "Немного", "Умеренно", "Сильно", "Очень"];
    private static readonly Dictionary<int, AnswerType> AnswerOptionIndexes = new()
    {
        { 0, AnswerType.AbsolutelyNot },
        { 1, AnswerType.Slightly },
        { 2, AnswerType.Moderately },
        { 3, AnswerType.Strongly },
        { 4, AnswerType.VeryStrongly }
    };
    private static readonly Dictionary<AnswerType, double> AnswerOptionSpacePercentages = new()
    {
        { AnswerType.AbsolutelyNot, 0.18 },
        { AnswerType.Slightly, 0.2 },
        { AnswerType.Moderately, 0.22 },
        { AnswerType.Strongly, 0.18 },
        { AnswerType.VeryStrongly, 0.2 }
    };

    private class QuestionPrivate
    {
        public required int Number { get; init; }
        
        public required TesseractRect Region { get; set; }
        
        public required string Text { get; set; }
    }
    
    public QuestionsExtractionResult ExtractFromImage(byte[] imageBytes)
    {
        using var ocrEngine = CreateTesseractEngine();
        using var pix = Pix.LoadFromMemory(imageBytes);
       
        var header = ExtractHeaderInfo(pix, ocrEngine);
        var additionalInformation = new Dictionary<string, string>
        {
            ["Главный вопрос"] = header.mainQuestion
        };

        using var ocrAppliedPage = ocrEngine.Process(pix, PageSegMode.SingleColumn);

        additionalInformation["Текст"] = ocrAppliedPage.GetText();
        additionalInformation["Значение \"Main Confidence\""] = ocrAppliedPage.GetMeanConfidence().ToString(CultureInfo.InvariantCulture);
        
        using var iterator = ocrAppliedPage.GetIterator();
        iterator.Begin();
        
        var questions = new List<QuestionPrivate>();
        QuestionPrivate? previous = null;
        
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
                if (!lineText.EndsWith(QuestionEndingSign)
                    || previous is not null && previous.Text.EndsWith(QuestionEndingSign)
                    || previous is null)
                {
                    continue;
                }
                
                _ = iterator.TryGetBoundingBox(PageIteratorLevel.TextLine, out var previousQuestionContinuationBoundingBox);
                previous.Text = $"{previous.Text} {lineText}";

                const int yOffset = 3;
                var width = previous.Region.Width < previousQuestionContinuationBoundingBox.Width ? 
                    previousQuestionContinuationBoundingBox.Width 
                    : 
                    previous.Region.Width;
                
                var height = previous.Region.Height + previousQuestionContinuationBoundingBox.Height - yOffset;
                    
                previous.Region = new TesseractRect(previous.Region.X1, previous.Region.Y1, width, height);
            }
            else
            {
                _ = iterator.TryGetBoundingBox(PageIteratorLevel.TextLine, out var questionBoundingBox);
                var questionNumber = int.Parse(match.Groups[1].Value);
                var questionText = match.Groups[2].Value;

                previous = new QuestionPrivate
                {
                    Number = questionNumber,
                    Text = questionText,
                    Region = questionBoundingBox,
                };

                questions.Add(previous);
            }
        }
        while (iterator.Next(PageIteratorLevel.TextLine));

        return new QuestionsExtractionResult(questions
            .Select(e => new Question(e.Number, e.Text, DetectAnswer(e.Region, header.answerOptionsBounds, imageBytes)))
            .ToList(), 
            additionalInformation);
    }

    private static AnswerType DetectAnswer(
        TesseractRect questionRegion,
        IReadOnlyCollection<(int x1, int x2, AnswerType answerType)> answerOptionsBounds, 
        byte[] sourceImageBytes)
    {
        try
        {
            using var decoded = Cv2.ImDecode(sourceImageBytes, ImreadModes.Color);
            using var binary = new Mat();
        
            Cv2.CvtColor(decoded, binary, ColorConversionCodes.BGR2GRAY);
            Cv2.Threshold(binary, binary, ThresholdValue, 255, ThresholdTypes.BinaryInv);

            const double yOffsetCoefficient = 0.4;
            var yOffset = (int)((answerOptionsBounds.First().x2 - answerOptionsBounds.First().x1) * yOffsetCoefficient);
            
            var y = (questionRegion.Y2 + questionRegion.Y1) / 2 - yOffset; 
            var width = answerOptionsBounds.First().x2 - answerOptionsBounds.First().x1;

            const double heightCoefficient = 0.8;
            var height = (int)(width * heightCoefficient);

            var fillRatios = new Dictionary<AnswerType, double>();
            foreach (var item in answerOptionsBounds)
            {
                using var answerRegion = new Mat(binary, new OpenCvRect(item.x1, y, width, height));

                var filledPixels = Cv2.CountNonZero(answerRegion);
                fillRatios[item.answerType] = (double)filledPixels / (answerRegion.Height * answerRegion.Width);
            }
            
            // TODO: Modify to detect undefined answers.
            return fillRatios.MaxBy(e => e.Value).Key;
        }
        catch (OpenCVException)
        {
            return AnswerType.Undefined;
        }
    }

    private static (string mainQuestion, IReadOnlyCollection<(int x1, int x2, AnswerType answerType)> answerOptionsBounds) ExtractHeaderInfo(Pix sourcePix, TesseractEngine ocrEngine)
    {
        const double headerSpacePercentage = 0.05;
        var height = (int)(sourcePix.Height * headerSpacePercentage);
        var halfWidth = sourcePix.Width / 2;

        var mainQuestionRegion = new TesseractRect(0, 0, halfWidth, height);
        var answerOptionsRegion = new TesseractRect(halfWidth, 0, halfWidth, height);

        using var mainQuestionPage = ocrEngine.Process(sourcePix, mainQuestionRegion);
        var mainQuestionText = mainQuestionPage.GetText()?.Trim() ?? string.Empty;

        mainQuestionPage.Dispose();

        using var answerOptionsPage = ocrEngine.Process(sourcePix, answerOptionsRegion);
        
        using var answerOptionsRegionIterator = answerOptionsPage.GetIterator();
        answerOptionsRegionIterator.Begin();

        const int answerOptionsCount = 5;
        var bounds = new List<(int x1, int x2, AnswerType answerType)>();
        
        do
        {
            var textLine = answerOptionsRegionIterator.GetText(PageIteratorLevel.TextLine)?.Trim();
            if (string.IsNullOrWhiteSpace(textLine) || !AnswerOptionsExpectedKeyWords.All(e => textLine.Contains(e, StringComparison.InvariantCultureIgnoreCase)))
            {
                continue;
            }

            do
            {
                var word = answerOptionsRegionIterator.GetText(PageIteratorLevel.Word)?.Trim();
                if (string.IsNullOrWhiteSpace(word))
                {
                    continue;
                }
            
                if (AnswerOptionsExpectedKeyWords.Contains(word, StringComparer.InvariantCultureIgnoreCase))
                {
                    var answerType = AnswerOptionIndexes[bounds.Count];
                    _ = answerOptionsRegionIterator.TryGetBoundingBox(PageIteratorLevel.Word, out var wordBoundingBox);
                    bounds.Add((wordBoundingBox.X1, wordBoundingBox.X2, answerType));
                }
            
                if (bounds.Count == answerOptionsCount || answerOptionsRegionIterator.IsAtFinalOf(PageIteratorLevel.TextLine, PageIteratorLevel.Word))
                {
                    break;
                }
                
            } while (answerOptionsRegionIterator.Next(PageIteratorLevel.Word));
        } while (answerOptionsRegionIterator.Next(PageIteratorLevel.TextLine));

        if (bounds.Count == answerOptionsCount)
        {
            return (mainQuestionText, bounds);
        }
        
        // This is another algorithm to extract answer options bounds.
        
        bounds.Clear();
        const double answerOptionsRegionSpacePercentage = 0.47;
        var answerOptionsRegionWidth = (int)(sourcePix.Width * answerOptionsRegionSpacePercentage);

        var firstOption = AnswerOptionIndexes.MinBy(e => e.Key).Value;
        var firstOptionSpacePercentage = AnswerOptionSpacePercentages[firstOption];
        
        (int x1, int x2, AnswerType answerType) previous = 
            (sourcePix.Width - answerOptionsRegionWidth, sourcePix.Width - answerOptionsRegionWidth + (int)(answerOptionsRegionWidth * firstOptionSpacePercentage), firstOption);
        bounds.Add(previous);
            
        for (var i = 1; i < answerOptionsCount; i++)
        {
            var answerType = AnswerOptionIndexes[bounds.Count];
            _ = AnswerOptionSpacePercentages.TryGetValue(answerType, out var spacePercentage);

            var current = (previous.x2, previous.x2 + (int)(answerOptionsRegionWidth * spacePercentage), answerType);
            bounds.Add(current);
            previous = current;
        }

        return (mainQuestionText, bounds);
    }
}