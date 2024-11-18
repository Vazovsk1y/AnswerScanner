using System.Collections.ObjectModel;
using System.Windows;
using AnswerScanner.WPF.Extensions;
using AnswerScanner.WPF.Infrastructure;
using AnswerScanner.WPF.Services.Interfaces;
using AnswerScanner.WPF.Services.Responses;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace AnswerScanner.WPF.ViewModels;

public partial class QuestionnairesUploadViewModel : ObservableRecipient
{
    public static readonly IEnumerable<EnumViewModel<QuestionnaireType>> AvailableQuestionnaireTypes = Enum
        .GetValues<QuestionnaireType>()
        .Select(e => new EnumViewModel<QuestionnaireType>(e));

    public ObservableCollection<SelectedFileViewModel> SelectedFiles { get; } = [];

    [ObservableProperty]
    private EnumViewModel<QuestionnaireType> _selectedQuestionnaireType = AvailableQuestionnaireTypes.First();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private bool _isUploadingStarted;

    [ObservableProperty]
    private int _progress;
    
    [ObservableProperty]
    private int _maxProgress;
    
    private CancellationTokenSource? _filesParsingCts;
    
    private readonly object _filesParsingCtsLock = new();
    
    private bool _dialogClosed;

    [RelayCommand]
    private void SelectFiles()
    {
        const string title = "Выберите файлы:";
        const string filter = "Files|*.pdf;*.jpg;*.jpeg";

        var fileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = title,
            RestoreDirectory = true,
            Multiselect = true,
            Filter = filter,
        };

        fileDialog.ShowDialog();
        
        SelectedFiles.Clear();
        foreach (var file in fileDialog.FileNames)
        {
            SelectedFiles.Add(new SelectedFileViewModel
            {
                FilePath = file,
                IsProcessed = false,
                SelectedQuestionnaireType = SelectedQuestionnaireType,
            });
        }
    }

    [RelayCommand]
    private async Task Confirm(Window window) // TODO: Migrate to MVVM.
    {
        if (SelectedFiles.Count == 0 || ConfirmCommand.IsRunning)
        {
            return;
        }
        
        _filesParsingCts = new CancellationTokenSource();
        
        using var scope = App.Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IQuestionnaireParserFactory>();

        Progress = 0;
        MaxProgress = SelectedFiles.Count;
        IsUploadingStarted = true;

        var results = await Task.Run(() =>
        {
            try
            {
                return SelectedFiles
                    .AsParallel()
                    // https://dotnettutorials.net/lesson/maximum-degree-of-parallelism-in-csharp/#:~:text=So%2C%20in%20order%20to%20use,threads%20to%20execute%20the%20code.
                    .WithDegreeOfParallelism(Environment.ProcessorCount - 1)
                    .WithCancellation(_filesParsingCts.Token)
                    .Select(e =>
                    {
                        var parser = factory.CreateParser(e.FilePath);
                        var result = parser.ParseFromFile(e.FilePath, SelectedQuestionnaireType.Value);
                        
                        Application.Current?.Dispatcher.Invoke(() =>
                        {
                            lock (_filesParsingCtsLock)
                            {
                                if (!IsUploadingStarted)
                                {
                                    return;
                                }
                                
                                Progress = ++Progress;
                                e.IsProcessed = true;
                            }
                        });
                        
                        return result.ToViewModel();
                    })
                    .ToList();
            }
            catch (OperationCanceledException)
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    Progress = 0;
                    MaxProgress = 0;
                    IsUploadingStarted = false;
                    foreach (var item in SelectedFiles)
                    {
                        item.IsProcessed = false;
                    }
                });

                if (!_dialogClosed)
                {
                    MessageBox.Show("Загрузка файлов отменена.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                return null;
            }
            finally
            {
                _filesParsingCts.Dispose();
                _filesParsingCts = null;
            }
        });

        if (results is not null)
        {
            MessageBox.Show("Все файлы успешно загружены.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            Messenger.Send(new QuestionnairesReadMessage(results));
            window.Close();
        }
    }

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel()
    {
        IsUploadingStarted = false;
        _filesParsingCts?.Cancel();
    }

    private bool CanCancel() => IsUploadingStarted;

    partial void OnSelectedQuestionnaireTypeChanged(EnumViewModel<QuestionnaireType> value)
    {
        foreach (var item in SelectedFiles)
        {
            item.SelectedQuestionnaireType = value;
        }
    }

    [RelayCommand]
    private void WindowClosed()
    {
        _filesParsingCts?.Cancel();
        _dialogClosed = true;
    }
}