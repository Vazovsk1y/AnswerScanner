using AnswerScanner.WPF.Services.Responses;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AnswerScanner.WPF.ViewModels;

public partial class QuestionViewModel : ObservableObject
{
    public required int Number { get; init; }
    
    public required string Text { get; init; }

    [ObservableProperty]
    private EnumViewModel<AnswerType> _answer = new(AnswerType.Undefined);
}