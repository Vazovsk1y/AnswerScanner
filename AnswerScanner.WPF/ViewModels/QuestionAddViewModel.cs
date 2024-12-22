using System.Windows;
using AnswerScanner.WPF.Services.Responses;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AnswerScanner.WPF.ViewModels;

public partial class QuestionAddViewModel : ObservableRecipient
{
    public required QuestionnaireViewModel Questionnaire { get; init; }
    
    public required int QuestionIndex { get; init; }
    
    public required int QuestionNumber { get; init; }
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private string? _questionText;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private EnumViewModel<AnswerType>? _answer;

    [RelayCommand(CanExecute = nameof(CanConfirm))]
    private void Confirm(Window window)
    {
        var question = new QuestionViewModel
        {
            Number = QuestionNumber,
            Text = QuestionText!,
            Answer = Answer!,
        };
        
        Questionnaire.Questions.Insert(QuestionIndex, question);
        window.Close();
    }
    
    private bool CanConfirm() => !string.IsNullOrWhiteSpace(QuestionText) &&
                                 Answer is not null;
}