using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DebugMenu : MonoBehaviour
{
    private Canvas     _canvas;
    private GameObject _panel;
    private bool       _open;

    void Start()
    {
        BuildIconButton();
    }

    void BuildIconButton()
    {
        var cObj = new GameObject("DebugCanvas");
        _canvas = cObj.AddComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 50;
        var scaler = cObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight  = 0.5f;
        cObj.AddComponent<GraphicRaycaster>();
        EnsureEventSystem();

        var btnObj = new GameObject("DebugBtn");
        btnObj.transform.SetParent(cObj.transform, false);
        var rt = btnObj.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 1f);
        rt.anchorMax        = new Vector2(0.5f, 1f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -20f);
        rt.sizeDelta        = new Vector2(120f, 80f);

        var img = btnObj.AddComponent<Image>();
        img.color = new Color(0.85f, 0.55f, 0.10f, 0.9f);

        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.colors = new ColorBlock
        {
            normalColor      = new Color(0.85f, 0.55f, 0.10f, 0.9f),
            highlightedColor = new Color(1f,    0.70f, 0.20f, 1f),
            pressedColor     = new Color(0.60f, 0.38f, 0.05f, 1f),
            selectedColor    = new Color(0.85f, 0.55f, 0.10f, 0.9f),
            disabledColor    = new Color(0.4f,  0.4f,  0.4f,  0.5f),
            colorMultiplier  = 1f, fadeDuration = 0.08f
        };
        btn.onClick.AddListener(TogglePanel);

        var lObj = new GameObject("Label");
        lObj.transform.SetParent(btnObj.transform, false);
        var lrt = lObj.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        var lTxt = lObj.AddComponent<Text>();
        lTxt.text          = "DEBUG";
        lTxt.font          = GetFont();
        lTxt.fontSize      = 28;
        lTxt.fontStyle     = FontStyle.Bold;
        lTxt.alignment     = TextAnchor.MiddleCenter;
        lTxt.color         = Color.white;
        lTxt.raycastTarget = false;
        lObj.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.6f);
    }

    void TogglePanel()
    {
        if (_open) ClosePanel();
        else OpenPanel();
    }

    void OpenPanel()
    {
        if (_panel != null) return;
        _open = true;

        _panel = new GameObject("DebugPanel");
        _panel.transform.SetParent(_canvas.transform, false);
        var prt = _panel.AddComponent<RectTransform>();
        prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one;
        prt.offsetMin = prt.offsetMax = Vector2.zero;

        var bg = _panel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.65f);

        var bgBtn = _panel.AddComponent<Button>();
        bgBtn.targetGraphic = bg;
        bgBtn.onClick.AddListener(ClosePanel);

        var card = new GameObject("Card");
        card.transform.SetParent(_panel.transform, false);
        var crt = card.AddComponent<RectTransform>();
        crt.anchorMin = crt.anchorMax = crt.pivot = new Vector2(0.5f, 0.5f);
        crt.anchoredPosition = Vector2.zero;
        crt.sizeDelta = new Vector2(720f, 880f);
        var cImg = card.AddComponent<Image>();
        cImg.color = new Color(0.12f, 0.14f, 0.18f, 0.97f);

        var accent = new GameObject("Accent");
        accent.transform.SetParent(card.transform, false);
        var aRt = accent.AddComponent<RectTransform>();
        aRt.anchorMin = new Vector2(0f, 1f); aRt.anchorMax = new Vector2(1f, 1f);
        aRt.pivot = new Vector2(0.5f, 1f); aRt.anchoredPosition = Vector2.zero; aRt.sizeDelta = new Vector2(0f, 12f);
        accent.AddComponent<Image>().color = new Color(0.85f, 0.55f, 0.10f);

        MakeLabel(card.transform, "DEBUG MENU", new Vector2(0f, -28f), new Vector2(660f, 70f), 48, FontStyle.Bold, Color.white, true);
        MakeLabel(card.transform, "Development tools", new Vector2(0f, -100f), new Vector2(660f, 40f), 22, FontStyle.Normal, new Color(0.7f, 0.7f, 0.75f), false);

        MakeButton(card.transform, "+10 Coins",    new Vector2(0f, -190f), new Vector2(620f, 90f), new Color(0.20f, 0.55f, 0.30f), () => AddCoins(10));
        MakeButton(card.transform, "+100 Coins",   new Vector2(0f, -290f), new Vector2(620f, 90f), new Color(0.18f, 0.62f, 0.34f), () => AddCoins(100));
        MakeButton(card.transform, "+1000 Coins",  new Vector2(0f, -390f), new Vector2(620f, 90f), new Color(0.16f, 0.70f, 0.40f), () => AddCoins(1000));
        MakeButton(card.transform, "Reset All Progress", new Vector2(0f, -510f), new Vector2(620f, 90f), new Color(0.62f, 0.32f, 0.10f), ResetProgress);
        MakeButton(card.transform, "Restart Game",       new Vector2(0f, -610f), new Vector2(620f, 90f), new Color(0.65f, 0.18f, 0.18f), RestartGame);
        MakeButton(card.transform, "Close",              new Vector2(0f, -780f), new Vector2(620f, 80f), new Color(0.30f, 0.32f, 0.36f), ClosePanel);
    }

    void ClosePanel()
    {
        if (_panel != null) Destroy(_panel);
        _panel = null;
        _open  = false;
    }

    void AddCoins(int amount)
    {
        GameStateManager.Ensure();
        GameStateManager.Instance.AddCoins(amount);
        Debug.Log($"[Debug] Added {amount} coins (total: {GameStateManager.Instance.Coins})");
    }

    void ResetProgress()
    {
        GameStateManager.Ensure();
        GameStateManager.Instance.ResetAllProgress();
        Debug.Log("[Debug] Progress reset");
        ClosePanel();
    }

    void RestartGame()
    {
        Debug.Log("[Debug] Restarting current scene");
        var current = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(current);
    }

    void MakeLabel(Transform parent, string text, Vector2 pos, Vector2 size, int fontSize, FontStyle style, Color color, bool outline)
    {
        var obj = new GameObject("Label");
        obj.transform.SetParent(parent, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f); rt.anchoredPosition = pos; rt.sizeDelta = size;
        var t = obj.AddComponent<Text>();
        t.text = text; t.font = GetFont(); t.fontSize = fontSize; t.fontStyle = style;
        t.alignment = TextAnchor.MiddleCenter; t.color = color; t.raycastTarget = false;
        if (outline) obj.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.6f);
    }

    void MakeButton(Transform parent, string label, Vector2 pos, Vector2 size, Color color, System.Action onClick)
    {
        var obj = new GameObject($"Btn_{label}");
        obj.transform.SetParent(parent, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f); rt.anchoredPosition = pos; rt.sizeDelta = size;
        var img = obj.AddComponent<Image>();
        img.color = color;
        var btn = obj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.colors = new ColorBlock
        {
            normalColor = color, highlightedColor = color * 1.2f,
            pressedColor = color * 0.7f, selectedColor = color,
            disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f),
            colorMultiplier = 1f, fadeDuration = 0.08f
        };
        btn.onClick.AddListener(() => onClick?.Invoke());

        var lObj = new GameObject("Label");
        lObj.transform.SetParent(obj.transform, false);
        var lrt = lObj.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        var t = lObj.AddComponent<Text>();
        t.text = label; t.font = GetFont(); t.fontSize = 38; t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter; t.color = Color.white; t.raycastTarget = false;
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
