using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerLookController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera viewCamera;

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 0.14f;
    [SerializeField] private float maxLookAngle = 85f;

    [Header("Camera Recoil")]
    [SerializeField] private float recoilSnappiness = 22f;
    [SerializeField] private float recoilRecoverySpeed = 16f;
    [SerializeField] private float maxRecoilPitch = 14f;
    [SerializeField] private float maxRecoilYaw = 6f;

    private float pitch;
    private Vector2 recoilTargetOffset;
    private Vector2 recoilCurrentOffset;

    public float Pitch => pitch;

    private void Awake()
    {
        ResolveReferences();
        ClampSettings();
    }

    private void OnValidate()
    {
        ResolveReferences();
        ClampSettings();
    }

    public void ApplyHostSettings(Camera hostCamera, float hostMouseSensitivity, float hostMaxLookAngle)
    {
        if (hostCamera != null)
        {
            viewCamera = hostCamera;
        }

        mouseSensitivity = hostMouseSensitivity;
        maxLookAngle = hostMaxLookAngle;
        ClampSettings();
    }

    public void TickLook(PrototypeFpsInput fpsInput)
    {
        ResolveReferences();
        UpdateRecoil(Time.deltaTime);

        if (viewCamera == null)
        {
            return;
        }

        if (fpsInput != null && Cursor.lockState == CursorLockMode.Locked)
        {
            Vector2 mouseDelta = fpsInput.LookDelta;
            float lookYawDelta = mouseDelta.x * mouseSensitivity;
            transform.Rotate(Vector3.up * lookYawDelta);

            pitch -= mouseDelta.y * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, -maxLookAngle, maxLookAngle);
        }

        ApplyCameraRotation();
    }

    public void AddRecoilImpulse(float pitchKick, float yawKick)
    {
        recoilTargetOffset.x = Mathf.Clamp(recoilTargetOffset.x - Mathf.Abs(pitchKick), -maxRecoilPitch, maxRecoilPitch);
        recoilTargetOffset.y = Mathf.Clamp(recoilTargetOffset.y + yawKick, -maxRecoilYaw, maxRecoilYaw);
    }

    public void ResetRecoilImmediate()
    {
        recoilTargetOffset = Vector2.zero;
        recoilCurrentOffset = Vector2.zero;
        ApplyCameraRotation();
    }

    public void ResetPitch()
    {
        pitch = 0f;
        ResetRecoilImmediate();
    }

    private void ResolveReferences()
    {
        if (viewCamera == null)
        {
            viewCamera = GetComponentInChildren<Camera>();
        }
    }

    private void ClampSettings()
    {
        mouseSensitivity = Mathf.Max(0.0001f, mouseSensitivity);
        maxLookAngle = Mathf.Clamp(maxLookAngle, 1f, 89f);
        recoilSnappiness = Mathf.Max(0.01f, recoilSnappiness);
        recoilRecoverySpeed = Mathf.Max(0.01f, recoilRecoverySpeed);
        maxRecoilPitch = Mathf.Max(0f, maxRecoilPitch);
        maxRecoilYaw = Mathf.Max(0f, maxRecoilYaw);
    }

    private void UpdateRecoil(float deltaTime)
    {
        if (deltaTime <= 0f)
        {
            return;
        }

        float recoveryBlend = 1f - Mathf.Exp(-recoilRecoverySpeed * deltaTime);
        recoilTargetOffset = Vector2.Lerp(recoilTargetOffset, Vector2.zero, recoveryBlend);

        float snapBlend = 1f - Mathf.Exp(-recoilSnappiness * deltaTime);
        recoilCurrentOffset = Vector2.Lerp(recoilCurrentOffset, recoilTargetOffset, snapBlend);
    }

    private void ApplyCameraRotation()
    {
        if (viewCamera != null)
        {
            float finalPitch = Mathf.Clamp(pitch + recoilCurrentOffset.x, -maxLookAngle, maxLookAngle);
            viewCamera.transform.localEulerAngles = new Vector3(finalPitch, recoilCurrentOffset.y, 0f);
        }
    }
}
