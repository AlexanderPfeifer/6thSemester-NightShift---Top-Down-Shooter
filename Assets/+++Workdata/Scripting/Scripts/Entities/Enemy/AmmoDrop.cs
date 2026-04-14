using UnityEngine;

public class AmmoDrop : MonoBehaviour
{
    [HideInInspector] public int ammoCount;

    [SerializeField] private GameObject pickUpBulletParticles;

    private void OnDestroy()
    {
        var _pickUpParticles = Instantiate(pickUpBulletParticles, PlayerBehaviour.Instance.transform.position, Quaternion.identity, Ride.Instance.transform);
        _pickUpParticles.GetComponent<ParticleSystem>().Play();
    }
}
