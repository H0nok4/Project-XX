using UnityEngine;

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
    [SerializeField] private Transform muzzle;

    [Header("Weapon View Anchors")]
    [SerializeField] private Transform primaryWeaponViewAnchor;
    [SerializeField] private Transform secondaryWeaponViewAnchor;
    [SerializeField] private Transform meleeWeaponViewAnchor;

    public Transform HitboxRigRoot => hitboxRigRoot;
    public Transform CharacterVisualRigRoot => characterVisualRigRoot;
    public Animator CharacterVisualAnimator => characterVisualAnimator;
    public Transform FirstPersonArmsRigRoot => firstPersonArmsRigRoot;
    public Camera ViewCamera => viewCamera;
    public Transform Muzzle => muzzle;
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
    }
#endif
}
