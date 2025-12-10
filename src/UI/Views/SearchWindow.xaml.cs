using System;
using System.Windows;
using ControlFileManager.UI.ViewModels;

namespace ControlFileManager.UI.Views
{
  public partial class SearchWindow : Window
  {
    public SearchWindow(SearchViewModel viewModel)
    {
      InitializeComponent();
      DataContext = viewModel;

      viewModel.RequestClose += () =>
      {
        this.DialogResult = true;
        this.Close();
      };
    }
  }
}
