using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.IO;
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
    
    private readonly object _filesParsingCtsLock = new();

    public ObservableCollection<SelectedFileViewModel> SelectedFiles { get; } = [];

    [ObservableProperty]
    private EnumViewModel<QuestionnaireType> _selectedQuestionnaireType = AvailableQuestionnaireTypes.First();

    [ObservableProperty] 
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    [NotifyPropertyChangedFor(nameof(IsEditable))]
    private bool _isUploadingRunning;
    
    public bool IsEditable => !IsUploadingRunning;

    [ObservableProperty] 
    private int _progress;

    [ObservableProperty] 
    private int _maxProgress;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private bool _isCancellingRunning;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private bool _isCancellingEnabled;

    private CancellationTokenSource? _filesParsingCts;

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
        if (SelectedFiles.Count == 0 ||
            ConfirmCommand.IsRunning ||
            IsUploadingRunning)
        {
            return;
        }

        Progress = 0;
        MaxProgress = SelectedFiles.Count;
        IsUploadingRunning = true;
        IsCancellingEnabled = true;
        
        var factory = App.Services.GetRequiredService<IQuestionnaireParserFactory>();
        
        _filesParsingCts = new CancellationTokenSource();
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount - 1, 
            CancellationToken = _filesParsingCts.Token
        };

        var chunkSize = SelectedFiles.Count / 2;
        var chunks = SelectedFiles
            .OrderBy(_ => Guid.NewGuid())
            .Chunk(chunkSize)
            .ToList();
        
        var results = new ConcurrentBag<QuestionnaireViewModel>();
        try
        {
            await Task.Run(async () =>
            {
                var chunkFiles = new List<(SelectedFileViewModel selectedFile, byte[] fileBytes)>();

                for (var i = 0; i < chunks.Count; i++)
                {
                    parallelOptions.CancellationToken.ThrowIfCancellationRequested();
                    
                    var isNotLastChunk = i != chunks.Count - 1;
                    Application.Current?.Dispatcher.Invoke(() => IsCancellingEnabled = isNotLastChunk);
                    
                    chunkFiles.Clear();
                    foreach (var chunkItem in chunks[i])
                    {
                        var bytes = await File.ReadAllBytesAsync(chunkItem.FilePath, parallelOptions.CancellationToken);
                        chunkFiles.Add((chunkItem, bytes));
                    }

                    await Parallel.ForEachAsync(chunkFiles, parallelOptions, (item, _) =>
                    {
                        var parser = factory.CreateParser(item.selectedFile.FilePath);
                        var result = parser.ParseFromFile(item.fileBytes, SelectedQuestionnaireType.Value);

                        Application.Current?.Dispatcher.Invoke(() =>
                        {
                            lock (_filesParsingCtsLock)
                            {
                                if (!IsUploadingRunning)
                                {
                                    return;
                                }

                                Progress = ++Progress;
                                item.selectedFile.IsProcessed = true;
                            }
                        });

                        results.Add(result.ToViewModel(item.selectedFile.FilePath));
                        return ValueTask.CompletedTask;
                    });
                }
            }, parallelOptions.CancellationToken);
            
            MessageBox.Show("Все файлы успешно загружены.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            Messenger.Send(new QuestionnairesReadMessage(results));
        
            window.Close();
        }
        catch (OperationCanceledException)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                Progress = 0;
                MaxProgress = 0;
                foreach (var item in SelectedFiles)
                {
                    item.IsProcessed = false;
                }
                
                IsCancellingRunning = false;
                IsUploadingRunning = false;
            });
            
            if (!_dialogClosed)
            {
                MessageBox.Show("Загрузка файлов отменена.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        finally
        {
            _filesParsingCts.Dispose();
            _filesParsingCts = null;
        }
    }

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel()
    {
        IsCancellingEnabled = false;
        IsCancellingRunning = true;
        _filesParsingCts?.Cancel();
    }

    private bool CanCancel() => IsUploadingRunning && 
                                !IsCancellingRunning && 
                                IsCancellingEnabled;

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
        IsCancellingEnabled = false;
        IsCancellingRunning = true;
        _filesParsingCts?.Cancel();
        _dialogClosed = true;
    }
}