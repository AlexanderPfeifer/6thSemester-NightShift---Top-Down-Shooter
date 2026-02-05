using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Serialization;

public class DialogueUI : MonoBehaviour
{
    [Header("DialogueType")]
    [FormerlySerializedAs("dialogue")] public Dialogues[] dialogueShop;
    public Dialogues[] dialogueWalkieTalkie;
    public TextMeshProUGUI shopText;
    [FormerlySerializedAs("dialogueText")] public TextMeshProUGUI walkieTalkieText;
    [HideInInspector] public TextMeshProUGUI currentTextBox;
    [HideInInspector] public int dialogueCountShop;
    [HideInInspector] public int dialogueCountWalkieTalkie;
    [HideInInspector] public int currentDialogueCount;
    
    [Header("Typing Settings")]
    public float maxTextDisplaySpeed = 0.00005f;
    [SerializeField] private float timeBetweenTextSkip = 0.2f;
    private bool isSkippingDialogue;
    
    [Header("Animations")]
    [SerializeField] private Animator radioAnim;
    [FormerlySerializedAs("dialogueBoxAnim")] public Animator walkieTalkieDialogueBoxAnim;
    
    [Header("DialogueState")]
    private int dialogueTextCount;
    [HideInInspector] public DialogueState dialogueState = DialogueState.DialogueNotPlaying;
    
    public enum DialogueState
    {
        DialogueNotPlaying,
        DialoguePlaying,
        DialogueAbleToGoNext,
        DialogueAbleToEnd,
    }
    private void OnEnable()
    {
        GameInputManager.Instance.OnSkipDialogueWithController += GameInputManagerOnSkipDialogueWithControllerAction;
    }
    
    private void GameInputManagerOnSkipDialogueWithControllerAction(object sender, EventArgs e)
    {
        SetDialogueState();
    }

    public void SetCurrentDialogueBox(bool inShop)
    {
        if (inShop)
        {
            currentTextBox = shopText;
            currentDialogueCount = dialogueCountShop;
        }
        else
        {
            currentTextBox = walkieTalkieText;
            currentDialogueCount = dialogueCountWalkieTalkie;
        }
    }
    
    public void DisplayNextDialogue()
    {
        if (currentTextBox == shopText)
        {
            if (dialogueShop.Length >= currentDialogueCount)
            {
                StopCurrentAndTypeNewTextCoroutine(dialogueShop[currentDialogueCount].dialogues[dialogueTextCount], dialogueShop, currentTextBox);
            }
        }
        else
        {
            if (dialogueWalkieTalkie.Length >= currentDialogueCount)
            {
                StopCurrentAndTypeNewTextCoroutine(dialogueWalkieTalkie[currentDialogueCount].dialogues[dialogueTextCount], dialogueWalkieTalkie, currentTextBox);
            }
        }
        
        dialogueTextCount++;
    }

    public void SetDialogueState()
    {
        switch (dialogueState)
        {
            case DialogueState.DialoguePlaying:
                if(!isSkippingDialogue)
                {
                    StopAllCoroutines();
                    StartCoroutine(TypeAllText());
                }
                break;
            case DialogueState.DialogueAbleToGoNext:
                PlayNextDialogueText();
                break;
            case DialogueState.DialogueAbleToEnd:
                EndDialogue();
                break;
            case DialogueState.DialogueNotPlaying:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private IEnumerator TypeAllText()
    {
        isSkippingDialogue = true;

        if (currentTextBox == shopText)
        {
            if (dialogueShop.Length >= currentDialogueCount)
            {
                //-1 because here the game thinks the dialogue was already played, but we want to finish the current one
                currentTextBox.text = dialogueShop[currentDialogueCount].dialogues[dialogueTextCount - 1];
            }
        }
        else
        {
            if (dialogueWalkieTalkie.Length >= currentDialogueCount)
            {
                currentTextBox.text = dialogueWalkieTalkie[currentDialogueCount].dialogues[dialogueTextCount - 1];
            }
        }

        yield return new WaitForSeconds(timeBetweenTextSkip);

        if (currentTextBox == shopText)
        {
            CheckDialogueEnd(dialogueShop);
        }
        else
        {
            CheckDialogueEnd(dialogueWalkieTalkie);
        }

        isSkippingDialogue = false;
    }

    public bool IsDialoguePlaying()
    {
        return dialogueState != DialogueState.DialogueNotPlaying;
    }

    public void StopCurrentAndTypeNewTextCoroutine(string text, Dialogues[] currentDialogue, TextMeshProUGUI textBox)
    {
        StopAllCoroutines();

        textBox.text = "";

        StartCoroutine(TypeTextCoroutine(text, currentDialogue, textBox));
    }
    
    public IEnumerator TypeTextCoroutine(string text, Dialogues[] currentDialogue, TextMeshProUGUI textBox)
    {
        if (currentDialogue != null)
        {
            dialogueState = DialogueState.DialoguePlaying;
            InGameUIManager.Instance.inGameUICanvasGroup.interactable = false;
        }

        textBox.text = "";
        textBox.textWrappingMode = TextWrappingModes.Normal;

        var _words = text.Split(' ');
        string _displayText = ""; 
        float _availableWidth = textBox.rectTransform.rect.width;
        int _bracketCount = 0;

        foreach (var _word in _words)
        {
            string _testLine = _displayText + _word + " ";
            textBox.text = _testLine;
            textBox.ForceMeshUpdate();
            float _textWidth = textBox.preferredWidth;

            if (_textWidth > _availableWidth) // If word doesn't fit, move to a new line before typing
            {
                _displayText += "\n";
            }

            if (_word.Contains("Peggy:") || _word.Contains("???:"))
            {
                _displayText += _word + " ";
                textBox.text = _displayText;
                continue;
            }

            // Update the text for each word, not each character, for performance reasons
            foreach (char _letter in _word + " ")
            {
                if (_letter == '<' || _bracketCount is > 0 and < 3)
                {
                    _displayText += _letter;

                    if (_letter == '<' && _bracketCount == 0)
                    {
                        _bracketCount = 1;
                    }
                    else if (_letter == '>' && _bracketCount > 0)
                    {
                        _bracketCount++;
                    }

                    continue;
                }

                _bracketCount = 0;
                _displayText += _letter;
                textBox.text = _displayText;
                yield return new WaitForSeconds(maxTextDisplaySpeed);
            }
        }

        if (currentDialogue != null)
        {
            yield return new WaitForSeconds(timeBetweenTextSkip);
            
            CheckDialogueEnd(currentDialogue);
        }
    }

    private void CheckDialogueEnd(IReadOnlyList<Dialogues> currentDialogue)
    {
        if (dialogueTextCount == currentDialogue[currentDialogueCount].dialogues.Count)
        {
            dialogueState = DialogueState.DialogueAbleToEnd;
        }
        else
        {
            dialogueState = DialogueState.DialogueAbleToGoNext;
        }
    }

    public void SetWalkieTalkieTextBoxAnimation(bool radioOn, bool putOn)
    {
        if (InGameUIManager.Instance.playerHUD.activeSelf)
        {
            radioAnim.SetBool("RadioOn", radioOn);
            radioAnim.SetBool("PutOn", putOn);

            if (!radioOn)
            {
                walkieTalkieText.gameObject.SetActive(false);
                walkieTalkieDialogueBoxAnim.SetBool("DialogueBoxOn", false);   
                PlayerBehaviour.Instance.SetPlayerBusy(false);
            }
            else
            {
                StopAllCoroutines();
                walkieTalkieText.gameObject.SetActive(true);
                PlayerBehaviour.Instance.SetPlayerBusy(true);
            }   
        }

        //Also some animation for the text box in the shop
    }

    public void EndDialogue()
    {
        dialogueTextCount = 0;
        currentDialogueCount++;
        currentTextBox.text = "";

        dialogueState = DialogueState.DialogueNotPlaying;
        
        InGameUIManager.Instance.inGameUICanvasGroup.interactable = true;

        if (currentTextBox != shopText)
        {
            SetWalkieTalkieTextBoxAnimation(false, true);
            dialogueWalkieTalkie[dialogueCountWalkieTalkie].dialogueEndAction?.Invoke();
            dialogueCountWalkieTalkie++;
            return;
        }

        StartCoroutine(TypeTextCoroutine("Peggy:" + "\n" + "...", null, currentTextBox));
        dialogueShop[dialogueCountShop].dialogueEndAction?.Invoke();
        dialogueCountShop++;
    }

    private void GetDialogueBasedOnLanguage(int currentText)
    {
        //dialogueWalkieTalkie[currentText].languages[LocalizationSettings.SelectedLocale.Identifier]
    }

    public void PlayNextDialogueText()
    {
        dialogueState = DialogueState.DialoguePlaying;
        currentTextBox.text = "";

        DisplayNextDialogue();
    }
}
