using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class MainAreaManager : MonoBehaviour
{
    [Header("Optional References")]
    [SerializeField] private Camera mainCamera;

    private Text _coinLabel;

    void Start()
    {
        GameStateManager.Ensure();
        LanguageManager.Ensure();

        if (mainCamera == null) mainCamera = Camera.main;

        EnsureEventSystem();
        SetupCoinDisplay();

        GameStateManager.Instance.CoinsChanged   += OnCoinsChanged;
        LanguageManager.Instance.LanguageChanged += OnLanguageChanged;
    }

    void OnDestroy()
    {
        if (GameStateManager.Instance != null) GameStateManager.Instance.CoinsChanged   -= OnCoinsChanged;
        if (LanguageManager.Instance  != null) LanguageManager.Instance.LanguageChanged -= OnLanguageChanged;
    }

    void OnCoinsChanged(int amount) => UpdateCoinDisplay(amount);
    void OnLanguageChanged()        => UpdateCoinDisplay(GameStateManager.Instance.Coins);

    void SetupCoinDisplay()
    {
        var canvasObj = new GameObject("CoinCanvas");
        var cv = canvasObj.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        var coinBg = new GameObject("CoinBg");
        coinBg.transform.SetParent(canvasObj.transform, false);
        var bgImg = coinBg.AddComponent<Image>();
        bgImg.color         = new Color(0f, 0f, 0f, 0.45f);
        bgImg.raycastTarget = false;
        var bgrt = coinBg.GetComponent<RectTransform>();
        bgrt.anchorMin        = new Vector2(0f, 1f); bgrt.anchorMax = new Vector2(0f, 1f);
        bgrt.pivot            = new Vector2(0f, 1f); bgrt.anchoredPosition = new Vector2(20f, -20f);
        bgrt.sizeDelta        = new Vector2(320f, 80f);

        var coinObj = new GameObject("CoinText");
        coinObj.transform.SetParent(coinBg.transform, false);
        var crt = coinObj.AddComponent<RectTransform>();
        crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
        crt.offsetMin = new Vector2(10f, 0f); crt.offsetMax = new Vector2(-10f, 0f);
        _coinLabel           = coinObj.AddComponent<Text>();
        _coinLabel.font      = GetFont();
        _coinLabel.fontSize  = 44;
        _coinLabel.fontStyle = FontStyle.Bold;
        _coinLabel.alignment = TextAnchor.MiddleCenter;
        _coinLabel.color     = Color.white;
        _coinLabel.raycastTarget = false;
        coinObj.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.6f);

        UpdateCoinDisplay(GameStateManager.Instance.Coins);
    }

    void UpdateCoinDisplay(int amount)
    {
        if (_coinLabel == null) return;
        var lm = LanguageManager.Instance;
        string text = lm != null ? lm.Get("shop_currency", amount) : amount + " coins";
        if (text == "[shop_currency]") text = amount + " coins";
        _coinLabel.text = text;
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
