using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerLookController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera viewCamera;
    [SerializeField] private Transform pitchRoot;

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 0.14f;
    [SerializeField] private float maxLookUpAngle = 40f;
    [SerializeField] private float maxLookDownAngle = 55f;

    private float pitch;

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

    public void ApplyHostSettings(
        Camera hostCamera,
        float hostMouseSensitivity,
        float hostMaxLookUpAngle,
        float hostMaxLookDownAngle)
    {
        if (hostCamera != null)
        {
            viewCamera = hostCamera;
        }

        mouseSensitivity = hostMouseSensitivity;
        maxLookUpAngle = hostMaxLookUpAngle;
        maxLookDownAngle = hostMaxLookDownAngle;
        ClampSettings();
    }

    public void TickLook(PrototypeFpsInput fpsInput)
    {
        ResolveReferences();

        if (fpsInput == null || viewCamera == null || pitchRoot == null || Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }

        Vector2 mouseDelta = fpsInput.LookDelta;
        float lookYawDelta = mouseDelta.x * mouseSensitivity;
        transform.Rotate(Vector3.up * lookYawDelta);

        pitch -= mouseDelta.y * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, -maxLookUpAngle, maxLookDownAngle);
        pitchRoot.localEulerAngles = new Vector3(pitch, 0f, 0f);
    }

    public void ResetPitch()
    {
        pitch = 0f;

        if (pitchRoot != null)
        {
            pitchRoot.localEulerAngles = Vector3.zero;
        }
    }

    private void ResolveReferences()
    {
        PlayerAnimationRigRefs rigRefs = GetComponent<PlayerAnimationRigRefs>();
        if (viewCamera == null)
        {
            viewCamera = rigRefs != null ? rigRefs.ViewCamera : GetComponentInChildren<Camera>();
        }

        if (pitchRoot == null)
        {
            pitchRoot = rigRefs != null
                ? rigRefs.CameraPitchRoot
                : viewCamera != null
                    ? viewCamera.transform.parent
                    : null;
        }
    }

    private void ClampSettings()
    {
        mouseSensitivity = Mathf.Max(0.0001f, mouseSensitivity);
        maxLookUpAngle = Mathf.Clamp(maxLookUpAngle, 1f, 89f);
        maxLookDownAngle = Mathf.Clamp(maxLookDownAngle, 1f, 89f);
    }
}
