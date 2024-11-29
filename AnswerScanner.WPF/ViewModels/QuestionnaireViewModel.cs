using AnswerScanner.WPF.Services.Responses;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AnswerScanner.WPF.ViewModels;

public partial class QuestionnaireViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = "Опросник";
    public string? FilePath { get; init; }
    public required EnumViewModel<QuestionnaireType> Type { get; init; }
    public required IReadOnlyCollection<AdditionalInformationItemViewModel> AdditionalInformation { get; init; }
    public required IReadOnlyCollection<QuestionViewModel> Questions { get; init; }
}


