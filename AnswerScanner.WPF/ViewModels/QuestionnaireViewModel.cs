using System.Collections.ObjectModel;
using AnswerScanner.WPF.Services.Responses;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AnswerScanner.WPF.ViewModels;

public partial class QuestionnaireViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = "Опросник";

    public ObservableCollection<uint> MissedQuestionNumbers { get; } = [];
    
    public string? FilePath { get; init; }
    
    public required EnumViewModel<QuestionnaireType> Type { get; init; }
    
    public required IReadOnlyCollection<AdditionalInformationItemViewModel> AdditionalInformation { get; init; }

    public ObservableCollection<QuestionViewModel> Questions { get; set; } = [];

    public void RefreshMissedQuestionNumbers()
    {
        MissedQuestionNumbers.Clear();
        
        var existingNumbers = new HashSet<uint>(Questions.Select(q => q.Number));
        const uint min = 1;
        var max = Questions.Count == 0 ? 1 : existingNumbers.Max();

        if (max <= min)
        {
            return;
        }

        for (var i = min; i <= max; i++)
        {
            if (!existingNumbers.Contains(i))
            {
                MissedQuestionNumbers.Add(i);
            }
        }
    }
}


