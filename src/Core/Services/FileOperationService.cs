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
        MessageBox.Show("Не вдалося відкрити: " + ex.Message);
      }
    }

    public async void DeleteAsync(FilePanelViewModel panel)
    {
      var item = panel.SelectedItem;
      if (item == null) return;

      var ok = MessageBox.Show($"Видалити {item.Name}?", "Підтвердження", MessageBoxButton.YesNo) == MessageBoxResult.Yes;
      if (!ok) return;

      try
      {
        await _fs.DeleteAsync(item.FullPath);
        await panel.LoadDirectoryAsync(panel.CurrentPath);
      }
      catch (Exception ex)
      {
        MessageBox.Show("Помилка видалення: " + ex.Message);
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
        MessageBox.Show("Помилка створення папки: " + ex.Message);
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
        MessageBox.Show("Помилка вставки: " + ex.Message);
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
        // Поиск отменен
      }
      catch (Exception ex)
      {
        MessageBox.Show("Помилка пошуку: " + ex.Message);
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
      // Application.Current.Dispatcher.Invoke заставляет код выполняться в UI-потоке
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
