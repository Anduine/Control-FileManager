using System;
using System.Windows.Input;

using ControlFileManager.Core.Models;
using ControlFileManager.UI.Commands;

namespace ControlFileManager.UI.ViewModels
{
  public class SearchViewModel : BaseViewModel
  {
    public string SearchPath { get; set; }
    public string NamePattern { get; set; }
    public string ContentText { get; set; }
    public bool IsRecursive { get; set; } = true;
    public bool CaseSensitive { get; set; } = false;

    public SearchOptions ResultOptions { get; private set; }

    public event Action RequestClose;

    public ICommand ConfirmCommand { get; }

    public SearchViewModel(string initialPath)
    {
      SearchPath = initialPath;
      NamePattern = "*";

      ConfirmCommand = new RelayCommand(_ => Confirm());
    }

    private void Confirm()
    {
      ResultOptions = new SearchOptions
      {
        RootPath = SearchPath,
        NamePattern = NamePattern,
        ContentText = ContentText,
        IsRecursive = IsRecursive,
        CaseSensitive = CaseSensitive
      };

      RequestClose?.Invoke();
    }
  }
}
