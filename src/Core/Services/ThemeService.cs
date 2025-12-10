using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ControlFileManager.Core.Services
{
  public static class ThemeService
  {
    private const string ControlsSource = "src/UI/Themes/Elements/Controls.xaml";
    private const string LightSource = "src/UI/Themes/Light.xaml";
    private const string DarkSource = "src/UI/Themes/Dark.xaml";

    public enum ThemeType { Light, Dark }
    public static ThemeType CurrentTheme { get; private set; } = ThemeType.Dark;

    public static void ChangeTheme(ThemeType theme)
    {
      var appResources = Application.Current.Resources;

      // 1. Очищаем все словари (и стили, и цвета)
      appResources.MergedDictionaries.Clear();

      // 2. Добавляем обратно СТИЛИ (Controls.xaml)
      appResources.MergedDictionaries.Add(new ResourceDictionary
      {
        Source = new Uri(ControlsSource, UriKind.Relative)
      });

      // 3. Добавляем нужный ЦВЕТ
      string colorSource = theme == ThemeType.Light ? LightSource : DarkSource;
      appResources.MergedDictionaries.Add(new ResourceDictionary
      {
        Source = new Uri(colorSource, UriKind.Relative)
      });

      CurrentTheme = theme;
    }

    public static void ToggleTheme()
    {
      ChangeTheme(CurrentTheme == ThemeType.Light ? ThemeType.Dark : ThemeType.Light);
    }
  }
}
