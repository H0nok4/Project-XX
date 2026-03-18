using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class RaidGameMode : MonoBehaviour
{
    public enum RaidState
    {
        Idle = 0,
        Running = 1,
        Extracted = 2,
        Failed = 3,
        Expired = 4
    }

    [Header("Raid")]
    [SerializeField] private float raidDurationSeconds = 900f;
    [SerializeField] private bool startOnAwake = true;
    [SerializeField] private bool showHud = true;

    [Header("Player")]
    [SerializeField] private PlayerInteractor playerInteractor;
    [SerializeField] private PrototypeUnitVitals playerVitals;
    [SerializeField] private PlayerInteractionState interactionState;

    [Header("Loot Progression")]
    [Range(ItemDefinition.MinItemLevel, ItemDefinition.MaxItemLevel)]
    [SerializeField] private int lootMinItemLevel = 4;
    [Range(ItemDefinition.MinItemLevel, ItemDefinition.MaxItemLevel)]
    [SerializeField] private int lootMaxItemLevel = 10;

    [Header("Runtime")]
    [SerializeField] private RaidState currentState = RaidState.Idle;
    [SerializeField] private float remainingSeconds;
    [SerializeField] private string lastResultMessage = string.Empty;
    [SerializeField] private List<ExtractionZone> extractionZones = new List<ExtractionZone>();
    [SerializeField] private List<RaidPlayerSpawnPoint> playerSpawnPoints = new List<RaidPlayerSpawnPoint>();

    private RectTransform hudRoot;
    private Text hudText;
    private RectTransform extractionRoot;
    private Text extractionText;
    private Image extractionFill;
    private RectTransform resultRoot;
    private Text resultTitleText;
    private Text resultBodyText;

    public event Action<RaidState> StateChanged;

    public RaidState CurrentState => currentState;
    public float RemainingSeconds => remainingSeconds;
    public string LastResultMessage => lastResultMessage;
    public bool IsRunning => currentState == RaidState.Running;
    public int LootMinItemLevel => Mathf.Clamp(lootMinItemLevel, ItemDefinition.MinItemLevel, ItemDefinition.MaxItemLevel);
    public int LootMaxItemLevel => Mathf.Clamp(lootMaxItemLevel, LootMinItemLevel, ItemDefinition.MaxItemLevel);

    private void Awake()
    {
        ResolveReferences();
        remainingSeconds = Mathf.Max(0f, raidDurationSeconds);
        ApplyResultUiFocus(currentState != RaidState.Running && currentState != RaidState.Idle);
        EnsureHudUi();

        if (startOnAwake)
        {
            StartRaid();
        }
    }

    private void OnEnable()
    {
        ResolveReferences();
        if (playerVitals != null)
        {
            playerVitals.Died += HandlePlayerDied;
        }

        ApplyResultUiFocus(currentState != RaidState.Running && currentState != RaidState.Idle);
    }

    private void OnDisable()
    {
        if (playerVitals != null)
        {
            playerVitals.Died -= HandlePlayerDied;
        }

        ApplyResultUiFocus(false);
        SetUiVisible(hudRoot, false);
        SetUiVisible(extractionRoot, false);
        SetUiVisible(resultRoot, false);
    }

    private void Update()
    {
        if (IsRunning)
        {
            remainingSeconds = Mathf.Max(0f, remainingSeconds - Time.deltaTime);
            if (remainingSeconds <= 0f)
            {
                ExpireRaid();
            }
        }

        UpdateHudUi();
    }

    private void OnValidate()
    {
        raidDurationSeconds = Mathf.Max(30f, raidDurationSeconds);
        lootMinItemLevel = Mathf.Clamp(lootMinItemLevel, ItemDefinition.MinItemLevel, ItemDefinition.MaxItemLevel);
        lootMaxItemLevel = Mathf.Clamp(lootMaxItemLevel, lootMinItemLevel, ItemDefinition.MaxItemLevel);
        ResolveReferences();

        if (currentState == RaidState.Idle || currentState == RaidState.Running)
        {
            remainingSeconds = Mathf.Clamp(remainingSeconds, 0f, raidDurationSeconds);
        }
    }

    public void Configure(PlayerInteractor interactor, PrototypeUnitVitals vitals)
    {
        playerInteractor = interactor;
        playerVitals = vitals;
    }

    public void Configure(PlayerInteractor interactor, PrototypeUnitVitals vitals, float durationSeconds)
    {
        playerInteractor = interactor;
        playerVitals = vitals;
        raidDurationSeconds = Mathf.Max(30f, durationSeconds);
        remainingSeconds = Mathf.Clamp(remainingSeconds, 0f, raidDurationSeconds);
    }

    public void ConfigureLootProgression(int minimumItemLevel, int maximumItemLevel)
    {
        lootMinItemLevel = Mathf.Clamp(minimumItemLevel, ItemDefinition.MinItemLevel, ItemDefinition.MaxItemLevel);
        lootMaxItemLevel = Mathf.Clamp(maximumItemLevel, lootMinItemLevel, ItemDefinition.MaxItemLevel);
    }

    public LootTableDefinition.LootGenerationContext CreateLootContext(int itemLevelBonus = 0, int rarityBias = 0, int bonusRolls = 0)
    {
        int minItemLevel = Mathf.Clamp(LootMinItemLevel + itemLevelBonus, ItemDefinition.MinItemLevel, ItemDefinition.MaxItemLevel);
        int maxItemLevel = Mathf.Clamp(LootMaxItemLevel + itemLevelBonus, minItemLevel, ItemDefinition.MaxItemLevel);
        return new LootTableDefinition.LootGenerationContext(minItemLevel, maxItemLevel, rarityBias, bonusRolls);
    }

    public void StartRaid()
    {
        ResolveReferences();
        RefreshPlayerSpawnPoints();
        MovePlayerToRandomSpawn();
        remainingSeconds = raidDurationSeconds;
        lastResultMessage = string.Empty;
        SetState(RaidState.Running);
        QuestEventHub.RaiseCustom("raid_started");
    }

    public bool CanExtract(PlayerInteractor interactor, ExtractionZone zone)
    {
        return IsRunning
            && zone != null
            && zone.isActiveAndEnabled
            && interactor != null
            && (playerInteractor == null || interactor == playerInteractor);
    }

    public bool TryExtract(PlayerInteractor interactor, ExtractionZone zone)
    {
        if (!CanExtract(interactor, zone))
        {
            return false;
        }

        QuestEventHub.RaiseExtract(zone != null ? zone.ExtractionName : string.Empty);
        lastResultMessage = $"Extracted via {zone.ExtractionName}";
        SetState(RaidState.Extracted);
        return true;
    }

    public void FailRaid(string reason)
    {
        if (!IsRunning)
        {
            return;
        }

        lastResultMessage = string.IsNullOrWhiteSpace(reason) ? "Raid failed." : reason.Trim();
        SetState(RaidState.Failed);
    }

    public void RegisterExtractionZone(ExtractionZone zone)
    {
        if (zone != null && !extractionZones.Contains(zone))
        {
            extractionZones.Add(zone);
        }
    }

    public void UnregisterExtractionZone(ExtractionZone zone)
    {
        extractionZones.Remove(zone);
    }

    public void RegisterPlayerSpawnPoint(RaidPlayerSpawnPoint spawnPoint)
    {
        if (spawnPoint != null && !playerSpawnPoints.Contains(spawnPoint))
        {
            playerSpawnPoints.Add(spawnPoint);
        }
    }

    public void UnregisterPlayerSpawnPoint(RaidPlayerSpawnPoint spawnPoint)
    {
        playerSpawnPoints.Remove(spawnPoint);
    }

    private void ExpireRaid()
    {
        lastResultMessage = "Raid timer expired.";
        SetState(RaidState.Expired);
    }

    private void HandlePlayerDied(PrototypeUnitVitals vitals)
    {
        if (!IsRunning)
        {
            return;
        }

        string damageSourceSummary = vitals != null ? vitals.GetLastDamageSourceSummary() : string.Empty;
        if (!string.IsNullOrWhiteSpace(damageSourceSummary))
        {
            FailRaid($"Player died.\nSource: {damageSourceSummary}");
            return;
        }

        FailRaid("Player died.");
    }

    private void SetState(RaidState nextState)
    {
        if (currentState == nextState)
        {
            return;
        }

        currentState = nextState;
        ApplyResultUiFocus(currentState != RaidState.Running && currentState != RaidState.Idle);
        StateChanged?.Invoke(currentState);
    }

    private void ResolveReferences()
    {
        if (playerInteractor == null)
        {
            playerInteractor = FindFirstObjectByType<PlayerInteractor>();
        }

        if (playerVitals == null && playerInteractor != null)
        {
            playerVitals = playerInteractor.GetComponent<PrototypeUnitVitals>();
        }

        if (interactionState == null)
        {
            if (playerInteractor != null)
            {
                interactionState = playerInteractor.GetComponent<PlayerInteractionState>();
            }

            if (interactionState == null && playerVitals != null)
            {
                interactionState = playerVitals.GetComponent<PlayerInteractionState>();
            }
        }

        SanitizeList(extractionZones);
        RefreshPlayerSpawnPoints();
    }

    private void ApplyResultUiFocus(bool focused)
    {
        if (interactionState == null)
        {
            return;
        }

        interactionState.SetUiFocused(this, focused);

        bool keepCursorFree = focused || interactionState.IsUiFocused;
        Cursor.lockState = keepCursorFree ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = keepCursorFree;
    }

    private void RefreshPlayerSpawnPoints()
    {
        if (playerSpawnPoints == null)
        {
            playerSpawnPoints = new List<RaidPlayerSpawnPoint>();
        }

        if (playerSpawnPoints.Count == 0)
        {
            playerSpawnPoints = new List<RaidPlayerSpawnPoint>(FindObjectsByType<RaidPlayerSpawnPoint>(FindObjectsSortMode.None));
        }
        else
        {
            SanitizeList(playerSpawnPoints);
        }
    }

    private void MovePlayerToRandomSpawn()
    {
        if (playerSpawnPoints == null || playerSpawnPoints.Count == 0)
        {
            return;
        }

        Transform playerTransform = playerInteractor != null ? playerInteractor.transform : playerVitals != null ? playerVitals.transform : null;
        if (playerTransform == null)
        {
            return;
        }

        RaidPlayerSpawnPoint spawnPoint = playerSpawnPoints[UnityEngine.Random.Range(0, playerSpawnPoints.Count)];
        if (spawnPoint == null)
        {
            return;
        }

        PrototypeFpsController controller = playerTransform.GetComponent<PrototypeFpsController>();
        if (controller != null)
        {
            controller.ApplySpawnPose(spawnPoint.transform.position, spawnPoint.transform.rotation);
            return;
        }

        playerTransform.SetPositionAndRotation(spawnPoint.transform.position, spawnPoint.transform.rotation);
    }

    private ExtractionZone GetActiveExtractionZone()
    {
        for (int index = 0; index < extractionZones.Count; index++)
        {
            ExtractionZone zone = extractionZones[index];
            if (zone != null && zone.HasActiveExtraction)
            {
                return zone;
            }
        }

        return null;
    }

    private void EnsureHudUi()
    {
        if (hudRoot != null)
        {
            return;
        }

        PrototypeRuntimeUiManager manager = PrototypeRuntimeUiManager.GetOrCreate();
        Font font = manager.RuntimeFont;

        hudRoot = PrototypeUiToolkit.CreatePanel(
            manager.GetLayerRoot(PrototypeUiLayer.Hud),
            "RaidHud",
            new Color(0.08f, 0.1f, 0.14f, 0.88f),
            new RectOffset(12, 12, 10, 10),
            0f);
        PrototypeUiToolkit.SetAnchor(hudRoot, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -18f), new Vector2(320f, 72f));
        hudText = PrototypeUiToolkit.CreateText(hudRoot, font, string.Empty, 14, FontStyle.Normal, Color.white, TextAnchor.UpperLeft);

        extractionRoot = PrototypeUiToolkit.CreatePanel(
            manager.GetLayerRoot(PrototypeUiLayer.Hud),
            "ExtractionProgress",
            new Color(0.08f, 0.1f, 0.14f, 0.92f),
            new RectOffset(10, 10, 8, 8),
            6f);
        PrototypeUiToolkit.SetAnchor(extractionRoot, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -96f), new Vector2(320f, 44f));
        extractionText = PrototypeUiToolkit.CreateText(extractionRoot, font, string.Empty, 14, FontStyle.Bold, Color.white, TextAnchor.UpperLeft);

        RectTransform barBackground = PrototypeUiToolkit.CreateRectTransform("BarBackground", extractionRoot);
        LayoutElement barLayout = barBackground.gameObject.AddComponent<LayoutElement>();
        barLayout.preferredHeight = 14f;
        Image barBackgroundImage = barBackground.gameObject.AddComponent<Image>();
        barBackgroundImage.color = new Color(0f, 0f, 0f, 0.45f);

        RectTransform fillRoot = PrototypeUiToolkit.CreateRectTransform("Fill", barBackground);
        fillRoot.anchorMin = new Vector2(0f, 0f);
        fillRoot.anchorMax = new Vector2(0f, 1f);
        fillRoot.pivot = new Vector2(0f, 0.5f);
        fillRoot.offsetMin = Vector2.zero;
        fillRoot.offsetMax = Vector2.zero;
        extractionFill = fillRoot.gameObject.AddComponent<Image>();
        extractionFill.color = new Color(0.22f, 0.86f, 0.48f, 0.95f);

        resultRoot = PrototypeUiToolkit.CreatePanel(
            manager.GetLayerRoot(PrototypeUiLayer.Modal),
            "RaidResult",
            new Color(0.08f, 0.1f, 0.14f, 0.96f),
            new RectOffset(18, 18, 16, 16),
            8f);
        PrototypeUiToolkit.SetAnchor(resultRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(500f, 220f));
        ContentSizeFitter resultFitter = resultRoot.gameObject.AddComponent<ContentSizeFitter>();
        resultFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        resultFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        resultTitleText = PrototypeUiToolkit.CreateText(resultRoot, font, string.Empty, 22, FontStyle.Bold, Color.white, TextAnchor.UpperCenter);
        resultTitleText.lineSpacing = 0.95f;
        resultBodyText = PrototypeUiToolkit.CreateText(resultRoot, font, string.Empty, 16, FontStyle.Normal, Color.white, TextAnchor.UpperLeft);
        resultBodyText.lineSpacing = 0.92f;

        SetUiVisible(hudRoot, false);
        SetUiVisible(extractionRoot, false);
        SetUiVisible(resultRoot, false);
    }

    private void UpdateHudUi()
    {
        EnsureHudUi();
        if (!showHud)
        {
            SetUiVisible(hudRoot, false);
            SetUiVisible(extractionRoot, false);
            SetUiVisible(resultRoot, false);
            return;
        }

        InventoryContainer inventory = playerInteractor != null ? playerInteractor.PrimaryInventory : null;
        string inventorySummary = inventory != null
            ? $"\nSlots {inventory.Items.Count}/{inventory.MaxSlots}  Weight {inventory.CurrentWeight:0.0}/{inventory.MaxWeight:0.0}"
            : string.Empty;

        if (hudText != null)
        {
            hudText.text = $"Raid {currentState}\nTime {FormatTime(remainingSeconds)}{inventorySummary}";
        }

        SetUiVisible(hudRoot, true);

        ExtractionZone activeExtractionZone = GetActiveExtractionZone();
        bool showExtractionProgress = activeExtractionZone != null;
        SetUiVisible(extractionRoot, showExtractionProgress);
        if (showExtractionProgress)
        {
            if (extractionText != null)
            {
                extractionText.text = $"Extracting {activeExtractionZone.ExtractionName}  {activeExtractionZone.ExtractionRemainingSeconds:0.0}s";
            }

            if (extractionFill != null)
            {
                extractionFill.rectTransform.sizeDelta = new Vector2(294f * activeExtractionZone.ExtractionProgressNormalized, 0f);
            }
        }

        bool showResult = !string.IsNullOrWhiteSpace(lastResultMessage) && currentState != RaidState.Running;
        SetUiVisible(resultRoot, showResult);
        if (!showResult)
        {
            return;
        }

        if (resultTitleText != null)
        {
            resultTitleText.text = GetResultTitle();
        }

        if (resultBodyText != null)
        {
            resultBodyText.text = BuildResultBodyText(inventory);
        }
    }

    private string BuildResultBodyText(InventoryContainer inventory)
    {
        string body = $"{lastResultMessage}\n\nTime Remaining: {FormatTime(remainingSeconds)}";
        if (inventory == null)
        {
            return body;
        }

        body += $"\nInventory: {inventory.Items.Count}/{inventory.MaxSlots} stacks   Weight: {inventory.CurrentWeight:0.0}/{inventory.MaxWeight:0.0}";
        if (inventory.IsEmpty)
        {
            return $"{body}\nNo loot carried.";
        }

        int visibleEntries = Mathf.Min(6, inventory.Items.Count);
        for (int index = 0; index < visibleEntries; index++)
        {
            ItemInstance item = inventory.Items[index];
            if (item == null || !item.IsDefined())
            {
                continue;
            }

            body += $"\n- {item.DisplayName} x{item.Quantity}";
        }

        if (inventory.Items.Count > visibleEntries)
        {
            body += $"\n- ...and {inventory.Items.Count - visibleEntries} more stacks";
        }

        return body;
    }

    private static void SetUiVisible(RectTransform root, bool visible)
    {
        PrototypeUiToolkit.SetVisible(root, visible);
    }

    private static string FormatTime(float seconds)
    {
        int totalSeconds = Mathf.Max(0, Mathf.CeilToInt(seconds));
        int minutes = totalSeconds / 60;
        int secondsRemainder = totalSeconds % 60;
        return $"{minutes:00}:{secondsRemainder:00}";
    }

    private string GetResultTitle()
    {
        switch (currentState)
        {
            case RaidState.Extracted:
                return "Extraction Successful";
            case RaidState.Failed:
                return "Raid Failed";
            case RaidState.Expired:
                return "Raid Expired";
            default:
                return "Raid Result";
        }
    }

    private static void SanitizeList<T>(List<T> list) where T : class
    {
        if (list == null)
        {
            return;
        }

        for (int index = list.Count - 1; index >= 0; index--)
        {
            if (list[index] == null)
            {
                list.RemoveAt(index);
            }
        }
    }

    private void OnDestroy()
    {
        if (hudRoot != null)
        {
            Destroy(hudRoot.gameObject);
        }

        if (extractionRoot != null)
        {
            Destroy(extractionRoot.gameObject);
        }

        if (resultRoot != null)
        {
            Destroy(resultRoot.gameObject);
        }
    }
}
