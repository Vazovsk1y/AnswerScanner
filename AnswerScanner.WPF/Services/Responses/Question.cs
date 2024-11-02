using System.ComponentModel.DataAnnotations;

namespace AnswerScanner.WPF.Services.Responses;

public record Question(
    int Number,
    string Text,
    AnswerType Answer);
    
public enum AnswerType
{
    [Display(Name = "Не определен")]
    Undefined,
    
    [Display(Name = "Да")]
    Yes,
    
    [Display(Name = "Нет")]
    No
}