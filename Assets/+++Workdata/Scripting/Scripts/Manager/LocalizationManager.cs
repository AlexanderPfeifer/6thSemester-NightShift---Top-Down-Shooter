using UnityEngine;
using UnityEngine.Localization.Settings;

public class LocalizationManager : SingletonPersistent<LocalizationManager>
{
    [Header("Localization")]
    public string LocalePlayerPrefs = "Locale";
    public int localeIndex;

    public void GetLocalePlayerPrefs()
    {
        localeIndex = PlayerPrefs.GetInt(LocalePlayerPrefs, localeIndex);
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[localeIndex];
    }
}
