using AnswerScanner.WPF.Services.Responses;

namespace AnswerScanner.WPF.ViewModels;

public record QuestionViewModel(
    int Number,
    string Text,
    EnumViewModel<AnswerType> Answer);