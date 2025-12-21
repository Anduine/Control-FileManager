using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ControlFileManager.Core.Models;
using ControlFileManager.Core.Services;

namespace ControlFileManager.Core.Services
{
  public class FileSystemService : IFileSystemService
  {
    public Task<IEnumerable<FileItem>> GetDrivesAsync(CancellationToken ct = default) =>
      Task.Run(() =>
      {
        var drives = DriveInfo.GetDrives()
                              .Where(d => d.IsReady)
                              .Select(d => new FileItem
                              {
                                Name = d.Name,
                                FullPath = d.RootDirectory.FullName,
                                IsDirectory = true,
                                Size = null,
                                LastWriteTime = d.RootDirectory.LastWriteTime
                              });

        return (IEnumerable<FileItem>)drives.ToList();
      }, ct);

    public Task<IEnumerable<FileItem>> GetDirectoryItemsAsync(
      string path,
      bool showHidden,
      bool showSystem,
      CancellationToken ct = default) =>
      Task.Run(() =>
      {
        try
        {
          var list = new List<FileItem>();
          var dirInfo = new DirectoryInfo(path);

          foreach (var i in dirInfo.EnumerateFileSystemInfos())
          {
            if (ct.IsCancellationRequested) break;
            var attrs = i.Attributes;

            bool isHidden = attrs.HasFlag(FileAttributes.Hidden);
            bool isSystem = attrs.HasFlag(FileAttributes.System);
            bool isReadOnly = attrs.HasFlag(FileAttributes.ReadOnly);
            bool isArchive = attrs.HasFlag(FileAttributes.Archive);

            if (!showHidden && isHidden) continue;
            if (!showSystem && isSystem) continue;

            list.Add(new FileItem
            {
              Name = i.Name,
              FullPath = i.FullName,
              IsDirectory = i.Attributes.HasFlag(FileAttributes.Directory),
              Size = i.Attributes.HasFlag(FileAttributes.Directory) ? null : (i as FileInfo)?.Length,
              CreationTime = i.CreationTime,
              LastWriteTime = i.LastWriteTime,
              LastAccessTime = i.LastAccessTime,
              IsHidden = isHidden,
              IsSystem = isSystem,
              IsReadOnly = isReadOnly,
              IsArchive = isArchive
            });
          }

          list.Sort((a, b) =>
          {
            if (a.IsDirectory && !b.IsDirectory) return -1;
            if (!a.IsDirectory && b.IsDirectory) return 1;
            return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
          });

          return (IEnumerable<FileItem>)list;
        }
        catch (DirectoryNotFoundException)
        {
          throw;
        }
      }, ct);

    public Task CreateDirectoryAsync(string parentPath, string name, CancellationToken ct = default) =>
      Task.Run(() =>
      {
        Directory.CreateDirectory(Path.Combine(parentPath, name));
      }, ct);

    public Task CreateFileAsync(string parentPath, string name, CancellationToken ct = default) =>
    Task.Run(() =>
    {
      string fullPath = Path.Combine(parentPath, name);

      try
      {
        if (!File.Exists(fullPath))
        {
          File.Create(fullPath).Dispose();
        }
        else
        {
          throw new IOException("File with this name already exist.");
        }
      }
      catch (Exception)
      {
        throw;
      }
    }, ct);

    public Task DeleteAsync(string path, CancellationToken ct = default) =>
      Task.Run(() =>
      {
        if (File.GetAttributes(path).HasFlag(FileAttributes.Directory))
          Directory.Delete(path, true);
        else
          File.Delete(path);
      }, ct);

    public Task CopyAsync(string sourcePath, string destinationPath, bool overwrite = false, CancellationToken ct = default) =>
      Task.Run(() =>
      {
        sourcePath = Path.GetFullPath(sourcePath);
        destinationPath = Path.GetFullPath(destinationPath);

        if (Directory.Exists(sourcePath) && destinationPath.StartsWith(sourcePath, StringComparison.OrdinalIgnoreCase))
        {
          throw new InvalidOperationException("Unable to copy directory inside itself.");
        }

        if (Directory.Exists(sourcePath))
        {
          CopyDirectoryRecursive(sourcePath, destinationPath, overwrite);
        }
        else
        {
          var destDir = Path.GetDirectoryName(destinationPath);
          if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
            Directory.CreateDirectory(destDir);

          File.Copy(sourcePath, destinationPath, overwrite);
        }
      }, ct);

    public Task MoveAsync(string sourcePath, string destinationPath, CancellationToken ct = default) =>
      Task.Run(() =>
      {
        if (Directory.Exists(sourcePath))
          Directory.Move(sourcePath, destinationPath);
        else
          File.Move(sourcePath, destinationPath);
      }, ct);

    public Task<FileItem> RenameAsync(string oldFullPath, string newName, CancellationToken ct = default) =>
      Task.Run(() =>
      {
        ct.ThrowIfCancellationRequested();

        string oldFull = Path.GetFullPath(oldFullPath);
        string parent = Path.GetDirectoryName(oldFull)
                              ?? throw new InvalidOperationException("Cannot determine parent directory.");

        string newFull = Path.Combine(parent, newName);

        if (string.Equals(oldFull, newFull, StringComparison.Ordinal))
        {
          return CreateItemFromPath(oldFull);
        }

        try
        {
          if (Directory.Exists(oldFull))
          {
            Directory.Move(oldFull, newFull);
          }
          else if (File.Exists(oldFull))
          {
            File.Move(oldFull, newFull);
          }
          else
          {
            throw new FileNotFoundException($"Source not found: {oldFull}");
          }
        }
        catch (IOException)
        {
          throw;
        }

        return CreateItemFromPath(newFull);

      }, ct);

    private static FileItem CreateItemFromPath(string fullPath)
    {
      if (Directory.Exists(fullPath))
      {
        var di = new DirectoryInfo(fullPath);
        return new FileItem
        {
          Name = di.Name,
          FullPath = di.FullName,
          IsDirectory = true,
          Size = null,
          LastWriteTime = di.LastWriteTime,
          CreationTime = di.CreationTime,
          LastAccessTime = di.LastAccessTime,
        };
      }
      else
      {
        var fi = new FileInfo(fullPath);
        return new FileItem
        {
          Name = fi.Name,
          FullPath = fi.FullName,
          IsDirectory = false,
          Size = fi.Length,
          LastWriteTime = fi.LastWriteTime,
          CreationTime = fi.CreationTime,
          LastAccessTime = fi.LastAccessTime,
        };
      }
    }

    private void CopyDirectoryRecursive(string sourceDir, string targetDir, bool overwrite)
    {
      if (targetDir.StartsWith(sourceDir, StringComparison.OrdinalIgnoreCase))
      {
        throw new InvalidOperationException("Unable to copy directory inside itself.");
      }

      var di = new DirectoryInfo(sourceDir);
      if (!Directory.Exists(targetDir))
        Directory.CreateDirectory(targetDir);

      foreach (var file in di.GetFiles())
      {
        var targetFilePath = Path.Combine(targetDir, file.Name);
        file.CopyTo(targetFilePath, overwrite);
      }

      foreach (var subDir in di.GetDirectories())
      {
        var newTargetDir = Path.Combine(targetDir, subDir.Name);
        CopyDirectoryRecursive(subDir.FullName, newTargetDir, overwrite);
      }
    }

    public async IAsyncEnumerable<FileItem> SearchAsync(SearchOptions options, [EnumeratorCancellation] CancellationToken ct)
    {
      var enumOptions = new EnumerationOptions
      {
        IgnoreInaccessible = true,
        RecurseSubdirectories = options.IsRecursive,
        AttributesToSkip = FileAttributes.ReparsePoint, // Пропуск сімлінки
        MatchCasing = options.CaseSensitive ? MatchCasing.CaseSensitive : MatchCasing.CaseInsensitive
      };

      string sysPattern = "*";
      if (!options.UseRegex && !options.UseFuzzy && !string.IsNullOrWhiteSpace(options.Querry))
      {
        sysPattern = options.Querry.Contains('*') ? options.Querry : $"*{options.Querry}*";
      }

      var fileSystemIterator = new DirectoryInfo(options.RootPath)
                                    .EnumerateFileSystemInfos(sysPattern, enumOptions);

      foreach (var info in fileSystemIterator)
      {
        if (ct.IsCancellationRequested) yield break;

        if ((options.UseRegex || options.UseFuzzy) && !IsNameMatch(info.Name, options))
        {
          continue;
        }

        bool isDir = (info.Attributes & FileAttributes.Directory) != 0;
        long? size = null;

        if (!isDir)
        {
          var fi = (FileInfo)info;
          size = fi.Length;

          if (!string.IsNullOrEmpty(options.ContentText))
          {
            if (size > options.MaxContentSize) continue;

            bool contentMatch = await ContainsTextAsync(info.FullName, options.ContentText, options.CaseSensitive, ct);
            if (!contentMatch) continue;
          }
        }
        else if (!string.IsNullOrEmpty(options.ContentText))
        {
          continue;
        }

        yield return new FileItem
        {
          Name = info.Name,
          FullPath = info.FullName,
          IsDirectory = isDir,
          Size = size,
          LastWriteTime = info.LastWriteTime,
          CreationTime = info.CreationTime,
          LastAccessTime = info.LastAccessTime,
          IsHidden = (info.Attributes & FileAttributes.Hidden) != 0,
          IsSystem = (info.Attributes & FileAttributes.System) != 0,
          IsReadOnly = (info.Attributes & FileAttributes.ReadOnly) != 0,
          IsArchive = (info.Attributes & FileAttributes.Archive) != 0
        };
      }
    }

    private bool IsNameMatch(string fileName, SearchOptions options)
    {
      if (string.IsNullOrEmpty(options.Querry)) return true;

      if (options.UseRegex)
      {
        try
        {
          return Regex.IsMatch(fileName, options.Querry, RegexOptions.IgnoreCase);
        }
        catch (ArgumentException)
        {
          return false;
        }
      }

      if (options.UseFuzzy)
      {
        string fName = fileName.ToLower();
        string query = options.Querry.Trim().ToLower();

        if (!query.Contains('.'))
        {
          fName = Path.GetFileNameWithoutExtension(fileName).ToLower();
        }

        if (fName.Contains(query))
          return true;

        if (Math.Abs(fName.Length - query.Length) > options.FuzzyTolerance)
          return false;

        return ComputeLevenshteinDistance(fName, query) <= options.FuzzyTolerance;
      }

      return fileName.Contains(options.Querry, options.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
    }

    private static int ComputeLevenshteinDistance(string s, string t)
    {
      if (string.IsNullOrEmpty(s)) return string.IsNullOrEmpty(t) ? 0 : t.Length;
      if (string.IsNullOrEmpty(t)) return s.Length;

      int n = s.Length;
      int m = t.Length;
      int[,] d = new int[n + 1, m + 1];

      for (int i = 0; i <= n; d[i, 0] = i++) ;
      for (int j = 0; j <= m; d[0, j] = j++) ;

      for (int i = 1; i <= n; i++)
      {
        for (int j = 1; j <= m; j++)
        {
          int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
          d[i, j] = Math.Min(
              Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
              d[i - 1, j - 1] + cost);
        }
      }
      return d[n, m];
    }

    private async Task<bool> ContainsTextAsync(string filePath, string searchText, bool caseSensitive, CancellationToken ct)
    {
      try
      {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, true);
        using var reader = new StreamReader(stream);

        string line;
        var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        while ((line = await reader.ReadLineAsync(ct)) != null)
        {
          if (line.Contains(searchText, comparison))
          {
            return true;
          }
        }
      }
      catch (IOException)
      {
        return false;
      }
      catch (UnauthorizedAccessException)
      {
        return false;
      }

      return false;
    }
  }
}
