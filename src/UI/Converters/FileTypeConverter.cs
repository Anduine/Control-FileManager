using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;

using ControlFileManager.Core.Models;

namespace ControlFileManager.UI.Converters
{
  public class FileTypeConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is FileItem item)
      {
        if (item.IsDirectory)
        {
          // TODO: return LocalizationService.Get("FolderType");
          return "Папка з файлами";
        }

        string extension = Path.GetExtension(item.Name).TrimStart('.').ToLower();

        if (string.IsNullOrEmpty(extension))
        {
          // TODO: return LocalizationService.Get("UnknownFile");
          return "Файл";
        }

        // TODO: return string.Format(LocalizationService.Get("FileTypeFormat"), extension);
        return $"{extension} Файл";
      }

      return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
