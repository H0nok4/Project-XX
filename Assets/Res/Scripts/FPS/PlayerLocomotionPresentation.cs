using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerLocomotionPresentation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera viewCamera;

    [Header("View Motion")]
    [SerializeField] private bool headBobEnabled = true;
    [SerializeField] private float headBobFrequency = 7.5f;
    [SerializeField] private float headBobVerticalAmplitude = 0.014f;
    [SerializeField] private float headBobHorizontalAmplitude = 0.008f;
    [SerializeField] private float headBobActivationSpeed = 0.18f;
    [SerializeField, Range(1f, 2f)] private float sprintHeadBobAmplitudeMultiplier = 1.22f;
    [SerializeField, Range(1f, 2f)] private float sprintHeadBobFrequencyMultiplier = 1.12f;
    [SerializeField, Range(0.1f, 1f)] private float crouchHeadBobAmplitudeMultiplier = 0.72f;
    [SerializeField] private float headBobSmoothing = 14f;
    [SerializeField] private float headBobResetSpeed = 10f;
    [SerializeField] private float crouchCameraDrop = 0.32f;
    [SerializeField] private float crouchTransitionSpeed = 10f;

    private float standingCameraLocalY;
    private Vector3 standingCameraLocalPosition;
    private float headBobCycle;
    private Vector2 headBobOffset;

    private void Awake()
    {
        ClampSettings();
        CacheCameraDefaults();
    }

    private void OnValidate()
    {
        ClampSettings();
        CacheCameraDefaults();
    }

    public void ApplyHostSettings(
        Camera hostCamera,
        bool hostHeadBobEnabled,
        float hostHeadBobFrequency,
        float hostHeadBobVerticalAmplitude,
        float hostHeadBobHorizontalAmplitude,
        float hostHeadBobActivationSpeed,
        float hostSprintHeadBobAmplitudeMultiplier,
        float hostSprintHeadBobFrequencyMultiplier,
        float hostCrouchHeadBobAmplitudeMultiplier,
        float hostHeadBobSmoothing,
        float hostHeadBobResetSpeed,
        float hostCrouchCameraDrop,
        float hostCrouchTransitionSpeed)
    {
        viewCamera = hostCamera;
        headBobEnabled = hostHeadBobEnabled;
        headBobFrequency = hostHeadBobFrequency;
        headBobVerticalAmplitude = hostHeadBobVerticalAmplitude;
        headBobHorizontalAmplitude = hostHeadBobHorizontalAmplitude;
        headBobActivationSpeed = hostHeadBobActivationSpeed;
        sprintHeadBobAmplitudeMultiplier = hostSprintHeadBobAmplitudeMultiplier;
        sprintHeadBobFrequencyMultiplier = hostSprintHeadBobFrequencyMultiplier;
        crouchHeadBobAmplitudeMultiplier = hostCrouchHeadBobAmplitudeMultiplier;
        headBobSmoothing = hostHeadBobSmoothing;
        headBobResetSpeed = hostHeadBobResetSpeed;
        crouchCameraDrop = hostCrouchCameraDrop;
        crouchTransitionSpeed = hostCrouchTransitionSpeed;

        ClampSettings();
        CacheCameraDefaults();
    }

    public void SetViewCamera(Camera camera)
    {
        if (camera == null)
        {
            return;
        }

        viewCamera = camera;
        CacheCameraDefaults();
    }

    public void ResetPresentationImmediate(bool isCrouching)
    {
        headBobCycle = 0f;
        headBobOffset = Vector2.zero;

        if (viewCamera == null)
        {
            return;
        }

        Vector3 targetCameraPosition = standingCameraLocalPosition;
        targetCameraPosition.y = isCrouching
            ? standingCameraLocalY - crouchCameraDrop
            : standingCameraLocalY;
        viewCamera.transform.localPosition = targetCameraPosition;
    }

    public void TickPresentation(
        float deltaTime,
        bool grounded,
        bool uiFocused,
        bool isCrouching,
        bool isSprinting,
        float currentPlanarSpeed,
        float groundedTopSpeed,
        float sprintTopSpeed)
    {
        if (viewCamera == null)
        {
            return;
        }

        bool bobActive = UpdateHeadBob(
            deltaTime,
            grounded,
            uiFocused,
            isCrouching,
            isSprinting,
            currentPlanarSpeed,
            groundedTopSpeed,
            sprintTopSpeed);

        Vector3 targetCameraPosition = standingCameraLocalPosition;
        targetCameraPosition.y = isCrouching
            ? standingCameraLocalY - crouchCameraDrop + headBobOffset.y
            : standingCameraLocalY + headBobOffset.y;
        targetCameraPosition.x += headBobOffset.x;

        if (deltaTime <= 0f)
        {
            viewCamera.transform.localPosition = targetCameraPosition;
            return;
        }

        Vector3 cameraLocalPosition = viewCamera.transform.localPosition;
        float lateralSmoothing = bobActive ? headBobSmoothing : headBobResetSpeed;
        float lateralBlend = 1f - Mathf.Exp(-lateralSmoothing * deltaTime);
        cameraLocalPosition.x = Mathf.Lerp(cameraLocalPosition.x, targetCameraPosition.x, lateralBlend);
        cameraLocalPosition.y = Mathf.MoveTowards(
            cameraLocalPosition.y,
            targetCameraPosition.y,
            Mathf.Max(crouchTransitionSpeed, headBobSmoothing) * deltaTime);
        cameraLocalPosition.z = Mathf.Lerp(cameraLocalPosition.z, targetCameraPosition.z, lateralBlend);
        viewCamera.transform.localPosition = cameraLocalPosition;
    }

    private bool UpdateHeadBob(
        float deltaTime,
        bool grounded,
        bool uiFocused,
        bool isCrouching,
        bool isSprinting,
        float currentPlanarSpeed,
        float groundedTopSpeed,
        float sprintTopSpeed)
    {
        if (!headBobEnabled || viewCamera == null)
        {
            headBobOffset = Vector2.zero;
            headBobCycle = 0f;
            return false;
        }

        float bobTopSpeed = isSprinting
            ? sprintTopSpeed
            : Mathf.Max(headBobActivationSpeed + 0.01f, groundedTopSpeed);
        float bobStrength = grounded && !uiFocused
            ? Mathf.InverseLerp(headBobActivationSpeed, bobTopSpeed, currentPlanarSpeed)
            : 0f;
        bool bobActive = bobStrength > 0.001f;
        Vector2 targetBobOffset = Vector2.zero;

        if (bobActive)
        {
            float amplitudeMultiplier = bobStrength;
            if (isSprinting)
            {
                amplitudeMultiplier *= sprintHeadBobAmplitudeMultiplier;
            }
            else if (isCrouching)
            {
                amplitudeMultiplier *= crouchHeadBobAmplitudeMultiplier;
            }

            float frequencyMultiplier = Mathf.Lerp(0.7f, 1f, bobStrength);
            if (isSprinting)
            {
                frequencyMultiplier *= sprintHeadBobFrequencyMultiplier;
            }

            headBobCycle += deltaTime * headBobFrequency * frequencyMultiplier;
            float horizontalBob = Mathf.Sin(headBobCycle) * headBobHorizontalAmplitude * amplitudeMultiplier;
            float verticalBob = Mathf.Sin(headBobCycle * 2f) * headBobVerticalAmplitude * amplitudeMultiplier;
            targetBobOffset = new Vector2(horizontalBob, verticalBob);
        }

        float smoothing = bobActive ? headBobSmoothing : headBobResetSpeed;
        float blend = deltaTime > 0f ? 1f - Mathf.Exp(-smoothing * deltaTime) : 1f;
        headBobOffset = Vector2.Lerp(headBobOffset, targetBobOffset, blend);
        if (!bobActive && headBobOffset.sqrMagnitude <= 0.000001f)
        {
            headBobOffset = Vector2.zero;
            headBobCycle = 0f;
        }

        return bobActive;
    }

    private void ClampSettings()
    {
        headBobFrequency = Mathf.Max(0f, headBobFrequency);
        headBobVerticalAmplitude = Mathf.Max(0f, headBobVerticalAmplitude);
        headBobHorizontalAmplitude = Mathf.Max(0f, headBobHorizontalAmplitude);
        headBobActivationSpeed = Mathf.Max(0.01f, headBobActivationSpeed);
        sprintHeadBobAmplitudeMultiplier = Mathf.Clamp(sprintHeadBobAmplitudeMultiplier, 1f, 2f);
        sprintHeadBobFrequencyMultiplier = Mathf.Clamp(sprintHeadBobFrequencyMultiplier, 1f, 2f);
        crouchHeadBobAmplitudeMultiplier = Mathf.Clamp(crouchHeadBobAmplitudeMultiplier, 0.1f, 1f);
        headBobSmoothing = Mathf.Max(0.1f, headBobSmoothing);
        headBobResetSpeed = Mathf.Max(0.1f, headBobResetSpeed);
        crouchCameraDrop = Mathf.Max(crouchCameraDrop, 0.05f);
        crouchTransitionSpeed = Mathf.Max(crouchTransitionSpeed, 0.1f);
    }

    private void CacheCameraDefaults()
    {
        if (viewCamera != null)
        {
            standingCameraLocalPosition = viewCamera.transform.localPosition;
            standingCameraLocalY = standingCameraLocalPosition.y;
        }
        else if (standingCameraLocalY <= 0f)
        {
            standingCameraLocalPosition = new Vector3(0f, 0.72f, 0f);
            standingCameraLocalY = 0.72f;
        }
    }
}
