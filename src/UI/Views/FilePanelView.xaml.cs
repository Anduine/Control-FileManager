using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using ControlFileManager.Core.Models;
using ControlFileManager.UI.ViewModels;

namespace ControlFileManager.UI.Views
{
  public partial class FilePanelView : UserControl
  {
    private FilePanelViewModel _vm;
    private FileItem? _itemBeingRenamed;
    public FilePanelView()
    {
      InitializeComponent();
    }

    public void StartRename(FileItem item)
    {
      if (item == null) return;
      _itemBeingRenamed = item;

      FilesGrid.ScrollIntoView(item);
      FilesGrid.UpdateLayout();

      DataGridCell? cell = FilesGrid.Columns[0].GetCellContent(item)?.Parent as DataGridCell;
      if (cell == null) return;

      var cellPos = cell.TranslatePoint(new Point(0, 0), this);

      RenameTb.Width = cell.ActualWidth;
      RenameTb.Height = cell.ActualHeight;
      RenameTb.Margin = new Thickness(cellPos.X, cellPos.Y, 0, 0);

      RenameTb.Text = item.Name;
      RenameTb.Visibility = Visibility.Visible;
      RenameTb.Focus();
      RenameTb.SelectAll();
    }

    private void FilePanelView_Loaded(object sender, RoutedEventArgs e)
    {
      _vm = DataContext as FilePanelViewModel;
      if (_vm != null)
      {
        _vm.RenameRequest += Vm_RenameRequest;
        _vm.CurrentItems.CollectionChanged += (_, _) =>
        {
          if (RenameTb.Visibility == Visibility.Visible)
            FinishRename(false);
        };
      }
    }

    private void FilePanel_MouseUp(object sender, MouseButtonEventArgs e)
    {
      switch (e.ChangedButton)
      {
        case MouseButton.XButton1:
          if (_vm.PrevDirCommand.CanExecute(null))
          {
            _vm.PrevDirCommand.Execute(null);
            e.Handled = true;
          }
          break;
        case MouseButton.XButton2:
          if (_vm.NextDirCommand.CanExecute(null))
          {
            _vm.NextDirCommand.Execute(null);
            e.Handled = true;
          }
          break;
      }
    }

    private void FilePanel_KeyUp(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.F2 && _vm?.SelectedItem != null)
      {
        StartRename(_vm.SelectedItem);
        e.Handled = true;
      }
    }

    private void CurrentPathTb_KeyUp(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Enter)
      {
        _vm.NavigateTo(CurrentPathTb.Text.Trim());
      }
    }

    private void FilesGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      if (_vm?.SelectedItem != null)
        _vm.OpenCommand.Execute(null);
    }

    private void FilesGrid_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
      if (RenameTb.Visibility == Visibility.Visible && e.VerticalChange != 0)
        FinishRename(false);
    }

    private void FilesGrid_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
      if (RenameTb.Visibility == Visibility.Visible)
        FinishRename(false);
    }

    private void RenameTb_KeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Enter)
      {
        FinishRename(true);
      }
      else if (e.Key == Key.Escape)
      {
        FinishRename(false);
      }
    }

    private void RenameTb_LostFocus(object sender, RoutedEventArgs e)
    {
      FinishRename(false);
    }

    private void FinishRename(bool confirm)
    {
      RenameTb.Visibility = Visibility.Collapsed;

      if (!confirm || _itemBeingRenamed == null)
        return;

      string newName = RenameTb.Text.Trim();
      if (string.IsNullOrWhiteSpace(newName) || newName == _itemBeingRenamed.Name)
        return;

      _vm.RenameSelected(newName);
    }

    private void Vm_RenameRequest(object? sender, FileItem item)
    {
      StartRename(item);
    }
  }
}
