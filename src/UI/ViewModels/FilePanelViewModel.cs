using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

using ControlFileManager.Core.Models;
using ControlFileManager.Core.Services;
using ControlFileManager.UI.Commands;

namespace ControlFileManager.UI.ViewModels
{
  public class FilePanelViewModel : BaseViewModel
  {
    private readonly IFileSystemService _fs;
    private readonly FileOperationService _ops;

    private CancellationTokenSource? _cts;

    public ObservableCollection<FileItem> NavigationRoots { get; } = new();
    public ObservableCollection<FileItem> CurrentItems { get; } = new();

    private readonly Stack<string> _backHistory = new();
    private readonly Stack<string> _forwardHistory = new();

    private bool _showHidden;
    public bool ShowHidden
    {
      get => _showHidden;
      set
      {
        _showHidden = value;
        Raise();
        if (CurrentPath != null)
          LoadDirectoryAsync(CurrentPath);
      }
    }

    private bool _showSystem;
    public bool ShowSystem
    {
      get => _showSystem;
      set
      {
        _showSystem = value;
        Raise();
        if (CurrentPath != null)
          LoadDirectoryAsync(CurrentPath);
      }
    }

    private FileItem? _selectedRoot;
    public FileItem? SelectedRoot
    {
      get => _selectedRoot;
      set
      {
        _selectedRoot = value;
        Raise();
        if (value != null)
          NavigateTo(value.FullPath);

        CreateFolderCommand.RaiseCanExecuteChanged();
        RefreshDirCommand.RaiseCanExecuteChanged();
      }
    }

    private FileItem? _selectedItem;
    public FileItem? SelectedItem
    {
      get => _selectedItem;
      set
      {
        _selectedItem = value;
        Raise();

        DeleteCommand.RaiseCanExecuteChanged();
        OpenCommand.RaiseCanExecuteChanged();
      }
    }

    private string _currentPath;
    public string CurrentPath
    {
      get => _currentPath;
      set
      {
        _currentPath = value;
        Raise();
      }
    }

    private bool _isActive;
    public bool IsActive
    {
      get => _isActive;
      set
      {
        _isActive = value;
        Raise();
      }
    }

    public event EventHandler<FileItem>? RenameRequest;
    public event Action<FilePanelViewModel> ActivationRequested;

    public RelayCommand RefreshDirCommand { get; }
    public RelayCommand OpenCommand { get; }
    public RelayCommand DeleteCommand { get; }
    public RelayCommand CreateFolderCommand { get; }
    public RelayCommand ShowPropertiesCommand { get; }
    public RelayCommand RenameCommand { get; }
    public RelayCommand CopyCommand { get; }
    public RelayCommand CutCommand { get; }
    public RelayCommand PasteCommand { get; }
    public RelayCommand PrevDirCommand { get; }
    public RelayCommand NextDirCommand { get; }
    public FilePanelViewModel(IFileSystemService fs, FileOperationService ops)
    {
      _fs = fs;
      _ops = ops;

      RefreshDirCommand = new RelayCommand(_ => LoadDirectoryAsync(CurrentPath), _ => SelectedRoot != null);

      OpenCommand = new RelayCommand(_ => _ops.Open(this), _ => SelectedItem != null);
      DeleteCommand = new RelayCommand(_ => _ops.DeleteAsync(this), _ => SelectedItem != null);

      CreateFolderCommand = new RelayCommand(_ => _ops.CreateFolderAsync(this), _ => SelectedRoot != null);
      ShowPropertiesCommand = new RelayCommand(_ => _ops.ShowPropertiesWindow(this), _ => SelectedItem != null);
      RenameCommand = new RelayCommand(_ => RequestRename(), _ => SelectedItem != null);

      CopyCommand = new RelayCommand(_ => _ops.Copy(this), _ => SelectedItem != null);
      CutCommand = new RelayCommand(_ => _ops.Cut(this), _ => SelectedItem != null);
      PasteCommand = new RelayCommand(_ => _ops.PasteAsync(this), _ => _ops.ClipboardContainsPath());

      PrevDirCommand = new RelayCommand(_ => PrevDir(), _ => _backHistory.Count > 0);
      NextDirCommand = new RelayCommand(_ => NextDir(), _ => _forwardHistory.Count > 0);


      _ = RefreshRootsAsync();
    }

    public void NavigateTo(string path)
    {
      if (!Directory.Exists(path))
      {
        MessageBox.Show($"Каталог {path} не існує");
        return;
      }

      if (!string.IsNullOrEmpty(CurrentPath))
        _backHistory.Push(CurrentPath);

      _forwardHistory.Clear();

      CurrentPath = path;

      LoadDirectoryAsync(_currentPath);

      PrevDirCommand.RaiseCanExecuteChanged();
      NextDirCommand.RaiseCanExecuteChanged();
    }

    public async Task LoadDirectoryAsync(string path)
    {
      _cts?.Cancel();
      _cts = new CancellationTokenSource();

      IEnumerable<FileItem> newItems;

      try
      {
        newItems = await _fs.GetDirectoryItemsAsync(path, ShowHidden, ShowSystem, _cts.Token);
      }
      catch (Exception ex)
      {
        MessageBox.Show("Помилка читання папки: " + ex.Message);
        return;
      }

      CurrentItems.Clear();
      foreach (var item in newItems)
      {
        CurrentItems.Add(item);
      }
    }

    public void RequestRename()
    {
      if (SelectedItem != null)
        RenameRequest?.Invoke(this, SelectedItem);
    }

    public async void RenameSelected(string newName)
    {
      await _ops.RenameAsync(this, newName);
    }

    public void RequestActivation()
    {
      ActivationRequested?.Invoke(this);
    }

    private async Task RefreshRootsAsync()
    {
      _cts?.Cancel();
      _cts = new CancellationTokenSource();
      try
      {
        NavigationRoots.Clear();
        var drives = await _fs.GetDrivesAsync(_cts.Token);
        foreach (var d in drives)
          NavigationRoots.Add(d);

        if (SelectedRoot == null && NavigationRoots.Count > 0)
        {
          var defaultRoot = NavigationRoots.First();

          SelectedRoot = defaultRoot;
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show("Не вдалося отримати перелік дисків: " + ex.Message);
      }
    }

    private void PrevDir()
    {
      if (_backHistory.Count == 0) return;

      _forwardHistory.Push(CurrentPath);
      CurrentPath = _backHistory.Pop();
      _ = LoadDirectoryAsync(CurrentPath);

      PrevDirCommand.RaiseCanExecuteChanged();
      NextDirCommand.RaiseCanExecuteChanged();
    }

    private void NextDir()
    {
      if (_forwardHistory.Count == 0) return;

      _backHistory.Push(CurrentPath);
      CurrentPath = _forwardHistory.Pop();
      _ = LoadDirectoryAsync(CurrentPath);

      PrevDirCommand.RaiseCanExecuteChanged();
      NextDirCommand.RaiseCanExecuteChanged();
    }
  }
}
