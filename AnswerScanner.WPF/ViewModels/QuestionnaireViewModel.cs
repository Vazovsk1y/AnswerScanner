using AnswerScanner.WPF.Services.Responses;

namespace AnswerScanner.WPF.ViewModels;

public record QuestionnaireViewModel(
    string? FilePath,
    EnumViewModel<QuestionnaireType> Type,
    IEnumerable<AdditionalInformationItem> AdditionalInformation,
    IReadOnlyCollection<QuestionViewModel> Questions);


