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
        [Tooltip("Optional. A 3D model prefab to spawn for this food (e.g. a watermelon model). If set, this is used instead of the generic prefab + color.")]
        public GameObject foodPrefab;
        [Tooltip("Optional per-item size. 0 = use the manager's global Target Item Size. Set a value here only if this one model needs to be bigger/smaller than the rest.")]
        public float sizeOverride = 0f;
        [Tooltip("Only used when no Food Prefab is assigned (the generic fallback sphere is tinted with this color).")]
        public Color displayColor = Color.white;
    }

    [Header("References")]
    [SerializeField] private HippoSwipeInput swipeInput;

    [Header("Scene Objects")]
    [SerializeField] private Transform itemSpawnPoint;
    [SerializeField] private Transform approvedZone;
    [SerializeField] private Transform notSuitableZone;
    [Tooltip("Fallback prefab used for any food item that has no Food Prefab assigned. Typically a simple sphere with a HippoFoodItemVisual.")]
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
    [Tooltip("Show a kid-friendly 'How to Play' explanation before the game starts.")]
    [SerializeField] private bool showHowToPlay = true;

    [Header("Back Button (PNG)")]
    [SerializeField] private Sprite backButtonSprite;
    [SerializeField] private Vector2 backButtonPos = new Vector2(30f, 30f);
    [SerializeField] private Vector2 backButtonSize = new Vector2(240f, 110f);

    [Header("Congrats Panel (PNG)")]
    [SerializeField] private Sprite congratsPanelSprite;
    [SerializeField] private Vector2 congratsPanelPos = new Vector2(0f, 0f);
    [SerializeField] private Vector2 congratsPanelSize = new Vector2(900f, 580f);
    [Range(0f, 1f)]
    [SerializeField] private float congratsPanelOpacity = 1f;

    [Header("Continue Button (PNG)")]
    [SerializeField] private Sprite continueButtonSprite;
    [SerializeField] private Vector2 continueButtonPos = new Vector2(0f, 32f);
    [SerializeField] private Vector2 continueButtonSize = new Vector2(500f, 110f);

    [Header("How To Play - Images")]
    [Tooltip("Instructional PNGs shown per line. The image swaps as lines advance. If fewer images than lines, the last image is reused.")]
    [SerializeField] private Sprite[] howToImages;
    [SerializeField] private Vector2 howToImagePos = new Vector2(0f, 180f);
    [SerializeField] private Vector2 howToImageSize = new Vector2(820f, 820f);

    [Header("How To Play - Text")]
    [SerializeField] private Vector2 howToTextPos = new Vector2(0f, -560f);
    [SerializeField] private Vector2 howToTextSize = new Vector2(900f, 240f);
    [SerializeField] private int howToTextFontSize = 34;
    [SerializeField] private Color howToTextColor = Color.white;
    [SerializeField] private TextAnchor howToTextAlignment = TextAnchor.UpperCenter;
    [SerializeField] private float howToTextPadLeft = 30f;
    [SerializeField] private float howToTextPadRight = 30f;
    [SerializeField] private float howToTextPadTop = 10f;
    [SerializeField] private float howToTextPadBottom = 10f;

    [Header("How To Play - Tap To Continue")]
    [SerializeField] private Vector2 howToTapPos = new Vector2(0f, -740f);
    [SerializeField] private Vector2 howToTapSize = new Vector2(440f, 50f);
    [SerializeField] private int howToTapFontSize = 26;
    [SerializeField] private Color howToTapColor = new Color(1f, 0.9f, 0.5f);

    [Header("How To Play - Lets Go Button (PNG)")]
    [SerializeField] private Sprite letsGoButtonSprite;
    [SerializeField] private Vector2 letsGoButtonPos = new Vector2(0f, -760f);
    [SerializeField] private Vector2 letsGoButtonSize = new Vector2(480f, 170f);

    [Header("How To Play - Background")]
    [Range(0f, 1f)]
    [SerializeField] private float howToDimOpacity = 0.78f;

    [Header("How To Play Voice")]
    [SerializeField] private LocalizedSoundData howToPlayLocalized;

    [Header("Item Size")]
    [Tooltip("Scale every spawned food so its largest dimension equals this many world units. Keeps each model's aspect ratio (uniform scale). Lower this if items look too big.")]
    [SerializeField] private bool normalizeItemSize = true;
    [SerializeField] private float targetItemSize = 1.5f;

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
    private Text _leftZoneLabel;
    private Text _rightZoneLabel;
    private GameObject _congratsCanvas;
    private GameObject _howToCanvas;
    private (string key, string fallback)[] _htLines;
    private int _htPage;
    private int _htLineCount;
    private Text _htText;
    private Image _htImage;
    private GameObject _htTapIndicator;
    private Button _htLetsGoBtn;

    void Start()
    {
        LanguageManager.Ensure();
        GameStateManager.Ensure();

        if (swipeInput == null) swipeInput = FindFirstObjectByType<HippoSwipeInput>();

        BuildUI();

        if (showHowToPlay) ShowHowToPlay();
        else StartMinigame();

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

        if (_currentFoodData.foodPrefab != null)
        {
            _currentFoodObject = Instantiate(
                _currentFoodData.foodPrefab,
                itemSpawnPoint.position,
                _currentFoodData.foodPrefab.transform.rotation);
        }
        else
        {
            _currentFoodObject = Instantiate(foodItemPrefab, itemSpawnPoint.position, Quaternion.identity);

            var visual = _currentFoodObject.GetComponent<HippoFoodItemVisual>();
            if (visual != null) visual.SetColor(_currentFoodData.displayColor);
        }

        if (normalizeItemSize)
        {
            float target = _currentFoodData.sizeOverride > 0f ? _currentFoodData.sizeOverride : targetItemSize;
            NormalizeItemSize(_currentFoodObject, target);
        }

        SetFoodName(GetFoodDisplayName(_currentFoodData));
        _state = SortingState.WaitingForSwipe;
    }

    void NormalizeItemSize(GameObject obj, float targetSize)
    {
        if (obj == null || targetSize <= 0f) return;

        var renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        // Measure unrotated so the bounding box matches the model, not its tilted AABB.
        Quaternion originalRot = obj.transform.rotation;
        obj.transform.rotation = Quaternion.identity;

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            b.Encapsulate(renderers[i].bounds);

        obj.transform.rotation = originalRot;

        float largest = Mathf.Max(b.size.x, b.size.y, b.size.z);
        if (largest <= 0.0001f) return;

        float factor = targetSize / largest;
        obj.transform.localScale *= factor;
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
        if (_instructionText != null) _instructionText.text = SafeGet("minigame_hippo_instruction", "Veeg naar links = Lekker  |  Veeg naar rechts = Niet lekker");
        if (_leftZoneLabel != null) _leftZoneLabel.text = "\u2190 " + SafeGet("minigame_hippo_zone_good", "Lekker!");
        if (_rightZoneLabel != null) _rightZoneLabel.text = SafeGet("minigame_hippo_zone_bad", "Niet lekker") + " \u2192";
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
            SafeGet("minigame_hippo_instruction", "Veeg naar links = Lekker  |  Veeg naar rechts = Niet lekker"),
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

        var leftZone = new GameObject("LeftZoneLabel");
        leftZone.transform.SetParent(cObj.transform, false);
        var lzRt = leftZone.AddComponent<RectTransform>();
        lzRt.anchorMin = new Vector2(0f, 0.5f); lzRt.anchorMax = new Vector2(0f, 0.5f);
        lzRt.pivot = new Vector2(0f, 0.5f);
        lzRt.anchoredPosition = new Vector2(30f, 120f);
        lzRt.sizeDelta = new Vector2(360f, 90f);
        var lzImg = leftZone.AddComponent<Image>();
        lzImg.color = new Color(0.16f, 0.55f, 0.24f, 0.85f);
        lzImg.raycastTarget = false;
        MakeLabel(leftZone.transform, "\u2190 " + SafeGet("minigame_hippo_zone_good", "Lekker!"),
            Vector2.zero, new Vector2(340f, 80f), 40, FontStyle.Bold, Color.white, out _leftZoneLabel);
        var lzlRt = _leftZoneLabel.rectTransform;
        lzlRt.anchorMin = Vector2.zero; lzlRt.anchorMax = Vector2.one;
        lzlRt.offsetMin = lzlRt.offsetMax = Vector2.zero; lzlRt.pivot = new Vector2(0.5f, 0.5f);
        _leftZoneLabel.alignment = TextAnchor.MiddleCenter;

        var rightZone = new GameObject("RightZoneLabel");
        rightZone.transform.SetParent(cObj.transform, false);
        var rzRt = rightZone.AddComponent<RectTransform>();
        rzRt.anchorMin = new Vector2(1f, 0.5f); rzRt.anchorMax = new Vector2(1f, 0.5f);
        rzRt.pivot = new Vector2(1f, 0.5f);
        rzRt.anchoredPosition = new Vector2(-30f, 120f);
        rzRt.sizeDelta = new Vector2(360f, 90f);
        var rzImg = rightZone.AddComponent<Image>();
        rzImg.color = new Color(0.62f, 0.20f, 0.20f, 0.85f);
        rzImg.raycastTarget = false;
        MakeLabel(rightZone.transform, SafeGet("minigame_hippo_zone_bad", "Niet lekker") + " \u2192",
            Vector2.zero, new Vector2(340f, 80f), 40, FontStyle.Bold, Color.white, out _rightZoneLabel);
        var rzlRt = _rightZoneLabel.rectTransform;
        rzlRt.anchorMin = Vector2.zero; rzlRt.anchorMax = Vector2.one;
        rzlRt.offsetMin = rzlRt.offsetMax = Vector2.zero; rzlRt.pivot = new Vector2(0.5f, 0.5f);
        _rightZoneLabel.alignment = TextAnchor.MiddleCenter;

        var stopBtn = MakeSpriteButton(cObj.transform, backButtonSprite, null, backButtonPos, backButtonSize);
        stopBtn.onClick.AddListener(ExitToMainArea);
    }

    void ShowHowToPlay()
    {
        var cObj = new GameObject("HowToCanvas");
        _howToCanvas = cObj;
        var cv = cObj.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 24;
        var scaler = cObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        cObj.AddComponent<GraphicRaycaster>();
        EnsureEventSystem();

        var bg = cObj.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, howToDimOpacity);
        var dimBtn = cObj.AddComponent<Button>();
        dimBtn.transition = Selectable.Transition.None;
        dimBtn.targetGraphic = bg;
        dimBtn.onClick.AddListener(AdvanceHowTo);

        var imgObj = new GameObject("HowToImage");
        imgObj.transform.SetParent(cObj.transform, false);
        var iRt = imgObj.AddComponent<RectTransform>();
        iRt.anchorMin = iRt.anchorMax = iRt.pivot = new Vector2(0.5f, 0.5f);
        iRt.anchoredPosition = howToImagePos; iRt.sizeDelta = howToImageSize;
        _htImage = imgObj.AddComponent<Image>();
        _htImage.raycastTarget = false;
        _htImage.preserveAspect = true;

        var txtObj = new GameObject("HowToTextBox");
        txtObj.transform.SetParent(cObj.transform, false);
        var tRt = txtObj.AddComponent<RectTransform>();
        tRt.anchorMin = tRt.anchorMax = tRt.pivot = new Vector2(0.5f, 0.5f);
        tRt.anchoredPosition = howToTextPos; tRt.sizeDelta = howToTextSize;

        var txtInner = new GameObject("Text");
        txtInner.transform.SetParent(txtObj.transform, false);
        var tiRt = txtInner.AddComponent<RectTransform>();
        tiRt.anchorMin = Vector2.zero; tiRt.anchorMax = Vector2.one;
        tiRt.offsetMin = new Vector2(howToTextPadLeft, howToTextPadBottom);
        tiRt.offsetMax = new Vector2(-howToTextPadRight, -howToTextPadTop);
        _htText = txtInner.AddComponent<Text>();
        _htText.font = GetFont();
        _htText.fontSize = howToTextFontSize;
        _htText.fontStyle = FontStyle.Bold;
        _htText.alignment = howToTextAlignment;
        _htText.color = howToTextColor;
        _htText.raycastTarget = false;
        _htText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _htText.verticalOverflow = VerticalWrapMode.Overflow;

        var tapObj = new GameObject("TapToContinue");
        tapObj.transform.SetParent(cObj.transform, false);
        var tapRt = tapObj.AddComponent<RectTransform>();
        tapRt.anchorMin = tapRt.anchorMax = tapRt.pivot = new Vector2(0.5f, 0.5f);
        tapRt.anchoredPosition = howToTapPos; tapRt.sizeDelta = howToTapSize;
        var tapTxt = tapObj.AddComponent<Text>();
        tapTxt.font = GetFont(); tapTxt.fontSize = howToTapFontSize; tapTxt.fontStyle = FontStyle.Bold;
        tapTxt.alignment = TextAnchor.MiddleCenter;
        tapTxt.color = howToTapColor;
        tapTxt.raycastTarget = false;
        tapTxt.text = SafeGet("intro_tap_continue", "Tik om verder \u25B6");
        _htTapIndicator = tapObj;

        var lgObj = new GameObject("LetsGoButton");
        lgObj.transform.SetParent(cObj.transform, false);
        var lgRt = lgObj.AddComponent<RectTransform>();
        lgRt.anchorMin = lgRt.anchorMax = lgRt.pivot = new Vector2(0.5f, 0.5f);
        lgRt.anchoredPosition = letsGoButtonPos; lgRt.sizeDelta = letsGoButtonSize;
        var lgImg = lgObj.AddComponent<Image>();
        lgImg.sprite = letsGoButtonSprite;
        lgImg.color = Color.white;
        lgImg.preserveAspect = true;
        _htLetsGoBtn = lgObj.AddComponent<Button>();
        _htLetsGoBtn.targetGraphic = lgImg;
        _htLetsGoBtn.onClick.AddListener(() =>
        {
            if (_howToCanvas != null) Destroy(_howToCanvas);
            _howToCanvas = null;
            StartMinigame();
        });
        lgObj.SetActive(false);

        _htLines = new (string, string)[]
        {
            ("minigame_hippo_howto_intro", "Het nijlpaard heeft honger! Sommig eten is gezond, en sommig eten is niet goed voor nijlpaarden."),
            ("minigame_hippo_howto_left", "Veeg naar LINKS voor eten dat goed is voor het nijlpaard."),
            ("minigame_hippo_howto_right", "Veeg naar RECHTS voor eten dat NIET goed is."),
        };
        _htLineCount = _htLines.Length;
        _htPage = 0;

        ShowHowToPage(0);
    }

    void AdvanceHowTo()
    {
        if (_htLines == null) return;
        if (_htPage >= _htLineCount - 1) return;
        ShowHowToPage(_htPage + 1);
    }

    void ShowHowToPage(int index)
    {
        if (_htLines == null || _htLineCount == 0) return;
        _htPage = Mathf.Clamp(index, 0, _htLineCount - 1);

        if (_htText != null)
            _htText.text = SafeGet(_htLines[_htPage].key, _htLines[_htPage].fallback);

        if (_htImage != null)
        {
            Sprite sp = (howToImages != null && howToImages.Length > 0)
                ? howToImages[Mathf.Min(_htPage, howToImages.Length - 1)]
                : null;
            _htImage.sprite = sp;
            _htImage.enabled = sp != null;
        }

        MinigameVoicePlayer.PlayLocalizedForPage(howToPlayLocalized, _htPage, true);

        bool last = _htPage >= _htLineCount - 1;
        if (_htTapIndicator != null) _htTapIndicator.SetActive(!last);
        if (_htLetsGoBtn != null) _htLetsGoBtn.gameObject.SetActive(last);
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
        crt.anchoredPosition = congratsPanelPos;
        crt.sizeDelta = congratsPanelSize;
        crt.localScale = Vector3.zero;
        var cImg = card.AddComponent<Image>();
        if (congratsPanelSprite != null)
        {
            cImg.sprite = congratsPanelSprite;
            cImg.type = Image.Type.Simple;
            cImg.preserveAspect = false;
            cImg.color = new Color(1f, 1f, 1f, congratsPanelOpacity);
        }
        else cImg.color = new Color(0.14f, 0.11f, 0.07f, 0.97f);

        var accent = new GameObject("Accent");
        accent.transform.SetParent(card.transform, false);
        var aRt = accent.AddComponent<RectTransform>();
        aRt.anchorMin = new Vector2(0f, 1f); aRt.anchorMax = new Vector2(1f, 1f);
        aRt.pivot = new Vector2(0.5f, 1f); aRt.anchoredPosition = Vector2.zero; aRt.sizeDelta = new Vector2(0f, 14f);
        accent.AddComponent<Image>().color = new Color(0.85f, 0.55f, 1f);

        MakeLabel(card.transform, SafeGet("minigame_complete", "Gefeliciteerd!"),
            new Vector2(0f, -55f), new Vector2(840f, 80f), 56, FontStyle.Bold, Color.white, out _);

        MakeLabel(card.transform, SafeGet("minigame_hippo_success_title", "Goed gesorteerd!"),
            new Vector2(0f, -150f), new Vector2(840f, 60f), 36, FontStyle.Normal, Color.white, out _);

        MakeLabel(card.transform,
            SafeGet("minigame_coins_earned", $"Je hebt {coinReward} munten verdiend!"),
            new Vector2(0f, -240f), new Vector2(840f, 60f), 38, FontStyle.Normal, Color.white, out _);

        MakeLabel(card.transform,
            SafeGet("minigame_hippo_success_desc", "De nijlpaard eet nu lekker en gezond!"),
            new Vector2(0f, -310f), new Vector2(840f, 50f), 26, FontStyle.Normal, Color.white, out _);

        var continueBtn = MakeSpriteButton(card.transform, continueButtonSprite, SafeGet("btn_continue", "Doorgaan"), continueButtonPos, continueButtonSize);
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

    Button MakeSpriteButton(Transform parent, Sprite sprite, string label, Vector2 pos, Vector2 size)
    {
        var obj = new GameObject("SpriteBtn");
        obj.transform.SetParent(parent, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f); rt.anchorMax = new Vector2(0f, 0f); rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;

        var img = obj.AddComponent<Image>();
        Color baseCol;
        if (sprite != null)
        {
            img.sprite = sprite;
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            img.color = Color.white;
            baseCol = Color.white;
        }
        else
        {
            baseCol = new Color(0.3f, 0.3f, 0.3f, 0.95f);
            img.color = baseCol;
        }

        var btn = obj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.colors = new ColorBlock
        {
            normalColor = baseCol,
            highlightedColor = baseCol * 1.12f,
            pressedColor = baseCol * 0.8f,
            selectedColor = baseCol,
            disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f),
            colorMultiplier = 1f,
            fadeDuration = 0.08f
        };

        if (!string.IsNullOrEmpty(label))
        {
            var lObj = new GameObject("Label");
            lObj.transform.SetParent(obj.transform, false);
            var lrt = lObj.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            var t = lObj.AddComponent<Text>();
            t.text = label; t.font = GetFont(); t.fontSize = 42; t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter; t.color = Color.white; t.raycastTarget = false;
        }

        return btn;
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