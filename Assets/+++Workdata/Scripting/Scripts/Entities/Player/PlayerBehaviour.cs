using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class PlayerBehaviour : Singleton<PlayerBehaviour>
{
    [Serializable]
    public class PlayerSaveData
    {
        public Dictionary<string, SavableVector3> PositionBySceneName = new();
    }
    [SerializeField] private PlayerSaveData playerSaveData;
    
    [Header("References")] 
    [HideInInspector] public WeaponBehaviour weaponBehaviour;
    [HideInInspector] public AbilityBehaviour abilityBehaviour;
    [HideInInspector] public PlayerCurrency playerCurrency;

    [Header("CharacterMovement")]
    [SerializeField] private float maxSprintTime;
    [SerializeField] private Image sprintBarFill;
    [SerializeField] private GameObject sprintBarImage;
    private float currentSprintTime;
    public float baseMoveSpeed = 6.25f;
    [SerializeField] private float sprintSpeed;
    public float slowDownSpeed;
    [HideInInspector] public float currentMoveSpeed;
    private Vector2 moveDirection = Vector2.down;
    private Rigidbody2D rb;

    [Header("Hit")]
    [SerializeField] private float hitVisualTime = .05f;
    [SerializeField] private float hitVignetteFadeOutTime = .2f;
    [SerializeField] private Volume hitVignette;
    [SerializeField] private float stunTimeOnEnemyCollision = 3;
    [HideInInspector] public bool gotHit;
    [SerializeField] private float knockBackDecay = 5f;
    [SerializeField] private Material hitWhiteMaterial;
    [SerializeField] private Material standartMaterial;

    [Header("Visuals")]
    public GameObject playerVisual;
    [SerializeField] private Animator anim;
    [SerializeField] private Animator animNoHand;
    public GameObject playerNoHandVisual;

    [Header("Light")] 
    [SerializeField] public GameObject globalLightObject;

    [Header("Interaction")]
    [SerializeField] private float interactRadius = 2;
    [SerializeField] private float dropRadius = 2;
    [SerializeField] public LayerMask shopLayer;
    [SerializeField] public LayerMask generatorLayer;
    [SerializeField] public LayerMask duckLayer;
    [SerializeField] private LayerMask collectibleLayer;
    [SerializeField] private LayerMask rideLayer;
    [SerializeField] private LayerMask ammoLayer;
    [SerializeField] private LayerMask currencyLayer;
    private bool isPlayerBusy;
    public TextMeshProUGUI ammoText;

    [Header("InteractableHighlight")] 
    [SerializeField] private SpriteRenderer generatorSpriteRenderer;
    [SerializeField] private SpriteRenderer shopSpriteRenderer;
    [SerializeField] private SpriteRenderer rideSpriteRenderer;
    [SerializeField] private Sprite generatorSprite;
    [SerializeField] private Sprite shopSprite;
    [SerializeField] private Sprite rideSprite;
    [SerializeField] private Sprite generatorSpriteHighlight;
    [SerializeField] private Sprite shopSpriteHighlight;
    [SerializeField] private Sprite rideSpriteHighlight;

    private void SetupFromData()
    {
        if (playerSaveData.PositionBySceneName.TryGetValue(gameObject.scene.name, out var _position))
            transform.position = _position;
    }

    protected override void Awake()
    {
        base.Awake();

        var _currentPlayerSaveData = GameSaveStateManager.Instance.saveGameDataManager.newPlayerSaveData;
        if (_currentPlayerSaveData != null)
        {
            playerSaveData = _currentPlayerSaveData;
            SetupFromData();
        }
        
        GameSaveStateManager.Instance.saveGameDataManager.newPlayerSaveData = playerSaveData;
    }

    private void OnEnable()
    {
        GameInputManager.Instance.OnInteractAction += GameInputManagerOnInteractAction;
        GameInputManager.Instance.OnSprinting += GameInputManagerOnSprintingAction;
        GameInputManager.Instance.OnNotSprinting += GameInputManagerOnNotSprintingAction;
        AudioManager.Instance.Play("InGameMusic");
    }

    private void OnDisable()
    {
        GameInputManager.Instance.OnInteractAction -= GameInputManagerOnInteractAction;
        GameInputManager.Instance.OnSprinting -= GameInputManagerOnSprintingAction;
        GameInputManager.Instance.OnNotSprinting -= GameInputManagerOnNotSprintingAction;
    }

    private void Start()
    {
        currentSprintTime = maxSprintTime;
        weaponBehaviour = GetComponentInChildren<WeaponBehaviour>();
        abilityBehaviour = GetComponentInChildren<AbilityBehaviour>();
        SceneManager.Instance.loadingScreenAnim.SetTrigger("End");
        AudioManager.Instance.FadeIn("InGameMusic");
        rb = GetComponent<Rigidbody2D>();
        currentMoveSpeed = baseMoveSpeed;
        playerCurrency = GetComponent<PlayerCurrency>();
        standartMaterial = playerNoHandVisual.GetComponent<SpriteRenderer>().material;
        if(!GameInputManager.Instance.mouseIsLastUsedDevice)
        {
            GameInputManager.Instance.OnInputDeviceChanged(false);
        }


        if (DebugMode.Instance != null)
        {
            if (DebugMode.Instance.activateRide)
            {
                InGameUIManager.Instance.generatorUI.StartGenerator(FindAnyObjectByType<RideActivation>());

                transform.position = new Vector3(36, 36, 0);   
            }

            DebugMode.Instance.GetDebuggedWeapon(DebugMode.Instance.equipWeapon);

            DebugMode.Instance.GetDebugWeapon();
            
            InGameUIManager.Instance.playerHUD.SetActive(true);

            playerCurrency.currencyBackground.gameObject.SetActive(true);
            
            playerCurrency.AddCurrency(DebugMode.Instance.currencyAtStart, true);
        }
    }

    private void Update()
    {
        HandleInteractionSpriteSwitch();
        UpdateSprintTime();
        UpdatePickUpDrops();
    }

    private void UpdatePickUpDrops()
    {
        if (GetDropInRange(currencyLayer, out Collider2D _currency))
        {
            playerCurrency.AddCurrency(_currency.gameObject.GetComponent<CurrencyDrop>().currencyCount, false);
            Destroy(_currency.gameObject);
        }
        else if (GetDropInRange(ammoLayer, out Collider2D _ammo))
        {
            weaponBehaviour.ObtainAmmoDrop(_ammo.gameObject.GetComponent<AmmoDrop>(), 0, false);
        }
        else if (!GetDropInRange(ammoLayer, out _) && !ammoText.text.Contains(weaponBehaviour.noAmmoString))
        {
            ammoText.text = "";
        }
    }

    private void FixedUpdate()
    {
        HandleMovementFixedUpdate();
    }
    
    private void LateUpdate()
    {
        /*
        we have to save the current position dependant on the scene the player is in.
        this way, the position can be retained across multiple scenes, and we can switch back and forth.
        */
        playerSaveData.PositionBySceneName[gameObject.scene.name] = transform.position;
        
        SetAnimationParameterLateUpdate();
    }

    private void GameInputManagerOnInteractAction(object sender, EventArgs e)
    {
        if (GetInteractionObjectInRange(shopLayer, out _))
        {
            InGameUIManager.Instance.OpenShop();
        }
        else if (GetInteractionObjectInRange(generatorLayer, out Collider2D _generator))
        {
            if (_generator.GetComponent<RideActivation>().interactable)
            {
                InGameUIManager.Instance.SetGeneratorUI();
                GameSaveStateManager.Instance.SaveGame();
            }
        }
        else if (GetInteractionObjectInRange(rideLayer, out Collider2D _ride))
        {
            if (_ride.TryGetComponent(out Ride ride) && !ride.waveStarted && !ride.rideActivation.interactable)
            {
                ride.rideActivation.gateAnim.SetBool("OpenGate", false);
                ride.rideActivation.SetUpFightArena();
            }
        }
        else if (GetInteractionObjectInRange(duckLayer, out _))
        {
            AudioManager.Instance.Play("DuckSound");
        }
    }    

    private void GameInputManagerOnSprintingAction(object sender, EventArgs e)
    {
        if(gotHit)
        {
            return;
        }

        if(currentMoveSpeed == baseMoveSpeed)
        {
            currentMoveSpeed = sprintSpeed;
        }
    }

    private void GameInputManagerOnNotSprintingAction(object sender, EventArgs e)
    {
        if (gotHit)
        {
            return;
        }

        if (currentMoveSpeed == sprintSpeed)
        {
            currentMoveSpeed = baseMoveSpeed;
        }
    }

    public void StartHitVisual()
    {
        if (gotHit)
            return;

        StartCoroutine(HitStop());
    }
 
    private IEnumerator HitStop()
    {
        gotHit = true;

        playerNoHandVisual.GetComponent<SpriteRenderer>().material = hitWhiteMaterial;
        AudioManager.Instance.Play("Paralyze");
        hitVignette.weight = 1;
        Time.timeScale = 0f;

        yield return new WaitForSecondsRealtime(hitVisualTime);
        
        Time.timeScale = 1f;
        playerNoHandVisual.GetComponent<SpriteRenderer>().material = standartMaterial;
        currentMoveSpeed = slowDownSpeed;

        var _elapsedTime = 0f;

        foreach (Transform _children in Ride.Instance.enemyParent.transform)
        {
            if (_children.TryGetComponent(out EnemyBase _enemyBase))
            {
                _enemyBase.target = Ride.Instance.transform;
            }
        }
        
        yield return new WaitForSeconds(stunTimeOnEnemyCollision);
        
        currentMoveSpeed = baseMoveSpeed;
        
        _elapsedTime = 0;
        
        while (_elapsedTime < hitVignetteFadeOutTime)
        {
            hitVignette.weight = Mathf.Lerp(hitVignette.weight, 0, _elapsedTime / hitVignetteFadeOutTime);
        
            _elapsedTime += Time.deltaTime;
            yield return null;
        }

        gotHit = false;
    }

    private void HandleMovementFixedUpdate()
    {
        if (IsPlayerBusy() || InGameUIManager.Instance.dialogueUI.walkieTalkieText == null ||
            InGameUIManager.Instance.dialogueUI.walkieTalkieText.gameObject.activeSelf || 
            TutorialManager.Instance.isExplainingCurrencyDialogue) 
            return;
        
        rb.linearVelocity = GameInputManager.Instance.GetMovementVectorNormalized() * currentMoveSpeed + weaponBehaviour.currentKnockBack;

        weaponBehaviour.currentKnockBack = Vector2.Lerp(weaponBehaviour.currentKnockBack, Vector2.zero, Time.fixedDeltaTime * knockBackDecay);
    }

    private void UpdateSprintTime()
    {
        float previousSprintTime = currentSprintTime;

        if (!gotHit)
        {
            if (currentSprintTime <= 0)
            {
                currentMoveSpeed = baseMoveSpeed;
            }

            if (currentMoveSpeed >= sprintSpeed)
            {
                currentSprintTime -= Time.deltaTime;
                sprintBarFill.fillAmount = currentSprintTime / maxSprintTime;
            }
            else if (currentMoveSpeed == baseMoveSpeed && currentSprintTime < maxSprintTime)
            {
                currentSprintTime += Time.deltaTime / 2;
                sprintBarFill.fillAmount = currentSprintTime / maxSprintTime;
            }
        }

        if (Mathf.Abs(currentSprintTime - previousSprintTime) > Mathf.Epsilon)
        {
            sprintBarImage.SetActive(true);
        }
        else
        {
            if (!gotHit)
            {
                sprintBarImage.SetActive(false);
            }
        }
    }

    private void SetAnimationParameterLateUpdate()
    {
        if ((IsPlayerBusy() && !InGameUIManager.Instance.dialogueUI.IsDialoguePlaying() && playerNoHandVisual.activeSelf) || TutorialManager.Instance.isExplainingCurrencyDialogue)
        {
            animNoHand.SetFloat("MoveSpeed", 0);

            return;
        }

        if (!playerNoHandVisual.activeSelf)
        {
            anim.SetFloat("MoveSpeed", rb.linearVelocity.sqrMagnitude);
            moveDirection = GameInputManager.Instance.GetMovementVectorNormalized();
            anim.SetFloat("MoveDirX", moveDirection.x);
            anim.SetFloat("MoveDirY", moveDirection.y);
        }
        else
        {
            if (GameInputManager.Instance.GetMovementVectorNormalized().sqrMagnitude <= 0.01f && weaponBehaviour.bulletsPerShot <= 1)
            {
                weaponBehaviour.currentBulletDirectionSpread = weaponBehaviour.bulletDirectionSpreadStandingStill;
            }
            else
            {
                weaponBehaviour.currentBulletDirectionSpread = weaponBehaviour.bulletDirectionSpread;
            }
            
            var _snapAngle = weaponBehaviour.LastSnappedAngle;
            animNoHand.SetBool("MovingUp", _snapAngle is >= 337.5f or <= 22.5f); //right
            animNoHand.SetBool("MovingSideWaysNoHand", _snapAngle is > 225f and < 337.5f);
            animNoHand.SetBool("MovingDown", _snapAngle is >= 157.5f and <= 225f);
            animNoHand.SetBool("MovingSideWaysHand", _snapAngle is > 22.5f and < 157.5f); //left

            animNoHand.SetFloat("MoveSpeed", rb.linearVelocity.sqrMagnitude);   
        }
    }    

    private void HandleInteractionSpriteSwitch()
    {
        shopSpriteRenderer.sprite = GetInteractionObjectInRange(shopLayer, out _) ? shopSpriteHighlight : shopSprite;

        if (GetInteractionObjectInRange(generatorLayer, out Collider2D _generator))
        {
            if (_generator.TryGetComponent(out RideActivation _rideActivation) && _rideActivation.interactable)
            {
                generatorSpriteRenderer.sprite = generatorSpriteHighlight;
            }
        }
        else
        {
            generatorSpriteRenderer.sprite = generatorSprite;
        }

        if (GetInteractionObjectInRange(rideLayer, out Collider2D _ride))
        {
            if (_ride.TryGetComponent(out Ride _rideBehaviour) && !_rideBehaviour.waveStarted && !_rideBehaviour.rideActivation.interactable)
            {
                rideSpriteRenderer.sprite = rideSpriteHighlight;
            }
        }
        else
        {
            rideSpriteRenderer.sprite = null;
        }
    }
    
    public bool GetInteractionObjectInRange(LayerMask layer, out Collider2D interactable)
    {
        interactable = Physics2D.OverlapCircle(transform.position, interactRadius, layer);
        return interactable != null;
    }

    private bool GetDropInRange(LayerMask layer, out Collider2D drop)
    {
        drop = Physics2D.OverlapCircle(transform.position, dropRadius, layer);
        return drop != null;
    }

    public void SetPlayerBusy(bool isBusy)
    {
        currentMoveSpeed = baseMoveSpeed;

        isPlayerBusy = isBusy;
    }

    public bool IsPlayerBusy()
    {
        return isPlayerBusy;
    }    

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, interactRadius);

        Gizmos.DrawWireSphere(transform.position, dropRadius);
    }
}