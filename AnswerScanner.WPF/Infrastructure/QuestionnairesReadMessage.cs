using AnswerScanner.WPF.ViewModels;

namespace AnswerScanner.WPF.Infrastructure;

public record QuestionnairesReadMessage(IEnumerable<QuestionnaireViewModel> Questionnaires);
