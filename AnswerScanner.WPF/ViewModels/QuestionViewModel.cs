using AnswerScanner.WPF.Services.Responses;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AnswerScanner.WPF.ViewModels;

public partial class QuestionViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsUnique))]
    private QuestionnaireViewModel? _parent;
    
    public required string Text { get; init; }

    [ObservableProperty]
    private EnumViewModel<AnswerType> _answer = new(AnswerType.Undefined);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsUnique))]
    private uint _number;
    
    public bool IsUnique => Parent?.Questions.Count(q => q.Number == Number) == 1;

    partial void OnNumberChanged(uint value)
    {
        Parent?.RefreshMissedQuestionNumbers();
    }
}