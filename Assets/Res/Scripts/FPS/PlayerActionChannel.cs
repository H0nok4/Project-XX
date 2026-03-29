using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerActionChannel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInteractionState interactionState;
    [SerializeField] private PrototypeUnitVitals playerVitals;
    [SerializeField] private PlayerWeaponController weaponController;
    [SerializeField] private PlayerMedicalController medicalController;
    [SerializeField] private PlayerThrowableController throwableController;

    private PlayerUpperBodyAction gameplayAction = PlayerUpperBodyAction.Idle;

    public PlayerUpperBodyAction GameplayAction => gameplayAction;
    public bool IsMedicalActionActive => medicalController != null && medicalController.IsMedicalActionActive;
    public bool IsThrowActionActive => throwableController != null && throwableController.IsThrowActionActive;
    public bool IsBlockingUpperBodyInput => IsMedicalActionActive || IsThrowActionActive;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    public void ApplyHostSettings(
        PlayerInteractionState hostInteractionState,
        PrototypeUnitVitals hostVitals,
        PlayerWeaponController hostWeaponController,
        PlayerMedicalController hostMedicalController,
        PlayerThrowableController hostThrowableController)
    {
        if (hostInteractionState != null)
        {
            interactionState = hostInteractionState;
        }

        if (hostVitals != null)
        {
            playerVitals = hostVitals;
        }

        if (hostWeaponController != null)
        {
            weaponController = hostWeaponController;
        }

        if (hostMedicalController != null)
        {
            medicalController = hostMedicalController;
        }

        if (hostThrowableController != null)
        {
            throwableController = hostThrowableController;
        }
    }

    public void BeginFrame()
    {
        ResolveReferences();
        gameplayAction = PlayerUpperBodyAction.Idle;
        weaponController?.BeginFrame();
        medicalController?.BeginFrame();
        throwableController?.BeginFrame();
    }

    public void ExecuteGameplayActions(PrototypeFpsInput fpsInput)
    {
        ResolveReferences();
        gameplayAction = PlayerUpperBodyAction.Idle;

        if (fpsInput == null)
        {
            return;
        }

        if (playerVitals != null && playerVitals.IsDead)
        {
            gameplayAction = PlayerUpperBodyAction.Dead;
            return;
        }

        if (interactionState != null && interactionState.IsUiFocused)
        {
            gameplayAction = PlayerUpperBodyAction.UiFocused;
            return;
        }

        if (IsMedicalActionActive)
        {
            gameplayAction = PlayerUpperBodyAction.Medical;
            return;
        }

        if (IsThrowActionActive)
        {
            gameplayAction = PlayerUpperBodyAction.Throwable;
            return;
        }

        bool usedMedical = medicalController != null && medicalController.HandleMedicalInput(fpsInput);
        if (usedMedical)
        {
            weaponController?.InterruptActiveUpperBodyAction();
            gameplayAction = PlayerUpperBodyAction.Medical;
            return;
        }

        bool usedThrowable = throwableController != null && throwableController.HandleThrowableInput(fpsInput);
        if (usedThrowable)
        {
            weaponController?.InterruptActiveUpperBodyAction();
            gameplayAction = PlayerUpperBodyAction.Throwable;
            return;
        }

        if (weaponController == null)
        {
            gameplayAction = PlayerUpperBodyAction.Idle;
            return;
        }

        weaponController.HandleCombat(fpsInput);

        if (weaponController.IsActiveWeaponReloading)
        {
            gameplayAction = PlayerUpperBodyAction.Reload;
            return;
        }

        gameplayAction = weaponController.HasEquippedWeapon
            ? PlayerUpperBodyAction.Weapon
            : PlayerUpperBodyAction.Idle;
    }

    public PlayerUpperBodyAction ResolveCurrentAction(bool isUiFocused, bool isDead)
    {
        if (isDead)
        {
            return PlayerUpperBodyAction.Dead;
        }

        if (isUiFocused)
        {
            return PlayerUpperBodyAction.UiFocused;
        }

        if (medicalController != null && medicalController.MedicalTriggeredThisFrame)
        {
            return PlayerUpperBodyAction.Medical;
        }

        if (IsMedicalActionActive)
        {
            return PlayerUpperBodyAction.Medical;
        }

        if (throwableController != null && throwableController.ThrowableTriggeredThisFrame)
        {
            return PlayerUpperBodyAction.Throwable;
        }

        if (IsThrowActionActive)
        {
            return PlayerUpperBodyAction.Throwable;
        }

        if (weaponController != null && weaponController.IsActiveWeaponReloading)
        {
            return PlayerUpperBodyAction.Reload;
        }

        return gameplayAction;
    }

    private void ResolveReferences()
    {
        if (interactionState == null)
        {
            interactionState = GetComponent<PlayerInteractionState>();
        }

        if (playerVitals == null)
        {
            playerVitals = GetComponent<PrototypeUnitVitals>();
        }

        if (weaponController == null)
        {
            weaponController = GetComponent<PlayerWeaponController>();
        }

        if (medicalController == null)
        {
            medicalController = GetComponent<PlayerMedicalController>();
        }

        if (throwableController == null)
        {
            throwableController = GetComponent<PlayerThrowableController>();
        }
    }
}
