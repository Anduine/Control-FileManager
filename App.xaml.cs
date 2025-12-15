using System.Windows;

using ControlFileManager.Core.Services;
using ControlFileManager.Main;
using ControlFileManager.UI.ViewModels;

namespace ControlFileManager
{
  public partial class ControlApp : Application
  {
    protected override void OnStartup(StartupEventArgs e)
    {
      base.OnStartup(e);

      var fileService = new FileSystemService();
      var mainVm = new MainViewModel(fileService);

      var mainWindow = new MainWindow(mainVm, fileService);
      mainWindow.Show();
    }
  }
}
