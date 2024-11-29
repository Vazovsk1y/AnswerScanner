using System.Windows;
using System.Windows.Input;

namespace AnswerScanner.WPF.Views.Windows;

public partial class QuestionnairesExportWindow : Window
{
    public QuestionnairesExportWindow()
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