using UnityEngine;

namespace JUTPSEditor.JUHeader
{
    [System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = true, Inherited = true)]
    public class JUHeader : PropertyAttribute
    {
        public string text;

        public JUHeader(string text)
        {
            this.text = text;
        }
    }

    [System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = true, Inherited = true)]
    public class JUSubHeader : PropertyAttribute
    {
        public string text;

        public JUSubHeader(string text)
        {
            this.text = text;
        }
    }

    [System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = true, Inherited = true)]
    public class JUReadOnly : PropertyAttribute
    {
        public string ConditionPropertyName;
        public bool Inverse;
        public bool DisableOnFalse;

        public JUReadOnly(string conditionPropertyName = "", bool inverse = false, bool disableonfalse = true)
        {
            ConditionPropertyName = conditionPropertyName;
            Inverse = inverse;
            DisableOnFalse = disableonfalse;
        }
    }

    [System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = true, Inherited = true)]
    public class JUButton : PropertyAttribute
    {
        public string methodName;
        public string labelText;

        private System.Type classType;

        public System.Type ClassType
        {
            get { return classType; }
            set { classType = value; }
        }

        public JUButton(string labelText = "Button", System.Type scriptType = default(System.Type), string methodName = "")
        {
            this.methodName = methodName;
            this.labelText = labelText;
            ClassType = scriptType;
        }
    }
}
