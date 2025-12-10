using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlFileManager.Core.Models
{
  public class SearchOptions
  {
    public string RootPath { get; set; }
    public string NamePattern { get; set; }     // "*.txt" "report"
    public string ContentText { get; set; }     // Text inside (null/empty - no search)
    public long MaxContentSize { get; set; }
    public bool IsRecursive { get; set; } = true;
    public bool CaseSensitive { get; set; } = false;
  }
}
