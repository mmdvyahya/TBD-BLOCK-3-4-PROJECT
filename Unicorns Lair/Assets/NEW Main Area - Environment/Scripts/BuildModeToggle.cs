using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BuildModeToggle : MonoBehaviour
{
    public static bool IsEnabled { get; private set; } = true;
    public static event System.Action StateChanged;

    private Canvas _canvas;
    private GameObject _btnObj;
    private Image _btnImg;
    private Text _btnLabel;
    private GameObject _alertBadge;
    private Image _alertBadgeImg;
    private bool _showAlert;

    private static readonly Color OnColor = new Color(0.18f, 0.62f, 0.28f, 0.92f);
    private static readonly Color OffColor = new Color(0.32f, 0.32f, 0.36f, 0.92f);

    void Start()
    {
        LanguageManager.Ensure();
        GameStateManager.Ensure();
        BuildButton();
        UpdateVisualState();

        GameStateManager.Instance.CoinsChanged += OnCoinsChanged;
        GameStateManager.Instance.ItemBought += OnItemStateChanged;
        GameStateManager.Instance.ItemBuilt += OnItemStateChanged;
        LanguageManager.Instance.LanguageChanged += UpdateLabel;

        StartCoroutine(PulseAlert());
        RefreshAlert();
    }

    void OnDestroy()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.CoinsChanged -= OnCoinsChanged;
            GameStateManager.Instance.ItemBought -= OnItemStateChanged;
            GameStateManager.Instance.ItemBuilt -= OnItemStateChanged;
        }
        if (LanguageManager.Instance != null)
            LanguageManager.Instance.LanguageChanged -= UpdateLabel;
    }

    void OnCoinsChanged(int amount) => RefreshAlert();
    void OnItemStateChanged(string itemId) => RefreshAlert();

    void BuildButton()
    {
        var cObj = new GameObject("BuildModeCanvas");
        _canvas = cObj.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 6;
        var scaler = cObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        cObj.AddComponent<GraphicRaycaster>();
        EnsureEventSystem();

        _btnObj = new GameObject("BuildModeBtn");
        _btnObj.transform.SetParent(cObj.transform, false);
        var rt = _btnObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = new Vector2(20f, 20f);
        rt.sizeDelta = new Vector2(280f, 100f);

        _btnImg = _btnObj.AddComponent<Image>();
        _btnImg.color = OnColor;

        var btn = _btnObj.AddComponent<Button>();
        btn.targetGraphic = _btnImg;
        btn.colors = new ColorBlock
        {
            normalColor = OnColor,
            highlightedColor = OnColor * 1.2f,
            pressedColor = OnColor * 0.75f,
            selectedColor = OnColor,
            disabledColor = OnColor * 0.5f,
            colorMultiplier = 1f,
            fadeDuration = 0.08f
        };
        btn.onClick.AddListener(Toggle);

        var lblObj = new GameObject("Label");
        lblObj.transform.SetParent(_btnObj.transform, false);
        var lrt = lblObj.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        _btnLabel = lblObj.AddComponent<Text>();
        _btnLabel.font = GetFont();
        _btnLabel.fontSize = 32;
        _btnLabel.fontStyle = FontStyle.Bold;
        _btnLabel.alignment = TextAnchor.MiddleCenter;
        _btnLabel.color = Color.white;
        _btnLabel.raycastTarget = false;
        lblObj.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.6f);

        _alertBadge = new GameObject("AlertBadge");
        _alertBadge.transform.SetParent(_btnObj.transform, false);
        var art = _alertBadge.AddComponent<RectTransform>();
        art.anchorMin = new Vector2(1f, 1f); art.anchorMax = new Vector2(1f, 1f);
        art.pivot = new Vector2(0.5f, 0.5f);
        art.anchoredPosition = new Vector2(-10f, -10f);
        art.sizeDelta = new Vector2(50f, 50f);
        _alertBadgeImg = _alertBadge.AddComponent<Image>();
        _alertBadgeImg.color = new Color(0.85f, 0.18f, 0.18f);
        _alertBadgeImg.raycastTarget = false;

        var alertTxt = new GameObject("AlertText");
        alertTxt.transform.SetParent(_alertBadge.transform, false);
        var atrt = alertTxt.AddComponent<RectTransform>();
        atrt.anchorMin = Vector2.zero; atrt.anchorMax = Vector2.one;
        atrt.offsetMin = atrt.offsetMax = Vector2.zero;
        var atTxt = alertTxt.AddComponent<Text>();
        atTxt.text = "!"; atTxt.font = GetFont();
        atTxt.fontSize = 38; atTxt.fontStyle = FontStyle.Bold;
        atTxt.alignment = TextAnchor.MiddleCenter;
        atTxt.color = Color.white; atTxt.raycastTarget = false;

        _alertBadge.SetActive(false);
        UpdateLabel();
    }

    void Toggle()
    {
        IsEnabled = !IsEnabled;
        UpdateVisualState();
        StateChanged?.Invoke();
    }

    void UpdateVisualState()
    {
        if (_btnImg == null) return;
        _btnImg.color = IsEnabled ? OnColor : OffColor;
        var btn = _btnObj.GetComponent<Button>();
        Color baseCol = IsEnabled ? OnColor : OffColor;
        btn.colors = new ColorBlock
        {
            normalColor = baseCol,
            highlightedColor = baseCol * 1.2f,
            pressedColor = baseCol * 0.75f,
            selectedColor = baseCol,
            disabledColor = baseCol * 0.5f,
            colorMultiplier = 1f,
            fadeDuration = 0.08f
        };
        UpdateLabel();
    }

    void UpdateLabel()
    {
        if (_btnLabel == null) return;
        var lm = LanguageManager.Instance;
        string baseLabel = lm != null ? lm.Get("build_mode_label") : "Build Mode";
        if (baseLabel == "[build_mode_label]") baseLabel = "Build Mode";
        string state = lm != null ? lm.Get(IsEnabled ? "build_mode_on" : "build_mode_off") : (IsEnabled ? "ON" : "OFF");
        if (state.StartsWith("[")) state = IsEnabled ? "ON" : "OFF";
        _btnLabel.text = $"{baseLabel}\n{state}";
    }

    void RefreshAlert()
    {
        var habitats = FindObjectsByType<Habitat>(FindObjectsSortMode.None);
        bool anyAffordable = false;
        int coins = GameStateManager.Instance != null ? GameStateManager.Instance.Coins : 0;
        foreach (var h in habitats)
        {
            if (h == null) continue;
            if (GameStateManager.Instance.IsBought(h.HabitatId)) continue;
            if (coins >= h.Cost) { anyAffordable = true; break; }
        }
        _showAlert = anyAffordable;
        if (_alertBadge != null) _alertBadge.SetActive(_showAlert);
    }

    IEnumerator PulseAlert()
    {
        while (true)
        {
            if (_alertBadge != null && _alertBadge.activeSelf)
            {
                float p = (Mathf.Sin(Time.time * 4f) + 1f) * 0.5f;
                float scale = Mathf.Lerp(0.85f, 1.15f, p);
                _alertBadge.transform.localScale = Vector3.one * scale;
                if (_alertBadgeImg != null)
                {
                    var c = _alertBadgeImg.color;
                    c.a = Mathf.Lerp(0.7f, 1f, p);
                    _alertBadgeImg.color = c;
                }
            }
            yield return null;
        }
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