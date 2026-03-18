using System;

[Serializable]
public sealed class DialogueOption
{
    public string optionText = string.Empty;
    public string targetNodeId = string.Empty;

    [NonSerialized] public Func<bool> availabilityEvaluator;
    [NonSerialized] public Func<string> labelProvider;
    [NonSerialized] public Action<DialogueSystem> onSelected;

    public string OptionText
    {
        get
        {
            string value = labelProvider != null ? labelProvider() : optionText;
            return string.IsNullOrWhiteSpace(value) ? "继续" : value.Trim();
        }
    }

    public string TargetNodeId => string.IsNullOrWhiteSpace(targetNodeId) ? string.Empty : targetNodeId.Trim();

    public bool IsAvailable()
    {
        return availabilityEvaluator == null || availabilityEvaluator();
    }

    public void Invoke(DialogueSystem system)
    {
        onSelected?.Invoke(system);
    }
}
