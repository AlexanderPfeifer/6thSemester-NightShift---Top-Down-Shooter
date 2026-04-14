using UnityEngine;

public class CurrencyDrop : MonoBehaviour
{
    [HideInInspector] public int currencyCount;

    [SerializeField] private GameObject pickUpCurrencyParticles;

    private void OnDestroy()
    {
        var _pickUpParticles = Instantiate(pickUpCurrencyParticles, PlayerBehaviour.Instance.transform.position, Quaternion.identity, Ride.Instance.transform);
        _pickUpParticles.GetComponent<ParticleSystem>().Play();
    }
}
