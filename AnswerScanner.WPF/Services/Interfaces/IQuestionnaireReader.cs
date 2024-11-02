using AnswerScanner.WPF.Services.Responses;

namespace AnswerScanner.WPF.Services.Interfaces;

public interface IQuestionnaireReader
{
    Questionnaire ReadFromFile(string filePath, QuestionnaireType questionnaireType);
}