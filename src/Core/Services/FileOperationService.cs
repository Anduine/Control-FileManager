using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ControlFileManager.Core.Models;
using ControlFileManager.UI.ViewModels;
using ControlFileManager.UI.Views;

namespace ControlFileManager.Core.Services
{
  public class FileOperationService
  {
    private readonly IFileSystemService _fs;

    private CancellationTokenSource? _searchCts;

    public bool IsSearching { get; set; }

    public FileOperationService(IFileSystemService fs)
    {
      _fs = fs;
    }

    public void Open(FilePanelViewModel panel)
    {
      var item = panel.SelectedItem;
      if (item == null) return;

      try
      {
        if (item.IsDirectory)
        {
          panel.NavigateTo(item.FullPath);
        }
        else
        {
          Process.Start(new ProcessStartInfo
          {
            FileName = item.FullPath,
            UseShellExecute = true
          });
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show("При відкритті виникла помилка: " + ex.Message, "Помилка відкриття");
      }
    }

    public async void DeleteAsync(FilePanelViewModel panel)
    {
      var items = panel.SelectedItems.ToList();
      if (items.Count == 0) return;

      if (items.Count == 1)
      {
        var ok = MessageBox.Show($"Видалити \"{items[0].Name}\"?", "Підтвердження", MessageBoxButton.YesNo) == MessageBoxResult.Yes;
        if (!ok) return;
      }
      else if (items.Count > 1)
      {
        var ok = MessageBox.Show($"Видалити {items.Count} елементів?", "Підтвердження", MessageBoxButton.YesNo) == MessageBoxResult.Yes;
        if (!ok) return;
      }

      try
      {
        foreach (var item in items)
        {
          await _fs.DeleteAsync(item.FullPath);
        }
        await panel.LoadDirectoryAsync(panel.CurrentPath);
      }
      catch (Exception ex)
      {
        MessageBox.Show("При видаленні виникла помилка:\n " + ex.Message, "Помилка видалення");
      }
      return;
    }

    public async Task RenameAsync(FilePanelViewModel panel, string newName)
    {
      var item = panel.SelectedItem;
      if (item == null || string.IsNullOrWhiteSpace(newName)) return;

      var updated = await _fs.RenameAsync(item.FullPath, newName);

      int index = panel.CurrentItems.IndexOf(item);
      if (index >= 0)
        panel.CurrentItems[index] = updated;

      panel.SelectedItem = updated;
    }

    public async Task CreateFolderAsync(FilePanelViewModel panel)
    {
      if (panel.SelectedRoot == null) return;

      const string name = "Нова папка";

      try
      {
        await _fs.CreateDirectoryAsync(panel.CurrentPath, name);
        await panel.LoadDirectoryAsync(panel.CurrentPath);
      }
      catch (Exception ex)
      {
        MessageBox.Show("При створенні директорії виникла помилка:\n " + ex.Message, "Помилка створення папки");
      }
    }

    public async Task CreateFileAsync(FilePanelViewModel panel)
    {
      if (panel.SelectedRoot == null) return;

      string baseName = "Новий файл";
      string extension = ".txt";
      string finalName = baseName + extension;

      try
      {
        await _fs.CreateFileAsync(panel.CurrentPath, finalName);
        await panel.LoadDirectoryAsync(panel.CurrentPath);
      }
      catch (Exception ex)
      {
        MessageBox.Show("При створенні файлу виникла помилка:\n " + ex.Message, "Помилка створення файлу");
      }
    }

    public void Copy(FilePanelViewModel panel)
    {
      SetClipboardData(panel, isCut: false);
    }

    public void Cut(FilePanelViewModel panel)
    {
      SetClipboardData(panel, isCut: true);
    }

    public async Task PasteAsync(FilePanelViewModel panel)
    {
      if (panel.SelectedRoot == null || string.IsNullOrEmpty(panel.CurrentPath)) return;
      if (!Clipboard.ContainsFileDropList()) return;

      var files = Clipboard.GetFileDropList();
      if (files == null || files.Count == 0) return;

      bool isMoveOperation = false;

      var data = Clipboard.GetDataObject();
      if (data != null && data.GetDataPresent("Preferred DropEffect"))
      {
        using (var stream = data.GetData("Preferred DropEffect") as MemoryStream)
        {
          if (stream != null)
          {
            byte[] bytes = stream.ToArray();
            // Якщо перший байт = 2, це Move
            if (bytes.Length > 0 && bytes[0] == 2)
            {
              isMoveOperation = true;
            }
          }
        }
      }

      try
      {
        foreach (string srcPath in files)
        {
          string fileName = Path.GetFileName(srcPath);
          string destPath = Path.Combine(panel.CurrentPath, fileName);

          // Захист від копіювання "сам у себе"
          if (string.Equals(srcPath, destPath, StringComparison.OrdinalIgnoreCase))
          {
            continue;
          }

          if (isMoveOperation)
          {
            await _fs.MoveAsync(srcPath, destPath);
          }
          else
          {
            await _fs.CopyAsync(srcPath, destPath);
          }
        }

        if (isMoveOperation)
        {
          Clipboard.Clear();
        }

        await panel.LoadDirectoryAsync(panel.CurrentPath);
      }
      catch (Exception ex)
      {
        MessageBox.Show("При вставці виникла помилка:\n " + ex.Message, "Помилка вставки");
      }
    }

    public bool ClipboardContainsPath()
    {
      return Clipboard.ContainsFileDropList();
    }

    public void ShowPropertiesWindow(FilePanelViewModel panel)
    {
      if (panel.SelectedItem == null) return;

      var propWindow = new PropertiesWindow(panel.SelectedItem)
      {
        Owner = Application.Current.MainWindow
      };

      propWindow.ShowDialog();

      if (panel.RefreshDirCommand.CanExecute(null))
        panel.RefreshDirCommand.Execute(null);
    }

    public async Task StartSearch(FilePanelViewModel panel, SearchOptions options)
    {
      if (IsSearching) return;

      IsSearching = true;
      _searchCts = new CancellationTokenSource();
      var token = _searchCts.Token;

      try
      {
        panel.CurrentItems.Clear();

        await Task.Run(async () =>
        {
          var foundFiles = new List<FileItem>();

          await foreach (var item in _fs.SearchAsync(options, token))
          {
            foundFiles.Add(item);

            if (foundFiles.Count >= 10)
            {
              UpdateUi(panel, foundFiles);
              foundFiles = new List<FileItem>();
            }
          }

          if (foundFiles.Count > 0)
          {
            UpdateUi(panel, foundFiles);
          }

        }, token);
      }
      catch (OperationCanceledException)
      {
        // Пошук скасовано
      }
      catch (Exception ex)
      {
        MessageBox.Show("При виконанні пошуку виникла помилка:\n " + ex.Message, "Помилка пошуку");
      }
      finally
      {
        IsSearching = false;
        _searchCts?.Dispose();
        _searchCts = null;
      }
    }

    public void CancelSearch()
    {
      _searchCts?.Cancel();
    }

    private void UpdateUi(FilePanelViewModel panel, List<FileItem> itemsToAdd)
    {
      // Application.Current.Dispatcher.Invoke - код виконується в UI-потоці
      Application.Current.Dispatcher.Invoke(() =>
      {
        foreach (var item in itemsToAdd)
        {
          panel.CurrentItems.Add(item);
        }
      });
    }

    private void SetClipboardData(FilePanelViewModel panel, bool isCut)
    {
      var items = panel.SelectedItems.Any()
          ? panel.SelectedItems.ToList() : new List<FileItem>();

      if (items.Count == 0) return;

      var paths = new System.Collections.Specialized.StringCollection();
      foreach (var item in items)
      {
        paths.Add(item.FullPath);
      }

      var data = new DataObject();

      data.SetFileDropList(paths);

      // 2 = Move (Cut), 5 = Copy (1) | Link (4)
      byte[] moveEffect = new byte[] { 2, 0, 0, 0 };
      byte[] copyEffect = new byte[] { 5, 0, 0, 0 };

      MemoryStream dropEffect = new MemoryStream(isCut ? moveEffect : copyEffect);
      data.SetData("Preferred DropEffect", dropEffect);

      Clipboard.SetDataObject(data, true);
    }
  }
}
