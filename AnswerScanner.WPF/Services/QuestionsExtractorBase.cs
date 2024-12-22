using System.Text.RegularExpressions;
using Tesseract;

namespace AnswerScanner.WPF.Services;

public abstract partial class QuestionsExtractorBase
{
    protected const char QuestionEndingSign = ';';
    protected const int ThresholdValue = 175;
    
    [GeneratedRegex(@".*?(\d+)[.,]\s+(.+)$")]
    protected static partial Regex ContainsQuestion();
    
    protected static TesseractEngine CreateTesseractEngine()
    {
        return new TesseractEngine("tessdata", "rus", EngineMode.LstmOnly);
    }
}