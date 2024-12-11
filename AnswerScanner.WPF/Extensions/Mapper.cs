using AnswerScanner.WPF.Services.Responses;
using AnswerScanner.WPF.ViewModels;

namespace AnswerScanner.WPF.Extensions;

public static class Mapper
{
    public static QuestionnaireViewModel ToViewModel(this Questionnaire questionnaire, string filePath, string name)
    {
        return new QuestionnaireViewModel
        {
            Name = name,
            FilePath = filePath,
            Questions = questionnaire.Questions.Select(e => e.ToViewModel()).ToList(),
            AdditionalInformation = questionnaire.AdditionalInformation.Select(e => new AdditionalInformationItemViewModel(e.Key, e.Value)).ToList(),
            Type = questionnaire.Type.ToViewModel(),
        };
    }

    private static QuestionViewModel ToViewModel(this Question question)
    {
        return new QuestionViewModel
        {
            Number = question.Number,
            Text = question.Text,
            Answer = question.Answer.ToViewModel(),
        };
    }

    private static EnumViewModel<T> ToViewModel<T>(this T @enum) where T : Enum
    {
        return new EnumViewModel<T>(@enum);
    }
}