using System;
using System.IO;

using ControlFileManager.Core.Models;

namespace ControlFileManager.UI.ViewModels
{
  public class PropertiesViewModel
  {
    private readonly string _path;
    private readonly bool _isDirectory;
    public string Name { get; set; }
    public string FullPath { get; set; }
    public string SizeText { get; set; }
    public string CreationTime { get; set; }
    public string LastWriteTime { get; set; }
    public string LastAccessTime { get; set; }

    public bool IsReadOnly { get; set; }
    public bool IsArchive { get; set; }
    public bool IsHidden { get; set; }
    public bool IsSystem { get; set; }

    public PropertiesViewModel(FileItem fileItem)
    {
      _path = fileItem.FullPath;

      _isDirectory = fileItem.IsDirectory;

      Name = fileItem.Name;
      FullPath = fileItem.FullPath;
      CreationTime = fileItem.CreationTime.ToString("g");
      LastWriteTime = fileItem.LastWriteTime.ToString("g");
      LastAccessTime = fileItem.LastAccessTime.ToString("g");

      if (!fileItem.IsDirectory)
        SizeText = $"{fileItem.Size / 1024.0:F2} KB";
      else
        SizeText = "<Папка>";

      IsReadOnly = fileItem.IsReadOnly;
      IsArchive = fileItem.IsArchive;
      IsHidden = fileItem.IsHidden;
      IsSystem = fileItem.IsSystem;
    }

    public void ApplyChanges()
    {
      try
      {
        FileSystemInfo info = _isDirectory ? new DirectoryInfo(_path) : new FileInfo(_path);

        FileAttributes attributes = info.Attributes;

        attributes = SetAttribute(attributes, FileAttributes.ReadOnly, IsReadOnly);
        attributes = SetAttribute(attributes, FileAttributes.Hidden, IsHidden);
        attributes = SetAttribute(attributes, FileAttributes.Archive, IsArchive);

        info.Attributes = attributes;
      }
      catch (Exception)
      {
        throw;
      }
    }

    private FileAttributes SetAttribute(FileAttributes current, FileAttributes target, bool value)
    {
      if (value)
        return current | target;
      else
        return current & ~target;
    }
  }
}
