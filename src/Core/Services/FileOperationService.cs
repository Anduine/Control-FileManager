using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

using ControlFileManager.UI.ViewModels;

namespace ControlFileManager.Core.Services
{
    public class FileOperationService
    {
        private readonly IFileSystemService _fs;

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
                panel.LoadDirectoryAsync(panel.CurrentPath);
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
                panel.LoadDirectoryAsync(panel.CurrentPath);
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
                panel.LoadDirectoryAsync(panel.CurrentPath);
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
    }
}
