using System.Globalization;
using System.Windows.Data;
using AnswerScanner.WPF.Services.Responses;
using AnswerScanner.WPF.ViewModels;

namespace AnswerScanner.WPF.Infrastructure;

public class QuestionnaireTypeAnswersConverter : IValueConverter
{
    private static readonly IReadOnlyCollection<EnumViewModel<AnswerType>> YesNoAnswers = 
    [
        new(AnswerType.Yes), 
        new(AnswerType.No)
    ];

    private static readonly IReadOnlyCollection<EnumViewModel<AnswerType>> FiveAnswers =
    [
        new(AnswerType.AbsolutelyNot), 
        new(AnswerType.Slightly), 
        new(AnswerType.Moderately), 
        new(AnswerType.Strongly),
        new(AnswerType.VeryStrongly)
    ];
     
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is QuestionnaireType questionnaireType)
        {
            return questionnaireType switch
            {
                QuestionnaireType.YesNoAnswerOptions => YesNoAnswers,
                QuestionnaireType.FiveAnswerOptions => FiveAnswers,
                _ => throw new KeyNotFoundException()
            };
        }
        
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}