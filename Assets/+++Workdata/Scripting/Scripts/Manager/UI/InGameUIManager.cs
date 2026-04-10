using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

public class InGameUIManager : Singleton<InGameUIManager>
{
    [Header("Saving")]
    public TextMeshProUGUI gameSavingText;
    
    [Header("UI Screens")]
    [FormerlySerializedAs("inGameUIScreen")] public GameObject playerHUD;
    public GameObject shopScreen;
    [SerializeField] private GameObject generatorScreen;
    public CanvasGroup inGameUICanvasGroup;

    [Header("End Sequence")]
    [HideInInspector] public bool changeLight;
    public Animator endScreen;
    static float t;

    [Header("LooseGame")] 
    public Image shutterLooseImage;
    public Animator loseShutterAnim;

    [Header("References")]
    public GeneratorUI generatorUI;
    public FortuneWheelUI fortuneWheelUI;
    public DialogueUI dialogueUI;
    public ShopUI shopUI;
    public PauseMenuUI pauseMenuUI;

    [Header("WalkieTalkie")] 
    [SerializeField] private TextMeshProUGUI walkieTalkieQuestLog;

    private void OnEnable()
    {
        GameInputManager.Instance.OnGamePausedAction += OnPressEscape;
    }

    private void OnDisable()
    {
        GameInputManager.Instance.OnGamePausedAction -= OnPressEscape;
    }

    private void Update()
    {
        SimulateDayLight();
    }

    private void OnPressEscape(object sender, EventArgs eventArgs)
    {
        CloseShop();
        
        CloseGeneratorUI();
    }

    public void SetWalkieTalkieQuestLog(string text)
    {
        if(!TutorialManager.Instance.tutorialDone)
        {
            walkieTalkieQuestLog.text = text;
        }
        //StartCoroutine(dialogueUI.TypeTextCoroutine(text, null, walkieTalkieQuestLog));
    }
    
    public void GoToMainMenu()
    {
        GameSaveStateManager.Instance.GoToMainMenu();

        AudioManager.Instance.Pause("InGameMusic");

        AudioManager.Instance.FadeIn("MainMenuMusic");
    }
    
    private void SimulateDayLight()
    {
        if (changeLight)
        {
            t += 0.5f * Time.deltaTime;

            PlayerBehaviour.Instance.globalLightObject.gameObject.GetComponent<Light2D>().intensity = Mathf.Lerp( PlayerBehaviour.Instance.globalLightObject.gameObject.GetComponent<Light2D>().intensity, 1, t);
        }
    }

    public void EndScreen()
    {
        AudioManager.Instance.Stop("InGameMusic"); 
        playerHUD.SetActive(false);
        endScreen.gameObject.SetActive(true);
        changeLight = true;
    }

    public void ForceSetGeneratorUI()
    {
        TutorialManager.Instance.isExplainingCurrencyDialogue = false;
        generatorScreen.SetActive(true);
    }

    public void SetGeneratorUI()
    {
        if (!TutorialManager.Instance.isExplainingCurrencyDialogue && (dialogueUI.IsDialoguePlaying() || dialogueUI.walkieTalkieText.gameObject.activeSelf))
        {
            return;
        }

        if (!TutorialManager.Instance.isExplainingCurrencyDialogue && !PlayerBehaviour.Instance.IsPlayerBusy())
        {
            if (!TutorialManager.Instance.talkedAboutCurrency)
            {
                TutorialManager.Instance.ExplainCurrency();
                return;
            }

            generatorScreen.SetActive(true);
        }
    }

    public void CloseGeneratorUI()
    {
        if (generatorScreen.activeSelf && generatorUI.changeFill)
        {
            generatorScreen.SetActive(false);
        }
    }

    public void OpenShop()
    {
        if (dialogueUI.IsDialoguePlaying() || dialogueUI.walkieTalkieText.gameObject.activeSelf)
        {
            return;
        }

        if (!PlayerBehaviour.Instance.IsPlayerBusy())
        {
            shopScreen.SetActive(true);
            
            AudioManager.Instance.FadeOut("InGameMusic", "ShopMusic");

            List<Transform> _enemies = Ride.Instance.enemyParent.transform.Cast<Transform>().ToList();

            Ride.Instance.CleanStage();
            
            if(Ride.Instance.rideActivation.interactable)
                Ride.Instance.rideActivation.gateAnim.SetBool("OpenGate", false);

            dialogueUI.SetCurrentDialogueBox(true); 
            dialogueUI.SetWalkieTalkieTextBoxAnimation(false, false);

            if (!shopUI.fortuneWheel.activeSelf)
            {
                GameInputManager.Instance.SetNewButtonAsSelected(shopUI.fillWeaponAmmoButton.gameObject);
            }
            else
            {
                GameInputManager.Instance.SetNewButtonAsSelected(shopUI.spinFortuneWheelButton.gameObject);
                
                StartCoroutine(dialogueUI.TypeTextCoroutine("Peggy:" + "\n" + "...", null, dialogueUI.currentTextBox));
            }

            if(GameSaveStateManager.Instance.saveGameDataManager.HasWavesFinished() == 2)
            {
                InputGraphicsManager.Instance.RemoveAllChalkSigns();
            }
            
            PlayerBehaviour.Instance.SetPlayerBusy(true);

            if (TutorialManager.Instance.shotSigns >= TutorialManager.Instance.shotSignsToGoAhead && !shopUI.fortuneWheel.activeSelf)
            {
                shopUI.ResetWeaponDescriptions();
            }

            TutorialManager.Instance.CheckDialogue();
        }
    }

    public void CloseShop()
    {
        if (shopScreen.activeSelf && !dialogueUI.IsDialoguePlaying())
        {
            if (fortuneWheelUI.gameObject.activeSelf && (fortuneWheelUI.rb.angularVelocity > 0 || fortuneWheelUI.receivingPrize))
            {
                return;
            }
            
            dialogueUI.currentTextBox.text = "";
            
            dialogueUI.SetCurrentDialogueBox(false);
            dialogueUI.SetWalkieTalkieTextBoxAnimation(false, true);
            
            shopScreen.SetActive(false);
            
            AudioManager.Instance.FadeOut("ShopMusic", "InGameMusic");

            PlayerBehaviour.Instance.SetPlayerBusy(false);   
        }
    }
}