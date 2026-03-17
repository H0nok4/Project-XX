using UnityEngine;

[DisallowMultipleComponent]
public sealed class MerchantUIManager : MonoBehaviour
{
    private static MerchantUIManager instance;

    [SerializeField] private BaseHubDirector director;
    [SerializeField] private PrototypeMainMenuController menuController;
    [SerializeField] private bool showGreetingFeedback = true;

    public static MerchantUIManager Instance => instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }

        instance = this;
        ResolveReferences();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public bool OpenMerchant(MerchantNPC merchantNpc)
    {
        if (merchantNpc == null)
        {
            return false;
        }

        ResolveReferences();

        bool opened = director != null
            ? director.OpenMerchant(merchantNpc.MerchantId, merchantNpc.MerchantName)
            : menuController != null && menuController.ShowMerchant(merchantNpc.MerchantId, merchantNpc.MerchantName);

        if (!opened)
        {
            return false;
        }

        if (showGreetingFeedback && menuController != null)
        {
            string feedback = string.IsNullOrWhiteSpace(merchantNpc.GreetingLine)
                ? $"正在与{merchantNpc.MerchantName}交易。"
                : merchantNpc.GreetingLine;
            menuController.SetFeedback(feedback);
        }

        return true;
    }

    private void ResolveReferences()
    {
        if (director == null)
        {
            director = FindFirstObjectByType<BaseHubDirector>();
        }

        if (menuController == null)
        {
            menuController = FindFirstObjectByType<PrototypeMainMenuController>();
        }
    }
}
