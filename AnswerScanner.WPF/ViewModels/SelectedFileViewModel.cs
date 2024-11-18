using AnswerScanner.WPF.Services.Responses;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AnswerScanner.WPF.ViewModels;

public partial class SelectedFileViewModel : ObservableObject
{
    public required string FilePath { get; init; }
    
    [ObservableProperty]
    private EnumViewModel<QuestionnaireType> _selectedQuestionnaireType = QuestionnairesUploadViewModel.AvailableQuestionnaireTypes.First();

    [ObservableProperty]
    private bool _isProcessed;
}