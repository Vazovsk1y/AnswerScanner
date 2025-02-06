using System.Text.RegularExpressions;
using Tesseract;

namespace AnswerScanner.WPF.Services;

public abstract partial class QuestionsExtractorBase
{
    protected const char QuestionEndingSign = ';';
    protected const int ThresholdValue = 175;
    private const string CharBlackList = "$#@";
    
    [GeneratedRegex(@".*?(\d+)[.,]\s+(.+)$")]
    protected static partial Regex ContainsQuestion();
    
    protected static TesseractEngine CreateTesseractEngine()
    {
        var engine = new TesseractEngine("tessdata", "rus", EngineMode.LstmOnly);
        
        // Tesseract OCR is misinterpreting some numbers as those chars.
        engine.SetVariable("tessedit_char_blacklist", CharBlackList);
        
        return engine;
    }
}