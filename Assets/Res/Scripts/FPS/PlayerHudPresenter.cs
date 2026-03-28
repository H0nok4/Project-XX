using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerHudPresenter : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool showHud = true;

    [Header("References")]
    [SerializeField] private PlayerStateHub stateHub;

    private PrototypeFpsHudView hudView;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    public void ApplyHostSettings(bool hostShowHud, PlayerStateHub hostStateHub)
    {
        showHud = hostShowHud;

        if (hostStateHub != null)
        {
            stateHub = hostStateHub;
        }
    }

    public void RefreshHud()
    {
        ResolveReferences();
        EnsureHudUi();

        if (!showHud)
        {
            hudView?.SetHudVisible(false);
            return;
        }

        if (stateHub == null)
        {
            hudView?.UpdateHud(true, true, false, 0f, Color.white, string.Empty, "StateHub missing");
            return;
        }

        PlayerRuntimeStateSnapshot snapshot = stateHub.Snapshot;
        hudView?.UpdateHud(
            true,
            snapshot.ShowCrosshair,
            snapshot.ShowHitMarker,
            snapshot.StaminaNormalized,
            snapshot.StaminaColor,
            snapshot.StaminaLabel ?? string.Empty,
            snapshot.HudDetailText ?? string.Empty);
    }

    public void SetHudVisible(bool visible)
    {
        if (!visible && hudView == null)
        {
            return;
        }

        EnsureHudUi();
        hudView?.SetHudVisible(visible);
    }

    private void EnsureHudUi()
    {
        hudView ??= PrototypeFpsHudView.GetOrCreate();
    }

    private void ResolveReferences()
    {
        if (stateHub == null)
        {
            stateHub = GetComponent<PlayerStateHub>();
        }
    }
}
