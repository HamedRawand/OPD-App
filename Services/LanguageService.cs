using System.IO;
using System.Windows;

namespace OPDClinic.Services;

public enum AppLanguage { English, Dari }

public static class LanguageService
{
    private static readonly string _settingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "OPDClinic", "language.txt");

    public static AppLanguage Current { get; private set; } = AppLanguage.English;

    public static bool IsRtl => Current == AppLanguage.Dari;

    public static event Action? LanguageChanged;

    public static void Initialize()
    {
        if (File.Exists(_settingsPath))
        {
            var saved = File.ReadAllText(_settingsPath).Trim();
            Current = saved == "Dari" ? AppLanguage.Dari : AppLanguage.English;
        }
        ApplyLanguage(Current, raiseEvent: false);
    }

    public static void Toggle()
    {
        Current = Current == AppLanguage.English ? AppLanguage.Dari : AppLanguage.English;
        File.WriteAllText(_settingsPath, Current.ToString());
        ApplyLanguage(Current, raiseEvent: true);
    }

    private static void ApplyLanguage(AppLanguage lang, bool raiseEvent)
    {
        var dicts = Application.Current.Resources.MergedDictionaries;
        var existing = dicts.FirstOrDefault(d =>
            d.Source?.OriginalString.Contains("Strings.") == true);
        if (existing != null) dicts.Remove(existing);

        var uri = lang == AppLanguage.Dari
            ? new Uri("pack://application:,,,/OPDClinic;component/Resources/Strings.dari.xaml")
            : new Uri("pack://application:,,,/OPDClinic;component/Resources/Strings.en.xaml");

        dicts.Add(new ResourceDictionary { Source = uri });

        if (raiseEvent) LanguageChanged?.Invoke();
    }
}
