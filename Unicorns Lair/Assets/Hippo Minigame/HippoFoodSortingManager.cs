using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HippoFoodSortingManager : MonoBehaviour
{
    private enum SortingState { WaitingForSwipe, MovingItem, Complete }
    private enum FoodCategory { Approved, NotSuitable }

    [System.Serializable]
    private class FoodItemData
    {
        public string foodName;
        [Tooltip("Optional. Localization key for this food's display name. If empty, foodName is shown literally.")]
        public string localizationKey;
        public FoodCategory correctCategory;
        public Color displayColor = Color.white;
    }

    [Header("References")]
    [SerializeField] private HippoSwipeInput swipeInput;

    [Header("Scene Objects")]
    [SerializeField] private Transform itemSpawnPoint;
    [SerializeField] private Transform approvedZone;
    [SerializeField] private Transform notSuitableZone;
    [SerializeField] private GameObject foodItemPrefab;
    [SerializeField] private Transform hippoVisual;

    [Header("Food Items")]
    [SerializeField]
    private FoodItemData[] foodItems =
    {
        new FoodItemData { foodName = "Watermelon", localizationKey = "food_watermelon", correctCategory = FoodCategory.Approved,    displayColor = new Color(0.20f, 0.80f, 0.30f) },
        new FoodItemData { foodName = "Lettuce",    localizationKey = "food_lettuce",    correctCategory = FoodCategory.Approved,    displayColor = new Color(0.45f, 0.85f, 0.30f) },
        new FoodItemData { foodName = "Cabbage",    localizationKey = "food_cabbage",    correctCategory = FoodCategory.Approved,    displayColor = new Color(0.60f, 0.90f, 0.50f) },
        new FoodItemData { foodName = "Apples",     localizationKey = "food_apples",     correctCategory = FoodCategory.Approved,    displayColor = new Color(0.85f, 0.20f, 0.20f) },
        new FoodItemData { foodName = "Candy",      localizationKey = "food_candy",      correctCategory = FoodCategory.NotSuitable, displayColor = new Color(0.95f, 0.30f, 0.75f) },
        new FoodItemData { foodName = "Chocolate",  localizationKey = "food_chocolate",  correctCategory = FoodCategory.NotSuitable, displayColor = new Color(0.35f, 0.18f, 0.08f) },
        new FoodItemData { foodName = "Chips",      localizationKey = "food_chips",      correctCategory = FoodCategory.NotSuitable, displayColor = new Color(0.95f, 0.85f, 0.35f) },
        new FoodItemData { foodName = "Bread",      localizationKey = "food_bread",      correctCategory = FoodCategory.NotSuitable, displayColor = new Color(0.80f, 0.55f, 0.25f) },
    };

    [Header("Settings")]
    [SerializeField] private float moveDuration = 0.45f;
    [SerializeField] private float nextItemDelay = 0.35f;

    [Header("Reward")]
    [Tooltip("How many coins the player earns when they win.")]
    [SerializeField] private int coinReward = 10;
    [Tooltip("Name of the scene to load when the minigame finishes or is closed.")]
    [SerializeField] private string returnSceneName = "MainArea";

    private SortingState _state;
    private int _currentItemIndex;
    private GameObject _currentFoodObject;
    private FoodItemData _currentFoodData;

    private Canvas _uiCanvas;
    private Text _titleText;
    private Text _instructionText;
    private Text _foodNameText;
    private Text _feedbackText;
    private GameObject _congratsCanvas;

    void Start()
    {
        LanguageManager.Ensure();
        GameStateManager.Ensure();

        if (swipeInput == null) swipeInput = FindFirstObjectByType<HippoSwipeInput>();

        BuildUI();
        StartMinigame();

        LanguageManager.Instance.LanguageChanged += OnLanguageChanged;
    }

    void OnDestroy()
    {
        if (LanguageManager.Instance != null)
            LanguageManager.Instance.LanguageChanged -= OnLanguageChanged;
    }

    void OnLanguageChanged()
    {
        RefreshStaticTexts();
        if (_currentFoodData != null) SetFoodName(GetFoodDisplayName(_currentFoodData));
    }

    void Update()
    {
        if (_state != SortingState.WaitingForSwipe) return;
        if (swipeInput == null || _currentFoodObject == null || itemSpawnPoint == null) return;

        if (swipeInput.IsDragging)
        {
            Vector3 target = swipeInput.DragWorldPosition;
            target.y = itemSpawnPoint.position.y;
            _currentFoodObject.transform.position = Vector3.Lerp(_currentFoodObject.transform.position, target, Time.deltaTime * 18f);
        }

        if (swipeInput.ReleasedLeftThisFrame) SortCurrentItem(FoodCategory.Approved);
        if (swipeInput.ReleasedRightThisFrame) SortCurrentItem(FoodCategory.NotSuitable);
    }

    public void StartMinigame()
    {
        _state = SortingState.WaitingForSwipe;
        _currentItemIndex = 0;
        SetFeedback("");
        RefreshStaticTexts();
        SpawnCurrentItem();
    }

    void SpawnCurrentItem()
    {
        if (_currentItemIndex >= foodItems.Length)
        {
            CompleteMinigame();
            return;
        }

        _currentFoodData = foodItems[_currentItemIndex];

        if (_currentFoodObject != null) Destroy(_currentFoodObject);
        _currentFoodObject = Instantiate(foodItemPrefab, itemSpawnPoint.position, Quaternion.identity);

        var visual = _currentFoodObject.GetComponent<HippoFoodItemVisual>();
        if (visual != null) visual.SetColor(_currentFoodData.displayColor);

        SetFoodName(GetFoodDisplayName(_currentFoodData));
        _state = SortingState.WaitingForSwipe;
    }

    void SortCurrentItem(FoodCategory chosen)
    {
        if (_currentFoodObject == null) return;
        _state = SortingState.MovingItem;

        bool correct = chosen == _currentFoodData.correctCategory;
        Transform targetZone = (correct ? chosen : _currentFoodData.correctCategory) == FoodCategory.Approved ? approvedZone : notSuitableZone;

        if (correct)
            SetFeedback(SafeGet("minigame_hippo_correct", "Goed gedaan!"), new Color(0.4f, 1f, 0.5f));
        else
            SetFeedback(SafeGet("minigame_hippo_wrong", "Niet helemaal!"), new Color(1f, 0.55f, 0.4f));

        StartCoroutine(MoveItemToZone(_currentFoodObject.transform, targetZone.position, correct));
    }

    IEnumerator MoveItemToZone(Transform item, Vector3 targetPos, bool correct)
    {
        Vector3 start = item.position;
        float t = 0f;
        while (t < moveDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / moveDuration);
            item.position = Vector3.Lerp(start, targetPos, p);
            yield return null;
        }
        item.position = targetPos;

        if (correct) StartCoroutine(HippoReactHappy());
        else StartCoroutine(HippoReactSmallShake());

        yield return new WaitForSeconds(nextItemDelay);

        Destroy(_currentFoodObject);
        _currentFoodObject = null;
        _currentItemIndex++;
        SetFeedback("");
        SpawnCurrentItem();
    }

    void CompleteMinigame()
    {
        _state = SortingState.Complete;
        SetFoodName("");
        StartCoroutine(HippoReactHappy());
        DestroyMainUI();
        ShowCongrats();
    }

    string GetFoodDisplayName(FoodItemData data)
    {
        if (data == null) return "";
        if (string.IsNullOrEmpty(data.localizationKey)) return data.foodName;
        return SafeGet(data.localizationKey, data.foodName);
    }

    void RefreshStaticTexts()
    {
        if (_titleText != null) _titleText.text = SafeGet("minigame_hippo_title", "Nijlpaard Eten Sorteren");
        if (_instructionText != null) _instructionText.text = SafeGet("minigame_hippo_instruction", "Swipe links = Lekker  |  Swipe rechts = Niet lekker");
    }

    void BuildUI()
    {
        var cObj = new GameObject("HippoCanvas");
        _uiCanvas = cObj.AddComponent<Canvas>();
        _uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _uiCanvas.sortingOrder = 20;
        var scaler = cObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        cObj.AddComponent<GraphicRaycaster>();
        EnsureEventSystem();

        var header = new GameObject("Header");
        header.transform.SetParent(cObj.transform, false);
        var hrt = header.AddComponent<RectTransform>();
        hrt.anchorMin = new Vector2(0f, 1f); hrt.anchorMax = new Vector2(1f, 1f);
        hrt.pivot = new Vector2(0.5f, 1f); hrt.anchoredPosition = Vector2.zero;
        hrt.sizeDelta = new Vector2(0f, 230f);
        header.AddComponent<Image>().color = new Color(0.10f, 0.07f, 0.13f, 0.92f);

        MakeLabel(header.transform,
            SafeGet("minigame_hippo_title", "Nijlpaard Eten Sorteren"),
            new Vector2(0f, -16f), new Vector2(1000f, 70f), 50, FontStyle.Bold,
            new Color(0.85f, 0.7f, 1f), out _titleText);

        MakeLabel(header.transform,
            SafeGet("minigame_hippo_instruction", "Swipe links = Lekker  |  Swipe rechts = Niet lekker"),
            new Vector2(0f, -100f), new Vector2(1000f, 60f), 28, FontStyle.Normal,
            new Color(0.95f, 0.88f, 1f), out _instructionText);

        MakeLabel(header.transform, "",
            new Vector2(0f, -170f), new Vector2(1000f, 50f), 30, FontStyle.Bold,
            Color.white, out _feedbackText);

        var nameBg = new GameObject("FoodNameBg");
        nameBg.transform.SetParent(cObj.transform, false);
        var nbg = nameBg.AddComponent<RectTransform>();
        nbg.anchorMin = new Vector2(0.5f, 0f); nbg.anchorMax = new Vector2(0.5f, 0f);
        nbg.pivot = new Vector2(0.5f, 0f); nbg.anchoredPosition = new Vector2(0f, 200f);
        nbg.sizeDelta = new Vector2(680f, 120f);
        nameBg.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

        MakeLabel(nameBg.transform, "",
            Vector2.zero, new Vector2(660f, 110f), 56, FontStyle.Bold,
            Color.white, out _foodNameText);
        var nrt = _foodNameText.rectTransform;
        nrt.anchorMin = Vector2.zero; nrt.anchorMax = Vector2.one;
        nrt.offsetMin = nrt.offsetMax = Vector2.zero;
        nrt.pivot = new Vector2(0.5f, 0.5f);
        nrt.anchoredPosition = Vector2.zero;
        _foodNameText.alignment = TextAnchor.MiddleCenter;
        _foodNameText.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.7f);

        var stopBtn = MakeButton(cObj.transform, SafeGet("btn_back", "Stop"),
            new Vector2(30f, 30f), new Vector2(240f, 110f), new Color(0.55f, 0.18f, 0.18f));
        stopBtn.onClick.AddListener(ExitToMainArea);
    }

    void ShowCongrats()
    {
        var cObj = new GameObject("CongratsCanvas");
        _congratsCanvas = cObj;
        var cv = cObj.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 25;
        var scaler = cObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        cObj.AddComponent<GraphicRaycaster>();
        EnsureEventSystem();

        var bg = cObj.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.55f);
        bg.raycastTarget = false;

        var card = new GameObject("Card");
        card.transform.SetParent(cObj.transform, false);
        var crt = card.AddComponent<RectTransform>();
        crt.anchorMin = crt.anchorMax = crt.pivot = new Vector2(0.5f, 0.5f);
        crt.anchoredPosition = Vector2.zero;
        crt.sizeDelta = new Vector2(900f, 580f);
        crt.localScale = Vector3.zero;
        var cImg = card.AddComponent<Image>();
        cImg.color = new Color(0.12f, 0.08f, 0.16f, 0.97f);

        var accent = new GameObject("Accent");
        accent.transform.SetParent(card.transform, false);
        var aRt = accent.AddComponent<RectTransform>();
        aRt.anchorMin = new Vector2(0f, 1f); aRt.anchorMax = new Vector2(1f, 1f);
        aRt.pivot = new Vector2(0.5f, 1f); aRt.anchoredPosition = Vector2.zero; aRt.sizeDelta = new Vector2(0f, 14f);
        accent.AddComponent<Image>().color = new Color(0.85f, 0.55f, 1f);

        MakeLabel(card.transform, SafeGet("minigame_complete", "Gefeliciteerd!"),
            new Vector2(0f, -55f), new Vector2(840f, 80f), 56, FontStyle.Bold, Color.white, out _);

        MakeLabel(card.transform, SafeGet("minigame_hippo_success_title", "Goed gesorteerd!"),
            new Vector2(0f, -150f), new Vector2(840f, 60f), 36, FontStyle.Normal, new Color(0.85f, 0.7f, 1f), out _);

        MakeLabel(card.transform,
            SafeGet("minigame_coins_earned", $"Je hebt {coinReward} munten verdiend!"),
            new Vector2(0f, -240f), new Vector2(840f, 60f), 38, FontStyle.Normal, new Color(0.35f, 1f, 0.55f), out _);

        MakeLabel(card.transform,
            SafeGet("minigame_hippo_success_desc", "De nijlpaard eet nu lekker en gezond!"),
            new Vector2(0f, -310f), new Vector2(840f, 50f), 26, FontStyle.Normal, new Color(0.95f, 0.88f, 1f), out _);

        var continueBtn = MakeButton(card.transform, SafeGet("btn_continue", "Doorgaan"),
            new Vector2(0f, 32f), new Vector2(500f, 110f), new Color(0.18f, 0.62f, 0.32f));
        continueBtn.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0f);
        continueBtn.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0f);
        continueBtn.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0f);
        continueBtn.onClick.AddListener(OnContinue);

        StartCoroutine(PopInCard(crt));
    }

    IEnumerator PopInCard(RectTransform rt)
    {
        float t = 0f;
        while (t < 0.35f)
        {
            t += Time.deltaTime;
            if (rt == null) yield break;
            float p = t / 0.35f;
            float overshoot = 1f + Mathf.Sin(p * Mathf.PI) * 0.15f;
            rt.localScale = Vector3.one * Mathf.SmoothStep(0f, 1f, p) * overshoot;
            yield return null;
        }
        if (rt != null) rt.localScale = Vector3.one;
    }

    void OnContinue()
    {
        GameStateManager.Instance.AddCoins(coinReward);
        SceneManager.LoadScene(returnSceneName);
    }

    void ExitToMainArea() => SceneManager.LoadScene(returnSceneName);

    void DestroyMainUI()
    {
        if (_uiCanvas != null) Destroy(_uiCanvas.gameObject);
        _uiCanvas = null;
    }

    IEnumerator HippoReactHappy()
    {
        if (hippoVisual == null) yield break;
        Vector3 baseScale = hippoVisual.localScale;
        float t = 0f;
        while (t < 0.6f)
        {
            t += Time.deltaTime;
            float s = 1f + Mathf.Sin(t * 20f) * 0.07f;
            hippoVisual.localScale = baseScale * s;
            yield return null;
        }
        hippoVisual.localScale = baseScale;
    }

    IEnumerator HippoReactSmallShake()
    {
        if (hippoVisual == null) yield break;
        Vector3 basePos = hippoVisual.localPosition;
        float t = 0f;
        while (t < 0.35f)
        {
            t += Time.deltaTime;
            float x = Mathf.Sin(t * 35f) * 0.06f;
            hippoVisual.localPosition = basePos + new Vector3(x, 0f, 0f);
            yield return null;
        }
        hippoVisual.localPosition = basePos;
    }

    void SetFoodName(string text)
    {
        if (_foodNameText != null) _foodNameText.text = text;
    }

    void SetFeedback(string text, Color? color = null)
    {
        if (_feedbackText == null) return;
        _feedbackText.text = text;
        _feedbackText.color = color ?? Color.white;
    }

    void MakeLabel(Transform parent, string text, Vector2 pos, Vector2 size, int fontSize, FontStyle style, Color color, out Text refOut)
    {
        var obj = new GameObject("Label");
        obj.transform.SetParent(parent, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f); rt.anchoredPosition = pos; rt.sizeDelta = size;
        var t = obj.AddComponent<Text>();
        t.text = text; t.font = GetFont(); t.fontSize = fontSize; t.fontStyle = style;
        t.alignment = TextAnchor.MiddleCenter; t.color = color; t.raycastTarget = false;
        refOut = t;
    }

    Button MakeButton(Transform parent, string label, Vector2 pos, Vector2 size, Color color)
    {
        var obj = new GameObject($"Btn_{label}");
        obj.transform.SetParent(parent, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        var img = obj.AddComponent<Image>();
        img.color = color;
        var btn = obj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.colors = new ColorBlock
        {
            normalColor = color,
            highlightedColor = color * 1.2f,
            pressedColor = color * 0.7f,
            selectedColor = color,
            disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f),
            colorMultiplier = 1f,
            fadeDuration = 0.08f
        };
        var lObj = new GameObject("Label");
        lObj.transform.SetParent(obj.transform, false);
        var lrt = lObj.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        var t = lObj.AddComponent<Text>();
        t.text = label; t.font = GetFont(); t.fontSize = 42; t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter; t.color = Color.white; t.raycastTarget = false;
        return btn;
    }

    string SafeGet(string key, string fallback)
    {
        var lm = LanguageManager.Instance;
        if (lm == null) return fallback;
        var result = lm.Get(key);
        return result == $"[{key}]" ? fallback : result;
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