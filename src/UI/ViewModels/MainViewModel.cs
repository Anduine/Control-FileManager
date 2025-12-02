using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

using ControlFileManager.Core.Models;
using ControlFileManager.Core.Services;
using ControlFileManager.UI.Commands;

namespace ControlFileManager.UI.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly IFileSystemService _fs;
        private CancellationTokenSource? _cts;

        internal TextBlock StatusTb;

        public ObservableCollection<FileItem> NavigationRoots { get; } = new();
        public ObservableCollection<FileItem> CurrentItems { get; } = new();

        private bool _showHidden;
        public bool ShowHidden
        {
            get => _showHidden;
            set { _showHidden = value; Raise(); LoadDirectory(CurrentPath); }
        }

        private bool _showSystem;
        public bool ShowSystem
        {
            get => _showSystem;
            set { _showSystem = value; Raise(); LoadDirectory(CurrentPath); }
        }

        private FileItem? _selectedRoot;
        public FileItem? SelectedRoot
        {
            get => _selectedRoot;
            set { 
                _selectedRoot = value; 
                Raise(); 
                if (value != null)
                    NavigateTo(value.FullPath);

                CreateFolderCommand.RaiseCanExecuteChanged();
            }
        }

        private FileItem? _selectedItem;
        public FileItem? SelectedItem
        {
            get => _selectedItem;
            set { 
                _selectedItem = value; 
                Raise();

                StatusTb.Text = _selectedItem?.CreatedTime.ToString();

                DeleteCommand.RaiseCanExecuteChanged();
                OpenCommand.RaiseCanExecuteChanged();
            }
        }

        private readonly Stack<string> _backHistory = new();
        private readonly Stack<string> _forwardHistory = new();

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

        public RelayCommand RefreshCommand { get; }
        public RelayCommand OpenCommand { get; }
        public RelayCommand DeleteCommand { get; }
        public RelayCommand RenameCommand { get; }
        public RelayCommand CopyCommand { get; }
        public RelayCommand CutCommand { get; }
        public RelayCommand PasteCommand { get; }
        public RelayCommand CreateFolderCommand { get; }
        public RelayCommand ShowPropertiesCommand { get; }
        public RelayCommand PrevDirCommand { get; }
        public RelayCommand NextDirCommand { get; }
        public EventHandler RenameRequested { get; internal set; }

        public MainViewModel(IFileSystemService fs)
        {
            _fs = fs;

            RefreshCommand = new RelayCommand(_ => RefreshRoots());
            OpenCommand = new RelayCommand(_ => OpenSelected(), _ => SelectedItem != null);
            DeleteCommand = new RelayCommand(_ => DeleteSelected(), _ => SelectedItem != null);
            RenameCommand = new RelayCommand(_ => BeginRename(), _ => SelectedItem != null);

            CopyCommand = new RelayCommand(_ => CopySelected(), _ => SelectedItem != null);
            CutCommand = new RelayCommand(_ => CutSelected(), _ => SelectedItem != null);
            PasteCommand = new RelayCommand(_ => PasteIntoCurrent(), _ => ClipboardContainsPath());

            CreateFolderCommand = new RelayCommand(_ => CreateFolder(), _ => SelectedRoot != null);
            ShowPropertiesCommand = new RelayCommand(_ => ShowProperties(), _ => SelectedItem != null);

            PrevDirCommand = new RelayCommand(_ => PrevDir(), _ => _backHistory.Count > 0);
            NextDirCommand = new RelayCommand(_ => NextDir(), _ => _forwardHistory.Count > 0);

            RefreshRoots();
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

            LoadDirectory(_currentPath);

            PrevDirCommand.RaiseCanExecuteChanged();
            NextDirCommand.RaiseCanExecuteChanged();
        }

        public async void RenameSelected(string newName)
        {
            if (SelectedItem is null || string.IsNullOrWhiteSpace(newName))
                return;

            try
            {
                var updated = await _fs.RenameAsync(SelectedItem.FullPath, newName);

                int index = CurrentItems.IndexOf(SelectedItem);
                if (index >= 0)
                    CurrentItems[index] = updated;

                SelectedItem = updated;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не вдалося перейменувати:" + ex.Message);
            }
        }

        private async void RefreshRoots()
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            try
            {
                NavigationRoots.Clear();
                var drives = await _fs.GetDrivesAsync(_cts.Token);
                foreach (var d in drives)
                    NavigationRoots.Add(d);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не вдалося отримати перелік дисків: " + ex.Message);
            }
        }

        private async void LoadDirectory(string path)
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

        private void OpenSelected()
        {
            if (SelectedItem == null) return;

            try
            {
                if (SelectedItem.IsDirectory)
                {
                    NavigateTo(SelectedItem.FullPath);
                }
                else
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = SelectedItem.FullPath,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не вдалося відкрити: " + ex.Message);
            }
        }

        private async void DeleteSelected()
        {
            if (SelectedItem == null) return;
            try
            {
                var ok = MessageBox.Show($"Видалити {SelectedItem.Name}?", "Підтвердження", MessageBoxButton.YesNo) == MessageBoxResult.Yes;
                if (!ok) return;
                await _fs.DeleteAsync(SelectedItem.FullPath);

                LoadDirectory(CurrentPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка видалення: " + ex.Message);
            }
        }

        private void BeginRename()
        {
            if (SelectedItem != null)
            {
                RenameRequested?.Invoke(this, EventArgs.Empty);
            }
        }

        private bool ClipboardContainsPath()
        {
            return Clipboard.ContainsData("FileDrop");
        }

        private void CopySelected()
        {
            if (SelectedItem == null) return;
            Clipboard.SetData("FileDrop", new string[] { SelectedItem.FullPath });
        }

        private void CutSelected()
        {
            if (SelectedItem == null) return;

            Clipboard.SetData("FileDrop", new string[] { SelectedItem.FullPath });
        }

        private async void PasteIntoCurrent()
        {
            if (SelectedRoot == null) return;

            if (Clipboard.GetData("FileDrop") is string[] files
                && files.Length > 0)
            {
                string src = files[0];
                string dst = Path.Combine(CurrentPath, Path.GetFileName(src));

                await _fs.CopyAsync(src, dst);
                LoadDirectory(CurrentPath);
            }
        }

        private async void CreateFolder()
        {
            if (SelectedRoot == null) return;
            const string name = "Нова папка";

            try
            {
                await _fs.CreateDirectoryAsync(CurrentPath, name);
                LoadDirectory(CurrentPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка створення папки: " + ex.Message);
            }
        }

        private void ShowProperties()
        {
            if (SelectedItem == null) return;

            var psi = new ProcessStartInfo("explorer.exe")
            {
                Arguments = $"/select,\"{SelectedItem.FullPath}\"",
                UseShellExecute = true
            };
            Process.Start(psi);
        }

        private void PrevDir()
        {
            if (_backHistory.Count == 0)
                return;

            _forwardHistory.Push(CurrentPath);
            CurrentPath = _backHistory.Pop();
            LoadDirectory(CurrentPath);

            PrevDirCommand.RaiseCanExecuteChanged();
            NextDirCommand.RaiseCanExecuteChanged();
        }

        private void NextDir()
        {
            if (_forwardHistory.Count == 0)
                return;

            _backHistory.Push(CurrentPath);
            CurrentPath = _forwardHistory.Pop();
            LoadDirectory(CurrentPath);

            PrevDirCommand.RaiseCanExecuteChanged();
            NextDirCommand.RaiseCanExecuteChanged();
        }
    }
}
