using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace AnswerScanner.WPF.ViewModels;

public record EnumViewModel<T> where T : Enum
{
    public string? DisplayTitle { get; }
    
    public T Value { get; }
    
    public EnumViewModel(T value)
    {
        Value = value;
        var field = value.GetType().GetField(value.ToString());
        var attribute = field?.GetCustomAttribute<DisplayAttribute>();
        DisplayTitle = attribute?.Name;
    }
}