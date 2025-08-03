using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using Random = UnityEngine.Random;

public class MeleeWeaponBehaviour : MonoBehaviour
{
    [Header("Delays")]
    [SerializeField] private float maxHitDelay;
    [HideInInspector] public float currentHitDelay;
    [Tooltip("The swing time has 3 phases - swinging back to get force, applying force, swinging back")]
    [SerializeField] private float[] swingTime;

    [Header("WeaponOut")]
    [HideInInspector] public bool meleeWeaponOut;

    [Header("Visuals")]
    [SerializeField] private float onHitScreenShakeValue = 3.5f;
    [SerializeField] private Sprite meleeWeapon;

    [Header("Gameplay")]
    [HideInInspector] public CapsuleCollider2D hitCollider;
    [SerializeField] private float damage;
    public float knockBack = 5;

    private WeaponBehaviour weaponBehaviour;

    private void OnEnable()
    {
        GameInputManager.Instance.OnMeleeWeaponAction += OnPressingMeleeAction;
    }

    private void OnDisable()
    {
        GameInputManager.Instance.OnMeleeWeaponAction -= OnPressingMeleeAction;
    }

    private void Start()
    {
        hitCollider = GetComponent<CapsuleCollider2D>();
        weaponBehaviour = GetComponent<WeaponBehaviour>();
        GetMeleeWeaponOut();
    }

    private void OnPressingMeleeAction(object sender, EventArgs eventArgs)
    {
        GetMeleeWeaponOut();

        HitAutomatic();
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col.gameObject.TryGetComponent(out EnemyHealthPoints _enemyHealthPoints))
        {
            WeaponVisualEffects();    
            _enemyHealthPoints.TakeDamage(damage, null);
            _enemyHealthPoints.StartCoroutine(_enemyHealthPoints.EnemyKnockBack(0, _enemyHealthPoints.transform.position - transform.position));
        }
        else if (col.TryGetComponent(out ShootingSignBehaviour _shootingSignBehaviour))
        {
            if (_shootingSignBehaviour.canGetHit && !_shootingSignBehaviour.isOnlyShootable)
            {
                WeaponVisualEffects();
                _shootingSignBehaviour.StartCoroutine(_shootingSignBehaviour.SnapDownOnHit());
            }
        }
    }

    private void WeaponVisualEffects()
    {
        AudioManager.Instance.Play("BatonHit");
        StartCoroutine(WeaponVisualCoroutine());
    }
    
    public IEnumerator WeaponVisualCoroutine()
    {
        PlayerBehaviour.Instance.weaponBehaviour.playerCam.GetComponent<CinemachineBasicMultiChannelPerlin>().AmplitudeGain = onHitScreenShakeValue;
        
        //AudioManager.Instance.Play("Shooting");
        
        yield return new WaitForSeconds(.1f);
        
        PlayerBehaviour.Instance.weaponBehaviour.playerCam.GetComponent<CinemachineBasicMultiChannelPerlin>().AmplitudeGain = 0;
    }

    public void SetMeleeWeaponTakeOut()
    {
        meleeWeaponOut = false;
    }

    public void GetMeleeWeaponOut()
    {
        weaponBehaviour.weapon.GetComponent<SpriteRenderer>().sprite = meleeWeapon;
        meleeWeaponOut = true;
    }

    public void HitAutomatic()
    {
        if (currentHitDelay > 0 || PlayerBehaviour.Instance.IsPlayerBusy() || InGameUIManager.Instance.dialogueUI.IsDialoguePlaying())
            return;
        weaponBehaviour.currentEnemyKnockBack = knockBack;

        StartCoroutine(MeleeWeaponSwingCoroutine());

        currentHitDelay = maxHitDelay;
    }

    IEnumerator MeleeWeaponSwingCoroutine()
    {
        var _steps = new[]
        {
            (-50f, Random.Range(swingTime[0] - .01f, swingTime[0] + .01f)),
            (230f, Random.Range(swingTime[1] - .01f, swingTime[1] + .01f)),
            (-170f, Random.Range(swingTime[2] - .01f, swingTime[2] + .01f)),
        };

        TrailRenderer _trailRendererBaton = GetComponentInChildren<TrailRenderer>();

        for (var _index = 0; _index < _steps.Length; _index++)
        {
            if (_index == 1)
            {
                _trailRendererBaton.emitting = true;
                GetComponent<MeleeWeaponBehaviour>().hitCollider.enabled = true;
            }

            (float _deltaZ, float _duration) = _steps[_index];
            float _elapsed = 0f;

            float _startZ = transform.localRotation.eulerAngles.z;
            float _targetZ = _startZ + _deltaZ;

            float _startZBaton = _trailRendererBaton.transform.localRotation.eulerAngles.z;
            float _swingTargetZBaton = _startZBaton - 90;
            float _pullBackZBaton = _startZBaton + 90;

            while (_elapsed < _duration)
            {
                _elapsed += Time.deltaTime;

                float _currentZ = Mathf.Lerp(_startZ, _targetZ, _elapsed / _duration);
                transform.localRotation = Quaternion.Euler(0f, 0f, _currentZ);

                switch (_index)
                {
                    case 0:
                        {
                            float _currentZBat = Mathf.Lerp(_startZBaton, _swingTargetZBaton, _elapsed / _duration);
                            _trailRendererBaton.transform.localRotation = Quaternion.Euler(0f, 0f, _currentZBat);

                            AudioManager.Instance.Play("BatonSwing");
                            break;
                        }
                    case 2:
                        {
                            float _currentZBat = Mathf.Lerp(_startZBaton, _pullBackZBaton, _elapsed / _duration);
                            _trailRendererBaton.transform.localRotation = Quaternion.Euler(0f, 0f, _currentZBat);
                            break;
                        }
                }

                yield return null;
            }

            transform.localRotation = Quaternion.Euler(0f, 0f, _targetZ);
        }

        _trailRendererBaton.Clear();
        _trailRendererBaton.emitting = false;

        GetComponent<MeleeWeaponBehaviour>().hitCollider.enabled = false;

        weaponBehaviour.currentEnemyKnockBack = weaponBehaviour.enemyShootingKnockBack;

        SetMeleeWeaponTakeOut();
    }
}
