using System.Collections.ObjectModel;
using System.Windows;
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
    public RelayCommand OpenSearchDialogCommand { get; }
    public RelayCommand ChangeTheme { get; }

    public MainViewModel(IFileSystemService fs)
    {
      _fs = fs;
      _ops = new FileOperationService(_fs);

      AddPanelCommand = new RelayCommand(_ => AddPanel());
      RemovePanelCommand = new RelayCommand(_ => RemovePanel(), _ => Panels.Count > 1);

      OpenSearchDialogCommand = new RelayCommand(_ => OpenSearchDialog());

      ChangeTheme = new RelayCommand(_ => ThemeService.ToggleTheme());

      AddPanel();
      AddPanel();

      Panels.CollectionChanged += (_, _) =>
      {
        RemovePanelCommand.RaiseCanExecuteChanged();
        AddPanelCommand.RaiseCanExecuteChanged();
      };
    }

    private void OpenSearchDialog()
    {
      var searchVm = new SearchViewModel(ActivePanel.CurrentPath);

      var searchWindow = new Views.SearchWindow(searchVm)
      {
        Owner = Application.Current.MainWindow
      };

      _ops.CancelSearch();

      bool? result = searchWindow.ShowDialog();

      if (result == true && searchVm.ResultOptions != null)
      {
        _ = _ops.StartSearch(ActivePanel, searchVm.ResultOptions);
      }
    }

    private void AddPanel()
    {
      var newPanel = new FilePanelViewModel(_fs, _ops);

      newPanel.ActivationRequested += SetActivePanel;

      Panels.Add(newPanel);
      SetActivePanel(newPanel);
    }

    private void RemovePanel()
    {
      if (Panels.Count <= 1) return;

      ActivePanel.ActivationRequested -= SetActivePanel;

      Panels.Remove(ActivePanel);
      SetActivePanel(Panels[0]);
    }

    private void SetActivePanel(FilePanelViewModel panel)
    {
      if (ActivePanel != null)
      {
        ActivePanel.IsPanelActive = false;
      }

      ActivePanel = panel;

      if (ActivePanel != null)
      {
        ActivePanel.IsPanelActive = true;
      }

      RemovePanelCommand.RaiseCanExecuteChanged();
    }
  }
}
