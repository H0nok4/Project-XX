using UnityEngine;

public sealed class MetaShellPresenter
{
    private readonly PrototypeMainMenuController host;

    public MetaShellPresenter(PrototypeMainMenuController host)
    {
        this.host = host;
    }

    public void DrawBackground()
    {
        Color previousColor = GUI.color;
        GUI.color = new Color(0.08f, 0.1f, 0.14f, 0.94f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = previousColor;

        GUI.Label(new Rect(40f, 32f, 640f, 48f), "Project-XX", host.TitleStyle);
        GUI.Label(new Rect(44f, 88f, 560f, 28f), "Single-player raid prototype", host.BodyStyle);
    }

    public void DrawNavigation()
    {
        Rect navRect = new Rect(40f, 140f, 220f, 300f);
        GUI.Box(navRect, string.Empty, host.SectionStyle);

        GUILayout.BeginArea(new Rect(navRect.x + 16f, navRect.y + 16f, navRect.width - 32f, navRect.height - 32f));
        GUILayout.Label("Operations", host.SectionStyle);
        GUILayout.Space(12f);

        if (GUILayout.Button("Deploy", host.ButtonStyle, GUILayout.Height(42f)))
        {
            host.CurrentPage = PrototypeMainMenuController.MenuPage.Home;
        }

        if (GUILayout.Button("Warehouse", host.ButtonStyle, GUILayout.Height(42f)))
        {
            host.CurrentPage = PrototypeMainMenuController.MenuPage.Warehouse;
        }

        if (GUILayout.Button("Merchants", host.ButtonStyle, GUILayout.Height(42f)))
        {
            host.CurrentPage = PrototypeMainMenuController.MenuPage.Merchants;
        }

        GUILayout.Space(10f);
        if (GUILayout.Button("Save Profile", host.ButtonStyle, GUILayout.Height(34f)))
        {
            host.SaveProfileFromContainers();
            host.SetFeedback("Profile saved.");
        }

        if (GUILayout.Button("Reset Profile", host.ButtonStyle, GUILayout.Height(34f)))
        {
            host.ResetProfile();
        }

        GUILayout.Space(10f);
        if (GUILayout.Button("Quit", host.ButtonStyle, GUILayout.Height(34f)))
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        GUILayout.EndArea();
    }

    public void DrawFooter()
    {
        if (string.IsNullOrWhiteSpace(host.FeedbackMessage) || Time.time > host.FeedbackUntilTime)
        {
            return;
        }

        GUI.Label(
            new Rect(44f, Screen.height - 42f, Mathf.Max(900f, Screen.width - 88f), 24f),
            host.FeedbackMessage,
            host.BodyStyle);
    }
}
