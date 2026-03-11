using UnityEngine;

[DisallowMultipleComponent]
public class BaseHubDirector : MonoBehaviour
{
    [SerializeField] private string hubTitle = "Base Hub";

    private GUIStyle titleStyle;
    private GUIStyle bodyStyle;

    private void Awake()
    {
        Debug.Log("[BaseHubDirector] Entered Base Hub (placeholder)." );
    }

    private void OnGUI()
    {
        EnsureStyles();
        Rect panelRect = new Rect(36f, 36f, Mathf.Min(520f, Screen.width - 72f), 140f);
        GUI.Box(panelRect, string.Empty);

        GUILayout.BeginArea(new Rect(panelRect.x + 16f, panelRect.y + 16f, panelRect.width - 32f, panelRect.height - 32f));
        GUILayout.Label(hubTitle, titleStyle);
        GUILayout.Space(6f);
        GUILayout.Label("This is the minimal BaseScene landing point for Stage 0. Build the real hub here.", bodyStyle);
        GUILayout.EndArea();
    }

    private void EnsureStyles()
    {
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }

        if (bodyStyle == null)
        {
            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                wordWrap = true,
                normal = { textColor = new Color(0.92f, 0.94f, 0.98f, 1f) }
            };
        }
    }
}
