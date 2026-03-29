using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerMovementNoiseEmitter : MonoBehaviour
{
    [Header("AI Awareness")]
    [SerializeField] private float walkNoiseRadius = 4.8f;
    [SerializeField] private float sprintNoiseRadius = 11.5f;
    [SerializeField] private float jumpNoiseRadius = 7.5f;
    [SerializeField] private float landingNoiseRadius = 12f;
    [SerializeField] private float movementNoiseInterval = 0.42f;
    [SerializeField, Range(0.1f, 1f)] private float crouchNoiseMultiplier = 0.45f;

    private float nextMovementNoiseTime;

    private void Awake()
    {
        ClampSettings();
    }

    private void OnValidate()
    {
        ClampSettings();
    }

    public void ApplyHostSettings(
        float hostWalkNoiseRadius,
        float hostSprintNoiseRadius,
        float hostJumpNoiseRadius,
        float hostLandingNoiseRadius,
        float hostMovementNoiseInterval,
        float hostCrouchNoiseMultiplier)
    {
        walkNoiseRadius = hostWalkNoiseRadius;
        sprintNoiseRadius = hostSprintNoiseRadius;
        jumpNoiseRadius = hostJumpNoiseRadius;
        landingNoiseRadius = hostLandingNoiseRadius;
        movementNoiseInterval = hostMovementNoiseInterval;
        crouchNoiseMultiplier = hostCrouchNoiseMultiplier;

        ClampSettings();
    }

    public void ResetRuntime()
    {
        nextMovementNoiseTime = 0f;
    }

    public void ReportJump(bool isCrouching)
    {
        float radius = isCrouching ? jumpNoiseRadius * crouchNoiseMultiplier : jumpNoiseRadius;
        ReportInstant(radius);
    }

    public void ReportLanding(float planarSpeed, bool isCrouching)
    {
        float radius = landingNoiseRadius + Mathf.Min(planarSpeed * 0.45f, 4f);
        if (isCrouching)
        {
            radius *= crouchNoiseMultiplier;
        }

        ReportInstant(radius);
        nextMovementNoiseTime = Time.time + movementNoiseInterval * 0.75f;
    }

    public void TickMovementNoise(
        float movementSpeed,
        bool isSprinting,
        bool isCrouching,
        float selectedTopSpeed,
        float selectedSpeedFactor)
    {
        if (Time.time < nextMovementNoiseTime || movementSpeed <= 0.65f)
        {
            return;
        }

        float speedFactor = Mathf.InverseLerp(0.65f, selectedTopSpeed, movementSpeed);
        float radius = isSprinting
            ? sprintNoiseRadius
            : Mathf.Lerp(walkNoiseRadius * 0.2f, walkNoiseRadius, selectedSpeedFactor);
        radius = isSprinting
            ? radius
            : Mathf.Lerp(radius * 0.68f, radius, speedFactor);
        if (isCrouching)
        {
            radius *= crouchNoiseMultiplier;
        }

        ReportInstant(radius);
        float intervalScale = isSprinting
            ? 0.72f
            : Mathf.Lerp(1.35f, 1f, selectedSpeedFactor);
        nextMovementNoiseTime = Time.time + movementNoiseInterval * intervalScale;
    }

    public void ReportInstant(float radius)
    {
        if (radius <= 0f)
        {
            return;
        }

        PrototypeCombatNoiseSystem.ReportNoise(transform.position + Vector3.up * 0.9f, radius, gameObject);
    }

    private void ClampSettings()
    {
        walkNoiseRadius = Mathf.Max(0f, walkNoiseRadius);
        sprintNoiseRadius = Mathf.Max(walkNoiseRadius, sprintNoiseRadius);
        jumpNoiseRadius = Mathf.Max(0f, jumpNoiseRadius);
        landingNoiseRadius = Mathf.Max(0f, landingNoiseRadius);
        movementNoiseInterval = Mathf.Max(0.05f, movementNoiseInterval);
        crouchNoiseMultiplier = Mathf.Clamp(crouchNoiseMultiplier, 0.1f, 1f);
    }
}
