using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Localization.Settings;

public class LocalizationManager : MonoBehaviour
{
    [Header("Localization")]
    private const string LocalizationPlayerPrefs = "Localization";
    private int localizationInt;

    public void SetLocalizationPlayerPrefs()
    {
        localizationInt = PlayerPrefs.GetInt(LocalizationPlayerPrefs, localizationInt);
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[localizationInt];
    }

    private void ChangeLanguage(int languageInt)
    {
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[languageInt];
        PlayerPrefs.SetFloat(LocalizationPlayerPrefs, languageInt);
    }
}
