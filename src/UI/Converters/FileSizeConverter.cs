using System;
using System.Globalization;
using System.Windows.Data;

namespace ControlFileManager.UI.Converters
{
  public class FileSizeConverter : IValueConverter
  {
    private static readonly string[] Suffixes = { "B", "KB", "MB", "GB", "TB" };

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is long size)
      {
        if (size < 0) return "";

        int order = 0;
        double len = size;

        while (len >= 1024 && order < Suffixes.Length - 1)
        {
          order++;
          len /= 1024;
        }

        var nfi = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
        nfi.NumberGroupSeparator = " ";

        return len.ToString("N0", nfi) + " " + Suffixes[order];
      }
      return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
