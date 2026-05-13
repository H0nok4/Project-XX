using System.Reflection;
using JUTPS.PhysicsScripts;
using UnityEditor;
using UnityEngine;

namespace CustomEditors
{
    [CustomEditor(typeof(JUTPS.JUFootPlacement), true)]
    [CanEditMultipleObjects]
    public sealed class JUFootPlacementEditor : Editor
    {
        private static readonly string[] DontInclude = { "m_Script" };

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            JUTPSEditor.CustomEditorUtilities.JUTPSTitle("JU Foot Placement for JU TPS");
            DrawPropertiesExcluding(serializedObject, DontInclude);
            serializedObject.ApplyModifiedProperties();
        }
    }

    internal static class JUFootPlacementSceneGizmos
    {
        private const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.NonPublic;

        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.InSelectionHierarchy)]
        private static void DrawGizmos(JUTPS.JUFootPlacement footPlacement, GizmoType gizmoType)
        {
            if (footPlacement == null)
                return;

            DrawFootPlacementGizmos(footPlacement);
            DrawGroundPlacementGizmos(footPlacement);
        }

        private static void DrawFootPlacementGizmos(JUTPS.JUFootPlacement footPlacement)
        {
            Transform leftFoot = GetPrivateField<Transform>(footPlacement, "LeftFoot");
            Transform rightFoot = GetPrivateField<Transform>(footPlacement, "RightFoot");
            if ((leftFoot == null || rightFoot == null) && footPlacement.TryGetComponent(out Animator animator))
            {
                leftFoot ??= animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                rightFoot ??= animator.GetBoneTransform(HumanBodyBones.RightFoot);
            }

            if (leftFoot == null || rightFoot == null || !footPlacement.EnableFootPlacement)
                return;

            GUIStyle textStyle = CreateTextStyle();
            Transform transform = footPlacement.transform;
            RaycastHit leftHitPlaceBase = GetPrivateField<RaycastHit>(footPlacement, "LeftHitPlaceBase");
            RaycastHit rightHitPlaceBase = GetPrivateField<RaycastHit>(footPlacement, "RightHitPlaceBase");
            Transform leftFootPlaceBase = GetPrivateField<Transform>(footPlacement, "LeftFootPlaceBase");
            Transform rightFootPlaceBase = GetPrivateField<Transform>(footPlacement, "RightFootPlaceBase");
            bool leftHit = GetPrivateField<bool>(footPlacement, "LeftHit");
            bool rightHit = GetPrivateField<bool>(footPlacement, "RightHit");
            float leftAnimationHeight = GetPrivateField<float>(footPlacement, "AnimationLeftFootPositionY");
            float rightAnimationHeight = GetPrivateField<float>(footPlacement, "AnimationRightFootPositionY");

            Handles.color = new Color(1f, 1f, 1f, 0.3f);
            Handles.DrawWireDisc(transform.position, transform.up, 0.6f);
            Handles.DrawDottedLine(transform.position - transform.forward * 0.6f, transform.position + transform.forward * 0.6f, 10f);
            Handles.DrawDottedLine(transform.position - transform.right * 0.6f, transform.position + transform.right * 0.6f, 10f);

            Handles.color = new Color(1f, 0.2f, 0.2f, 0.5f);
            Handles.DrawWireDisc(transform.position + transform.up * footPlacement.MaxStepHeight, transform.up, 0.3f);
            Handles.DrawDottedLine(
                transform.position + transform.up * footPlacement.MaxStepHeight - transform.forward * 0.3f,
                transform.position + transform.up * footPlacement.MaxStepHeight + transform.forward * 0.3f,
                2f);
            Handles.DrawDottedLine(
                transform.position + transform.up * footPlacement.MaxStepHeight - transform.right * 0.3f,
                transform.position + transform.up * footPlacement.MaxStepHeight + transform.right * 0.3f,
                2f);

            textStyle.normal.textColor = new Color(1f, 0.4f, 0.4f, 1f);
            Handles.Label(transform.position + transform.up * (footPlacement.MaxStepHeight + 0.1f) + transform.right * 0.4f, "Step Limit", textStyle);

            if (footPlacement.UseDynamicFootPlacing)
            {
                Vector3 leftFootPosition = transform.position - transform.right * 0.6f;
                Handles.color = Color.yellow;
                Handles.DrawDottedLine(leftFootPosition, leftFootPosition + transform.up * leftAnimationHeight, 1f);
                textStyle.normal.textColor = Color.yellow;
                Handles.Label(leftFootPosition + transform.up * leftAnimationHeight, "LF_Y \n\r" + rightAnimationHeight.ToString("#0.000"), textStyle);

                Vector3 rightFootPosition = transform.position + transform.right * 0.6f;
                Handles.color = new Color(0.2f, 0.4f, 1f);
                Handles.DrawDottedLine(rightFootPosition, rightFootPosition + transform.up * rightAnimationHeight, 1f);
                textStyle.normal.textColor = new Color(0.2f, 0.4f, 1f);
                Handles.Label(rightFootPosition + transform.up * rightAnimationHeight, "RL_Y \n\r" + rightAnimationHeight.ToString("#0.000"), textStyle);
            }

            if (footPlacement.EnableDynamicBodyPlacing && footPlacement.NewAnimationBodyPosition != Vector3.zero)
            {
                Handles.color = Color.green;
                textStyle.normal.textColor = Color.green;
                Handles.Label(footPlacement.NewAnimationBodyPosition + transform.right * 0.4f + transform.up * 0.1f, "Body Position", textStyle);
                Handles.DrawWireDisc(footPlacement.NewAnimationBodyPosition, transform.up, 0.2f);

                if (leftHitPlaceBase.point != Vector3.zero)
                {
                    Handles.color = Color.yellow;
                    Handles.DrawDottedLine(footPlacement.NewAnimationBodyPosition - transform.right * 0.2f, leftHitPlaceBase.point, 1f);
                }

                if (rightHitPlaceBase.point != Vector3.zero)
                {
                    Handles.color = new Color(0.3f, 0.6f, 1f);
                    Handles.DrawDottedLine(footPlacement.NewAnimationBodyPosition + transform.right * 0.2f, rightHitPlaceBase.point, 1f);
                }
            }

            if (leftFootPlaceBase != null && rightFootPlaceBase != null && leftHit && rightHit)
            {
                Handles.color = Color.yellow;
                Handles.ArrowHandleCap(0, leftFootPlaceBase.position, Quaternion.FromToRotation(Vector3.forward, leftHitPlaceBase.normal), 0.2f, EventType.Repaint);
                Handles.DrawWireDisc(leftFootPlaceBase.position, leftFootPlaceBase.up, footPlacement.radius);

                Handles.color = new Color(0.2f, 0.4f, 1f);
                Handles.ArrowHandleCap(0, rightFootPlaceBase.position, Quaternion.FromToRotation(Vector3.forward, rightHitPlaceBase.normal), 0.2f, EventType.Repaint);
                Handles.DrawWireDisc(rightFootPlaceBase.position, rightFootPlaceBase.up, footPlacement.radius);
            }

            if (!leftHit)
            {
                Gizmos.color = Color.yellow;
                float distance = footPlacement.RaycastMaxDistance - footPlacement.RaycastHeight;
                Gizmos.DrawLine(leftFoot.position + transform.up * footPlacement.RaycastHeight, leftFoot.position - transform.up * distance);
            }

            if (!rightHit)
            {
                Gizmos.color = new Color(0.2f, 0.4f, 1f);
                float distance = footPlacement.RaycastMaxDistance - footPlacement.RaycastHeight;
                Gizmos.DrawLine(rightFoot.position + transform.up * footPlacement.RaycastHeight, rightFoot.position - transform.up * distance);
            }
        }

        private static void DrawGroundPlacementGizmos(JUTPS.JUFootPlacement footPlacement)
        {
            if (!footPlacement.KeepCharacterOnGround)
                return;

            Transform transform = footPlacement.transform;
            RaycastHit hitGroundBodyPlacement = GetPrivateField<RaycastHit>(footPlacement, "HitGroundBodyPlacement");

            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position + transform.up * footPlacement.RaycastDistanceToGround, transform.position);
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position + transform.up * footPlacement.RaycastDistanceToGround, 0.01f);
            Gizmos.DrawWireSphere(transform.position, 0.01f);

            if (hitGroundBodyPlacement.point != Vector3.zero)
                Gizmos.DrawWireSphere(hitGroundBodyPlacement.point + transform.up * footPlacement.GroundCheckRadius, footPlacement.GroundCheckRadius);
            else
                Gizmos.DrawWireSphere(transform.position + transform.up * footPlacement.GroundCheckRadius, footPlacement.GroundCheckRadius);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position + transform.up * footPlacement.BodyHeightPosition, 0.01f);
            Handles.Label(transform.position + transform.up * footPlacement.BodyHeightPosition, "Body Position");
        }

        private static GUIStyle CreateTextStyle()
        {
            return new GUIStyle(EditorStyles.boldLabel);
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, InstanceFields);
            return field != null ? (T)field.GetValue(target) : default;
        }
    }

    internal static class JUSlipCapsuleSceneGizmos
    {
        private const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.NonPublic;

        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.InSelectionHierarchy)]
        private static void DrawGizmos(JUSlipCapsule slipCapsule, GizmoType gizmoType)
        {
            if (slipCapsule == null)
                return;

            Vector3 center = GetPrivateField<Vector3>(slipCapsule, "Center");
            float radius = GetPrivateField<float>(slipCapsule, "Radius");
            float height = GetPrivateField<float>(slipCapsule, "Height");

            Vector3 centerPosition = slipCapsule.transform.position
                + slipCapsule.transform.right * center.x
                + slipCapsule.transform.up * center.y
                + slipCapsule.transform.forward * center.z;

            float halfHeight = height / 4f;
            float innerRadius = Mathf.Max(0f, radius - 0.1f);

            Handles.color = Color.yellow;
            Handles.DrawWireDisc(centerPosition + slipCapsule.transform.up * halfHeight, slipCapsule.transform.up, radius);
            Handles.DrawWireDisc(centerPosition - slipCapsule.transform.up * halfHeight, slipCapsule.transform.up, radius);
            Handles.DrawWireDisc(centerPosition + slipCapsule.transform.up * halfHeight, slipCapsule.transform.up, innerRadius);
            Handles.DrawWireDisc(centerPosition - slipCapsule.transform.up * halfHeight, slipCapsule.transform.up, innerRadius);

            Handles.DrawLine(
                centerPosition + slipCapsule.transform.right * radius + slipCapsule.transform.up * halfHeight,
                centerPosition + slipCapsule.transform.right * radius - slipCapsule.transform.up * halfHeight);
            Handles.DrawLine(
                centerPosition - slipCapsule.transform.right * radius + slipCapsule.transform.up * halfHeight,
                centerPosition - slipCapsule.transform.right * radius - slipCapsule.transform.up * halfHeight);
            Handles.DrawLine(
                centerPosition + slipCapsule.transform.forward * radius + slipCapsule.transform.up * halfHeight,
                centerPosition + slipCapsule.transform.forward * radius - slipCapsule.transform.up * halfHeight);
            Handles.DrawLine(
                centerPosition - slipCapsule.transform.forward * radius + slipCapsule.transform.up * halfHeight,
                centerPosition - slipCapsule.transform.forward * radius - slipCapsule.transform.up * halfHeight);
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, InstanceFields);
            return field != null ? (T)field.GetValue(target) : default;
        }
    }
}
