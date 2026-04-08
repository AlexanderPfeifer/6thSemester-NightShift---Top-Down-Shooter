using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AllButtonsConfiguration : Singleton<AllButtonsConfiguration>
{
    private void Start()
    {
        foreach (Button _button in FindObjectsByType<Button>(FindObjectsInactive.Include))
        {
            AddHoverEvent(_button.gameObject);
            _button.onClick.AddListener(() => AudioManager.Instance.Play("ButtonClick"));
        }

        foreach (Button _button in FindObjectsByType<Button>(FindObjectsInactive.Include))
        {
            AddHoverEvent(_button.gameObject);
            _button.onClick.AddListener(() => AudioManager.Instance.Play("ButtonClick"));
        }
    }

    public void AddHoverEvent(GameObject buttonObject)
    {
        if (!TryGetComponent(out EventTrigger _eventTrigger))
        {
            _eventTrigger = buttonObject.AddComponent<EventTrigger>();
        }

        EventTrigger.Entry _entry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerEnter
        };

        _entry.callback.AddListener(_ => { OnHover(buttonObject); });
        _eventTrigger.triggers.Add(_entry);
    }

    private void OnHover(GameObject buttonObject)
    {
        GameInputManager.Instance.SetNewButtonAsSelected(buttonObject);
    }
}
