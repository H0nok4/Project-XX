using System;
using System.Collections.Generic;
using UnityEngine;

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

    private GUIStyle hudStyle;
    private GUIStyle resultStyle;

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
    }

    private void Update()
    {
        if (!IsRunning)
        {
            return;
        }

        remainingSeconds = Mathf.Max(0f, remainingSeconds - Time.deltaTime);
        if (remainingSeconds <= 0f)
        {
            ExpireRaid();
        }
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

    private void OnGUI()
    {
        if (!showHud)
        {
            return;
        }

        EnsureHudStyles();

        InventoryContainer inventory = playerInteractor != null ? playerInteractor.PrimaryInventory : null;
        string inventorySummary = inventory != null
            ? $"\nSlots {inventory.Items.Count}/{inventory.MaxSlots}  Weight {inventory.CurrentWeight:0.0}/{inventory.MaxWeight:0.0}"
            : string.Empty;

        GUI.Box(
            new Rect(18f, 18f, 320f, 72f),
            $"Raid {currentState}\nTime {FormatTime(remainingSeconds)}{inventorySummary}",
            hudStyle);

        ExtractionZone activeExtractionZone = GetActiveExtractionZone();
        if (activeExtractionZone != null)
        {
            DrawExtractionProgress(new Rect(18f, 96f, 320f, 44f), activeExtractionZone);
        }

        if (string.IsNullOrWhiteSpace(lastResultMessage) || currentState == RaidState.Running)
        {
            return;
        }

        Rect resultRect = new Rect(Screen.width * 0.5f - 250f, 24f, 500f, 220f);
        GUI.Box(resultRect, string.Empty, resultStyle);

        GUILayout.BeginArea(new Rect(resultRect.x + 16f, resultRect.y + 14f, resultRect.width - 32f, resultRect.height - 28f));
        GUILayout.Label(GetResultTitle(), resultStyle);
        GUILayout.Label(lastResultMessage, resultStyle);
        GUILayout.Space(8f);
        GUILayout.Label($"Time Remaining: {FormatTime(remainingSeconds)}", resultStyle);

        if (inventory != null)
        {
            GUILayout.Label($"Inventory: {inventory.Items.Count}/{inventory.MaxSlots} stacks   Weight: {inventory.CurrentWeight:0.0}/{inventory.MaxWeight:0.0}", resultStyle);
            GUILayout.Space(4f);

            if (inventory.IsEmpty)
            {
                GUILayout.Label("No loot carried.", resultStyle);
            }
            else
            {
                int visibleEntries = Mathf.Min(6, inventory.Items.Count);
                for (int index = 0; index < visibleEntries; index++)
                {
                    ItemInstance item = inventory.Items[index];
                    if (item == null || !item.IsDefined())
                    {
                        continue;
                    }

                    GUILayout.Label($"- {item.DisplayName} x{item.Quantity}", resultStyle);
                }

                if (inventory.Items.Count > visibleEntries)
                {
                    GUILayout.Label($"- ...and {inventory.Items.Count - visibleEntries} more stacks", resultStyle);
                }
            }
        }

        GUILayout.EndArea();
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

    private void DrawExtractionProgress(Rect rect, ExtractionZone zone)
    {
        GUI.Box(rect, GUIContent.none, hudStyle);

        Rect fillRect = new Rect(rect.x + 3f, rect.y + 22f, Mathf.Max(0f, (rect.width - 6f) * zone.ExtractionProgressNormalized), rect.height - 25f);
        Color previousColor = GUI.color;
        GUI.color = new Color(0.22f, 0.86f, 0.48f, 0.95f);
        GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
        GUI.color = previousColor;

        GUI.Label(
            new Rect(rect.x + 10f, rect.y + 4f, rect.width - 20f, 18f),
            $"Extracting {zone.ExtractionName}  {zone.ExtractionRemainingSeconds:0.0}s",
            hudStyle);
    }

    private void EnsureHudStyles()
    {
        if (hudStyle == null)
        {
            hudStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 14,
                normal = { textColor = Color.white }
            };
            hudStyle.padding = new RectOffset(12, 12, 10, 10);
        }

        if (resultStyle == null)
        {
            resultStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                normal = { textColor = Color.white }
            };
            resultStyle.padding = new RectOffset(16, 16, 12, 12);
        }
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
}
