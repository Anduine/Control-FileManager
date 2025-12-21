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
    public bool UseRegex { get; set; } = false;
    public bool UseFuzzy { get; set; } = false;

    private bool _searchInside;
    public bool SearchInside
    {
      get => _searchInside;
      set
      {
        if (_searchInside != value)
        {
          _searchInside = value;
          Raise();
        }
      }
    }

    public SearchOptions ResultOptions { get; private set; }

    public event Action RequestClose;

    public RelayCommand ConfirmCommand { get; }

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
        Querry = NamePattern,
        ContentText = ContentText = SearchInside ? ContentText : string.Empty,
        IsRecursive = IsRecursive,
        CaseSensitive = CaseSensitive,
        UseRegex = UseRegex,
        UseFuzzy = UseFuzzy
      };

      RequestClose?.Invoke();
    }
  }
}
