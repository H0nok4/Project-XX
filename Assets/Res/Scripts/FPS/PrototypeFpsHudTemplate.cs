using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PrototypeFpsHudTemplate : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private TMP_Text crosshairText;
    [SerializeField] private TMP_Text controlsText;
    [SerializeField] private RectTransform staminaTrack;
    [SerializeField] private Image staminaFillImage;
    [SerializeField] private TMP_Text staminaLabelText;
    [SerializeField] private TMP_Text weaponInfoText;

    public RectTransform Root => root != null ? root : transform as RectTransform;
    public TMP_Text CrosshairText => crosshairText;
    public TMP_Text ControlsText => controlsText;
    public RectTransform StaminaTrack => staminaTrack;
    public Image StaminaFillImage => staminaFillImage;
    public TMP_Text StaminaLabelText => staminaLabelText;
    public TMP_Text WeaponInfoText => weaponInfoText;

    public void ConfigureReferences(
        RectTransform rectTransform,
        TMP_Text crosshair,
        TMP_Text controls,
        RectTransform staminaTrackRect,
        Image staminaFill,
        TMP_Text staminaLabel,
        TMP_Text weaponInfo)
    {
        root = rectTransform;
        crosshairText = crosshair;
        controlsText = controls;
        staminaTrack = staminaTrackRect;
        staminaFillImage = staminaFill;
        staminaLabelText = staminaLabel;
        weaponInfoText = weaponInfo;
    }
}
