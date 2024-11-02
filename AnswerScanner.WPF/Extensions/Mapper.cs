using AnswerScanner.WPF.Services.Responses;
using AnswerScanner.WPF.ViewModels;

namespace AnswerScanner.WPF.Extensions;

public static class Mapper
{
    public static QuestionnaireViewModel ToViewModel(this Questionnaire questionnaire)
    {
        return new QuestionnaireViewModel(
            questionnaire.FilePath,
            questionnaire.Type.ToViewModel(),
            questionnaire.AdditionalInformation.Select(e => new AdditionalInformationItem(e.Key, e.Value)),
            questionnaire.Questions.Select(e => e.ToViewModel()).ToList()
        );
    }

    private static QuestionViewModel ToViewModel(this Question question)
    {
        return new QuestionViewModel(question.Number, question.Text, question.Answer.ToViewModel());
    }

    private static EnumViewModel<T> ToViewModel<T>(this T @enum) where T : Enum
    {
        return new EnumViewModel<T>(@enum);
    }
}