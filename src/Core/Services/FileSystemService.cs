using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ControlFileManager.Core.Models;
using ControlFileManager.Core.Services;

namespace FileManager.Core.Services
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
                    LastModified = d.RootDirectory.LastWriteTime
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

            // directories
            foreach (var d in dirInfo.EnumerateDirectories())
            {
              if (ct.IsCancellationRequested) break;

              var attrs = d.Attributes;

              bool isHidden = attrs.HasFlag(FileAttributes.Hidden);
              bool isSystem = attrs.HasFlag(FileAttributes.System);
              bool isReadOnly = attrs.HasFlag(FileAttributes.ReadOnly);

              if (!showHidden && isHidden) continue;
              if (!showSystem && isSystem) continue;

              list.Add(new FileItem
              {
                Name = d.Name,
                FullPath = d.FullName,
                IsDirectory = true,
                Size = null,
                CreatedTime = d.CreationTime,
                LastModified = d.LastWriteTime,
                IsHidden = isHidden,
                IsSystem = isSystem,
                IsReadOnly = isReadOnly
              });
            }

            // files
            foreach (var f in dirInfo.EnumerateFiles())
            {
              if (ct.IsCancellationRequested) break;

              var attrs = f.Attributes;

              bool isHidden = attrs.HasFlag(FileAttributes.Hidden);
              bool isSystem = attrs.HasFlag(FileAttributes.System);
              bool isReadOnly = attrs.HasFlag(FileAttributes.ReadOnly);

              if (!showHidden && isHidden) continue;
              if (!showSystem && isSystem) continue;

              list.Add(new FileItem
              {
                Name = f.Name,
                FullPath = f.FullName,
                IsDirectory = false,
                Size = f.Length,
                CreatedTime = f.CreationTime,
                LastModified = f.LastWriteTime,
                IsHidden = isHidden,
                IsSystem = isSystem,
                IsReadOnly = isReadOnly
              });
            }
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
            throw new InvalidOperationException("Unable to copy directory inside itself.");

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
          LastModified = di.LastWriteTime,
          CreatedTime = di.CreationTime
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
          LastModified = fi.LastWriteTime,
          CreatedTime = fi.CreationTime
        };
      }
    }

    private void CopyDirectoryRecursive(string sourceDir, string targetDir, bool overwrite)
    {
      if (targetDir.StartsWith(sourceDir, StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException("Unable to copy directory inside itself.");

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
  }
}
