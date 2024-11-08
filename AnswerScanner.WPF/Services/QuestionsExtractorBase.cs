using System.Text.RegularExpressions;

namespace AnswerScanner.WPF.Services;

public abstract partial class QuestionsExtractorBase
{
    protected const char QuestionEndingSign = ';';
    
    [GeneratedRegex(@"^(\d+)\.\s+(.+)$")]
    protected static partial Regex ContainsQuestion();
}