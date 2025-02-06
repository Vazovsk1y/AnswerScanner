using System.Collections.ObjectModel;
using AnswerScanner.WPF.Services.Responses;
using AnswerScanner.WPF.ViewModels;

namespace AnswerScanner.WPF.Extensions;

public static class Mapper
{
    public static QuestionnaireViewModel ToViewModel(this Questionnaire questionnaire, string filePath, string name)
    {
        var result = new QuestionnaireViewModel
        {
            Name = name,
            FilePath = filePath,
            AdditionalInformation = questionnaire.AdditionalInformation.Select(e => new AdditionalInformationItemViewModel(e.Key, e.Value)).ToList(),
            Type = questionnaire.Type.ToViewModel(),
        };

        var questions = new ObservableCollection<QuestionViewModel>(questionnaire.Questions.Select(e => e.ToViewModel(result)));
        foreach (var question in questions)
        {
            question.Parent = result;
        }

        result.Questions = questions;
        result.RefreshMissedQuestionNumbers();
        
        return result;
    }

    private static QuestionViewModel ToViewModel(this Question question, QuestionnaireViewModel parent)
    {
        return new QuestionViewModel
        {
            Parent = parent,
            Number = (uint)question.Number,
            Text = question.Text,
            Answer = question.Answer.ToViewModel(),
        };
    }

    public static EnumViewModel<T> ToViewModel<T>(this T @enum) where T : Enum
    {
        return new EnumViewModel<T>(@enum);
    }
}