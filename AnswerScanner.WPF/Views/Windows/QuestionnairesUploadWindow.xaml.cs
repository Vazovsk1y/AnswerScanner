using System.Windows;

namespace AnswerScanner.WPF.Views.Windows;

public partial class QuestionnairesUploadWindow : Window
{
    public QuestionnairesUploadWindow()
    {
        InitializeComponent();
    }

    private void OnCancelButtonClick(object sender, RoutedEventArgs e)
    {
        // TODO: Migrate to MVVM.
        
        Close();
    }
}