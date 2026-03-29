using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerThirdPersonWeaponPresentationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerAnimationRigRefs rigRefs;
    [SerializeField] private PlayerStateHub stateHub;
    [SerializeField] private PlayerWeaponController weaponController;
    [SerializeField] private Transform thirdPersonWeaponSocket;

    [Header("Offsets")]
    [SerializeField] private Vector3 primaryLocalPosition = Vector3.zero;
    [SerializeField] private Vector3 primaryLocalEulerAngles = Vector3.zero;
    [SerializeField] private Vector3 secondaryLocalPosition = Vector3.zero;
    [SerializeField] private Vector3 secondaryLocalEulerAngles = Vector3.zero;
    [SerializeField] private Vector3 meleeLocalPosition = Vector3.zero;
    [SerializeField] private Vector3 meleeLocalEulerAngles = Vector3.zero;

    private GameObject currentWeaponInstance;
    private PrototypeWeaponDefinition currentWeaponDefinition;
    private PlayerWeaponSlotType currentSlotType = PlayerWeaponSlotType.None;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnValidate()
    {
        ResolveReferences();
    }

    private void LateUpdate()
    {
        ResolveReferences();
        EnsureSocket();
        RefreshPresentation();
    }

    public void ApplyHostSettings(PlayerAnimationRigRefs hostRigRefs, PlayerStateHub hostStateHub, PlayerWeaponController hostWeaponController)
    {
        if (hostRigRefs != null)
        {
            rigRefs = hostRigRefs;
        }

        if (hostStateHub != null)
        {
            stateHub = hostStateHub;
        }

        if (hostWeaponController != null)
        {
            weaponController = hostWeaponController;
        }

        EnsureSocket();
    }

    public void RefreshPresentation()
    {
        EnsureSocket();

        if (thirdPersonWeaponSocket == null || weaponController == null)
        {
            DestroyCurrentWeaponInstance();
            return;
        }

        PlayerWeaponSlotType activeSlot = weaponController.ActiveWeaponSlotType;
        PrototypeWeaponDefinition activeDefinition = weaponController.ActiveWeaponDefinition;
        GameObject nextPrefab = activeDefinition != null ? activeDefinition.FirstPersonViewPrefab : null;
        bool hideForUtilityAction = stateHub != null && (stateHub.Snapshot.UpperBodyAction == PlayerUpperBodyAction.Medical
            || stateHub.Snapshot.UpperBodyAction == PlayerUpperBodyAction.Throwable);

        if (activeDefinition == null || nextPrefab == null)
        {
            DestroyCurrentWeaponInstance();
            currentWeaponDefinition = null;
            currentSlotType = PlayerWeaponSlotType.None;
            return;
        }

        if (currentWeaponInstance == null || currentWeaponDefinition != activeDefinition || currentSlotType != activeSlot)
        {
            DestroyCurrentWeaponInstance();
            currentWeaponInstance = Instantiate(nextPrefab, thirdPersonWeaponSocket, false);
            currentWeaponInstance.name = nextPrefab.name;
            SetLayerRecursively(currentWeaponInstance, LayerMask.NameToLayer("LocalFullBody"));
            currentWeaponDefinition = activeDefinition;
            currentSlotType = activeSlot;
        }

        currentWeaponInstance.SetActive(!hideForUtilityAction);
        if (!hideForUtilityAction)
        {
            ApplySlotOffset(currentWeaponInstance.transform, activeSlot);
        }
    }

    private void ResolveReferences()
    {
        if (rigRefs == null)
        {
            rigRefs = GetComponent<PlayerAnimationRigRefs>();
        }

        if (stateHub == null)
        {
            stateHub = GetComponent<PlayerStateHub>();
        }

        if (weaponController == null)
        {
            weaponController = GetComponent<PlayerWeaponController>();
        }
    }

    private void EnsureSocket()
    {
        if (thirdPersonWeaponSocket != null)
        {
            return;
        }

        Transform rigRoot = rigRefs != null ? rigRefs.CharacterVisualRigRoot : null;
        if (rigRoot == null)
        {
            return;
        }

        Transform hand = rigRoot.Find("素体/Armature/root/pelvis/spine_01/spine_02/spine_03/clavicle_r/upperarm_r/lowerarm_r/hand_r");
        if (hand == null)
        {
            return;
        }

        Transform socket = hand.Find("ThirdPersonWeaponSocket");
        if (socket == null)
        {
            GameObject socketObject = new GameObject("ThirdPersonWeaponSocket");
            socket = socketObject.transform;
            socket.SetParent(hand, false);

            Vector3 parentLossyScale = hand.lossyScale;
            socket.localScale = new Vector3(
                parentLossyScale.x != 0f ? 1f / parentLossyScale.x : 1f,
                parentLossyScale.y != 0f ? 1f / parentLossyScale.y : 1f,
                parentLossyScale.z != 0f ? 1f / parentLossyScale.z : 1f);
        }

        thirdPersonWeaponSocket = socket;
    }

    private void ApplySlotOffset(Transform weaponTransform, PlayerWeaponSlotType slotType)
    {
        if (weaponTransform == null)
        {
            return;
        }

        switch (slotType)
        {
            case PlayerWeaponSlotType.Primary:
                weaponTransform.localPosition = primaryLocalPosition;
                weaponTransform.localRotation = Quaternion.Euler(primaryLocalEulerAngles);
                break;

            case PlayerWeaponSlotType.Secondary:
                weaponTransform.localPosition = secondaryLocalPosition;
                weaponTransform.localRotation = Quaternion.Euler(secondaryLocalEulerAngles);
                break;

            case PlayerWeaponSlotType.Melee:
                weaponTransform.localPosition = meleeLocalPosition;
                weaponTransform.localRotation = Quaternion.Euler(meleeLocalEulerAngles);
                break;

            default:
                weaponTransform.localPosition = Vector3.zero;
                weaponTransform.localRotation = Quaternion.identity;
                break;
        }
    }

    private void DestroyCurrentWeaponInstance()
    {
        if (currentWeaponInstance == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(currentWeaponInstance);
        }
        else
        {
            DestroyImmediate(currentWeaponInstance);
        }

        currentWeaponInstance = null;
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        if (root == null || layer < 0)
        {
            return;
        }

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            child.gameObject.layer = layer;
        }
    }
}
