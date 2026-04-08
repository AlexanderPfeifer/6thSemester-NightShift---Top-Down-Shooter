using NUnit.Framework;
using System.Collections;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.Serialization;

public class TutorialManager : SingletonPersistent<TutorialManager>
{
    [Header("Shooting Signs")]
    [HideInInspector] public int shotSigns;
    public int shotSignsToGoAhead = 5;

    [Header("Shop Elements")]
    [SerializeField] private GameObject escapeInShop;

    [Header("Finished Sequences")]
    [FormerlySerializedAs("openShutterWheelOfFortune")] [HideInInspector] public bool newWeaponsCanBeUnlocked;
    [HideInInspector] public bool explainedRideSequences;
    [HideInInspector] public bool fillAmmoForFree;
    [HideInInspector] public bool tutorialDone;
    [HideInInspector] public bool talkedAboutCurrency;
    [HideInInspector] public bool playedFirstDialogue;
    private bool openedShopAfterFirstFight;
    private bool toldAboutAmmoRefill;

    [Header("QuestLogTexts")]
    [SerializeField] private string[] shootAllSignsTranslated;
    [SerializeField] private string[] fillAmmoTranslated;
    [SerializeField] private string[] activateGenTranslated;
    [SerializeField] public string[] activateRideTranslated;
    [SerializeField] private string[] doYourJobTranslated;
    public string[] getNewWeaponsTranslated;

    [Header("Dialogue")] 
    [HideInInspector] public bool isExplainingCurrencyDialogue;
    [SerializeField, TextArea] private string[] shotAllSignsText;

    protected override void Awake()
    {
        fillAmmoForFree = true;
        talkedAboutCurrency = false;
    }

    private void Start()
    {
        InGameUIManager.Instance.SetWalkieTalkieQuestLog(shootAllSignsTranslated[LocalizationManager.Instance.localeIndex]);
    }

    public void ExplainCurrency()
    {
        if (!talkedAboutCurrency)
        {
            isExplainingCurrencyDialogue = true;
            InGameUIManager.Instance.dialogueUI.SetWalkieTalkieTextBoxAnimation(true, true);
            PlayerBehaviour.Instance.playerCurrency.currencyBackground.gameObject.SetActive(true);
            talkedAboutCurrency = true;
        }    
    }

    public void CheckDialogue()
    {
        if (!playedFirstDialogue)
        {
            PlayStartingDialogue();
        }
        else if (GameSaveStateManager.Instance.saveGameDataManager.HasWavesFinished() > 0 && !openedShopAfterFirstFight)
        {
            FinishedFirstFight();
        }
        else if (shotSigns >= shotSignsToGoAhead && !toldAboutAmmoRefill)
        {
            toldAboutAmmoRefill = true;
            InGameUIManager.Instance.inGameUICanvasGroup.interactable = true;
            InGameUIManager.Instance.dialogueUI.StopCurrentAndTypeNewTextCoroutine(shotAllSignsText[LocalizationManager.Instance.localeIndex], null, InGameUIManager.Instance.dialogueUI.currentTextBox);
        }
    }

    public void ExplainStartupSequences()
    {
        AudioManager.Instance.Play("RideShutDown");
        foreach (var _light in Ride.Instance.rideLight)
        {
            _light.SetActive(false);
        }
        StartCoroutine(WaitForShutdown());
        
        explainedRideSequences = true;
    }

    private IEnumerator WaitForShutdown()
    {
        while (AudioManager.Instance.IsPlaying("RideShutDown"))
        {
            yield return null;
        }

        InGameUIManager.Instance.generatorUI.changeFill = false;

        InGameUIManager.Instance.generatorUI.gameObject.SetActive(true);

        yield return new WaitForSeconds(.5f);

        for (int i = 0; i < 5; i++)
        {
            foreach (var fuse in Ride.Instance.fuses)
            {
                fuse.sprite = (i % 2 == 0) ? Ride.Instance.DeactivateFuse() : Ride.Instance.ActivateFuse();
            }

            yield return new WaitForSeconds(0.25f);
        }


        yield return new WaitForSeconds(.5f);

        InGameUIManager.Instance.generatorUI.gameObject.SetActive(false);

        InGameUIManager.Instance.generatorUI.changeFill = true;

        InGameUIManager.Instance.dialogueUI.SetWalkieTalkieTextBoxAnimation(true, true);

        Ride.Instance.rideActivation.gateAnim.SetBool("OpenGate", true);

        yield return null;
    }

    private void FinishedFirstFight()
    {
        InGameUIManager.Instance.shopUI.SetShopWindow();

        InGameUIManager.Instance.dialogueUI.shopText.text = "";
            
        InGameUIManager.Instance.dialogueUI.DisplayNextDialogue();

        openedShopAfterFirstFight = true;
        
        InGameUIManager.Instance.shopUI.changeShopWindowButton.SetActive(true);
        InGameUIManager.Instance.shopUI.switchWindowButtons.SetActive(true);
        InGameUIManager.Instance.SetWalkieTalkieQuestLog(doYourJobTranslated[LocalizationManager.Instance.localeIndex]);
        tutorialDone = true;
    }

    public void ExplainGenerator()
    {
        if (shotSigns >= shotSignsToGoAhead && GameSaveStateManager.Instance.saveGameDataManager.HasWavesFinished() == 0 && Ride.Instance.rideActivation.interactable == false)
        {
            InGameUIManager.Instance.dialogueUI.DisplayNextDialogue();
            Ride.Instance.rideActivation.interactable = true;
            InGameUIManager.Instance.SetWalkieTalkieQuestLog(activateGenTranslated[LocalizationManager.Instance.localeIndex]);
            escapeInShop.SetActive(true);
        }
    }

    public void AddAndCheckShotSigns()
    {
        shotSigns++;
        
        if (shotSigns == shotSignsToGoAhead)
        {
            InGameUIManager.Instance.dialogueUI.SetWalkieTalkieTextBoxAnimation(true, true);
            InGameUIManager.Instance.SetWalkieTalkieQuestLog(fillAmmoTranslated[LocalizationManager.Instance.localeIndex]);
        }
    }
    
    private void PlayStartingDialogue()
    {
        InGameUIManager.Instance.dialogueUI.DisplayNextDialogue();
        playedFirstDialogue = true;
    }
    
    public void GetFirstWeaponAndWalkieTalkie()
    {
        if (!InGameUIManager.Instance.playerHUD.activeSelf)
        {
            InGameUIManager.Instance.playerHUD.SetActive(true);
        
            WeaponObjectSO _brokenPistol = PlayerBehaviour.Instance.weaponBehaviour.allWeaponPrizes.FirstOrDefault(w => w.weaponName == "Broken Pistol");
        
            PlayerBehaviour.Instance.weaponBehaviour.GetWeapon(_brokenPistol);
            PlayerBehaviour.Instance.weaponBehaviour.weaponSlot.SetActive(true);
            
            InGameUIManager.Instance.CloseShop();

            InGameUIManager.Instance.dialogueUI.SetWalkieTalkieTextBoxAnimation(true, true);   
        }
    }
}
