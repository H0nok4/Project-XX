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

    [Header("Runtime")]
    [SerializeField] private RaidState currentState = RaidState.Idle;
    [SerializeField] private float remainingSeconds;
    [SerializeField] private string lastResultMessage = string.Empty;
    [SerializeField] private List<ExtractionZone> extractionZones = new List<ExtractionZone>();

    private GUIStyle hudStyle;
    private GUIStyle resultStyle;

    public event Action<RaidState> StateChanged;

    public RaidState CurrentState => currentState;
    public float RemainingSeconds => remainingSeconds;
    public string LastResultMessage => lastResultMessage;
    public bool IsRunning => currentState == RaidState.Running;

    private void Awake()
    {
        ResolveReferences();
        remainingSeconds = Mathf.Max(0f, raidDurationSeconds);

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
    }

    private void OnDisable()
    {
        if (playerVitals != null)
        {
            playerVitals.Died -= HandlePlayerDied;
        }
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

    public void StartRaid()
    {
        ResolveReferences();
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

        FailRaid("Player died.");
    }

    private void SetState(RaidState nextState)
    {
        if (currentState == nextState)
        {
            return;
        }

        currentState = nextState;
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
}
