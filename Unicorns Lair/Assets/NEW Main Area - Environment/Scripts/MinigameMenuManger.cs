using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MinigameMenuManager : MonoBehaviour
{
    private Canvas _canvas;
    private bool _menuOpen;
    private GameObject _menuPanel;

    private static readonly (string labelKey, string fallback, Color color, string scene)[] Minigames =
    {
        ("minigame_parrot",     "Papegaai",    new Color(0.18f, 0.62f, 0.25f), "ParrotFeeding_minigame"),
        ("minigame_polarbear",  "IJsbeer",     new Color(0.18f, 0.48f, 0.78f), "PolarBear"),
        ("minigame_prairiedog", "Prairiehond", new Color(0.72f, 0.48f, 0.12f), "PrairieDogminigame"),
        ("minigame_hippo",      "Nijlpaard",   new Color(0.46f, 0.42f, 0.62f), "HippoMinigame"),
        ("minigame_baboon",     "Baviaan",     new Color(0.55f, 0.32f, 0.16f), "Minigame"),
    };

    void Start()
    {
        LanguageManager.Ensure();
        BuildButton();
    }

    void BuildButton()
    {
        var cObj = new GameObject("MinigameMenuCanvas");
        _canvas = cObj.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 6;
        var scaler = cObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        cObj.AddComponent<GraphicRaycaster>();

        EnsureEventSystem();

        var btnObj = new GameObject("MinigamesBtn");
        btnObj.transform.SetParent(cObj.transform, false);
        var rt = btnObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-20f, -20f);
        rt.sizeDelta = new Vector2(280f, 100f);

        var img = btnObj.AddComponent<Image>();
        img.color = new Color(0.12f, 0.18f, 0.32f, 0.92f);

        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.colors = new ColorBlock
        {
            normalColor = new Color(0.12f, 0.18f, 0.32f, 0.92f),
            highlightedColor = new Color(0.20f, 0.30f, 0.50f, 1f),
            pressedColor = new Color(0.08f, 0.12f, 0.22f, 1f),
            selectedColor = new Color(0.12f, 0.18f, 0.32f, 0.92f),
            disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f),
            colorMultiplier = 1f,
            fadeDuration = 0.08f
        };
        btn.onClick.AddListener(ToggleMenu);

        var lObj = new GameObject("Label");
        lObj.transform.SetParent(btnObj.transform, false);
        var lrt = lObj.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        var lTxt = lObj.AddComponent<Text>();
        lTxt.text = "Minigames";
        lTxt.font = GetFont();
        lTxt.fontSize = 38;
        lTxt.fontStyle = FontStyle.Bold;
        lTxt.alignment = TextAnchor.MiddleCenter;
        lTxt.color = Color.white;
        lTxt.raycastTarget = false;
    }

    void ToggleMenu()
    {
        if (_menuOpen)
            CloseMenu();
        else
            OpenMenu();
    }

    void OpenMenu()
    {
        if (_menuPanel != null) return;
        _menuOpen = true;

        var lm = LanguageManager.Instance;

        _menuPanel = new GameObject("MinigameMenu");
        _menuPanel.transform.SetParent(_canvas.transform, false);

        var overlay = _menuPanel.AddComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.55f);
        var ort = _menuPanel.GetComponent<RectTransform>();
        ort.anchorMin = Vector2.zero;
        ort.anchorMax = Vector2.one;
        ort.offsetMin = ort.offsetMax = Vector2.zero;

        var overlayBtn = _menuPanel.AddComponent<Button>();
        overlayBtn.targetGraphic = overlay;
        overlayBtn.colors = new ColorBlock
        {
            normalColor = new Color(0f, 0f, 0f, 0.55f),
            highlightedColor = new Color(0f, 0f, 0f, 0.55f),
            pressedColor = new Color(0f, 0f, 0f, 0.55f),
            selectedColor = new Color(0f, 0f, 0f, 0.55f),
            disabledColor = new Color(0f, 0f, 0f, 0.55f),
            colorMultiplier = 1f,
            fadeDuration = 0f
        };
        overlayBtn.onClick.AddListener(CloseMenu);

        var card = new GameObject("Card");
        card.transform.SetParent(_menuPanel.transform, false);
        var crt = card.AddComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.5f, 0.5f);
        crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.pivot = new Vector2(0.5f, 0.5f);
        crt.anchoredPosition = Vector2.zero;
        crt.sizeDelta = new Vector2(820f, 100f + Minigames.Length * 160f);
        var cImg = card.AddComponent<Image>();
        cImg.color = new Color(0.08f, 0.12f, 0.20f, 0.97f);
        cImg.raycastTarget = true;

        var accent = new GameObject("Accent");
        accent.transform.SetParent(card.transform, false);
        var aRt = accent.AddComponent<RectTransform>();
        aRt.anchorMin = new Vector2(0f, 1f); aRt.anchorMax = new Vector2(1f, 1f);
        aRt.pivot = new Vector2(0.5f, 1f); aRt.anchoredPosition = Vector2.zero; aRt.sizeDelta = new Vector2(0f, 12f);
        accent.AddComponent<Image>().color = new Color(0.22f, 0.55f, 1f);

        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(card.transform, false);
        var trt = titleObj.AddComponent<RectTransform>();
        trt.anchorMin = new Vector2(0.5f, 1f); trt.anchorMax = new Vector2(0.5f, 1f);
        trt.pivot = new Vector2(0.5f, 1f);
        trt.anchoredPosition = new Vector2(0f, -20f);
        trt.sizeDelta = new Vector2(780f, 76f);
        var tTxt = titleObj.AddComponent<Text>();
        tTxt.text = lm != null ? lm.Get("minigames_title") : "Minigames";
        tTxt.font = GetFont(); tTxt.fontSize = 50; tTxt.fontStyle = FontStyle.Bold;
        tTxt.alignment = TextAnchor.MiddleCenter; tTxt.color = Color.white; tTxt.raycastTarget = false;
        titleObj.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.6f);

        for (int i = 0; i < Minigames.Length; i++)
        {
            var (key, fallback, color, scene) = Minigames[i];
            float yPos = -108f - i * 160f;

            var row = new GameObject($"Row_{i}");
            row.transform.SetParent(card.transform, false);
            var rrt = row.AddComponent<RectTransform>();
            rrt.anchorMin = new Vector2(0.5f, 1f); rrt.anchorMax = new Vector2(0.5f, 1f);
            rrt.pivot = new Vector2(0.5f, 1f);
            rrt.anchoredPosition = new Vector2(0f, yPos);
            rrt.sizeDelta = new Vector2(760f, 140f);
            var rImg = row.AddComponent<Image>();
            rImg.color = new Color(color.r * 0.6f, color.g * 0.6f, color.b * 0.6f, 0.5f);

            var rowBtn = row.AddComponent<Button>();
            rowBtn.targetGraphic = rImg;
            string capturedScene = scene;
            rowBtn.colors = new ColorBlock
            {
                normalColor = new Color(color.r * 0.6f, color.g * 0.6f, color.b * 0.6f, 0.5f),
                highlightedColor = new Color(color.r, color.g, color.b, 0.9f),
                pressedColor = new Color(color.r * 0.4f, color.g * 0.4f, color.b * 0.4f, 1f),
                selectedColor = new Color(color.r * 0.6f, color.g * 0.6f, color.b * 0.6f, 0.5f),
                disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f),
                colorMultiplier = 1f,
                fadeDuration = 0.1f
            };
            rowBtn.onClick.AddListener(() => SceneManager.LoadScene(capturedScene));

            var colorBar = new GameObject("ColorBar");
            colorBar.transform.SetParent(row.transform, false);
            var cbRt = colorBar.AddComponent<RectTransform>();
            cbRt.anchorMin = new Vector2(0f, 0f); cbRt.anchorMax = new Vector2(0f, 1f);
            cbRt.pivot = new Vector2(0f, 0.5f); cbRt.anchoredPosition = Vector2.zero; cbRt.sizeDelta = new Vector2(12f, 0f);
            colorBar.AddComponent<Image>().color = color;

            string label = lm != null ? lm.Get(key) : fallback;
            if (label == $"[{key}]") label = fallback;

            var nameObj = new GameObject("Name");
            nameObj.transform.SetParent(row.transform, false);
            var nrt = nameObj.AddComponent<RectTransform>();
            nrt.anchorMin = new Vector2(0f, 0.5f); nrt.anchorMax = new Vector2(1f, 0.5f);
            nrt.pivot = new Vector2(0f, 0.5f); nrt.anchoredPosition = new Vector2(36f, 0f);
            nrt.sizeDelta = new Vector2(-100f, 80f);
            var nTxt = nameObj.AddComponent<Text>();
            nTxt.text = label; nTxt.font = GetFont(); nTxt.fontSize = 46; nTxt.fontStyle = FontStyle.Bold;
            nTxt.alignment = TextAnchor.MiddleLeft; nTxt.color = Color.white; nTxt.raycastTarget = false;

            var arrow = new GameObject("Arrow");
            arrow.transform.SetParent(row.transform, false);
            var arRt = arrow.AddComponent<RectTransform>();
            arRt.anchorMin = new Vector2(1f, 0.5f); arRt.anchorMax = new Vector2(1f, 0.5f);
            arRt.pivot = new Vector2(1f, 0.5f); arRt.anchoredPosition = new Vector2(-18f, 0f); arRt.sizeDelta = new Vector2(50f, 60f);
            var arTxt = arrow.AddComponent<Text>();
            arTxt.text = "▶"; arTxt.font = GetFont(); arTxt.fontSize = 40;
            arTxt.alignment = TextAnchor.MiddleCenter;
            arTxt.color = new Color(1f, 1f, 1f, 0.5f); arTxt.raycastTarget = false;
        }
    }

    void CloseMenu()
    {
        if (_menuPanel != null) { Destroy(_menuPanel); _menuPanel = null; }
        _menuOpen = false;
    }

    void EnsureEventSystem()
    {
        if (UnityEngine.EventSystems.EventSystem.current != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
    }

    static Font _font;
    static Font GetFont()
    {
        if (_font != null) return _font;
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (_font == null) _font = Font.CreateDynamicFontFromOSFont("Arial", 24);
        return _font;
    }
}