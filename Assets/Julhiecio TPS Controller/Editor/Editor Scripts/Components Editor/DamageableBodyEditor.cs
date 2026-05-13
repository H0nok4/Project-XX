using JUTPS.ArmorSystem;
using UnityEditor;
using UnityEngine;

namespace JUTPS.CustomEditors
{
    [CustomEditor(typeof(DamageableBody))]
    public sealed class DamageableBodyEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            DamageableBody damageableBody = (DamageableBody)target;
            if (!GUILayout.Button("Distribute Values"))
            {
                return;
            }

            Animator animator = damageableBody.GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogError("Unable to find Animator component");
                return;
            }

            if (!animator.isHuman)
            {
                Debug.LogError("Your character needs to be humanoid");
                return;
            }

            DamageableBody.DistributeDamageableComponentsInTheBody(
                animator,
                damageableBody.HeadDamageIntensity,
                damageableBody.TorsoDamageIntensity,
                damageableBody.LegsDamageIntensity,
                damageableBody.ArmsDamageIntensity);

            Debug.Log("Damageable Body Parts values have been successfully updated");
        }
    }
}
