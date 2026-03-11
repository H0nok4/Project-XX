using UnityEngine;

public static class MetaMenuStyleUtility
{
    public static void EnsureStyles(
        ref GUIStyle titleStyle,
        ref GUIStyle sectionStyle,
        ref GUIStyle bodyStyle,
        ref GUIStyle listStyle,
        ref GUIStyle buttonStyle)
    {
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 36,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }

        if (sectionStyle == null)
        {
            sectionStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 18,
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = Color.white }
            };
            sectionStyle.padding = new RectOffset(14, 14, 12, 12);
        }

        if (bodyStyle == null)
        {
            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                wordWrap = true,
                richText = true,
                normal = { textColor = new Color(0.92f, 0.94f, 0.98f, 1f) }
            };
        }

        if (listStyle == null)
        {
            listStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.UpperLeft
            };
            listStyle.padding = new RectOffset(10, 10, 8, 8);
            listStyle.margin = new RectOffset(0, 0, 0, 8);
        }

        if (buttonStyle == null)
        {
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14
            };
        }
    }
}
