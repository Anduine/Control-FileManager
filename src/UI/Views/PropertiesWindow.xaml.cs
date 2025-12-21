using System;
using System.Windows;

using ControlFileManager.Core.Models;
using ControlFileManager.UI.ViewModels;

namespace ControlFileManager.UI.Views
{
  public partial class PropertiesWindow : Window
  {
    private PropertiesViewModel _vm;

    public PropertiesWindow(FileItem fileItem)
    {
      InitializeComponent();

      _vm = new PropertiesViewModel(fileItem);

      this.DataContext = _vm;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        _vm.ApplyChanges();
        this.DialogResult = true;
      }
      catch (Exception ex)
      {
        MessageBox.Show("Не вдалося зберегти зміни атрибутів:\n " + ex.Message, "Помилка збереження атрибутів", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
      this.DialogResult = false;
    }
  }
}
