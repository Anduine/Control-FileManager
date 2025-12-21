using System;

namespace ControlFileManager.Core.Models
{
  public class SearchOptions
  {
    public string RootPath { get; set; }
    public string Querry { get; set; }     // "*.txt" "report"
    public string? ContentText { get; set; }     // Text inside (null/empty - no search)
    public long MaxContentSize { get; set; }
    public bool IsRecursive { get; set; } = true;
    public bool CaseSensitive { get; set; } = false;

    public bool UseRegex { get; set; } = true;
    public bool UseFuzzy { get; set; } = false; // Інтелектуальний пошук
    public int FuzzyTolerance { get; set; } = 3;
  }
}
