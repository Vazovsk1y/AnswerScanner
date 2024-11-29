using AnswerScanner.WPF.ViewModels;

namespace AnswerScanner.WPF.Infrastructure;

public record QuestionnairesReadMessage(IReadOnlyCollection<QuestionnaireViewModel> Questionnaires);