using AnswerScanner.WPF.Services.Responses;

namespace AnswerScanner.WPF.Services.Interfaces;

public interface IQuestionsExtractor
{
    IReadOnlyCollection<Question> Extract(
        Tesseract.Page ocrAppliedImage, 
        byte[] sourceImageBytes,
        AnswerRegionOccupancyDetectionSettings settings);
}

public record AnswerRegionOccupancyDetectionSettings
{
    public static readonly AnswerRegionOccupancyDetectionSettings Default = new();
    
    public double ThresholdValue { get; init; } = 174;

    public int YOffset { get; init; } = 20;
    
    public int XOffset { get; init; } = 3;

    public double SizeScaleCoefficient { get; init; } = 1.5;
}