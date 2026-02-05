using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class Dialogues
{
    [SerializeField] private string situation;

    public TranslatedDialogue[] languages;

    [TextArea(3, 10)] public List<string> dialogues = new();

    public UnityEvent dialogueEndAction;
}