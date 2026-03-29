using UnityEngine;

[CreateAssetMenu(menuName = "Prototype/Raid/Weapon Presentation Profile", fileName = "WeaponPresentationProfile")]
public sealed class WeaponPresentationProfile : ScriptableObject
{
    [Header("ADS Fallback")]
    [SerializeField] private Vector3 adsLocalPosition = Vector3.zero;
    [SerializeField] private Vector3 adsLocalEulerAngles = Vector3.zero;

    [Header("Recoil")]
    [SerializeField] private float fireKickBack = 0.045f;
    [SerializeField] private float fireKickPitch = 5.5f;
    [SerializeField] private float recoilReturnSpeed = 16f;

    [Header("Action Motion")]
    [SerializeField] private float equipDuration = 0.22f;
    [SerializeField] private float equipDipDistance = 0.08f;
    [SerializeField] private float equipPitch = 10f;
    [SerializeField] private float reloadDuration = 0.36f;
    [SerializeField] private float reloadDipDistance = 0.06f;
    [SerializeField] private float reloadPitch = 16f;
    [SerializeField] private float meleeDuration = 0.18f;
    [SerializeField] private float meleeSwingDistance = 0.08f;
    [SerializeField] private float meleeSwingYaw = 18f;

    [Header("Locomotion Sway")]
    [SerializeField] private float locomotionBobFrequency = 6.6f;
    [SerializeField] private float locomotionBobVerticalAmplitude = 0.018f;
    [SerializeField] private float locomotionBobHorizontalAmplitude = 0.012f;
    [SerializeField] private float locomotionRollAmplitude = 2.4f;
    [SerializeField] private float locomotionPitchAmplitude = 1.7f;
    [SerializeField] private float aimSwayMultiplier = 0.45f;
    [SerializeField] private float locomotionSwaySmoothing = 14f;

    public Vector3 AdsLocalPosition => adsLocalPosition;
    public Vector3 AdsLocalEulerAngles => adsLocalEulerAngles;
    public float FireKickBack => Mathf.Max(0f, fireKickBack);
    public float FireKickPitch => fireKickPitch;
    public float RecoilReturnSpeed => Mathf.Max(0.01f, recoilReturnSpeed);
    public float EquipDuration => Mathf.Max(0.01f, equipDuration);
    public float EquipDipDistance => Mathf.Max(0f, equipDipDistance);
    public float EquipPitch => equipPitch;
    public float ReloadDuration => Mathf.Max(0.01f, reloadDuration);
    public float ReloadDipDistance => Mathf.Max(0f, reloadDipDistance);
    public float ReloadPitch => reloadPitch;
    public float MeleeDuration => Mathf.Max(0.01f, meleeDuration);
    public float MeleeSwingDistance => Mathf.Max(0f, meleeSwingDistance);
    public float MeleeSwingYaw => meleeSwingYaw;
    public float LocomotionBobFrequency => Mathf.Max(0f, locomotionBobFrequency);
    public float LocomotionBobVerticalAmplitude => Mathf.Max(0f, locomotionBobVerticalAmplitude);
    public float LocomotionBobHorizontalAmplitude => Mathf.Max(0f, locomotionBobHorizontalAmplitude);
    public float LocomotionRollAmplitude => locomotionRollAmplitude;
    public float LocomotionPitchAmplitude => locomotionPitchAmplitude;
    public float AimSwayMultiplier => Mathf.Clamp(aimSwayMultiplier, 0.01f, 1f);
    public float LocomotionSwaySmoothing => Mathf.Max(0.01f, locomotionSwaySmoothing);
}
