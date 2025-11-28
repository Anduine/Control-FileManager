using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using ControlFileManager.Core.Models;
using ControlFileManager.Core.Services;
using ControlFileManager.UI.ViewModels;

namespace ControlFileManager.Main
{
    public partial class MainWindow : Window
    {
        private string _oldName = "";
        private bool _renameConfirmed;

        public MainWindow(MainViewModel vm, IFileSystemService fileService)
        {
            InitializeComponent();
            DataContext = vm;
            vm.RenameRequested += Vm_RenameRequested;
        }

        private void FilesGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            if (vm?.SelectedItem != null)
                vm.OpenCommand.Execute(null);
        }

        private void Vm_RenameRequested(object sender, EventArgs e)
        {
            if (FilesGrid.SelectedItem is null)
                return;

            FilesGrid.IsReadOnly = false;

            var col = FilesGrid.Columns[0];
            FilesGrid.CurrentCell = new DataGridCellInfo(FilesGrid.SelectedItem, col);

            // запоминаем имя, НЕ изменяем FileItem
            if (FilesGrid.SelectedItem is FileItem item)
                _oldName = item.Name;

            FilesGrid.BeginEdit();
        }

        private void FilesGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Подтверждаем переименование
                _renameConfirmed = true;

                // Завершаем редактирование ячейки и строки
                if (FilesGrid.CommitEdit(DataGridEditingUnit.Cell, true))
                    MessageBox.Show("CommitEdit отработало");

                // Принудительно выключаем редактирование
                FilesGrid.IsReadOnly = true;

            }
            else if (e.Key == Key.Escape)
            {
                // Отменяем редактирование
                _renameConfirmed = false;

                // Отмена редактирования ячейки и строки
                if (FilesGrid.CancelEdit())
                    MessageBox.Show("CancelEdit отработало");
                

                // Принудительно выключаем редактирование
                FilesGrid.IsReadOnly = true;
            }
        }

        private async void FilesGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.Column.Header?.ToString() != "Name")
                return;

            // отмена
            if (!_renameConfirmed)
            {
                e.Cancel = true;
                FilesGrid.IsReadOnly = true;
                return;
            }

            if (e.EditingElement is TextBox tb &&
                e.Row.Item is FileItem)
            {
                string newName = tb.Text.Trim();

                if (string.IsNullOrWhiteSpace(newName))
                {
                    e.Cancel = true;
                    return;
                }

                // только здесь – VM создаёт новый FileItem
                await ((MainViewModel)DataContext).RenameSelected(newName);
            }

            FilesGrid.IsReadOnly = true;
        }
    }
}