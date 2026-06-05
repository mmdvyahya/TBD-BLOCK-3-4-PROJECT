using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class MainAreaManager : MonoBehaviour
{
    [Header("Optional References")]
    [SerializeField] private Camera mainCamera;

    [Header("Coin UI Background")]
    [Tooltip("Optional PNG used as the coin panel background. If empty, the old dark box is used.")]
    [SerializeField] private Sprite coinBackgroundSprite;
    [Tooltip("Transparency of the PNG background. 1 = opaque, 0 = invisible.")]
    [Range(0f, 1f)]
    [SerializeField] private float coinBackgroundOpacity = 1f;
    [Tooltip("Position of the panel from the top-left corner. X = right, Y = down (negative).")]
    [SerializeField] private Vector2 coinPanelPosition = new Vector2(20f, -20f);
    [Tooltip("Size of the panel in reference pixels. Match this to your PNG (it's wider than the old box).")]
    [SerializeField] private Vector2 coinPanelSize = new Vector2(480f, 110f);

    [Header("Coin Number Text")]
    [SerializeField] private float coinTextSize = 48f;
    [SerializeField] private TextAnchor coinTextAlignment = TextAnchor.MiddleCenter;
    [SerializeField] private Color coinTextColor = Color.white;
    [Tooltip("Padding from the panel edges to the number, so it lines up with your PNG's number area.")]
    [SerializeField] private float coinTextPadLeft = 20f;
    [SerializeField] private float coinTextPadRight = 20f;
    [SerializeField] private float coinTextPadTop = 0f;
    [SerializeField] private float coinTextPadBottom = 0f;

    private Text _coinLabel;

    void Start()
    {
        GameStateManager.Ensure();
        LanguageManager.Ensure();

        if (mainCamera == null) mainCamera = Camera.main;

        EnsureEventSystem();
        SetupCoinDisplay();

        GameStateManager.Instance.CoinsChanged += OnCoinsChanged;
        LanguageManager.Instance.LanguageChanged += OnLanguageChanged;
    }

    void OnDestroy()
    {
        if (GameStateManager.Instance != null) GameStateManager.Instance.CoinsChanged -= OnCoinsChanged;
        if (LanguageManager.Instance != null) LanguageManager.Instance.LanguageChanged -= OnLanguageChanged;
    }

    void OnCoinsChanged(int amount) => UpdateCoinDisplay(amount);
    void OnLanguageChanged() => UpdateCoinDisplay(GameStateManager.Instance.Coins);

    void SetupCoinDisplay()
    {
        var canvasObj = new GameObject("CoinCanvas");
        var cv = canvasObj.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        var coinBg = new GameObject("CoinBg");
        coinBg.transform.SetParent(canvasObj.transform, false);
        var bgImg = coinBg.AddComponent<Image>();
        bgImg.raycastTarget = false;

        if (coinBackgroundSprite != null)
        {
            bgImg.sprite = coinBackgroundSprite;
            bgImg.type = Image.Type.Simple;
            bgImg.preserveAspect = false;
            bgImg.color = new Color(1f, 1f, 1f, coinBackgroundOpacity);
        }
        else
        {
            bgImg.color = new Color(0f, 0f, 0f, 0.45f);
        }

        var bgrt = coinBg.GetComponent<RectTransform>();
        bgrt.anchorMin = new Vector2(0f, 1f); bgrt.anchorMax = new Vector2(0f, 1f);
        bgrt.pivot = new Vector2(0f, 1f); bgrt.anchoredPosition = coinPanelPosition;
        bgrt.sizeDelta = coinPanelSize;

        var coinObj = new GameObject("CoinText");
        coinObj.transform.SetParent(coinBg.transform, false);
        var crt = coinObj.AddComponent<RectTransform>();
        crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
        crt.offsetMin = new Vector2(coinTextPadLeft, coinTextPadBottom);
        crt.offsetMax = new Vector2(-coinTextPadRight, -coinTextPadTop);
        _coinLabel = coinObj.AddComponent<Text>();
        _coinLabel.font = GetFont();
        _coinLabel.fontSize = Mathf.RoundToInt(coinTextSize);
        _coinLabel.fontStyle = FontStyle.Bold;
        _coinLabel.alignment = coinTextAlignment;
        _coinLabel.color = coinTextColor;
        _coinLabel.raycastTarget = false;
        coinObj.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.6f);

        UpdateCoinDisplay(GameStateManager.Instance.Coins);
    }

    void UpdateCoinDisplay(int amount)
    {
        if (_coinLabel == null) return;
        _coinLabel.text = amount.ToString();
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