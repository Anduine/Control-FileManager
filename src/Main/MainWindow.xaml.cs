using System.Windows;

using ControlFileManager.Core.Services;
using ControlFileManager.UI.ViewModels;

namespace ControlFileManager.Main
{
  public partial class MainWindow : Window
  {
    public MainWindow(MainViewModel vm, IFileSystemService fileService)
    {
      InitializeComponent();
      DataContext = vm;
      vm.StatusTb = StatusTb;
    }
  }
}