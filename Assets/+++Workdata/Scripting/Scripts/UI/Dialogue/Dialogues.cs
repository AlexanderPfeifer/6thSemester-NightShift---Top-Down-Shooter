using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class Dialogues
{
    [SerializeField] private string situation;

    public TranslatedDialogue[] languages;

    public UnityEvent dialogueEndAction;
}