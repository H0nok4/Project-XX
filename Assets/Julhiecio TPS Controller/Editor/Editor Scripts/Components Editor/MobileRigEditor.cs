using JUTPS.UI;
using UnityEditor;
using UnityEngine;

namespace JUTPS.CustomEditors
{
    [CustomEditor(typeof(MobileRig))]
    public sealed class MobileRigEditor : Editor
    {
        private static readonly string[] DontInclude = { "m_Script" };

        public override void OnInspectorGUI()
        {
            MobileRig mobileRig = (MobileRig)target;

            serializedObject.Update();

            if (GUILayout.Button(" ► Auto Setup", GUILayout.Height(30)))
            {
                mobileRig.FindButtonsAndTouches();
                EditorUtility.SetDirty(mobileRig);
            }

            DrawPropertiesExcluding(serializedObject, DontInclude);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
