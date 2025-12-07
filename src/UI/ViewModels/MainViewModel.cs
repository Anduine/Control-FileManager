using System.Collections.ObjectModel;
using System.Windows.Controls;
using ControlFileManager.Core.Services;
using ControlFileManager.UI.Commands;

namespace ControlFileManager.UI.ViewModels
{
  public class MainViewModel : BaseViewModel
  {
    private readonly IFileSystemService _fs;
    private readonly FileOperationService _ops;

    public TextBlock StatusTb;

    public ObservableCollection<FilePanelViewModel> Panels { get; } = new();

    private FilePanelViewModel _activePanel;
    public FilePanelViewModel ActivePanel
    {
      get => _activePanel;
      set { _activePanel = value; Raise(); }
    }

    private bool _showHidden;
    public bool ShowHidden
    {
      get => _showHidden;
      set
      {
        _showHidden = value;
        Raise();
        foreach (var panel in Panels)
        {
          panel.ShowHidden = value;
        }
      }
    }

    private bool _showSystem;
    public bool ShowSystem
    {
      get => _showSystem;
      set
      {
        _showSystem = value;
        Raise();
        foreach (var panel in Panels)
        {
          panel.ShowSystem = value;
        }
      }
    }

    public RelayCommand AddPanelCommand { get; }
    public RelayCommand RemovePanelCommand { get; }

    public MainViewModel(IFileSystemService fs)
    {
      _fs = fs;
      _ops = new FileOperationService(_fs);

      AddPanel();
      AddPanel();

      AddPanelCommand = new RelayCommand(_ => AddPanel());
      RemovePanelCommand = new RelayCommand(_ => RemovePanel(), _ => Panels.Count > 1);

      Panels.CollectionChanged += (_, _) =>
      {
        RemovePanelCommand.RaiseCanExecuteChanged();
        AddPanelCommand.RaiseCanExecuteChanged();
      };
    }

    private void AddPanel()
    {
      var newPanel = new FilePanelViewModel(_fs, _ops);
      Panels.Add(newPanel);
      ActivePanel = newPanel;
    }

    private void RemovePanel()
    {
      if (Panels.Count <= 1) return;

      Panels.Remove(ActivePanel);
      ActivePanel = Panels[0];
    }
  }
}
