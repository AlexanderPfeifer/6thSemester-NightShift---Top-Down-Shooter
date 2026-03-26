using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GeneratorUI : MonoBehaviour
{
    [Header("Acceleration")]
    [SerializeField] private AnimationCurve accelerationCurve;
    [SerializeField] private float acceleration = .375f;
    [SerializeField] private GameObject firstGeneratorSelected;
    private float finalAcc;

    [Header("Fill")]
    [HideInInspector] public bool changeFill = true;
    [SerializeField] private float activateGeneratorFillAmount;
    [SerializeField] private Image generatorFillImage;
    [SerializeField] private float reduceFillSpeedMultiplier;
    private float fillTime;
    private bool resetDone = true;

    [Header("Button")] 
    [SerializeField] private Image buttonSpriteRenderer;
    [SerializeField] private Sprite buttonOn;
    [SerializeField] private Sprite buttonOff;

    private void OnEnable()
    {
        PlayerBehaviour.Instance.SetPlayerBusy(true);
        generatorFillImage.fillAmount = 0;
        GameInputManager.Instance.SetNewButtonAsSelected(firstGeneratorSelected);
        buttonSpriteRenderer.sprite = buttonOff;
    }

    private void Update()
    {
        if (changeFill)
        {
            SliderFillOverTime();
        }
    }

    private void SliderFillOverTime()
    {
        if (resetDone)
        {
            finalAcc = acceleration * accelerationCurve.Evaluate(generatorFillImage.fillAmount);
            fillTime += finalAcc * Time.deltaTime;

            generatorFillImage.fillAmount = Mathf.PingPong(fillTime, 1);
        }
        
        if (generatorFillImage.fillAmount > activateGeneratorFillAmount)
        {
            buttonSpriteRenderer.sprite = buttonOn;
        }
        else
        {
            buttonSpriteRenderer.sprite = buttonOff;
        }
    }

    public void StartGeneratorEngine()
    {
        AudioManager.Instance.Play("GeneratorButtonClickDown");

        if (generatorFillImage.fillAmount > activateGeneratorFillAmount)
        {
            if (PlayerBehaviour.Instance.GetInteractionObjectInRange(PlayerBehaviour.Instance.generatorLayer, out Collider2D _generator))
            {
                StartGenerator(_generator.GetComponent<RideActivation>());
            }

            gameObject.SetActive(false);
        }
        else
        {
            StartCoroutine(SmoothlyReduceFill(1f)); // Adjust duration as needed
        }
    }

    public void StartGenerator(RideActivation _generator)
    {
        _generator.gateAnim.SetBool("OpenGate", true);
        _generator.interactable = false;
        InGameUIManager.Instance.SetWalkieTalkieQuestLog(TutorialManager.Instance.activateRideTranslated[LocalizationManager.Instance.localeIndex]);
        Ride.Instance.currentRideHealth = Ride.Instance.maxRideHealth;
        Ride.Instance.rideHealthFill.fillAmount = Ride.Instance.currentRideHealth / Ride.Instance.maxRideHealth;
        Ride.Instance.canWinGame = false;
        Ride.Instance.ResetRide();
        Ride.Instance.rideActivation.interactable = false;
    }
    
    IEnumerator SmoothlyReduceFill(float duration)
    {
        resetDone = false;

        float _startFillTime = fillTime;
        float _startFillAmount = generatorFillImage.fillAmount;
        float _elapsedTime = 0f;

        while (_elapsedTime < duration)
        {
            _elapsedTime += Time.deltaTime * reduceFillSpeedMultiplier;
            float _t = _elapsedTime / duration; 

            fillTime = Mathf.Lerp(_startFillTime, 0, _t);
            generatorFillImage.fillAmount = Mathf.Lerp(_startFillAmount, 0, _t);

            yield return null; 
        }

        fillTime = 0;
        generatorFillImage.fillAmount = 0;
        resetDone = true;
    }

    public void GeneratorButtonUp()
    {
        AudioManager.Instance.Play("GeneratorButtonClickUp");
    }

    private void OnDisable()
    {
        PlayerBehaviour.Instance.SetPlayerBusy(false);
        fillTime = 0;
        generatorFillImage.fillAmount = 0;
    }
}
