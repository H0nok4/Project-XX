using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class DialogueNode
{
    public string nodeId = string.Empty;
    public string speakerName = string.Empty;
    [TextArea(2, 6)] public string dialogueText = string.Empty;
    public string nextNodeId = string.Empty;
    public List<DialogueOption> options = new List<DialogueOption>();

    [NonSerialized] public Func<string> dynamicSpeakerProvider;
    [NonSerialized] public Func<string> dynamicTextProvider;

    public string NodeId => string.IsNullOrWhiteSpace(nodeId) ? string.Empty : nodeId.Trim();

    public string SpeakerName
    {
        get
        {
            string value = dynamicSpeakerProvider != null ? dynamicSpeakerProvider() : speakerName;
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public string DialogueText
    {
        get
        {
            string value = dynamicTextProvider != null ? dynamicTextProvider() : dialogueText;
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public string NextNodeId => string.IsNullOrWhiteSpace(nextNodeId) ? string.Empty : nextNodeId.Trim();

    public bool HasOptions()
    {
        return options != null && options.Count > 0;
    }
}
