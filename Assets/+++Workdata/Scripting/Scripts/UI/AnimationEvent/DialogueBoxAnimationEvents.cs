using UnityEngine;

public class DialogueBoxAnimationEvents : MonoBehaviour
{
    public void ShowDialogue()
    {
        InGameUIManager.Instance.dialogueUI.DisplayNextDialogue();
    }
}
