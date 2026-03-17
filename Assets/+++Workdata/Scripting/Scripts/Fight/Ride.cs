using System.Collections;
using System.Linq;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class Ride : Singleton<Ride>
{
    [Header("Spawning")] 
    [SerializeField] private Wave[] waves;
    [HideInInspector] public bool waveStarted;
    public GameObject enemyParent;
    [SerializeField] private float radiusInsideGroupSpawning = 2;
    [SerializeField] private Transform spawnCenter;
    [SerializeField] private float squareSizeY;
    [SerializeField] private float squareSizeX;

    [Header("Health")] 
    public Image rideHealthFill;
    public float maxRideHealth;
    [HideInInspector] public float currentRideHealth;

    [Header("HitVisual")]
    [SerializeField] private float hitVisualTime = .05f;
    [SerializeField] private ParticleSystem hitParticles;
    private bool rideGotHit;
    [SerializeField] private float rideLoseScreenShakeStrength = 6;
    [SerializeField] private SpriteRenderer bottomRideRenderer;
    [SerializeField] private SpriteRenderer topRideRenderer;
    [SerializeField] private Material hitWhiteMaterial;
    [SerializeField] private Material standartMaterial;

    [Header("Activation")]
    public GameObject[] rideLight;
    public RideActivation rideActivation;

    [Header("Win")] 
    [SerializeField] private ParticleSystem winConfettiParticles;
    private int currentSpawnedEnemies;
    private int spawnedEnemiesInCluster;
    [HideInInspector] public bool canWinGame;
    public Image[] fuses;
    public Sprite activeFuse;
    public Sprite inActiveFuse;
    public TextMeshProUGUI prizeText;
    [HideInInspector] public Color startColorPrizeText;
    private int countedCurrency;
    private float currentTimeBetweenAddingNumbers;

    [Header("Loose")]
    [SerializeField] private Vector2 restartPosition;
    [SerializeField, TextArea(3, 10)] private string peggyLooseText;

    private void Start()
    {
        startColorPrizeText = prizeText.color;
        InGameUIManager.Instance.dialogueUI.dialogueCountShop = GameSaveStateManager.Instance.saveGameDataManager.HasWavesFinished();
    }

    private void Update()
    {
        if(!waveStarted && prizeText.text != "0" && rideActivation.interactable == true)
        {
            PlayerBehaviour.Instance.playerCurrency.UpdateCurrencyTextNumberByNumber(0, ref countedCurrency, prizeText, ref currentTimeBetweenAddingNumbers);
        }
        else if(prizeText.text == "0")
        {
            prizeText.color = Color.black;
        }
    }

    public void StartEnemyClusterCoroutines()
    {
        if (DebugMode.Instance != null)
        {
            DebugMode.Instance.AddWaves();
        }

        spawnedEnemiesInCluster = 0;
        currentSpawnedEnemies = 0;
        prizeText.color = startColorPrizeText;

        foreach (var _enemyCluster in waves[GameSaveStateManager.Instance.saveGameDataManager.HasWavesFinished()].enemyClusters)
        {            
            spawnedEnemiesInCluster += _enemyCluster.enemyPrefab.Length * _enemyCluster.repeatCount;

            StartCoroutine(SpawnEnemies(_enemyCluster));
        }
    }

    private IEnumerator SpawnEnemies(EnemyClusterData enemyCluster)
    {
        yield return new WaitForSeconds(enemyCluster.spawnStartTime);

        Vector2 _groupPos = GetRandomEdgePosition();

        for (int _i = 0; _i < enemyCluster.repeatCount; _i++)
        {
            foreach (var _enemy in enemyCluster.enemyPrefab)
            {
                if (!enemyCluster.spawnAsGroup)
                {
                    _groupPos = GetRandomEdgePosition();
                }

                Vector2 _randomOffset = Random.insideUnitCircle * radiusInsideGroupSpawning;
                Instantiate(_enemy, _groupPos + new Vector2(_randomOffset.x, _randomOffset.y), Quaternion.identity, enemyParent.transform);
                currentSpawnedEnemies++;
            }

            if (_i != enemyCluster.repeatCount - 1)
                yield return new WaitForSeconds(enemyCluster.timeBetweenSpawns);
        }

        if (spawnedEnemiesInCluster == currentSpawnedEnemies)
        {
            canWinGame = true;
        }
    }
    
    private Vector2 GetRandomEdgePosition()
    {
        float _halfSizeY = squareSizeY / 2f;
        float _halfSizeX = squareSizeX / 2f;
        Vector2 _spawnPos = spawnCenter.position;

        for (int i = 0; i < 10; i++)
        {
            _spawnPos = spawnCenter.position;

            switch (Random.Range(0, 4))
            {
                case 0: // Top Edge
                    _spawnPos += new Vector2(Random.Range(-_halfSizeX, _halfSizeX), _halfSizeY);
                    break;
                case 1: // Right Edge
                    _spawnPos += new Vector2(_halfSizeX, Random.Range(-_halfSizeY, _halfSizeY));
                    break;
                case 2: // Bottom Edge
                    _spawnPos += new Vector2(Random.Range(-_halfSizeX, _halfSizeX), -_halfSizeY);
                    break;
                case 3: // Left Edge
                    _spawnPos += new Vector2(-_halfSizeX, Random.Range(-_halfSizeY, _halfSizeY));
                    break;
            }

            //Check if there is a tree so the player does not see how an enemy is spawned to keep immersion
            Collider2D hit = Physics2D.OverlapPoint(_spawnPos);
            if (hit == null || hit.GetComponent<TreeBehaviour>() == null)
            {
                continue;
            }

            return _spawnPos;
        }

        //We want to spawn anyways, even if the player could see how enemies appear - spawning it is more important
        return _spawnPos;
    }

    public void LostWave()
    {
        if(!waveStarted)
            return;

        AudioManager.Instance.Play("FightMusicLoss");
        
        ResetRide();
        StartCoroutine(LooseVisuals());
    }

    private IEnumerator LooseVisuals()
    {
        PlayerBehaviour.Instance.SetPlayerBusy(true);

        while (AudioManager.Instance.IsPlaying("FightMusicLoss"))
        {
            PlayerBehaviour.Instance.weaponBehaviour.playerCam.GetComponent<CinemachineBasicMultiChannelPerlin>().AmplitudeGain = rideLoseScreenShakeStrength;
            hitParticles.Play();
            yield return new WaitForSeconds(0.75f);
        }

        AudioManager.Instance.Play("RideShutDown");
        foreach (var _light in rideLight)
        {
            _light.SetActive(false);
        }

        var loseAnim = InGameUIManager.Instance.loseShutterAnim;
        loseAnim.SetBool("GoUp", false);

        while (!loseAnim.GetCurrentAnimatorStateInfo(0).IsName("ShopStallShutterDown"))
        {
            yield return null;
        }

        while (loseAnim.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
        {
            yield return null;
        }

        PlayerBehaviour.Instance.weaponBehaviour.playerCam.GetComponent<CinemachineBasicMultiChannelPerlin>().AmplitudeGain = 0;
        PlayerBehaviour.Instance.transform.position = restartPosition;
        prizeText.text = GetCurrentWavePrize().ToString();
        prizeText.color = startColorPrizeText;
        CleanStage();

        //Set player busy false because otherwise the shop won't open 
        PlayerBehaviour.Instance.SetPlayerBusy(false);
        InGameUIManager.Instance.OpenShop();

        loseAnim.SetBool("GoUp", true);

        while (!loseAnim.GetCurrentAnimatorStateInfo(0).IsName("ShopStallShutterUp"))
        {
            yield return null;
        }

        while (loseAnim.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
        {
            yield return null;
        }

        InGameUIManager.Instance.dialogueUI.StopCurrentAndTypeNewTextCoroutine(peggyLooseText, null, InGameUIManager.Instance.dialogueUI.currentTextBox);
    }

    public void WonWave()
    {
        if (!waveStarted)
            return;

        winConfettiParticles.Play();

        foreach (var _balloonCart in FindObjectsByType<BalloonCartBehaviour>(FindObjectsSortMode.None))
        {
            _balloonCart.ResetBalloons();
        }
            
        InGameUIManager.Instance.SetWalkieTalkieQuestLog(TutorialManager.Instance.getNewWeapons);
        
        PlayerBehaviour.Instance.playerCurrency.AddCurrency(GetCurrentWavePrize(), true);
        
        ResetRide();
        
        AudioManager.Instance.Play("FightMusicWon");

        GameSaveStateManager.Instance.saveGameDataManager.AddWaveCount();

        if (TutorialManager.Instance.explainedRideSequences)
        {
            StartCoroutine(PlayRideSoundsAfterOneAnother());
        }
        else
        {
            InGameUIManager.Instance.dialogueUI.SetWalkieTalkieTextBoxAnimation(true, true);
        }
        
        AudioManager.Instance.FadeIn("InGameMusic");

        GameSaveStateManager.Instance.SaveGame();
    }
    
    private IEnumerator PlayRideSoundsAfterOneAnother()
    {
        PlayerBehaviour.Instance.SetPlayerBusy(true);

        while (AudioManager.Instance.IsPlaying("FightMusicWon"))
        {
            yield return null;
        }
        
        AudioManager.Instance.Play("RideShutDown");
        foreach (var _light in rideLight)
        {
            _light.SetActive(false);
        }

        while (AudioManager.Instance.IsPlaying("RideShutDown"))
        {
            yield return null;
        }

        InGameUIManager.Instance.generatorUI.changeFill = false;

        InGameUIManager.Instance.generatorUI.gameObject.SetActive(true);

        yield return new WaitForSeconds (.5f);

        for (int i = 0; i < 6; i++)
        {
            fuses[GameSaveStateManager.Instance.saveGameDataManager.HasWavesFinished() - 2].sprite = (i % 2 == 0) ? DeactivateFuse() : ActivateFuse();
            yield return new WaitForSeconds(0.25f);
        }

        yield return new WaitForSeconds(.5f);

        InGameUIManager.Instance.generatorUI.gameObject.SetActive(false);

        InGameUIManager.Instance.generatorUI.changeFill = true;

        if (InGameUIManager.Instance.dialogueUI.dialogueCountWalkieTalkie < InGameUIManager.Instance.dialogueUI.dialogueWalkieTalkie.Length)
        {
            InGameUIManager.Instance.dialogueUI.SetWalkieTalkieTextBoxAnimation(true, true);
        }

        rideActivation.gateAnim.SetBool("OpenGate", true);

        yield return null;
    }

    public Sprite DeactivateFuse()
    {
        AudioManager.Instance.Play("FuseOff");

        return inActiveFuse;
    }

    public Sprite ActivateFuse()
    {
        AudioManager.Instance.Play("FuseOn");

        return activeFuse;
    }

    public void CleanStage()
    {
        for (int _i = 0; _i < enemyParent.transform.childCount; _i++)
        {
            Transform _child = enemyParent.transform.GetChild(_i);

            if (_child.TryGetComponent(out EnemyBase _enemyBase))
            {
                _enemyBase.addHelpDropsOnDeath = false;
            }
            
            Destroy(_child.gameObject);
        }
    }

    public int GetCurrentWavePrize()
    {
        return Mathf.RoundToInt(waves[GameSaveStateManager.Instance.saveGameDataManager.HasWavesFinished()].currencyPrize * (currentRideHealth / maxRideHealth));
    }

    public void ResetRide()
    {
        StopAllCoroutines();
        Time.timeScale = 1f;
        PlayerBehaviour.Instance.weaponBehaviour.playerCam.GetComponent<CinemachineBasicMultiChannelPerlin>().AmplitudeGain = 0;
        bottomRideRenderer.material = standartMaterial;
        topRideRenderer.material = standartMaterial;
        rideGotHit = false;
        countedCurrency = GetCurrentWavePrize();
        waveStarted = false;
        rideActivation.fightMusic.Stop();
        rideActivation.interactable = true;
        prizeText.text = GetCurrentWavePrize().ToString();
        prizeText.color = startColorPrizeText;
    }

    public void ReceiveDamage(float rideAttackDamage, float screenShakeStrength)
    {
        currentRideHealth -= rideAttackDamage;
        rideHealthFill.fillAmount = currentRideHealth / maxRideHealth;

        prizeText.text = GetCurrentWavePrize().ToString();

        AudioManager.Instance.Play("RideHit");

        StartRideHitVisual(screenShakeStrength);
    }

    public void CheckWin()
    {
        if (currentRideHealth <= 0)
        {
            LostWave();

            prizeText.text = "0";
            prizeText.color = Color.black;
        }
        else if (canWinGame)
        {
            int _enemies = enemyParent.transform.Cast<Transform>().Count(child => child.GetComponent<EnemyBase>());

            if (_enemies <= 1)
            {
                WonWave();
            }
        }
    }

    public void StartRideHitVisual(float screenShakeStrength)
    {
        if (rideGotHit)
            return;

        rideGotHit = true;

        hitParticles.Play();

        PlayerBehaviour.Instance.weaponBehaviour.playerCam.GetComponent<CinemachineBasicMultiChannelPerlin>().AmplitudeGain = screenShakeStrength;

        bottomRideRenderer.material = hitWhiteMaterial;
        topRideRenderer.material = hitWhiteMaterial;

        Time.timeScale = 0f;
        
        StartCoroutine(HitStop());
    }
    
    private IEnumerator HitStop()
    {
        yield return new WaitForSecondsRealtime(hitVisualTime);

        PlayerBehaviour.Instance.weaponBehaviour.playerCam.GetComponent<CinemachineBasicMultiChannelPerlin>().AmplitudeGain = 0;
        Time.timeScale = 1f;
        bottomRideRenderer.material = standartMaterial;
        topRideRenderer.material = standartMaterial;
        rideGotHit = false;
    }
    
    private void OnValidate()
    {
        for (var _i = 0; _i < waves.Length; _i++)
        {
            waves[_i].waveName = "Wave " + _i;

            foreach (var _cluster in waves[_i].enemyClusters)
            {
                _cluster.UpdateClusterName();
            }
        }
    }
    
    private void OnDrawGizmos()
    {
        if (spawnCenter == null) 
            return;

        Vector3 _center = spawnCenter.position;
        float _halfSizeY = squareSizeY / 2;
        float _halfSizeX = squareSizeX / 2;

        Vector3 _topLeft = _center + new Vector3(-_halfSizeX, _halfSizeY, 0);
        Vector3 _topRight = _center + new Vector3(_halfSizeX, _halfSizeY, 0);
        Vector3 _bottomRight = _center + new Vector3(_halfSizeX, -_halfSizeY, 0);
        Vector3 _bottomLeft = _center + new Vector3(-_halfSizeX, -_halfSizeY, -0);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(_topLeft, _topRight);
        Gizmos.DrawLine(_topRight, _bottomRight);
        Gizmos.DrawLine(_bottomRight, _bottomLeft);
        Gizmos.DrawLine(_bottomLeft, _topLeft);
    }
}