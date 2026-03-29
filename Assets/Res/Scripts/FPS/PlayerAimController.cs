using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerAimController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera viewCamera;
    [SerializeField] private PlayerWeaponController weaponController;
    [SerializeField] private PrototypeFpsMovementModule movementModule;

    [Header("Aim")]
    [SerializeField] private float fallbackAimFieldOfView = 58f;
    [SerializeField] private float fallbackAimInDuration = 0.12f;
    [SerializeField] private float fallbackAimOutDuration = 0.1f;
    [SerializeField, Range(0f, 1f)] private float crosshairHideThreshold = 0.2f;

    private float hipFieldOfView = 75f;
    private float aimBlend;

    public bool IsAiming => aimBlend >= 0.999f;
    public float AimBlend => aimBlend;
    public bool ShouldHideHipFireCrosshair => aimBlend >= crosshairHideThreshold;

    private void Awake()
    {
        ResolveReferences();
        CacheHipFieldOfView();
    }

    private void OnDisable()
    {
        ResetAimImmediately();
    }

    public void ApplyHostSettings(
        Camera hostCamera,
        PlayerWeaponController hostWeaponController,
        PrototypeFpsMovementModule hostMovementModule)
    {
        if (hostCamera != null)
        {
            viewCamera = hostCamera;
        }

        if (hostWeaponController != null)
        {
            weaponController = hostWeaponController;
        }

        if (hostMovementModule != null)
        {
            movementModule = hostMovementModule;
        }

        CacheHipFieldOfView();
    }

    public void TickAim(PrototypeFpsInput fpsInput, bool uiFocused)
    {
        ResolveReferences();

        bool canAim = CanAim(fpsInput, uiFocused);
        float targetBlend = canAim ? 1f : 0f;
        float targetFieldOfView = hipFieldOfView;
        float targetSpreadMultiplier = 1f;
        float transitionDuration = targetBlend > aimBlend
            ? Mathf.Max(0.01f, fallbackAimInDuration)
            : Mathf.Max(0.01f, fallbackAimOutDuration);

        if (weaponController != null
            && weaponController.TryGetActiveAimSettings(
                out float weaponAimFieldOfView,
                out float weaponAimSpreadMultiplier,
                out float weaponAimInDuration,
                out float weaponAimOutDuration))
        {
            targetFieldOfView = weaponAimFieldOfView;
            targetSpreadMultiplier = weaponAimSpreadMultiplier;
            transitionDuration = targetBlend > aimBlend
                ? Mathf.Max(0.01f, weaponAimInDuration)
                : Mathf.Max(0.01f, weaponAimOutDuration);
        }
        else
        {
            targetFieldOfView = Mathf.Clamp(fallbackAimFieldOfView, 1f, 179f);
        }

        float blendStep = transitionDuration > 0.0001f ? Time.deltaTime / transitionDuration : 1f;
        aimBlend = Mathf.MoveTowards(aimBlend, targetBlend, blendStep);

        if (viewCamera != null)
        {
            viewCamera.fieldOfView = Mathf.Lerp(hipFieldOfView, targetFieldOfView, aimBlend);
        }

        weaponController?.SetExternalSpreadMultiplier(Mathf.Lerp(1f, targetSpreadMultiplier, aimBlend));
    }

    public void ResetAimImmediately()
    {
        aimBlend = 0f;

        if (viewCamera != null)
        {
            viewCamera.fieldOfView = hipFieldOfView;
        }

        weaponController?.SetExternalSpreadMultiplier(1f);
    }

    private bool CanAim(PrototypeFpsInput fpsInput, bool uiFocused)
    {
        if (uiFocused || fpsInput == null || !fpsInput.AimHeld || viewCamera == null || weaponController == null)
        {
            return false;
        }

        if (movementModule != null && movementModule.IsSprinting)
        {
            return false;
        }

        return weaponController.CanAimActiveWeapon;
    }

    private void ResolveReferences()
    {
        if (viewCamera == null)
        {
            viewCamera = GetComponentInChildren<Camera>();
        }

        if (weaponController == null)
        {
            weaponController = GetComponent<PlayerWeaponController>();
        }

        if (movementModule == null)
        {
            movementModule = GetComponent<PrototypeFpsMovementModule>();
        }
    }

    private void CacheHipFieldOfView()
    {
        if (viewCamera == null)
        {
            return;
        }

        hipFieldOfView = Mathf.Clamp(viewCamera.fieldOfView, 1f, 179f);
    }
}
