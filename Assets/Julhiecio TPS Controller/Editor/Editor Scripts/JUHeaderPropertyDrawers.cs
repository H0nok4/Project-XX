using System.Reflection;
using JUTPSEditor.JUHeader;
using UnityEditor;
using UnityEngine;

namespace JUTPSEditor.JUHeader
{
#if !ODIN_INSPECTOR
    [CustomPropertyDrawer(typeof(JUHeader))]
    internal sealed class JUHeaderDecoratorDrawer : DecoratorDrawer
    {
        private JUHeader Header => (JUHeader)attribute;

        public override float GetHeight()
        {
            return base.GetHeight() + 5;
        }

        public override void OnGUI(Rect position)
        {
            GUIStyle style = new GUIStyle(EditorStyles.toolbar)
            {
                alignment = TextAnchor.LowerLeft,
                fontSize = 16,
                richText = true
            };

            style.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;

            Rect headerPosition = new Rect(position.x - 17, position.y, position.width + 28, position.height);
            EditorGUI.LabelField(headerPosition, "  " + Header.text, style);
        }
    }

    [CustomPropertyDrawer(typeof(JUSubHeader))]
    internal sealed class JUSubHeaderDecoratorDrawer : DecoratorDrawer
    {
        private JUSubHeader Header => (JUSubHeader)attribute;

        public override float GetHeight()
        {
            return base.GetHeight() + 5;
        }

        public override void OnGUI(Rect position)
        {
            GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                fontSize = 15,
                richText = true
            };

            style.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;

            Rect headerPosition = new Rect(position.x - 17, position.y + 1, position.width + 19, position.height);
            EditorGUI.LabelField(headerPosition, "   " + Header.text, style);
        }
    }

    [CustomPropertyDrawer(typeof(JUReadOnly))]
    internal sealed class JUReadOnlyPropertyDrawer : PropertyDrawer
    {
        private JUReadOnly ReadOnlyAttribute => (JUReadOnly)attribute;
        private bool drawing;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return drawing ? base.GetPropertyHeight(property, label) : 0f;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (ReadOnlyAttribute.DisableOnFalse && !GetAttributeConditionValue(ReadOnlyAttribute, property))
            {
                drawing = false;
                return;
            }

            drawing = true;
            bool previousEnabled = GUI.enabled;
            GUI.enabled = ReadOnlyAttribute.ConditionPropertyName == "" || GetAttributeConditionValue(ReadOnlyAttribute, property);
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = previousEnabled;
        }

        private static bool GetAttributeConditionValue(JUReadOnly readOnlyAttribute, SerializedProperty property)
        {
            if (readOnlyAttribute.ConditionPropertyName == "")
            {
                return true;
            }

            SerializedProperty booleanCondition = property.serializedObject.FindProperty(readOnlyAttribute.ConditionPropertyName);
            bool enabled = booleanCondition != null && booleanCondition.boolValue;
            return readOnlyAttribute.Inverse ? !enabled : enabled;
        }
    }

    [CustomPropertyDrawer(typeof(JUButton))]
    internal sealed class JUButtonPropertyDrawer : DecoratorDrawer
    {
        private JUButton ButtonAttribute => (JUButton)attribute;

        public override float GetHeight()
        {
            return base.GetHeight() + 5;
        }

        public override void OnGUI(Rect position)
        {
            GameObject selectedObject = Selection.activeGameObject;
            UnityEngine.Object targetObject = selectedObject != null && ButtonAttribute.ClassType != null
                ? selectedObject.GetComponent(ButtonAttribute.ClassType) as UnityEngine.Object
                : null;

            if (ButtonAttribute.methodName == "")
            {
                GUI.Button(position, ButtonAttribute.labelText);
                return;
            }

            if (!GUI.Button(position, ButtonAttribute.labelText) || targetObject == null)
            {
                return;
            }

            MethodInfo method = targetObject.GetType().GetMethod(ButtonAttribute.methodName);
            method?.Invoke(targetObject, null);
        }
    }
#endif
}
