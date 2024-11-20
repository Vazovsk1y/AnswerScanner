using AnswerScanner.WPF.Services.Responses;

namespace AnswerScanner.WPF.Services.Interfaces;

public interface IQuestionnaireParser
{
    Questionnaire ParseFromFile(byte[] fileBytes, QuestionnaireType questionnaireType);
}