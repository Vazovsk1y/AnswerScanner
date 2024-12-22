using System.Windows;
using System.Windows.Input;

namespace AnswerScanner.WPF.Views.Windows;

public partial class QuestionnairesUploadWindow : Window
{
    public QuestionnairesUploadWindow()
    {
        InitializeComponent();
    }

    private void StartWindowMoving(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            DragMove();
        }
    }
}