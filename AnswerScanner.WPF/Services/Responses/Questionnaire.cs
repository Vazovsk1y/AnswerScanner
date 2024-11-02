using System.ComponentModel.DataAnnotations;

namespace AnswerScanner.WPF.Services.Responses;

public record Questionnaire(
    string? FilePath,
    QuestionnaireType Type,
    IReadOnlyDictionary<string, string> AdditionalInformation,
    IReadOnlyCollection<Question> Questions);
    
public enum QuestionnaireType
{
    [Display(Name = "Варианты ответа \"Да\" и \"Нет\"")]
    YesNoPossibleAnswers,
}