using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
            vm.StatusTb = StatusTb;
        }

        private void FilesGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            if (vm?.SelectedItem != null)
                vm.OpenCommand.Execute(null);
        }

        private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var vm = DataContext as MainViewModel;

            if (e.ChangedButton == MouseButton.XButton1)
            {
                if (vm.PrevDirCommand.CanExecute(null))
                {
                    vm.PrevDirCommand.Execute(null);
                    e.Handled = true;
                }
            }
            else if (e.ChangedButton == MouseButton.XButton2)
            {
                if (vm.NextDirCommand.CanExecute(null))
                {
                    vm.NextDirCommand.Execute(null);
                    e.Handled = true;
                }
            }
        }

        private void Vm_RenameRequested(object sender, EventArgs e)
        {
            if (FilesGrid.SelectedItem is null)
                return;

            FilesGrid.IsReadOnly = false;

            var col = FilesGrid.Columns[0];
            FilesGrid.CurrentCell = new DataGridCellInfo(FilesGrid.SelectedItem, col);

            if (FilesGrid.SelectedItem is FileItem item)
                _oldName = item.Name;

            FilesGrid.BeginEdit();
        }

        private void FilesGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            //MessageBox.Show(e.Key.ToString());
            if (e.Key == Key.Enter)
            {

                FilesGrid.CommitEdit(DataGridEditingUnit.Cell, true);

                //MessageBox.Show("Ветка Key.Enter");

                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                _renameConfirmed = false;


                FilesGrid.CancelEdit(DataGridEditingUnit.Cell);

                MessageBox.Show("Ветка Key.Escape");

                e.Handled = true;

                FilesGrid.IsReadOnly = true;
            }
        }

        private void FilesGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.Column.Header?.ToString() != "Name")
                return;

            var vm = DataContext as MainViewModel;
            var fileItem = e.Row.Item as FileItem;

            if (!_renameConfirmed)
            {
                e.Cancel = true;

                FilesGrid.IsReadOnly = true;
                return;
            }

            if (e.EditingElement is TextBox tb && fileItem != null)
            {
                string newName = tb.Text.Trim();

                if (string.IsNullOrWhiteSpace(newName) || newName == _oldName)
                {
                    e.Cancel = true;

                    FilesGrid.IsReadOnly = true;
                    return;
                }
                else
                {
                    vm.RenameSelected(newName);
                }
            }

            _renameConfirmed = false;

            FilesGrid.IsReadOnly = true;
        }

        private void CurrentPathTb_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var vm = DataContext as MainViewModel;
                vm.NavigateTo(CurrentPathTb.Text.Trim());
            }
        }
    }
}