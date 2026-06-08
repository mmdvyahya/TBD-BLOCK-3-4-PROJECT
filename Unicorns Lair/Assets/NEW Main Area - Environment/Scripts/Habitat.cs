using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Habitat : MonoBehaviour
{
    [Header("Identity")]
    [Tooltip("Unique save id, e.g. beaver_habitat, polarbear_habitat")]
    [SerializeField] private string habitatId = "beaver_habitat";

    [Header("Localization keys")]
    [Tooltip("LanguageManager key for short name shown on the buy button (e.g. habitat_beaver_name)")]
    [SerializeField] private string nameKey = "habitat_beaver_name";

    [Header("Pricing")]
    [SerializeField] private int cost = 100;

    public string HabitatId => habitatId;
    public int Cost => cost;
    public Transform GetButtonAnchor() => buttonAnchor;

    [Header("Scene References")]
    [Tooltip("The 'Built' child object — disabled at start, enabled when build animation completes.")]
    [SerializeField] private GameObject builtChild;
    [Tooltip("World position where the Buy/Build button hovers. Often an empty child.")]
    [SerializeField] private Transform buttonAnchor;
    [Tooltip("Scene main camera. Auto-finds Camera.main if left empty.")]
    [SerializeField] private Camera mainCamera;

    [Header("Visual")]
    [Tooltip("Optional. If left empty the habitat shows itself when bought (mid-build state). " +
             "If set, this child is enabled during the build animation phase and disabled when 'Built' takes over.")]
    [SerializeField] private GameObject midBuildChild;

    [Header("Buy Button (PNG)")]
    [Tooltip("PNG background for the buy button (replaces the green box). If empty, the green box is used.")]
    [SerializeField] private Sprite buyButtonSprite;
    [SerializeField] private Vector2 buyButtonSize = new Vector2(420f, 160f);

    [Header("Buy Button - Name Text")]
    [SerializeField] private Vector2 nameTextPos = new Vector2(0f, 34f);
    [SerializeField] private Vector2 nameTextSize = new Vector2(400f, 70f);
    [SerializeField] private int nameTextFontSize = 40;
    [SerializeField] private Color nameTextColor = Color.white;
    [SerializeField] private TextAnchor nameTextAlignment = TextAnchor.MiddleCenter;

    [Header("Buy Button - Price PNG (under the name)")]
    [Tooltip("Optional PNG shown under the name (e.g. a coin / price pill). The price text sits next to it.")]
    [SerializeField] private Sprite pricePngSprite;
    [SerializeField] private Vector2 pricePngPos = new Vector2(-70f, -42f);
    [SerializeField] private Vector2 pricePngSize = new Vector2(60f, 60f);

    [Header("Buy Button - Price Text")]
    [SerializeField] private Vector2 priceTextPos = new Vector2(40f, -42f);
    [SerializeField] private Vector2 priceTextSize = new Vector2(180f, 60f);
    [SerializeField] private int priceTextFontSize = 36;
    [SerializeField] private Color priceTextColor = Color.white;
    [SerializeField] private TextAnchor priceTextAlignment = TextAnchor.MiddleLeft;

    private enum HabitatState { NotPlaced, Building, Built }
    private HabitatState _state;

    private Canvas _worldCanvas;
    private GameObject _buttonObj;
    private Text _nameLabel;
    private Text _priceLabel;
    private HabitatBuilder _builder;

    void Start()
    {
        GameStateManager.Ensure();
        LanguageManager.Ensure();

        if (mainCamera == null) mainCamera = Camera.main;

        _builder = GetComponent<HabitatBuilder>();
        if (_builder == null) _builder = gameObject.AddComponent<HabitatBuilder>();
        _builder.itemId = habitatId;
        _builder.BuildComplete += OnBuildComplete;

        SetState(DetermineInitialState());

        GameStateManager.Instance.CoinsChanged += OnCoinsChanged;
        LanguageManager.Instance.LanguageChanged += OnLanguageChanged;
        BuildModeToggle.StateChanged += OnBuildModeChanged;
        TutorialManager.UnlockChanged += OnTutorialUnlockChanged;
        ApplyBuildModeVisibility();
    }

    void OnDestroy()
    {
        if (GameStateManager.Instance != null) GameStateManager.Instance.CoinsChanged -= OnCoinsChanged;
        if (LanguageManager.Instance != null) LanguageManager.Instance.LanguageChanged -= OnLanguageChanged;
        BuildModeToggle.StateChanged -= OnBuildModeChanged;
        TutorialManager.UnlockChanged -= OnTutorialUnlockChanged;
    }

    void OnTutorialUnlockChanged() => ApplyBuildModeVisibility();

    void OnBuildModeChanged() => ApplyBuildModeVisibility();

    void ApplyBuildModeVisibility()
    {
        if (_worldCanvas == null) return;
        bool tutorialOk = TutorialManager.Instance == null || TutorialManager.Instance.IsHabitatUnlocked(habitatId);
        _worldCanvas.gameObject.SetActive(BuildModeToggle.IsEnabled && _state == HabitatState.NotPlaced && tutorialOk);
    }

    HabitatState DetermineInitialState()
    {
        if (GameStateManager.Instance.IsBuilt(habitatId)) return HabitatState.Built;
        if (GameStateManager.Instance.IsBought(habitatId)) return HabitatState.Building;
        return HabitatState.NotPlaced;
    }

    void SetState(HabitatState next)
    {
        _state = next;

        switch (_state)
        {
            case HabitatState.NotPlaced:
                if (builtChild != null) builtChild.SetActive(false);
                if (midBuildChild != null) midBuildChild.SetActive(false);
                SpawnWorldButton();
                UpdateButtonLabel();
                break;

            case HabitatState.Building:
                if (builtChild != null) builtChild.SetActive(false);
                if (midBuildChild != null) midBuildChild.SetActive(true);
                DestroyWorldButton();
                _builder?.StartBuild();
                break;

            case HabitatState.Built:
                if (midBuildChild != null) midBuildChild.SetActive(false);
                if (builtChild != null) builtChild.SetActive(true);
                DestroyWorldButton();
                break;
        }
    }

    void OnBuildComplete(string id)
    {
        GameStateManager.Instance.NotifyItemBuilt(habitatId);
        SetState(HabitatState.Built);
    }

    void OnCoinsChanged(int amount)
    {
        if (_state == HabitatState.NotPlaced) UpdateButtonLabel();
    }

    void OnLanguageChanged() => UpdateButtonLabel();

    void SpawnWorldButton()
    {
        if (buttonAnchor == null) { Debug.LogWarning($"[Habitat:{habitatId}] no Button Anchor set"); return; }
        if (_worldCanvas != null) return;

        EnsureEventSystem();

        var canvasObj = new GameObject($"HabitatBuyCanvas_{habitatId}");
        _worldCanvas = canvasObj.AddComponent<Canvas>();
        _worldCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _worldCanvas.sortingOrder = 5;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        _buttonObj = new GameObject("BuyBtn");
        _buttonObj.transform.SetParent(canvasObj.transform, false);
        var rt = _buttonObj.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = buyButtonSize;

        var img = _buttonObj.AddComponent<Image>();
        Color baseCol;
        if (buyButtonSprite != null)
        {
            img.sprite = buyButtonSprite;
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            img.color = Color.white;
            baseCol = Color.white;
        }
        else
        {
            baseCol = new Color(0.12f, 0.68f, 0.34f);
            img.color = baseCol;
        }

        var btn = _buttonObj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.colors = new ColorBlock
        {
            normalColor = baseCol,
            highlightedColor = baseCol * 1.12f,
            pressedColor = baseCol * 0.8f,
            selectedColor = baseCol,
            disabledColor = new Color(0.35f, 0.35f, 0.35f),
            colorMultiplier = 1f,
            fadeDuration = 0.1f
        };
        btn.onClick.AddListener(OnButtonPressed);

        // Name text (overlaid on the button PNG)
        var nameObj = new GameObject("NameLabel");
        nameObj.transform.SetParent(_buttonObj.transform, false);
        var nrt = nameObj.AddComponent<RectTransform>();
        nrt.anchorMin = nrt.anchorMax = nrt.pivot = new Vector2(0.5f, 0.5f);
        nrt.anchoredPosition = nameTextPos; nrt.sizeDelta = nameTextSize;
        _nameLabel = nameObj.AddComponent<Text>();
        _nameLabel.font = GetFont();
        _nameLabel.fontSize = nameTextFontSize;
        _nameLabel.fontStyle = FontStyle.Bold;
        _nameLabel.alignment = nameTextAlignment;
        _nameLabel.color = nameTextColor;
        _nameLabel.raycastTarget = false;
        _nameLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
        _nameLabel.verticalOverflow = VerticalWrapMode.Overflow;
        nameObj.AddComponent<Outline>().effectColor = new Color(0f, 0.2f, 0.1f, 0.7f);

        // Price PNG (under the name)
        if (pricePngSprite != null)
        {
            var pObj = new GameObject("PricePng");
            pObj.transform.SetParent(_buttonObj.transform, false);
            var prt2 = pObj.AddComponent<RectTransform>();
            prt2.anchorMin = prt2.anchorMax = prt2.pivot = new Vector2(0.5f, 0.5f);
            prt2.anchoredPosition = pricePngPos; prt2.sizeDelta = pricePngSize;
            var pImg = pObj.AddComponent<Image>();
            pImg.sprite = pricePngSprite;
            pImg.preserveAspect = true;
            pImg.raycastTarget = false;
        }

        // Price text (next to the price PNG)
        var priceObj = new GameObject("PriceLabel");
        priceObj.transform.SetParent(_buttonObj.transform, false);
        var prt = priceObj.AddComponent<RectTransform>();
        prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(0.5f, 0.5f);
        prt.anchoredPosition = priceTextPos; prt.sizeDelta = priceTextSize;
        _priceLabel = priceObj.AddComponent<Text>();
        _priceLabel.font = GetFont();
        _priceLabel.fontSize = priceTextFontSize;
        _priceLabel.fontStyle = FontStyle.Bold;
        _priceLabel.alignment = priceTextAlignment;
        _priceLabel.color = priceTextColor;
        _priceLabel.raycastTarget = false;
        priceObj.AddComponent<Outline>().effectColor = new Color(0f, 0.2f, 0.1f, 0.7f);

        StartCoroutine(TrackWorldPosition(rt));
        ApplyBuildModeVisibility();
    }

    void DestroyWorldButton()
    {
        if (_worldCanvas != null) { Destroy(_worldCanvas.gameObject); _worldCanvas = null; }
        _buttonObj = null;
        _nameLabel = null;
        _priceLabel = null;
    }

    IEnumerator TrackWorldPosition(RectTransform rt)
    {
        var canvasRt = _worldCanvas.GetComponent<RectTransform>();
        float offset = Random.Range(0f, Mathf.PI * 2f);
        while (rt != null && _state == HabitatState.NotPlaced)
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null || buttonAnchor == null) { yield return null; continue; }

            Vector3 screen = mainCamera.WorldToScreenPoint(buttonAnchor.position);
            if (screen.z > 0f)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screen, null, out Vector2 local);
                local.y += Mathf.Sin(Time.time * 2f + offset) * 8f;
                rt.anchoredPosition = local;
            }
            yield return null;
        }
    }

    void OnButtonPressed()
    {
        if (_state != HabitatState.NotPlaced) return;

        if (GameStateManager.Instance.Coins < cost)
        {
            StartCoroutine(ShakeButton());
            return;
        }
        GameStateManager.Instance.TrySpendCoins(cost);
        GameStateManager.Instance.NotifyItemBought(habitatId);
        SetState(HabitatState.Building);
    }

    void UpdateButtonLabel()
    {
        if (_nameLabel == null && _priceLabel == null) return;
        if (_state != HabitatState.NotPlaced) return;

        var lm = LanguageManager.Instance;

        if (_nameLabel != null)
        {
            string animalName = lm != null ? lm.Get(nameKey) : nameKey;
            if (animalName == $"[{nameKey}]") animalName = nameKey;
            _nameLabel.text = animalName;
        }

        if (_priceLabel != null)
        {
            string priceLine = lm != null ? lm.Get("shop_currency_short", cost) : cost.ToString();
            if (priceLine == "[shop_currency_short]") priceLine = cost.ToString();
            _priceLabel.text = priceLine;
        }
    }

    IEnumerator ShakeButton()
    {
        if (_buttonObj == null) yield break;
        var rt = _buttonObj.GetComponent<RectTransform>();
        var origin = rt.anchoredPosition;
        foreach (float off in new[] { -18f, 18f, -14f, 14f, -8f, 8f, 0f })
        {
            if (rt == null) yield break;
            rt.anchoredPosition = origin + new Vector2(off, 0f);
            yield return new WaitForSeconds(0.04f);
        }
        if (rt != null) rt.anchoredPosition = origin;
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