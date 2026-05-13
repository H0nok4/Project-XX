using JUTPSActions;
using UnityEngine;

namespace JUTPS.FX
{
    [AddComponentMenu("JU TPS/FX/Body Lean")]
    public class BodyLeanInert : JUTPSAction
    {
        public JUFootPlacement JUFootPlacer;
        public Transform RootBone;

        public bool RootBoneSpineLean = true;
        public bool RootBoneSpineMovement = true;

        public float RootBoneLeanIntensity = 20;
        public float RootBoneLeanSpeed = 8;
        public float RootBoneDownMovementIntensity = 0.25f;
        public float BlockForwardLeanWeight = 4;

        float Speed;
        float Lean;

        Vector3 NotAffectedEulerAngles;
        Vector3 NotAffectedUpward;

        public Axis AxisToLean;


        public enum Axis { X, Y, Z }


        protected override void Awake()
        {
            base.Awake();
            if (JUFootPlacer == null) JUFootPlacer = GetComponent<JUFootPlacement>();
            if (RootBone == null) RootBone = anim.GetBoneTransform(HumanBodyBones.Hips);
            anim.updateMode = AnimatorUpdateMode.Fixed;
        }
        void OnAnimatorIK()
        {
            NotAffectedEulerAngles = RootBone.localEulerAngles;
        }
        void LateUpdate()
        {
            DoInert();
        }
        private void OnEnable()
        {
            Speed = 0;
            Lean = 0;
        }
        void DoInert()
        {
            Vector3 euler = NotAffectedEulerAngles;
            NotAffectedUpward = RootBone.up;

            if (TPSCharacter.IsMeleeAttacking || TPSCharacter.IsRagdolled || TPSCharacter.IsAiming || TPSCharacter.FiringMode || TPSCharacter.IsDriving || TPSCharacter.IsDead || !TPSCharacter.IsGrounded)
            {
                Speed = 0;
                Lean = 0;
                return;
            }


            Speed = Mathf.Lerp(Speed, TPSCharacter.VelocityMultiplier, 10 * Time.deltaTime);


            if (TPSCharacter.IsMoving)
            {
                Lean = Mathf.Lerp(Lean, (Speed * RootBoneLeanIntensity / BlockForwardLeanWeight), RootBoneLeanSpeed * Time.deltaTime);
            }
            else
            {
                Lean = Mathf.Lerp(Lean, -(Speed * RootBoneLeanIntensity / 2), RootBoneLeanSpeed * Time.deltaTime);
                if (JUFootPlacer != null && RootBoneSpineMovement)
                {
                    JUFootPlacer.LastBodyPositionY -= RootBoneDownMovementIntensity * Mathf.Abs(Lean) / 10 * Time.deltaTime;
                }
            }

            switch (AxisToLean)
            {
                case Axis.X:
                    euler.x += Lean;
                    break;
                case Axis.Y:
                    euler.y += Lean;
                    break;
                case Axis.Z:
                    euler.z += Lean;
                    break;
            }

            RootBone.localRotation = Quaternion.Euler(euler);
        }
        private void OnDrawGizmos()
        {
            if (RootBone == null)
                return;

            float angle = Vector3.SignedAngle(NotAffectedUpward, RootBone.up, RootBone.right);
            if (Mathf.Approximately(angle, 0f))
                return;

            Color color = Color.Lerp(Color.green, Color.red, angle / 10);
            Vector3 origin = RootBone.position;
            Vector3 currentUp = RootBone.up * 0.5f;
            Vector3 referenceUp = NotAffectedUpward * 0.5f;

            Gizmos.color = color;
            Gizmos.DrawLine(origin, origin + currentUp);
            Gizmos.DrawWireSphere(origin + currentUp, 0.025f);

            Gizmos.color = Color.white;
            Gizmos.DrawLine(origin, origin + referenceUp);
            Gizmos.DrawWireSphere(origin + referenceUp, 0.02f);
        }
    }
}
