﻿using System.ComponentModel.DataAnnotations;

namespace AnswerScanner.WPF.Services.Responses;

public record Questionnaire(
    QuestionnaireType Type,
    IReadOnlyDictionary<string, string> AdditionalInformation,
    IReadOnlyCollection<Question> Questions);
    
public enum QuestionnaireType
{
    [Display(Name = "Варианты ответа \"Да\" и \"Нет\"")]
    YesNoAnswerOptions,
    
    [Display(Name = "5 вариантов ответа")]
    FiveAnswerOptions,
}