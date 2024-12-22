namespace AnswerScanner.WPF.Services.Responses;

public record QuestionsExtractionResult(
    IReadOnlyCollection<Question> Questions, 
    IReadOnlyDictionary<string, string> AdditionalInformation);