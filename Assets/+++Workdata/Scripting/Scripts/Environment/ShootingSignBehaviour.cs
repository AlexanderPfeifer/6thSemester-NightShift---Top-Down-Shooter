using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class ShootingSignBehaviour : MonoBehaviour
{
    [HideInInspector] public bool canGetHit = true;
    public bool isOnlyShootable;
    [SerializeField] private float shrinkYSizeOnShot = .6f;
    [SerializeField] string sortingLayerOnShot;

    public IEnumerator SnapDownOnHit()
    {
        canGetHit = false;

        float _scale = transform.localScale.x;

        transform.localScale = new Vector3(_scale, shrinkYSizeOnShot, _scale);

        GetComponent<SpriteRenderer>().sortingLayerName = sortingLayerOnShot;

        if (TutorialManager.Instance.playedFirstDialogue)
        {
            
        }

        TutorialManager.Instance.AddAndCheckShotSigns();

        yield return new WaitForSeconds(1);
        
        //transform.localScale = new Vector3(_scale, _scale, _scale);

        //canGetHit = true;
    }
}
