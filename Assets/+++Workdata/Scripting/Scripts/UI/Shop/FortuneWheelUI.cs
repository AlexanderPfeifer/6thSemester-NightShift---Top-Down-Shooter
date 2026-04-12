using System.Collections;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class FortuneWheelUI : MonoBehaviour
{
    [Header("Price Settings")] 
    [SerializeField] private UnityEvent[] prizes;
    [Tooltip("Starts higher because the first time the probability of winning should be higher")]
    [SerializeField] private int spinPrice;
    [FormerlySerializedAs("weaponPrizePlaces")] [SerializeField] private int[] winPrizePlaces = { 0, 9, 13, 18, 22 };
    [SerializeField] private int spinCounter;
    [SerializeField] private AnimationCurve winProbability;
    [SerializeField] private int maxSpinsUntilWin;
    private int priceIndex;

    [Header("Prize Visual")]
    [SerializeField] private Animator newWeaponAnim;
    [SerializeField] private Image weaponImage;
    [SerializeField] private TextMeshProUGUI weaponName;

    [Header("Spinning Movement")] 
    [SerializeField] private Vector2Int timeUntilStop;
    [SerializeField] private Vector2Int fullSpinsUntilStop;
    public Rigidbody2D rb;
    [HideInInspector] public bool receivingPrize;
    [SerializeField] private Image mark;
    [SerializeField] private float easeOutStrength = 3f;
    
    [Header("EventSystem Controlling")]
    [SerializeField] private GameObject firstFortuneWheelButtonSelected;

    private void OnEnable()
    {
        GameInputManager.Instance.SetNewButtonAsSelected(firstFortuneWheelButtonSelected);
        
        mark.transform.localScale = new Vector3(1, 1, 1);
    }

    private void Start()
    {
        rb.transform.eulerAngles = new Vector3(0, 0, Random.Range(0, 360));
    }

    private IEnumerator WheelOverTimeCoroutine()
    {        
        int _randomPrize = Random.Range(0, prizes.Length);
        if (Random.value < winProbability.Evaluate((float)spinCounter / maxSpinsUntilWin))
        {
            _randomPrize = winPrizePlaces[Random.Range(0, winPrizePlaces.Length)];
        }
        
        float _startRotation = rb.rotation % 360f;
        float _pieSize = 360f / prizes.Length;
        float _endRotation = _startRotation + Random.Range(fullSpinsUntilStop.x, fullSpinsUntilStop.y) * 360f + _randomPrize * _pieSize;
        
        float _timeUntilStop = Random.Range(timeUntilStop.x, timeUntilStop.y);
        float _elapsed = 0f;
        while (_elapsed < _timeUntilStop)
        {
            _elapsed += Time.deltaTime;
            float _t = 1f - Mathf.Pow(1f - _elapsed / _timeUntilStop, easeOutStrength);
            float _currentRotation = Mathf.Lerp(_startRotation, _endRotation, _t);

            int GetPrizeIndex(float rotation)
            {
                float normalized = (rotation % 360f + 360f) % 360f;
                return Mathf.FloorToInt(normalized / _pieSize) % prizes.Length;
            }

            int oldIndex = GetPrizeIndex(rb.rotation);

            rb.MoveRotation(_currentRotation);

            int newIndex = GetPrizeIndex(_currentRotation);

            if (oldIndex != newIndex)
            {
                AudioManager.Instance.Play("WheelPrizeChanged");
            }

            yield return null;
        }

        rb.MoveRotation(_endRotation);

        spinCounter++;

        priceIndex = Mathf.FloorToInt((rb.transform.eulerAngles.z) / _pieSize) % prizes.Length;
        StartCoroutine(LocationHighlight());
        
        receivingPrize = true;
    }
    
    public void SpinWheel()
    {
        if (rb.angularVelocity > 0 || receivingPrize || !PlayerBehaviour.Instance.playerCurrency.SpendCurrency(spinPrice)) 
            return;

        InGameUIManager.Instance.inGameUICanvasGroup.interactable = false;
        StartCoroutine(WheelOverTimeCoroutine());
    }

    private IEnumerator LocationHighlight()
    {
        for (int i = 0; i < 6; i++)
        {
            mark.transform.localScale = (i % 2 == 0) ? new Vector3(2, 2, 1) : new Vector3(1, 1, 1);
            yield return new WaitForSeconds(0.25f);
        }

        InGameUIManager.Instance.inGameUICanvasGroup.interactable = true;

        var spinButton = firstFortuneWheelButtonSelected.GetComponent<Button>();
        var spinButtonColor = spinButton.colors;
        spinButtonColor.normalColor = spinButtonColor.selectedColor;
        spinButton.colors = spinButtonColor;

        receivingPrize = false;

        prizes[priceIndex]?.Invoke();
    }

    public void WinWeapon(WeaponObjectSO weapon)
    {
        if (!GameSaveStateManager.Instance.saveGameDataManager.HasWeapon(weapon.weaponIdentifier))
        {
            receivingPrize = true;

            InGameUIManager.Instance.inGameUICanvasGroup.interactable = false;

            PlayerBehaviour.Instance.weaponBehaviour.GetWeapon(weapon);
            
            InGameUIManager.Instance.shopUI.ResetWeaponDescriptions();
            
            rb.transform.GetChild(priceIndex).GetComponent<Image>().color = Color.gray;
            rb.transform.GetChild(priceIndex).transform.GetChild(0).GetComponent<Image>().color = Color.gray;
            
            AudioManager.Instance.Play("WeaponWin");

            StartCoroutine(WinWeaponVisual(weapon));

            spinCounter = 0;
        }
        else
        {
            StartCoroutine(InGameUIManager.Instance.dialogueUI.TypeTextCoroutine(InGameUIManager.Instance.dialogueUI.fortuneWheelDialogues[0].languages[LocalizationManager.Instance.localeIndex].dialogues[0], null, InGameUIManager.Instance.dialogueUI.currentTextBox));
        }
    }

    private IEnumerator WinWeaponVisual(WeaponObjectSO weapon)
    {
        newWeaponAnim.gameObject.SetActive(true);

        weaponImage.sprite = weapon.uiWeaponVisual;

        weaponName.text = weapon.weaponNameTranslated[LocalizationManager.Instance.localeIndex];

        newWeaponAnim.SetBool("NewWeaponScreenActive", true);

        while (AudioManager.Instance.IsPlaying("WeaponWin"))
        {
            yield return null;
        }

        yield return new WaitForSeconds(2.5f);

        receivingPrize = false;

        newWeaponAnim.SetBool("NewWeaponScreenActive", false);

        InGameUIManager.Instance.inGameUICanvasGroup.interactable = true;
    }

    public void WinBlank()
    {
        StartCoroutine(InGameUIManager.Instance.dialogueUI.TypeTextCoroutine(InGameUIManager.Instance.dialogueUI.fortuneWheelDialogues[0].languages[LocalizationManager.Instance.localeIndex].dialogues[0], null, InGameUIManager.Instance.dialogueUI.currentTextBox));
        
        AudioManager.Instance.Play("Blank");
    }   
    
    public void WinSmallMoney(int money)
    {
        PlayerBehaviour.Instance.playerCurrency.AddCurrency(money, true);
        
        StartCoroutine(InGameUIManager.Instance.dialogueUI.TypeTextCoroutine(InGameUIManager.Instance.dialogueUI.fortuneWheelDialogues[2].languages[LocalizationManager.Instance.localeIndex].dialogues[0], null, InGameUIManager.Instance.dialogueUI.currentTextBox));
    }  
    
    public void WinMediumMoney(int money)
    {
        PlayerBehaviour.Instance.playerCurrency.AddCurrency(money, true);
        
        StartCoroutine(InGameUIManager.Instance.dialogueUI.TypeTextCoroutine(InGameUIManager.Instance.dialogueUI.fortuneWheelDialogues[1].languages[LocalizationManager.Instance.localeIndex].dialogues[0], null, InGameUIManager.Instance.dialogueUI.currentTextBox));
    }    
    
    public void WinLargeCurrency(int money)
    {
        PlayerBehaviour.Instance.playerCurrency.AddCurrency(money, true);
        
        StartCoroutine(InGameUIManager.Instance.dialogueUI.TypeTextCoroutine(InGameUIManager.Instance.dialogueUI.fortuneWheelDialogues[3].languages[LocalizationManager.Instance.localeIndex].dialogues[0], null, InGameUIManager.Instance.dialogueUI.currentTextBox));
        
        spinCounter = 0;
    }

    private void OnDisable()
    {
        newWeaponAnim.gameObject.SetActive(false);
    }

    [Header("Debugging Stuff")]
    [SerializeField] private float labelRadiusMultiplier = 0.8f;
    //Font size does not change through inspector
    private readonly int labelFontSize = 50;
    
#if UNITY_EDITOR
    private GUIStyle labelStyle;
#endif

    private void OnDrawGizmos()
    {
        RectTransform _rt = rb.GetComponent<RectTransform>();
        Vector3 _center = _rt.position;
        float _radius = _rt.rect.width * _rt.lossyScale.x * 0.5f;
        float _angleStep = 360f / prizes.Length;
        float _imageRotation = _rt.eulerAngles.z;

#if UNITY_EDITOR
        labelStyle ??= new GUIStyle(EditorStyles.label)
        {
            fontSize = labelFontSize,
            normal =
            {
                textColor = Color.white
            },
            alignment = TextAnchor.MiddleCenter
        };
#endif

        Gizmos.color = Color.white;

        for (int _i = 0; _i < prizes.Length; _i++)
        {
            int _currentIndex = _i % prizes.Length;
            //Add 90 degrees to make the fortune wheel start at "12 o'clock" otherwise unity default is pointing to right(3 o'clock)
            float _startAngle = -(_currentIndex * _angleStep) + _imageRotation + 90f;
            float _midAngle = _startAngle - _angleStep / 2f;

            float _radStart = _startAngle * Mathf.Deg2Rad;
            float _radMid = _midAngle * Mathf.Deg2Rad;

            Vector3 _dirStart = new Vector3(Mathf.Cos(_radStart), Mathf.Sin(_radStart), 0f);
            Vector3 _dirMid = new Vector3(Mathf.Cos(_radMid), Mathf.Sin(_radMid), 0f);

            Gizmos.DrawLine(_center, _center + _dirStart * _radius);

#if UNITY_EDITOR
            Vector3 _labelPos = _center + _dirMid * _radius * labelRadiusMultiplier;
            Handles.Label(_labelPos, _currentIndex.ToString(), labelStyle);
#endif
        }
    }
}
