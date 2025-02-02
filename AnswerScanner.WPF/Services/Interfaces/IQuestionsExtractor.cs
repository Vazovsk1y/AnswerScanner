﻿using AnswerScanner.WPF.Services.Responses;

namespace AnswerScanner.WPF.Services.Interfaces;

public interface IQuestionsExtractor
{
    QuestionsExtractionResult ExtractFromImage(byte[] imageBytes);
}