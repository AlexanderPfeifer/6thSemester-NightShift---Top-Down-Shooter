using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TranslatedDialogue
{
    [SerializeField] private string language;

    [TextArea(3, 10)] public List<string> dialogues = new();
}
