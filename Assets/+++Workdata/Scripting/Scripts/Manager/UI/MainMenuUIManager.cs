using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEditor.Localization.Editor;

public class MainMenuUIManager : Singleton<MainMenuUIManager>
{
    [Header("LoadGameScreen")]
    private GameObject newLoadButton;
    private GameObject newDeleteButton;
    [SerializeField] private List<GameObject> loadButtonsList;
    [SerializeField] private GameObject loadLevelButtonPrefab;
    [SerializeField] private GameObject deleteSaveStateButtonPrefab;
    [SerializeField] private Transform saveStateLayoutGroup;
    [SerializeField] private Button deleteSaveStateCheckButton;
    [SerializeField] private GameObject deleteSaveStateCheckPanel;
    [HideInInspector] public bool gameStateLoaded;
    [FormerlySerializedAs("configureButtonsManager")] [SerializeField] private AllButtonsConfiguration allButtonsConfiguration;

    [Header("MainMenuScreens")]
    [SerializeField] private GameObject loadScreen;
    [SerializeField] private GameObject optionsScreen;
    [SerializeField] private GameObject creditsScreen;
    
    [Header("Controls")]
    [SerializeField] private GameObject keyboardControls;
    [SerializeField] private GameObject gamePadControls;
    
    [Header("MainMenuButtons")]
    [SerializeField] private GameObject loadButton;
    [SerializeField] private GameObject optionsButton;
    [SerializeField] private GameObject creditsButton;
    public GameObject firstMainMenuSelected;
    [SerializeField] private GameObject wishlistButton;
    [SerializeField] private GameObject joinDiscordButton;

    [Header("Visuals")]
    [SerializeField] private Animator stallShutterAnimator;
    [SerializeField] private GameObject sunnyBackground;

    [Header("AudioMaster")]
    [SerializeField] private GameObject[] masterPoints;
    [SerializeField] Button minusMasterButton;
    [SerializeField] Button plusMasterButton;

    [Header("AudioMusic")]  
    [SerializeField] private GameObject[] musicPoints;
    [SerializeField] Button minusMusicButton;
    [SerializeField] Button plusMusicButton;

    [Header("AudioSFX")]
    [SerializeField] private GameObject[] sfxPoints;
    [SerializeField] Button minusSFXButton;
    [SerializeField] Button plusSFXButton;

    [Header("Fullscreen")]
    [SerializeField] private GameObject fullScreenCheck;
    [SerializeField] private Button fullScreenButton;

    private void Start()
    {        
        AudioManager.Instance.SetAudioPlayerPrefs(masterPoints, musicPoints, sfxPoints);
        SceneManager.Instance.SetFullscreenPlayerPrefs(fullScreenCheck);

        minusMasterButton.onClick.AddListener(() => AudioManager.Instance.ChangeSpecificVolume(AudioManager.VolumeType.Master, masterPoints, false));
        plusMasterButton.onClick.AddListener(() => AudioManager.Instance.ChangeSpecificVolume(AudioManager.VolumeType.Master, masterPoints, true));

        minusMusicButton.onClick.AddListener(() => AudioManager.Instance.ChangeSpecificVolume(AudioManager.VolumeType.Music, musicPoints, false));
        plusMusicButton.onClick.AddListener(() => AudioManager.Instance.ChangeSpecificVolume(AudioManager.VolumeType.Music, musicPoints, true));

        minusSFXButton.onClick.AddListener(() => AudioManager.Instance.ChangeSpecificVolume(AudioManager.VolumeType.Sfx, sfxPoints, false));
        plusSFXButton.onClick.AddListener(() => AudioManager.Instance.ChangeSpecificVolume(AudioManager.VolumeType.Sfx, sfxPoints, true));

        fullScreenButton.onClick.AddListener(() => SceneManager.Instance.ChangeFullScreenMode(fullScreenCheck));

        loadScreen.SetActive(false);
        optionsScreen.SetActive(false);
        creditsScreen.SetActive(false);
        
        AudioManager.Instance.Play("MainMenuMusic");

        if (GameSaveStateManager.Instance.gameGotFinished)
        {
            sunnyBackground.SetActive(true);
        }
    }

    public void ChangeLanguage()
    {
        
    }

    public void JoinOurDiscord()
    {
        Application.OpenURL("https://discord.gg/mmYek3rY/");
    }

    public void WishlistOnSteam()
    {
        Application.OpenURL("https://store.steampowered.com/app/3206760/Night_Shift/");
    }

    public void OpenOptionsMenu()
    {
        StartCoroutine(SetScreen(false, true, false));
    }

    public void OpenCreditsMenu()
    {
        StartCoroutine(SetScreen(false, false, true));
    }
    
    public void QuitGame()
    {
        AudioManager.Instance.Stop("MainMenuMusic");
        Application.Quit();
    }

    private void DeleteLoadMenuButtons()
    {
        loadButtonsList.ForEach(Destroy);
        gameStateLoaded = false;
    }

    private void DeleteSaveState(string saveName)
    {
        SaveFileManager.DeleteSaveState(saveName);
        DeleteLoadMenuButtons();
        CreateLoadMenuButtons();
        deleteSaveStateCheckButton.onClick.RemoveListener(() => DeleteSaveState(saveName));
    }
    
    public void CreateLoadMenuButtons()
    {
        StartCoroutine(SetScreen(true, false, false));

        if (!gameStateLoaded)
        {
            string[] _saveGameNames = SaveFileManager.GetAllSaveFileNames();
            
            foreach (var _saveGameName in _saveGameNames)
            {
                newLoadButton = Instantiate(loadLevelButtonPrefab, saveStateLayoutGroup);
                loadButtonsList.Add(newLoadButton);
                
                TextMeshProUGUI _buttonText = newLoadButton.GetComponentInChildren<TextMeshProUGUI>();
                _buttonText.text = _saveGameName;
                string _saveStateName = _saveGameName;

                var _loadButton = newLoadButton.GetComponent<Button>();
                _loadButton.onClick.AddListener(() => AudioManager.Instance.Play("ButtonClick"));
                _loadButton.onClick.AddListener(() => LoadGame(_saveStateName));
                
                newDeleteButton = Instantiate(deleteSaveStateButtonPrefab, saveStateLayoutGroup);
                loadButtonsList.Add(newDeleteButton);
                
                var _deleteButton = newDeleteButton.GetComponent<Button>();
                _deleteButton.onClick.AddListener(() => AudioManager.Instance.Play("ButtonClick"));
                _deleteButton.onClick.AddListener(delegate{SetDeleteSaveStateCheck(_saveStateName);});
            }

            foreach (var _loadButtons in loadButtonsList)
            {
                allButtonsConfiguration.AddHoverEvent(_loadButtons);
            }

            gameStateLoaded = true;
        }
    }

    private void SetDeleteSaveStateCheck(string saveStateName)
    {
        deleteSaveStateCheckPanel.SetActive(true);
        deleteSaveStateCheckButton.onClick.AddListener(() => DeleteSaveState(saveStateName));
    }
    
    private void LoadGame(string saveName)
    {
        EventSystem.current.SetSelectedGameObject(null);
        GameSaveStateManager.Instance.LoadFromSave(saveName);
        AudioManager.Instance.FadeOut("MainMenuMusic", "InGameMusic");
        gameStateLoaded = false;
    }

    public void SetLoadingScreen()
    {
        SceneManager.Instance.loadingScreenAnim.SetTrigger("Start");
    }

    private IEnumerator SetScreen(bool shouldSetLoadScreen, bool shouldSetOptionsScreen, bool shouldSetCreditsScreen)
    {
        if (!optionsScreen.activeSelf)
            SetAnimation();

        if (!stallShutterAnimator.GetBool("GoUp"))
        {
            yield return new WaitForSeconds(0.7f);
        }

        loadScreen.SetActive(shouldSetLoadScreen);

        if(!optionsScreen.activeSelf)
            optionsScreen.SetActive(shouldSetOptionsScreen);

        creditsScreen.SetActive(shouldSetCreditsScreen);
    }

    public void SetAnimation()
    {
        if (stallShutterAnimator.GetBool("GoUp"))
        {
            stallShutterAnimator.SetTrigger("ChangeScreen");
        }

        stallShutterAnimator.SetBool("GoUp", true);
    }
}
