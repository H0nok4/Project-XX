using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerOrientationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerAnimationRigRefs rigRefs;
    [SerializeField] private PrototypeFpsMovementModule movementModule;
    [SerializeField] private PlayerAimController aimController;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Transform hitboxRoot;

    [Header("Turning")]
    [SerializeField] private float locomotionTurnSpeed = 540f;
    [SerializeField] private float aimTurnSpeed = 720f;
    [SerializeField] private float movementFacingSpeedThreshold = 0.15f;
    [SerializeField] private float facingCameraTolerance = 4f;

    private Transform cachedVisualRoot;
    private Transform cachedHitboxRoot;
    private Quaternion visualBaseLocalRotation = Quaternion.identity;
    private Quaternion hitboxBaseLocalRotation = Quaternion.identity;

    public float BodyYawDeltaToCamera
    {
        get
        {
            Transform primaryRoot = visualRoot != null ? visualRoot : hitboxRoot;
            Quaternion baseRotation = primaryRoot == visualRoot ? visualBaseLocalRotation : hitboxBaseLocalRotation;
            return primaryRoot != null ? GetCurrentLocalYaw(primaryRoot, baseRotation) : 0f;
        }
    }

    public bool IsFacingCameraYaw => Mathf.Abs(BodyYawDeltaToCamera) <= facingCameraTolerance;
    public float BodyWorldYaw => visualRoot != null ? visualRoot.eulerAngles.y : transform.eulerAngles.y;

    private void Awake()
    {
        ResolveReferences();
        ClampSettings();
        CacheBaseRotations();
    }

    private void OnEnable()
    {
        ResolveReferences();
        ClampSettings();
        CacheBaseRotations();
        SnapBodyToCameraYaw();
    }

    private void OnValidate()
    {
        ResolveReferences();
        ClampSettings();
        CacheBaseRotations();
    }

    public void ApplyHostSettings(
        PrototypeFpsMovementModule hostMovementModule,
        PlayerAimController hostAimController)
    {
        if (hostMovementModule != null)
        {
            movementModule = hostMovementModule;
        }

        if (hostAimController != null)
        {
            aimController = hostAimController;
        }
    }

    public void TickOrientation()
    {
        ResolveReferences();
        if (!HasControllableRoot())
        {
            return;
        }

        bool useAimFacing = aimController != null && aimController.AimBlend > 0.01f;
        float targetLocalYaw = BodyYawDeltaToCamera;
        if (useAimFacing)
        {
            targetLocalYaw = 0f;
        }
        else if (movementModule != null)
        {
            Vector3 planarVelocity = movementModule.PlanarVelocity;
            planarVelocity.y = 0f;
            if (planarVelocity.sqrMagnitude >= movementFacingSpeedThreshold * movementFacingSpeedThreshold)
            {
                float desiredWorldYaw = Quaternion.LookRotation(planarVelocity.normalized, Vector3.up).eulerAngles.y;
                targetLocalYaw = Mathf.DeltaAngle(transform.eulerAngles.y, desiredWorldYaw);
            }
        }

        float turnSpeed = useAimFacing ? aimTurnSpeed : locomotionTurnSpeed;
        ApplyLocalYawOffset(targetLocalYaw, turnSpeed);
    }

    public void SnapBodyToCameraYaw()
    {
        CacheBaseRotations();
        ApplyRootRotation(visualRoot, visualBaseLocalRotation, 0f);
        ApplyRootRotation(hitboxRoot, hitboxBaseLocalRotation, 0f);
    }

    private void ResolveReferences()
    {
        if (rigRefs == null)
        {
            rigRefs = GetComponent<PlayerAnimationRigRefs>();
        }

        if (movementModule == null)
        {
            movementModule = GetComponent<PrototypeFpsMovementModule>();
        }

        if (aimController == null)
        {
            aimController = GetComponent<PlayerAimController>();
        }

        if (visualRoot == null)
        {
            visualRoot = rigRefs != null ? rigRefs.CharacterVisualRigRoot : null;
        }

        if (hitboxRoot == null)
        {
            hitboxRoot = rigRefs != null ? rigRefs.HitboxRigRoot : null;
        }
    }

    private void ClampSettings()
    {
        locomotionTurnSpeed = Mathf.Max(0f, locomotionTurnSpeed);
        aimTurnSpeed = Mathf.Max(0f, aimTurnSpeed);
        movementFacingSpeedThreshold = Mathf.Max(0f, movementFacingSpeedThreshold);
        facingCameraTolerance = Mathf.Clamp(facingCameraTolerance, 0.1f, 45f);
    }

    private void CacheBaseRotations()
    {
        if (visualRoot != cachedVisualRoot)
        {
            cachedVisualRoot = visualRoot;
            visualBaseLocalRotation = visualRoot != null ? visualRoot.localRotation : Quaternion.identity;
        }

        if (hitboxRoot != cachedHitboxRoot)
        {
            cachedHitboxRoot = hitboxRoot;
            hitboxBaseLocalRotation = hitboxRoot != null ? hitboxRoot.localRotation : Quaternion.identity;
        }
    }

    private bool HasControllableRoot()
    {
        return visualRoot != null || hitboxRoot != null;
    }

    private void ApplyLocalYawOffset(float targetLocalYaw, float turnSpeed)
    {
        if (visualRoot != null)
        {
            float currentYaw = GetCurrentLocalYaw(visualRoot, visualBaseLocalRotation);
            float nextYaw = Mathf.MoveTowardsAngle(currentYaw, targetLocalYaw, turnSpeed * Time.deltaTime);
            ApplyRootRotation(visualRoot, visualBaseLocalRotation, nextYaw);
        }

        if (hitboxRoot != null)
        {
            float currentYaw = GetCurrentLocalYaw(hitboxRoot, hitboxBaseLocalRotation);
            float nextYaw = Mathf.MoveTowardsAngle(currentYaw, targetLocalYaw, turnSpeed * Time.deltaTime);
            ApplyRootRotation(hitboxRoot, hitboxBaseLocalRotation, nextYaw);
        }
    }

    private static float GetCurrentLocalYaw(Transform targetRoot, Quaternion baseLocalRotation)
    {
        Quaternion delta = targetRoot.localRotation * Quaternion.Inverse(baseLocalRotation);
        return NormalizeSignedAngle(delta.eulerAngles.y);
    }

    private static void ApplyRootRotation(Transform targetRoot, Quaternion baseLocalRotation, float localYaw)
    {
        if (targetRoot == null)
        {
            return;
        }

        targetRoot.localRotation = Quaternion.AngleAxis(localYaw, Vector3.up) * baseLocalRotation;
    }

    private static float NormalizeSignedAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f)
        {
            angle -= 360f;
        }

        return angle;
    }
}
