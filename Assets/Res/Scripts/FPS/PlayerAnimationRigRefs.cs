using UnityEngine;
using Unity.Cinemachine;

[DisallowMultipleComponent]
public class PlayerAnimationRigRefs : MonoBehaviour
{
    [Header("Rig Roots")]
    [SerializeField] private Transform hitboxRigRoot;
    [SerializeField] private Transform characterVisualRigRoot;
    [SerializeField] private Animator characterVisualAnimator;
    [SerializeField] private Transform firstPersonArmsRigRoot;

    [Header("View")]
    [SerializeField] private Camera viewCamera;
    [SerializeField] private Camera renderCamera;
    [SerializeField] private Transform cameraPitchRoot;
    [SerializeField] private Transform shoulderFollowTarget;
    [SerializeField] private Transform muzzle;
    [SerializeField] private CinemachineCamera shoulderCamera;

    [Header("Weapon View Anchors")]
    [SerializeField] private Transform primaryWeaponViewAnchor;
    [SerializeField] private Transform secondaryWeaponViewAnchor;
    [SerializeField] private Transform meleeWeaponViewAnchor;

    public Transform HitboxRigRoot => hitboxRigRoot;
    public Transform CharacterVisualRigRoot => characterVisualRigRoot;
    public Animator CharacterVisualAnimator => characterVisualAnimator;
    public Transform FirstPersonArmsRigRoot => firstPersonArmsRigRoot;
    public Camera ViewCamera => viewCamera;
    public Camera RenderCamera => renderCamera;
    public Transform CameraPitchRoot => cameraPitchRoot;
    public Transform ShoulderFollowTarget => shoulderFollowTarget;
    public Transform Muzzle => muzzle;
    public CinemachineCamera ShoulderCamera => shoulderCamera;
    public Transform PrimaryWeaponViewAnchor => primaryWeaponViewAnchor;
    public Transform SecondaryWeaponViewAnchor => secondaryWeaponViewAnchor;
    public Transform MeleeWeaponViewAnchor => meleeWeaponViewAnchor;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (characterVisualAnimator == null && characterVisualRigRoot != null)
        {
            characterVisualAnimator = characterVisualRigRoot.GetComponentInChildren<Animator>(true);
        }

        if (viewCamera == null)
        {
            Transform gameplayCameraTransform = transform.Find("ViewRoot/PitchPivot/ViewCamera");
            if (gameplayCameraTransform != null)
            {
                viewCamera = gameplayCameraTransform.GetComponent<Camera>();
            }
        }

        if (cameraPitchRoot == null)
        {
            cameraPitchRoot = transform.Find("ViewRoot/PitchPivot");
        }

        if (shoulderFollowTarget == null && cameraPitchRoot != null)
        {
            shoulderFollowTarget = cameraPitchRoot.Find("ShoulderFollowTarget");
        }

        if (renderCamera == null && cameraPitchRoot != null)
        {
            Transform renderCameraTransform = cameraPitchRoot.Find("WorldCamera");
            if (renderCameraTransform != null)
            {
                renderCamera = renderCameraTransform.GetComponent<Camera>();
            }
        }

        if (shoulderCamera == null)
        {
            shoulderCamera = GetComponentInChildren<CinemachineCamera>(true);
        }
    }
#endif
}
