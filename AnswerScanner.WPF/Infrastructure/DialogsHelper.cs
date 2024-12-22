using AnswerScanner.WPF.ViewModels;

namespace AnswerScanner.WPF.Infrastructure;

public static class DialogsHelper
{
    private static readonly string SelectFilesDialogFilter = $"Files|{string.Join(";", MainWindowViewModel.AllowedExtensions.Select(e => $"*{e}"))}";
    
    public static string[] ShowSelectAvailableToUploadFilesDialog()
    {
        const string title = "Выберите файлы:";

        var fileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = title,
            RestoreDirectory = true,
            Multiselect = true,
            Filter = SelectFilesDialogFilter,
        };

        fileDialog.ShowDialog();
        return fileDialog.FileNames;
    }
}