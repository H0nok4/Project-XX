using System;
using System.Reflection;
using UnityEngine;
using Unity.Cinemachine;

[DisallowMultipleComponent]
public sealed class PlayerShoulderCameraController : MonoBehaviour
{
    private static readonly FieldInfo AvoidObstaclesField =
        typeof(CinemachineThirdPersonFollow).GetField("AvoidObstacles", BindingFlags.Public | BindingFlags.Instance);

    private static readonly Type ObstacleSettingsType =
        typeof(CinemachineThirdPersonFollow).GetNestedType("ObstacleSettings", BindingFlags.Public | BindingFlags.NonPublic);

    private static readonly FieldInfo ObstacleEnabledField =
        ObstacleSettingsType?.GetField("Enabled", BindingFlags.Public | BindingFlags.Instance);

    private static readonly FieldInfo ObstacleCollisionFilterField =
        ObstacleSettingsType?.GetField("CollisionFilter", BindingFlags.Public | BindingFlags.Instance);

    private static readonly FieldInfo ObstacleIgnoreTagField =
        ObstacleSettingsType?.GetField("IgnoreTag", BindingFlags.Public | BindingFlags.Instance);

    private static readonly FieldInfo ObstacleCameraRadiusField =
        ObstacleSettingsType?.GetField("CameraRadius", BindingFlags.Public | BindingFlags.Instance);

    private static readonly FieldInfo ObstacleDampingIntoField =
        ObstacleSettingsType?.GetField("DampingIntoCollision", BindingFlags.Public | BindingFlags.Instance);

    private static readonly FieldInfo ObstacleDampingFromField =
        ObstacleSettingsType?.GetField("DampingFromCollision", BindingFlags.Public | BindingFlags.Instance);

    [Header("References")]
    [SerializeField] private PlayerAnimationRigRefs rigRefs;
    [SerializeField] private PrototypeFpsInput fpsInput;
    [SerializeField] private PlayerInteractionState interactionState;
    [SerializeField] private PlayerAimController aimController;
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private Camera renderCamera;
    [SerializeField] private Transform shoulderFollowTarget;
    [SerializeField] private CinemachineCamera shoulderCamera;
    [SerializeField] private CinemachineBrain renderCameraBrain;
    [SerializeField] private CinemachineThirdPersonFollow thirdPersonFollow;

    [Header("Zoom")]
    [SerializeField] private float defaultDistance = 2.8f;
    [SerializeField] private float minDistance = 1.75f;
    [SerializeField] private float maxDistance = 4.2f;
    [SerializeField] private float zoomStep = 0.25f;
    [SerializeField] private float distanceDamping = 14f;
    [SerializeField] private float aimDistance = 1.6f;

    [Header("Rig")]
    [SerializeField] private Vector3 shoulderOffset = new Vector3(0.58f, -0.22f, 0f);
    [SerializeField] private float verticalArmLength = 0.18f;
    [SerializeField, Range(0f, 1f)] private float cameraSide = 1f;
    [SerializeField] private Vector3 followDamping = new Vector3(0.08f, 0.12f, 0.1f);

    [Header("Collision")]
    [SerializeField] private bool avoidObstacles = true;
    [SerializeField] private LayerMask obstacleLayers;
    [SerializeField] private string ignoreTag = "Player";
    [SerializeField, Range(0.01f, 0.5f)] private float cameraRadius = 0.08f;
    [SerializeField, Range(0f, 2f)] private float collisionDampingIn = 0.08f;
    [SerializeField, Range(0f, 2f)] private float collisionDampingOut = 0.2f;

    private float userDistance;

    public float CurrentDistance => thirdPersonFollow != null
        ? thirdPersonFollow.CameraDistance
        : Mathf.Clamp(userDistance > 0f ? userDistance : defaultDistance, minDistance, maxDistance);

    public float CameraYaw => renderCamera != null
        ? renderCamera.transform.eulerAngles.y
        : gameplayCamera != null
            ? gameplayCamera.transform.eulerAngles.y
            : transform.eulerAngles.y;

    public bool IsAimCamera => aimController != null && aimController.AimBlend > 0.01f;
    public bool IsShoulderRight => cameraSide >= 0.5f;

    private void Awake()
    {
        ResolveReferences();
        ClampSettings();
        if (userDistance <= 0f)
        {
            userDistance = defaultDistance;
        }

        ApplyCameraStackState();
        ApplyThirdPersonSettings(true);
    }

    private void OnEnable()
    {
        ResolveReferences();
        ClampSettings();
        ApplyCameraStackState();
        ApplyThirdPersonSettings(true);
    }

    private void OnDisable()
    {
        SetGameplayCameraRenderingEnabled(true);
        SetRenderCameraRenderingEnabled(false);
    }

    private void OnValidate()
    {
        ResolveReferences();
        ClampSettings();
        if (!Application.isPlaying && userDistance <= 0f)
        {
            userDistance = defaultDistance;
        }
    }

    private void LateUpdate()
    {
        ResolveReferences();
        if (gameplayCamera == null || renderCamera == null || shoulderCamera == null || thirdPersonFollow == null)
        {
            return;
        }

        UpdateZoomInput();
        ApplyCameraStackState();
        ApplyThirdPersonSettings(false);
        SyncLensFromGameplayCamera();
    }

    private void UpdateZoomInput()
    {
        if (fpsInput == null || (interactionState != null && interactionState.IsUiFocused))
        {
            return;
        }

        if (fpsInput.SpeedAdjustModifierHeld)
        {
            return;
        }

        float scrollDelta = fpsInput.MoveSpeedScrollDelta;
        if (Mathf.Abs(scrollDelta) <= 0.01f)
        {
            return;
        }

        userDistance = Mathf.Clamp(
            userDistance - Mathf.Sign(scrollDelta) * zoomStep,
            minDistance,
            maxDistance);
    }

    private void ApplyCameraStackState()
    {
        SetGameplayCameraRenderingEnabled(false);
        SetRenderCameraRenderingEnabled(true);

        if (shoulderCamera == null || shoulderFollowTarget == null)
        {
            return;
        }

        shoulderCamera.Target.TrackingTarget = shoulderFollowTarget;
        shoulderCamera.Target.CustomLookAtTarget = false;
        shoulderCamera.Target.LookAtTarget = null;
        shoulderCamera.Priority = 100;
    }

    private void ApplyThirdPersonSettings(bool instant)
    {
        if (thirdPersonFollow == null)
        {
            return;
        }

        float currentAimBlend = aimController != null ? aimController.AimBlend : 0f;
        float targetDistance = Mathf.Lerp(userDistance, Mathf.Min(userDistance, aimDistance), currentAimBlend);
        float currentDistance = thirdPersonFollow.CameraDistance;
        float nextDistance = instant
            ? targetDistance
            : Mathf.Lerp(currentDistance, targetDistance, 1f - Mathf.Exp(-distanceDamping * Time.deltaTime));

        thirdPersonFollow.ShoulderOffset = shoulderOffset;
        thirdPersonFollow.VerticalArmLength = verticalArmLength;
        thirdPersonFollow.CameraSide = cameraSide;
        thirdPersonFollow.Damping = followDamping;
        thirdPersonFollow.CameraDistance = nextDistance;
        ApplyObstacleSettings();
    }

    private void SyncLensFromGameplayCamera()
    {
        if (gameplayCamera == null || shoulderCamera == null)
        {
            return;
        }

        LensSettings lens = shoulderCamera.Lens;
        lens.FieldOfView = gameplayCamera.fieldOfView;
        shoulderCamera.Lens = lens;
    }

    private void SetGameplayCameraRenderingEnabled(bool enabled)
    {
        if (gameplayCamera == null)
        {
            return;
        }

        gameplayCamera.enabled = enabled;
        if (enabled)
        {
            gameplayCamera.tag = "MainCamera";
        }
        else if (gameplayCamera.CompareTag("MainCamera"))
        {
            gameplayCamera.tag = "Untagged";
        }

        AudioListener listener = gameplayCamera.GetComponent<AudioListener>();
        if (listener != null)
        {
            listener.enabled = enabled;
        }
    }

    private void SetRenderCameraRenderingEnabled(bool enabled)
    {
        if (renderCamera == null)
        {
            return;
        }

        renderCamera.gameObject.SetActive(enabled);
        renderCamera.enabled = enabled;
        if (enabled)
        {
            renderCamera.tag = "MainCamera";
        }
        else if (renderCamera.CompareTag("MainCamera"))
        {
            renderCamera.tag = "Untagged";
        }

        if (renderCameraBrain != null)
        {
            renderCameraBrain.enabled = enabled;
        }

        AudioListener listener = renderCamera.GetComponent<AudioListener>();
        if (listener != null)
        {
            listener.enabled = enabled;
        }
    }

    private void ResolveReferences()
    {
        if (rigRefs == null)
        {
            rigRefs = GetComponent<PlayerAnimationRigRefs>();
        }

        if (fpsInput == null)
        {
            fpsInput = GetComponent<PrototypeFpsInput>();
        }

        if (interactionState == null)
        {
            interactionState = GetComponent<PlayerInteractionState>();
        }

        if (aimController == null)
        {
            aimController = GetComponent<PlayerAimController>();
        }

        if (gameplayCamera == null)
        {
            gameplayCamera = rigRefs != null ? rigRefs.ViewCamera : null;
        }

        if (renderCamera == null)
        {
            renderCamera = rigRefs != null ? rigRefs.RenderCamera : null;
        }

        if (shoulderFollowTarget == null)
        {
            shoulderFollowTarget = rigRefs != null ? rigRefs.ShoulderFollowTarget : null;
        }

        if (shoulderCamera == null)
        {
            shoulderCamera = rigRefs != null ? rigRefs.ShoulderCamera : null;
        }

        if (renderCameraBrain == null && renderCamera != null)
        {
            renderCameraBrain = renderCamera.GetComponent<CinemachineBrain>();
        }

        if (thirdPersonFollow == null && shoulderCamera != null)
        {
            thirdPersonFollow = shoulderCamera.GetComponent<CinemachineThirdPersonFollow>();
        }
    }

    private void ClampSettings()
    {
        minDistance = Mathf.Max(0.5f, minDistance);
        maxDistance = Mathf.Max(minDistance, maxDistance);
        defaultDistance = Mathf.Clamp(defaultDistance, minDistance, maxDistance);
        aimDistance = Mathf.Clamp(aimDistance, minDistance, maxDistance);
        zoomStep = Mathf.Max(0.01f, zoomStep);
        distanceDamping = Mathf.Max(0.01f, distanceDamping);
        verticalArmLength = Mathf.Max(0f, verticalArmLength);
        cameraSide = Mathf.Clamp01(cameraSide);
        followDamping.x = Mathf.Max(0f, followDamping.x);
        followDamping.y = Mathf.Max(0f, followDamping.y);
        followDamping.z = Mathf.Max(0f, followDamping.z);
        cameraRadius = Mathf.Clamp(cameraRadius, 0.01f, 0.5f);
        collisionDampingIn = Mathf.Max(0f, collisionDampingIn);
        collisionDampingOut = Mathf.Max(0f, collisionDampingOut);
        if (obstacleLayers.value == 0 || obstacleLayers.value == ~0)
        {
            obstacleLayers = GetDefaultObstacleLayers();
        }
    }

    private void ApplyObstacleSettings()
    {
        if (thirdPersonFollow == null || AvoidObstaclesField == null)
        {
            return;
        }

        object obstacleSettings = AvoidObstaclesField.GetValue(thirdPersonFollow);
        if (obstacleSettings == null)
        {
            return;
        }

        ObstacleEnabledField?.SetValue(obstacleSettings, avoidObstacles);
        ObstacleCollisionFilterField?.SetValue(obstacleSettings, obstacleLayers);
        ObstacleIgnoreTagField?.SetValue(obstacleSettings, ignoreTag ?? string.Empty);
        ObstacleCameraRadiusField?.SetValue(obstacleSettings, cameraRadius);
        ObstacleDampingIntoField?.SetValue(obstacleSettings, collisionDampingIn);
        ObstacleDampingFromField?.SetValue(obstacleSettings, collisionDampingOut);
        AvoidObstaclesField.SetValue(thirdPersonFollow, obstacleSettings);
    }

    private static LayerMask GetDefaultObstacleLayers()
    {
        int namedMask = LayerMask.GetMask("SceneObject", "Ground");
        return namedMask != 0 ? namedMask : Physics.DefaultRaycastLayers;
    }
}
