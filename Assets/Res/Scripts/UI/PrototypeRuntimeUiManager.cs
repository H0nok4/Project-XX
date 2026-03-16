using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public enum PrototypeUiLayer
{
    Background = 0,
    World = 1,
    Hud = 2,
    Window = 3,
    Modal = 4,
    Overlay = 5
}

[DefaultExecutionOrder(-1000)]
[DisallowMultipleComponent]
public sealed class PrototypeRuntimeUiManager : MonoBehaviour
{
    private static readonly PrototypeUiLayer[] OrderedLayers =
    {
        PrototypeUiLayer.Background,
        PrototypeUiLayer.World,
        PrototypeUiLayer.Hud,
        PrototypeUiLayer.Window,
        PrototypeUiLayer.Modal,
        PrototypeUiLayer.Overlay
    };

    private static PrototypeRuntimeUiManager instance;

    [SerializeField] private int sortingOrder = 300;
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);
    [SerializeField] private RectTransform canvasRoot;
    [SerializeField] private Font runtimeFont;

    private readonly Dictionary<PrototypeUiLayer, RectTransform> layerRoots = new Dictionary<PrototypeUiLayer, RectTransform>();
    private bool built;

    public static PrototypeRuntimeUiManager Instance => GetOrCreate();

    public RectTransform CanvasRoot
    {
        get
        {
            EnsureBuilt();
            return canvasRoot;
        }
    }

    public Font RuntimeFont
    {
        get
        {
            runtimeFont ??= PrototypeUiToolkit.ResolveDefaultFont();
            return runtimeFont;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        EnsureBuilt();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public static PrototypeRuntimeUiManager GetOrCreate()
    {
        if (instance != null)
        {
            instance.EnsureBuilt();
            return instance;
        }

        instance = FindFirstObjectByType<PrototypeRuntimeUiManager>();
        if (instance != null)
        {
            instance.EnsureBuilt();
            return instance;
        }

        GameObject managerObject = new GameObject("PrototypeRuntimeUiManager");
        instance = managerObject.AddComponent<PrototypeRuntimeUiManager>();
        instance.EnsureBuilt();
        return instance;
    }

    public RectTransform GetLayerRoot(PrototypeUiLayer layer)
    {
        EnsureBuilt();
        if (!layerRoots.TryGetValue(layer, out RectTransform layerRoot) || layerRoot == null)
        {
            layerRoot = CreateLayerRoot(layer);
            layerRoots[layer] = layerRoot;
        }

        return layerRoot;
    }

    public RectTransform CreateViewRoot(string name, PrototypeUiLayer layer, bool stretchToLayer = true)
    {
        RectTransform layerRoot = GetLayerRoot(layer);
        RectTransform viewRoot = PrototypeUiToolkit.CreateRectTransform(string.IsNullOrWhiteSpace(name) ? "View" : name.Trim(), layerRoot);
        if (stretchToLayer)
        {
            PrototypeUiToolkit.SetStretch(viewRoot, 0f, 0f, 0f, 0f);
        }

        return viewRoot;
    }

    public void DestroyViewRoot(ref RectTransform root)
    {
        if (root == null)
        {
            return;
        }

        Destroy(root.gameObject);
        root = null;
    }

    private void EnsureBuilt()
    {
        if (built && canvasRoot != null)
        {
            return;
        }

        runtimeFont ??= PrototypeUiToolkit.ResolveDefaultFont();

        if (canvasRoot == null)
        {
            canvasRoot = GetComponent<RectTransform>();
            if (canvasRoot == null)
            {
                canvasRoot = gameObject.AddComponent<RectTransform>();
            }
        }

        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = gameObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution;
        scaler.matchWidthOrHeight = 0.5f;

        if (GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }

        PrototypeUiToolkit.SetStretch(canvasRoot, 0f, 0f, 0f, 0f);

        for (int index = 0; index < OrderedLayers.Length; index++)
        {
            PrototypeUiLayer layer = OrderedLayers[index];
            if (!layerRoots.TryGetValue(layer, out RectTransform root) || root == null)
            {
                layerRoots[layer] = CreateLayerRoot(layer);
            }
        }

        EnsureEventSystem();
        built = true;
    }

    private RectTransform CreateLayerRoot(PrototypeUiLayer layer)
    {
        RectTransform root = PrototypeUiToolkit.CreateRectTransform(layer + "Layer", canvasRoot);
        PrototypeUiToolkit.SetStretch(root, 0f, 0f, 0f, 0f);
        root.SetSiblingIndex((int)layer);
        return root;
    }

    private static void EnsureEventSystem()
    {
        EventSystem current = EventSystem.current;
        if (current == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            current = eventSystemObject.GetComponent<EventSystem>();
        }

        if (current == null)
        {
            return;
        }

        if (current.GetComponent<BaseInputModule>() == null)
        {
            current.gameObject.AddComponent<InputSystemUIInputModule>();
        }

        current.sendNavigationEvents = true;
    }
}
