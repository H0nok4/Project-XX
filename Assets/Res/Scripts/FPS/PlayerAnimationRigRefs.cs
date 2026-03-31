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

    public Transform HitboxRigRoot
    {
        get
        {
            ResolveReferences();
            return hitboxRigRoot;
        }
    }

    public Transform CharacterVisualRigRoot
    {
        get
        {
            ResolveReferences();
            return characterVisualRigRoot;
        }
    }

    public Animator CharacterVisualAnimator
    {
        get
        {
            ResolveReferences();
            return characterVisualAnimator;
        }
    }

    public Transform FirstPersonArmsRigRoot
    {
        get
        {
            ResolveReferences();
            return firstPersonArmsRigRoot;
        }
    }

    public Camera ViewCamera
    {
        get
        {
            ResolveReferences();
            return viewCamera;
        }
    }

    public Camera RenderCamera
    {
        get
        {
            ResolveReferences();
            return renderCamera;
        }
    }

    public Transform CameraPitchRoot
    {
        get
        {
            ResolveReferences();
            return cameraPitchRoot;
        }
    }

    public Transform ShoulderFollowTarget
    {
        get
        {
            ResolveReferences();
            return shoulderFollowTarget;
        }
    }

    public Transform Muzzle
    {
        get
        {
            ResolveReferences();
            return muzzle;
        }
    }

    public CinemachineCamera ShoulderCamera
    {
        get
        {
            ResolveReferences();
            return shoulderCamera;
        }
    }

    public Transform PrimaryWeaponViewAnchor
    {
        get
        {
            ResolveReferences();
            return primaryWeaponViewAnchor;
        }
    }

    public Transform SecondaryWeaponViewAnchor
    {
        get
        {
            ResolveReferences();
            return secondaryWeaponViewAnchor;
        }
    }

    public Transform MeleeWeaponViewAnchor
    {
        get
        {
            ResolveReferences();
            return meleeWeaponViewAnchor;
        }
    }

    private void Awake()
    {
        ResolveReferences();
    }

    private void ResolveReferences()
    {
        if (hitboxRigRoot == null)
        {
            hitboxRigRoot = transform.Find("HitboxRig");
        }

        if (characterVisualRigRoot == null)
        {
            characterVisualRigRoot = transform.Find("CharacterVisualRig");
        }

        if (characterVisualAnimator == null && characterVisualRigRoot != null)
        {
            characterVisualAnimator = characterVisualRigRoot.GetComponentInChildren<Animator>(true);
        }

        if (cameraPitchRoot == null)
        {
            cameraPitchRoot = transform.Find("ViewRoot/PitchPivot");
        }

        if (viewCamera == null)
        {
            Transform gameplayCameraTransform = cameraPitchRoot != null
                ? cameraPitchRoot.Find("ViewCamera")
                : transform.Find("ViewRoot/PitchPivot/ViewCamera");
            if (gameplayCameraTransform != null)
            {
                viewCamera = gameplayCameraTransform.GetComponent<Camera>();
            }
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

        if (muzzle == null && viewCamera != null)
        {
            muzzle = FindChildRecursive(viewCamera.transform, "Muzzle");
        }

        if (primaryWeaponViewAnchor == null && viewCamera != null)
        {
            primaryWeaponViewAnchor = FindChildRecursive(viewCamera.transform, "WeaponView_Primary");
        }

        if (secondaryWeaponViewAnchor == null && viewCamera != null)
        {
            secondaryWeaponViewAnchor = FindChildRecursive(viewCamera.transform, "WeaponView_Secondary");
        }

        if (meleeWeaponViewAnchor == null && viewCamera != null)
        {
            meleeWeaponViewAnchor = FindChildRecursive(viewCamera.transform, "WeaponView_Melee");
        }
    }

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent == null || string.IsNullOrWhiteSpace(childName))
        {
            return null;
        }

        Transform directMatch = parent.Find(childName);
        if (directMatch != null)
        {
            return directMatch;
        }

        for (int childIndex = 0; childIndex < parent.childCount; childIndex++)
        {
            Transform match = FindChildRecursive(parent.GetChild(childIndex), childName);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ResolveReferences();
    }
#endif
}
