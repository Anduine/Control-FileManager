using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ControlFileManager.Core.Models;
using ControlFileManager.UI.ViewModels;

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
      var item = panel.SelectedItem;
      if (item == null) return;

      var ok = MessageBox.Show($"Видалити \"{item.Name}\"?", "Підтвердження", MessageBoxButton.YesNo) == MessageBoxResult.Yes;
      if (!ok) return;

      try
      {
        await _fs.DeleteAsync(item.FullPath);
        await panel.LoadDirectoryAsync(panel.CurrentPath);
      }
      catch (Exception ex)
      {
        MessageBox.Show("При видаленні виникла помилка:\n " + ex.Message, "Помилка видалення");
      }
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
      if (panel.SelectedItem == null) return;

      Clipboard.SetData("FileDrop", new string[] { panel.SelectedItem.FullPath });
    }

    public void Cut(FilePanelViewModel panel)
    {
      if (panel.SelectedItem == null) return;

      Clipboard.SetData("FileDrop", new string[] { panel.SelectedItem.FullPath });
    }

    public async Task PasteAsync(FilePanelViewModel panel)
    {
      if (panel.SelectedRoot == null) return;
      if (!Clipboard.ContainsData("FileDrop")) return;

      var files = Clipboard.GetData("FileDrop") as string[];
      if (files == null || files.Length == 0) return;

      string src = files[0];
      string dst = Path.Combine(panel.CurrentPath, Path.GetFileName(src));

      try
      {
        await _fs.CopyAsync(src, dst);
        await panel.LoadDirectoryAsync(panel.CurrentPath);
      }
      catch (Exception ex)
      {
        MessageBox.Show("При спробі вставки виникла помилка:\n " + ex.Message, "Помилка вставки");
      }
    }

    public bool ClipboardContainsPath()
    {
      return Clipboard.ContainsData("FileDrop");
    }

    public void ShowPropertiesWindow(FilePanelViewModel panel)
    {
      if (panel.SelectedItem == null) return;

      var psi = new ProcessStartInfo("explorer.exe")
      {
        Arguments = $"/select,\"{panel.SelectedItem.FullPath}\"",
        UseShellExecute = true
      };
      Process.Start(psi);
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
  }
}
