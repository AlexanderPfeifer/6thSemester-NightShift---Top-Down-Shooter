using UnityEngine;

public class SpriteSortingManager : MonoBehaviour
{
    private void Start()
    {
        SpriteRenderer[] _allSprites = FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include);

        foreach (var _sprite in _allSprites)
        {
            if (_sprite.GetComponent<MovingSpritesSorting>() == null && _sprite.sortingLayerName != "Ground") 
            {
                _sprite.sortingOrder = Mathf.RoundToInt(-_sprite.transform.position.y * 100);
            }
        }
    }
}
