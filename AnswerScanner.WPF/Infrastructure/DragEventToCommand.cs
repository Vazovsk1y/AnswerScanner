using System.Windows;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

namespace AnswerScanner.WPF.Infrastructure;

public class DragEventToCommand : TriggerAction<DependencyObject>
{
    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(DragEventToCommand), new PropertyMetadata(null));

    public ICommand Command
    {
        get => (ICommand)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    protected override void Invoke(object parameter)
    {
        if (Command.CanExecute(parameter))
        {
            Command.Execute(parameter);
        }
    }
}