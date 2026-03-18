using UnityEngine;

[DisallowMultipleComponent]
public sealed class QuestNPC : MonoBehaviour, IInteractable
{
    [SerializeField] private string npcId = string.Empty;
    [SerializeField] private string npcName = "任务官";
    [SerializeField] private string npcTitle = string.Empty;
    [TextArea(2, 4)]
    [SerializeField] private string greetingLine = "有事就说，任务不会自己完成。";
    [SerializeField] private string interactionLabelOverride = string.Empty;
    [SerializeField] private float interactionRange = 3.2f;

    public string NpcId => string.IsNullOrWhiteSpace(npcId) ? string.Empty : npcId.Trim();
    public string NpcName => string.IsNullOrWhiteSpace(npcName) ? "任务官" : npcName.Trim();
    public string DisplayName => NpcName;
    public string Title => string.IsNullOrWhiteSpace(npcTitle) ? string.Empty : npcTitle.Trim();
    public string GreetingLine => string.IsNullOrWhiteSpace(greetingLine) ? string.Empty : greetingLine.Trim();

    private void OnValidate()
    {
        interactionRange = Mathf.Max(1.25f, interactionRange);
        npcId ??= string.Empty;
        npcName ??= string.Empty;
        npcTitle ??= string.Empty;
        greetingLine ??= string.Empty;
        interactionLabelOverride ??= string.Empty;
    }

    public void Configure(string id, string displayName, string title, string greeting, float range = 3.2f)
    {
        npcId = string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();
        npcName = string.IsNullOrWhiteSpace(displayName) ? "任务官" : displayName.Trim();
        npcTitle = string.IsNullOrWhiteSpace(title) ? string.Empty : title.Trim();
        greetingLine = string.IsNullOrWhiteSpace(greeting) ? string.Empty : greeting.Trim();
        interactionRange = Mathf.Max(1.25f, range);
    }

    public string GetInteractionLabel(PlayerInteractor interactor)
    {
        if (!string.IsNullOrWhiteSpace(interactionLabelOverride))
        {
            return interactionLabelOverride.Trim();
        }

        QuestManager questManager = QuestManager.GetOrCreate();
        if (questManager.TryInitialize())
        {
            if (questManager.GetCompletableQuestsForNpc(NpcId).Count > 0)
            {
                return $"向{NpcName}提交任务";
            }

            if (questManager.GetAvailableQuestsForNpc(NpcId).Count > 0)
            {
                return $"与{NpcName}对话（有新任务）";
            }
        }

        return $"与{NpcName}对话";
    }

    public bool CanInteract(PlayerInteractor interactor)
    {
        if (interactor == null)
        {
            return true;
        }

        Transform interactionTransform = GetInteractionTransform();
        if (interactionTransform == null)
        {
            return true;
        }

        Vector3 planarDelta = interactor.transform.position - interactionTransform.position;
        planarDelta.y = 0f;
        return planarDelta.sqrMagnitude <= interactionRange * interactionRange;
    }

    public void Interact(PlayerInteractor interactor)
    {
        DialogueSystem.GetOrCreate().OpenNpcDialogue(this, interactor);
    }

    public Transform GetInteractionTransform()
    {
        return transform;
    }
}
