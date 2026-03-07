using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class PrototypeMainMenuController : MonoBehaviour
{
    private enum MenuPage
    {
        Home = 0,
        Stash = 1
    }

    [Header("Profile")]
    [SerializeField] private PrototypeItemCatalog itemCatalog;
    [SerializeField] private string raidSceneName = "SampleScene";
    [SerializeField] private int stashSlots = 32;
    [SerializeField] private float stashMaxWeight = 0f;
    [SerializeField] private int loadoutSlots = 12;
    [SerializeField] private float loadoutMaxWeight = 20f;
    [SerializeField] private bool autoSaveOnInventoryChange = true;

    [Header("Scene Dressing")]
    [SerializeField] private Color stashColor = new Color(0.2f, 0.65f, 0.38f, 1f);
    [SerializeField] private Color loadoutColor = new Color(0.75f, 0.26f, 0.22f, 1f);

    private InventoryContainer stashInventory;
    private InventoryContainer loadoutInventory;
    private PrototypeProfileService.ProfileData profile;
    private MenuPage currentPage;
    private Vector2 stashScroll;
    private Vector2 loadoutScroll;
    private string feedbackMessage = string.Empty;
    private float feedbackUntilTime;
    private GUIStyle titleStyle;
    private GUIStyle sectionStyle;
    private GUIStyle bodyStyle;
    private GUIStyle listStyle;
    private GUIStyle buttonStyle;

    private void Awake()
    {
        ResolveCatalog();
        EnsureContainers();
        LoadProfileIntoContainers();
    }

    private void OnDisable()
    {
        SaveProfileFromContainers();
    }

    private void OnApplicationQuit()
    {
        SaveProfileFromContainers();
    }

    private void OnValidate()
    {
        stashSlots = Mathf.Max(4, stashSlots);
        stashMaxWeight = Mathf.Max(0f, stashMaxWeight);
        loadoutSlots = Mathf.Max(4, loadoutSlots);
        loadoutMaxWeight = Mathf.Max(0f, loadoutMaxWeight);
        ResolveCatalog();
    }

    private void OnGUI()
    {
        EnsureStyles();
        DrawBackground();
        DrawNavigation();

        if (currentPage == MenuPage.Stash)
        {
            DrawStashPage();
        }
        else
        {
            DrawHomePage();
        }

        DrawFooter();
    }

    private void DrawBackground()
    {
        Color previousColor = GUI.color;
        GUI.color = new Color(0.08f, 0.1f, 0.14f, 0.94f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = previousColor;

        GUI.Label(new Rect(40f, 32f, 640f, 48f), "Project-XX", titleStyle);
        GUI.Label(new Rect(44f, 88f, 560f, 28f), "Single-player raid prototype", bodyStyle);
    }

    private void DrawNavigation()
    {
        Rect navRect = new Rect(40f, 140f, 220f, 250f);
        GUI.Box(navRect, string.Empty, sectionStyle);

        GUILayout.BeginArea(new Rect(navRect.x + 16f, navRect.y + 16f, navRect.width - 32f, navRect.height - 32f));
        GUILayout.Label("Operations", sectionStyle);
        GUILayout.Space(12f);

        if (GUILayout.Button("Deploy", buttonStyle, GUILayout.Height(42f)))
        {
            currentPage = MenuPage.Home;
        }

        if (GUILayout.Button("Warehouse", buttonStyle, GUILayout.Height(42f)))
        {
            currentPage = MenuPage.Stash;
        }

        GUILayout.Space(10f);
        if (GUILayout.Button("Save Profile", buttonStyle, GUILayout.Height(34f)))
        {
            SaveProfileFromContainers();
            SetFeedback("Profile saved.");
        }

        if (GUILayout.Button("Reset Profile", buttonStyle, GUILayout.Height(34f)))
        {
            ResetProfile();
        }

        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Quit", buttonStyle, GUILayout.Height(34f)))
        {
            SaveProfileFromContainers();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        GUILayout.EndArea();
    }

    private void DrawHomePage()
    {
        Rect panelRect = new Rect(292f, 140f, Mathf.Max(560f, Screen.width - 336f), 340f);
        GUI.Box(panelRect, string.Empty, sectionStyle);

        GUILayout.BeginArea(new Rect(panelRect.x + 18f, panelRect.y + 16f, panelRect.width - 36f, panelRect.height - 32f));
        GUILayout.Label("Ready Room", sectionStyle);
        GUILayout.Space(10f);
        GUILayout.Label(
            "Prepare your loadout in the warehouse, then deploy into the indoor combat test. Deploying consumes the staged loadout. Extracting sends carried items back to stash.",
            bodyStyle);

        GUILayout.Space(18f);
        GUILayout.Label(BuildSummaryText(), bodyStyle);

        GUILayout.Space(20f);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Enter Battle", buttonStyle, GUILayout.Width(180f), GUILayout.Height(48f)))
        {
            StartRaid();
        }

        if (GUILayout.Button("Open Warehouse", buttonStyle, GUILayout.Width(180f), GUILayout.Height(48f)))
        {
            currentPage = MenuPage.Stash;
        }

        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    private void DrawStashPage()
    {
        float panelTop = 140f;
        float panelHeight = Mathf.Max(360f, Screen.height - 220f);
        float panelWidth = Mathf.Max(320f, (Screen.width - 356f) * 0.5f);
        float stashX = 292f;
        float loadoutX = stashX + panelWidth + 16f;

        DrawInventoryPanel(
            new Rect(stashX, panelTop, panelWidth, panelHeight),
            "Stash",
            stashInventory,
            ref stashScroll,
            stashColor,
            MoveFromStashToLoadout,
            "Stage");

        DrawInventoryPanel(
            new Rect(loadoutX, panelTop, panelWidth, panelHeight),
            "Raid Loadout",
            loadoutInventory,
            ref loadoutScroll,
            loadoutColor,
            MoveFromLoadoutToStash,
            "Return");
    }

    private void DrawInventoryPanel(
        Rect rect,
        string title,
        InventoryContainer inventory,
        ref Vector2 scrollPosition,
        Color accent,
        System.Action<int> moveAction,
        string buttonLabel)
    {
        GUI.Box(rect, string.Empty, sectionStyle);

        Color previousColor = GUI.color;
        GUI.color = accent;
        GUI.DrawTexture(new Rect(rect.x + 16f, rect.y + 18f, 72f, 4f), Texture2D.whiteTexture);
        GUI.color = previousColor;

        GUILayout.BeginArea(new Rect(rect.x + 16f, rect.y + 18f, rect.width - 32f, rect.height - 36f));
        GUILayout.Label(title, sectionStyle);
        GUILayout.Label(
            inventory != null
                ? $"Stacks {inventory.Items.Count}/{inventory.MaxSlots}  Weight {inventory.CurrentWeight:0.0}/{inventory.MaxWeight:0.0}"
                : "No inventory",
            bodyStyle);

        GUILayout.Space(10f);
        if (inventory == null || inventory.IsEmpty)
        {
            GUILayout.Label("Empty.", bodyStyle);
        }
        else
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(rect.height - 130f));
            for (int index = 0; index < inventory.Items.Count; index++)
            {
                ItemInstance item = inventory.Items[index];
                if (item == null || !item.IsDefined())
                {
                    continue;
                }

                GUILayout.BeginVertical(listStyle);
                GUILayout.Label($"{item.DisplayName} x{item.Quantity}", bodyStyle);
                GUILayout.Label($"Weight {item.TotalWeight:0.00}", bodyStyle);
                if (GUILayout.Button(buttonLabel, buttonStyle, GUILayout.Width(120f)))
                {
                    moveAction?.Invoke(index);
                    GUIUtility.ExitGUI();
                }

                GUILayout.EndVertical();
            }

            GUILayout.EndScrollView();
        }

        GUILayout.EndArea();
    }

    private void DrawFooter()
    {
        if (string.IsNullOrWhiteSpace(feedbackMessage) || Time.time > feedbackUntilTime)
        {
            return;
        }

        GUI.Label(new Rect(44f, Screen.height - 42f, 900f, 24f), feedbackMessage, bodyStyle);
    }

    private void MoveFromStashToLoadout(int itemIndex)
    {
        if (stashInventory == null || loadoutInventory == null || itemIndex < 0 || itemIndex >= stashInventory.Items.Count)
        {
            return;
        }

        ItemInstance item = stashInventory.Items[itemIndex];
        if (item == null || !item.IsDefined())
        {
            return;
        }

        if (!stashInventory.TryTransferItemTo(loadoutInventory, itemIndex, item.Quantity, out int movedQuantity) || movedQuantity <= 0)
        {
            SetFeedback("Loadout has no space for that stack.");
            return;
        }

        SetFeedback($"Staged {item.DisplayName} x{movedQuantity}.");
        AutoSaveIfNeeded();
    }

    private void MoveFromLoadoutToStash(int itemIndex)
    {
        if (loadoutInventory == null || stashInventory == null || itemIndex < 0 || itemIndex >= loadoutInventory.Items.Count)
        {
            return;
        }

        ItemInstance item = loadoutInventory.Items[itemIndex];
        if (item == null || !item.IsDefined())
        {
            return;
        }

        if (!loadoutInventory.TryTransferItemTo(stashInventory, itemIndex, item.Quantity, out int movedQuantity) || movedQuantity <= 0)
        {
            SetFeedback("Stash has no space for that stack.");
            return;
        }

        SetFeedback($"Returned {item.DisplayName} x{movedQuantity}.");
        AutoSaveIfNeeded();
    }

    private void StartRaid()
    {
        SaveProfileFromContainers();
        SceneManager.LoadScene(raidSceneName);
    }

    private void ResetProfile()
    {
        profile = PrototypeProfileService.CreateDefaultProfile(itemCatalog);
        ApplyProfileToContainers(profile);
        SaveProfileFromContainers();
        SetFeedback("Profile reset to defaults.");
    }

    private void ResolveCatalog()
    {
        if (itemCatalog == null)
        {
            itemCatalog = Resources.Load<PrototypeItemCatalog>("PrototypeItemCatalog");
        }
    }

    private void EnsureContainers()
    {
        if (stashInventory == null)
        {
            stashInventory = CreateRuntimeContainer("Profile_Stash", "Warehouse", stashSlots, stashMaxWeight);
        }
        else
        {
            stashInventory.Configure("Warehouse", stashSlots, stashMaxWeight);
        }

        if (loadoutInventory == null)
        {
            loadoutInventory = CreateRuntimeContainer("Profile_Loadout", "Raid Loadout", loadoutSlots, loadoutMaxWeight);
        }
        else
        {
            loadoutInventory.Configure("Raid Loadout", loadoutSlots, loadoutMaxWeight);
        }
    }

    private InventoryContainer CreateRuntimeContainer(string objectName, string label, int slots, float maxWeight)
    {
        Transform child = transform.Find(objectName);
        GameObject containerObject = child != null ? child.gameObject : new GameObject(objectName);
        containerObject.transform.SetParent(transform, false);
        containerObject.hideFlags = HideFlags.HideInHierarchy;

        InventoryContainer inventory = containerObject.GetComponent<InventoryContainer>();
        if (inventory == null)
        {
            inventory = containerObject.AddComponent<InventoryContainer>();
        }

        inventory.Configure(label, slots, maxWeight);
        return inventory;
    }

    private void LoadProfileIntoContainers()
    {
        profile = PrototypeProfileService.LoadProfile(itemCatalog);
        ApplyProfileToContainers(profile);
    }

    private void ApplyProfileToContainers(PrototypeProfileService.ProfileData sourceProfile)
    {
        EnsureContainers();
        PrototypeProfileService.PopulateInventory(stashInventory, sourceProfile != null ? sourceProfile.stashItems : null, itemCatalog);
        PrototypeProfileService.PopulateInventory(loadoutInventory, sourceProfile != null ? sourceProfile.loadoutItems : null, itemCatalog);
    }

    private void SaveProfileFromContainers()
    {
        ResolveCatalog();
        EnsureContainers();

        if (profile == null)
        {
            profile = new PrototypeProfileService.ProfileData();
        }

        profile.stashItems = PrototypeProfileService.CaptureInventory(stashInventory);
        profile.loadoutItems = PrototypeProfileService.CaptureInventory(loadoutInventory);
        PrototypeProfileService.SaveProfile(profile, itemCatalog);
    }

    private void AutoSaveIfNeeded()
    {
        if (autoSaveOnInventoryChange)
        {
            SaveProfileFromContainers();
        }
    }

    private string BuildSummaryText()
    {
        int stashStacks = stashInventory != null ? stashInventory.Items.Count : 0;
        int stagedStacks = loadoutInventory != null ? loadoutInventory.Items.Count : 0;
        float stagedWeight = loadoutInventory != null ? loadoutInventory.CurrentWeight : 0f;
        float stagedCapacity = loadoutInventory != null ? loadoutInventory.MaxWeight : 0f;
        return $"Warehouse stacks: {stashStacks}\nStaged loadout stacks: {stagedStacks}\nStaged weight: {stagedWeight:0.0}/{stagedCapacity:0.0}\nProfile file: {PrototypeProfileService.SavePath}";
    }

    private void SetFeedback(string message)
    {
        feedbackMessage = message ?? string.Empty;
        feedbackUntilTime = Time.time + 2.6f;
    }

    private void EnsureStyles()
    {
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 36,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }

        if (sectionStyle == null)
        {
            sectionStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 18,
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = Color.white }
            };
            sectionStyle.padding = new RectOffset(14, 14, 12, 12);
        }

        if (bodyStyle == null)
        {
            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                wordWrap = true,
                normal = { textColor = new Color(0.92f, 0.94f, 0.98f, 1f) }
            };
        }

        if (listStyle == null)
        {
            listStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.UpperLeft
            };
            listStyle.padding = new RectOffset(10, 10, 8, 8);
            listStyle.margin = new RectOffset(0, 0, 0, 8);
        }

        if (buttonStyle == null)
        {
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14
            };
        }
    }
}
