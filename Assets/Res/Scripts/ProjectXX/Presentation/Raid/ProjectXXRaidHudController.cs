using ProjectXX.Domain.Raid;
using TMPro;
using UnityEngine;

namespace ProjectXX.Presentation.Raid
{
    [DisallowMultipleComponent]
    public sealed class ProjectXXRaidHudController : MonoBehaviour
    {
        [SerializeField] private RaidSessionRuntime sessionRuntime;

        private RectTransform root;
        private TMP_Text statusLabel;
        private TMP_Text promptLabel;
        private TMP_Text crosshairLabel;

        private void Awake()
        {
            EnsureHud();
            BindRuntime(sessionRuntime);
            UpdateHud();
        }

        private void OnDestroy()
        {
            BindRuntime(null);

            if (root != null)
            {
                Destroy(root.gameObject);
            }
        }

        public void Configure(RaidSessionRuntime runtime)
        {
            BindRuntime(runtime);
            UpdateHud();
        }

        private void EnsureHud()
        {
            if (root != null)
            {
                return;
            }

            PrototypeRuntimeUiManager uiManager = PrototypeRuntimeUiManager.GetOrCreate();
            root = uiManager.CreateViewRoot("ProjectXXRaidHud", PrototypeUiLayer.Hud);

            RectTransform statusRoot = PrototypeUiToolkit.CreateRectTransform("StatusRoot", root);
            statusRoot.anchorMin = new Vector2(0f, 1f);
            statusRoot.anchorMax = new Vector2(0f, 1f);
            statusRoot.pivot = new Vector2(0f, 1f);
            statusRoot.anchoredPosition = new Vector2(24f, -24f);
            statusRoot.sizeDelta = new Vector2(560f, 160f);

            statusLabel = PrototypeUiToolkit.CreateTmpText(
                statusRoot,
                uiManager.RuntimeFont,
                string.Empty,
                24,
                FontStyle.Bold,
                Color.white,
                TextAnchor.UpperLeft);
            PrototypeUiToolkit.SetStretch((RectTransform)statusLabel.transform, 0f, 0f, 0f, 0f);

            RectTransform promptRoot = PrototypeUiToolkit.CreateRectTransform("PromptRoot", root);
            promptRoot.anchorMin = new Vector2(0.5f, 0f);
            promptRoot.anchorMax = new Vector2(0.5f, 0f);
            promptRoot.pivot = new Vector2(0.5f, 0f);
            promptRoot.anchoredPosition = new Vector2(0f, 28f);
            promptRoot.sizeDelta = new Vector2(720f, 48f);

            promptLabel = PrototypeUiToolkit.CreateTmpText(
                promptRoot,
                uiManager.RuntimeFont,
                string.Empty,
                20,
                FontStyle.Bold,
                new Color(0.95f, 0.86f, 0.52f),
                TextAnchor.MiddleCenter);
            PrototypeUiToolkit.SetStretch((RectTransform)promptLabel.transform, 0f, 0f, 0f, 0f);

            RectTransform crosshairRoot = PrototypeUiToolkit.CreateRectTransform("CrosshairRoot", root);
            crosshairRoot.anchorMin = new Vector2(0.5f, 0.5f);
            crosshairRoot.anchorMax = new Vector2(0.5f, 0.5f);
            crosshairRoot.pivot = new Vector2(0.5f, 0.5f);
            crosshairRoot.anchoredPosition = Vector2.zero;
            crosshairRoot.sizeDelta = new Vector2(48f, 48f);

            crosshairLabel = PrototypeUiToolkit.CreateTmpText(
                crosshairRoot,
                uiManager.RuntimeFont,
                "+",
                30,
                FontStyle.Bold,
                Color.white,
                TextAnchor.MiddleCenter);
            PrototypeUiToolkit.SetStretch((RectTransform)crosshairLabel.transform, 0f, 0f, 0f, 0f);
        }

        private void UpdateHud()
        {
            if (statusLabel == null || promptLabel == null || crosshairLabel == null)
            {
                return;
            }

            if (sessionRuntime == null)
            {
                statusLabel.text = "ProjectXX Raid HUD\nSession runtime pending.";
                promptLabel.text = "Waiting for scene bootstrap...";
                return;
            }

            RaidPlayerRuntime playerRuntime = sessionRuntime.PlayerRuntime;
            statusLabel.text =
                $"{playerRuntime.DisplayName}\n" +
                $"HP {Mathf.CeilToInt(playerRuntime.CurrentHealth)} / {Mathf.CeilToInt(playerRuntime.MaxHealth)}\n" +
                $"Weapon {playerRuntime.WeaponName}\n" +
                $"Ammo {playerRuntime.AmmoInMagazine} / {playerRuntime.ReserveAmmo}\n" +
                $"Enemies {sessionRuntime.AliveEnemyCount} alive  Kills {sessionRuntime.KilledEnemyCount}";

            if (playerRuntime.Dead)
            {
                promptLabel.text = "You are down. Stop Play Mode to reset the slice.";
                crosshairLabel.gameObject.SetActive(false);
                return;
            }

            crosshairLabel.gameObject.SetActive(true);

            if (sessionRuntime.ExtractedSuccessfully)
            {
                promptLabel.text = "Extraction confirmed. Core R1 slice complete.";
                return;
            }

            if (!string.IsNullOrWhiteSpace(sessionRuntime.InteractionPrompt))
            {
                promptLabel.text = sessionRuntime.InteractionPrompt;
                return;
            }

            promptLabel.text = sessionRuntime.PlayerInsideExtractionZone
                ? "Press [E] to use the placeholder extraction point."
                : "Move, aim, fire, survive, then reach the extraction cylinder.";
        }

        private void HandleRuntimeStateChanged()
        {
            UpdateHud();
        }

        private void BindRuntime(RaidSessionRuntime runtime)
        {
            if (sessionRuntime == runtime)
            {
                return;
            }

            if (sessionRuntime != null)
            {
                sessionRuntime.StateChanged -= HandleRuntimeStateChanged;
            }

            sessionRuntime = runtime;

            if (sessionRuntime != null)
            {
                sessionRuntime.StateChanged += HandleRuntimeStateChanged;
            }
        }
    }
}
