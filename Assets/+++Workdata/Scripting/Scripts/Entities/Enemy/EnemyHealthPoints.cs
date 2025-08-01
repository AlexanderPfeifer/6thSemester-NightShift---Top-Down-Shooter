using System.Collections;
using UnityEngine;

public class EnemyHealthPoints : MonoBehaviour
{
    [Header("HealthPoints")]
    [SerializeField] private int maximumHitPoints = 5;
    private float currentHitPoints;

    [Header("Knock Back")] 
    [SerializeField] private float knockBackTime = .15f;

    [Header("Follow")]
    public bool followsPlayerOnBeingShot;

    private void Start()
    {
        currentHitPoints = maximumHitPoints;
    }

    public void TakeDamage(float damageAmount, Transform bulletTransform)
    {
        currentHitPoints -= damageAmount;
        
        var _enemy = gameObject.GetComponent<EnemyBase>();
        StartCoroutine(_enemy.HitVisual());

        if (currentHitPoints <= 0)
        {
            Destroy(gameObject);
            return;
        }

        if (followsPlayerOnBeingShot && !PlayerBehaviour.Instance.gotHit)
        {
            _enemy.target = PlayerBehaviour.Instance.transform;
            _enemy.currentSpeed = _enemy.speedOnHit;
        }

        if (bulletTransform != null)
        {
            var _enemyShotParticles = Instantiate(_enemy.enemyShotConfetti, transform.position, Quaternion.identity, Ride.Instance.enemyParent.transform);
            _enemyShotParticles.transform.localRotation = bulletTransform.localRotation;
            _enemyShotParticles.Play();
        }
    }
    
    public IEnumerator EnemyKnockBack(float bulletFlyingTime, Vector2 travelDirection)
    {
        float _knockBackWithEnemyResistance = Mathf.Max(PlayerBehaviour.Instance.weaponBehaviour.currentEnemyKnockBack - bulletFlyingTime - GetComponent<EnemyBase>().knockBackResistance, 0);
        GetComponent<Rigidbody2D>().AddForce(travelDirection * _knockBackWithEnemyResistance, ForceMode2D.Impulse);

        gameObject.GetComponent<EnemyBase>().currentSpeed = 0;

        yield return new WaitForSeconds(knockBackTime);

        gameObject.GetComponent<EnemyBase>().currentSpeed = gameObject.GetComponent<EnemyBase>().speedOnHit;
    }
}
