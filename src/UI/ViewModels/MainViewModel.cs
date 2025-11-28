using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

using ControlFileManager.Core.Models;
using ControlFileManager.Core.Services;
using ControlFileManager.UI.Commands;

namespace ControlFileManager.UI.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly IFileSystemService _fs;
        private CancellationTokenSource? _cts;

        private string _lastNameBeforeRename;

        public ObservableCollection<FileItem> NavigationRoots { get; } = new();
        public ObservableCollection<FileItem> CurrentItems { get; } = new();

        private FileItem? _selectedRoot;
        public FileItem? SelectedRoot
        {
            get => _selectedRoot;
            set { 
                _selectedRoot = value; 
                Raise(); 
                if (value != null) LoadDirectory(value.FullPath);

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

                DeleteCommand.RaiseCanExecuteChanged();
                OpenCommand.RaiseCanExecuteChanged();
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

            PrevDirCommand = new RelayCommand(_ => CreateFolder(), _ => SelectedRoot != null);
            NextDirCommand = new RelayCommand(_ => CreateFolder(), _ => SelectedRoot != null);

            // initial load
            _ = RefreshRoots();
        }

        public async Task RefreshRoots()
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

        public async Task LoadDirectory(string path)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            try
            {
                CurrentItems.Clear();
                var items = await _fs.GetDirectoryItemsAsync(path, _cts.Token);
                foreach (var it in items) CurrentItems.Add(it);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка читання папки: " + ex.Message);
            }
        }

        private void OpenSelected()
        {
            if (SelectedItem == null) return;

            try
            {
                if (SelectedItem.IsDirectory)
                {
                    SelectedRoot = SelectedItem;
                    LoadDirectory(SelectedRoot.FullPath);
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
                if (SelectedRoot != null) await LoadDirectory(SelectedRoot.FullPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка видалення: " + ex.Message);
            }
        }

        public void BeginRename()
        {
            if (SelectedItem != null)
            {
                _lastNameBeforeRename = SelectedItem.Name;
                RenameRequested?.Invoke(this, EventArgs.Empty);
            }
        }

        public string getLastName()
        {
            if (!string.IsNullOrEmpty(_lastNameBeforeRename))
            {
                _lastNameBeforeRename = string.Empty;
                return _lastNameBeforeRename;
            }

            return string.Empty;
        }

        public async Task RenameSelected(string newName)
        {
            if (SelectedItem is null || string.IsNullOrWhiteSpace(newName))
                return;

            try
            {
                var updated = await _fs.RenameAsync(SelectedItem.FullPath, newName);

                // заменить элемент в коллекции
                int index = CurrentItems.IndexOf(SelectedItem);
                if (index >= 0)
                    CurrentItems[index] = updated;

                SelectedItem = updated;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не вдалося перейменувати: {ex.Message}");
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
                string dst = Path.Combine(SelectedRoot.FullPath, Path.GetFileName(src));

                await _fs.CopyAsync(src, dst);
                await LoadDirectory(SelectedRoot.FullPath);
            }
        }

        private async void CreateFolder()
        {
            if (SelectedRoot == null) return;
            var name = "Нова папка";
            try
            {
                await _fs.CreateDirectoryAsync(SelectedRoot.FullPath, name);
                await LoadDirectory(SelectedRoot.FullPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка створення папки: " + ex.Message);
            }
        }

        private void ShowProperties()
        {
            if (SelectedItem == null) return;

            var psi = new ProcessStartInfo("explorer.exe");
            psi.Arguments = $"/select,\"{SelectedItem.FullPath}\"";
            psi.UseShellExecute = true;
            Process.Start(psi);
        }

        private void PrevDir()
        {
            if (SelectedRoot == null) return;
            
        }
    }
}
