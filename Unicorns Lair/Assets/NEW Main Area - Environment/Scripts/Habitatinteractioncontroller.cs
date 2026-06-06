using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HabitatInteractionController : MonoBehaviour
{
    public static event System.Action<Button, Button, Button, InspectableHabitat> CardShown;
    public static event System.Action<Button> InspectBackButtonShown;
    public static event System.Action<InspectableHabitat> MinigamePressed;
    public static event System.Action CardClosed;

    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private HabitatInspectionManager inspectionManager;

    [Header("Button Icons (SVG sprites)")]
    [Tooltip("Assign the imported BACK.svg sprite. Used for the card's back button and the inspect back button.")]
    [SerializeField] private Sprite backSprite;
    [Tooltip("Assign the imported INSPECT.svg sprite.")]
    [SerializeField] private Sprite inspectSprite;
    [Tooltip("Assign the imported PLAY.svg sprite. Used for the minigame button.")]
    [SerializeField] private Sprite playSprite;

    [Header("Button Layout")]
    [Tooltip("Height of each icon button in reference pixels. Width is derived from each sprite so aspect ratio is kept. Increase to make the buttons bigger.")]
    [SerializeField] private float buttonHeight = 150f;
    [Tooltip("Gap between buttons in the row. Decrease to bring them closer together.")]
    [SerializeField] private float buttonSpacing = 20f;
    [Tooltip("Bottom-left position of the button row (and of the inspect back button).")]
    [SerializeField] private Vector2 buttonRowOrigin = new Vector2(30f, 30f);

    [Header("Name Panel (PNG, top middle)")]
    [Tooltip("PNG used as the top-middle name panel. If empty, the old dark box is used.")]
    [SerializeField] private Sprite namePanelSprite;
    [Tooltip("Position from the TOP-CENTER of the screen. X = right, Y = down (negative).")]
    [SerializeField] private Vector2 namePanelPos = new Vector2(0f, -30f);
    [SerializeField] private Vector2 namePanelSize = new Vector2(960f, 160f);
    [Range(0f, 1f)]
    [SerializeField] private float namePanelOpacity = 1f;
    [SerializeField] private int nameTextSize = 52;
    [SerializeField] private Color nameTextColor = Color.white;
    [SerializeField] private TextAnchor nameTextAlignment = TextAnchor.MiddleCenter;
    [SerializeField] private float nameTextPadLeft = 40f;
    [SerializeField] private float nameTextPadRight = 40f;
    [SerializeField] private float nameTextPadTop = 20f;
    [SerializeField] private float nameTextPadBottom = 20f;

    [Header("Info Button (PNG, between inspect and play)")]
    [Tooltip("Icon for the new info button placed between the inspect and minigame buttons. Opens the info popup.")]
    [SerializeField] private Sprite infoSprite;

    [Header("Info Popup (PNG)")]
    [Tooltip("PNG background of the popup that shows the habitat description and educational fact.")]
    [SerializeField] private Sprite infoPanelSprite;
    [SerializeField] private Vector2 infoPanelPos = new Vector2(0f, 0f);
    [SerializeField] private Vector2 infoPanelSize = new Vector2(900f, 820f);
    [Range(0f, 1f)]
    [SerializeField] private float infoPanelOpacity = 1f;
    [Tooltip("Darkening behind the popup. Tap anywhere to close.")]
    [Range(0f, 1f)]
    [SerializeField] private float infoDimOpacity = 0.6f;

    [Header("Info Popup - Description Text")]
    [SerializeField] private Vector2 infoDescPos = new Vector2(0f, 150f);
    [SerializeField] private Vector2 infoDescSize = new Vector2(760f, 320f);
    [SerializeField] private int infoDescFontSize = 34;
    [SerializeField] private Color infoDescColor = Color.white;
    [SerializeField] private TextAnchor infoDescAlignment = TextAnchor.UpperCenter;

    [Header("Info Popup - Fact Text")]
    [SerializeField] private Vector2 infoFactPos = new Vector2(0f, -230f);
    [SerializeField] private Vector2 infoFactSize = new Vector2(760f, 260f);
    [SerializeField] private int infoFactFontSize = 30;
    [SerializeField] private Color infoFactColor = Color.white;
    [SerializeField] private TextAnchor infoFactAlignment = TextAnchor.UpperCenter;

    [Header("Info Popup - Tap To Continue")]
    [Tooltip("Anchor/pivot inside the panel (0.5,0 = bottom-center).")]
    [SerializeField] private Vector2 infoTapAnchor = new Vector2(0.5f, 0f);
    [SerializeField] private Vector2 infoTapPosition = new Vector2(0f, 24f);
    [SerializeField] private Vector2 infoTapSize = new Vector2(420f, 48f);
    [SerializeField] private int infoTapFontSize = 26;
    [SerializeField] private Color infoTapColor = new Color(1f, 0.9f, 0.5f);
    [SerializeField] private TextAnchor infoTapAlignment = TextAnchor.MiddleCenter;

    [Header("Tuning")]
    [SerializeField] private float cameraMoveDuration = 1.1f;
    [SerializeField] private float cameraReturnDuration = 1.0f;

    public bool IsBusy => _state != State.Idle;

    private enum State { Idle, MovingIn, ShowingCard, Inspecting, MovingOut }
    private State _state = State.Idle;

    private CameraController _cameraController;
    private Vector3 _originalCamPos;
    private Quaternion _originalCamRot;

    private InspectableHabitat _currentHabitat;
    private GameObject _cardCanvas;
    private GameObject _infoPopup;
    private RectTransform _infoPanelRt;
    private bool _infoClosing;

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera != null) _cameraController = mainCamera.GetComponent<CameraController>();
        if (inspectionManager == null) inspectionManager = FindFirstObjectByType<HabitatInspectionManager>();

        LanguageManager.Ensure();
    }

    public void OpenHabitat(InspectableHabitat habitat)
    {
        if (_state != State.Idle || habitat == null) return;
        StartCoroutine(OpenHabitatRoutine(habitat));
    }

    public void CloseHabitat()
    {
        if (_state != State.ShowingCard) return;
        StartCoroutine(CloseHabitatRoutine());
    }

    IEnumerator OpenHabitatRoutine(InspectableHabitat habitat)
    {
        _state = State.MovingIn;
        _currentHabitat = habitat;

        _originalCamPos = mainCamera.transform.position;
        _originalCamRot = mainCamera.transform.rotation;

        if (_cameraController != null) _cameraController.enabled = false;

        Vector3 targetPos = habitat.HasCamView ? habitat.CamViewPosition : _originalCamPos;
        Quaternion targetRot = habitat.HasCamView ? habitat.CamViewRotation : _originalCamRot;

        Vector3 startPos = mainCamera.transform.position;
        Quaternion startRot = mainCamera.transform.rotation;
        float t = 0f;
        while (t < cameraMoveDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / cameraMoveDuration));
            mainCamera.transform.position = Vector3.Lerp(startPos, targetPos, p);
            mainCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, p);
            yield return null;
        }
        mainCamera.transform.position = targetPos;
        mainCamera.transform.rotation = targetRot;

        _state = State.ShowingCard;
        ShowCard(habitat);
    }

    IEnumerator CloseHabitatRoutine()
    {
        _state = State.MovingOut;
        DestroyCard();

        Vector3 startPos = mainCamera.transform.position;
        Quaternion startRot = mainCamera.transform.rotation;
        float t = 0f;
        while (t < cameraReturnDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / cameraReturnDuration));
            mainCamera.transform.position = Vector3.Lerp(startPos, _originalCamPos, p);
            mainCamera.transform.rotation = Quaternion.Slerp(startRot, _originalCamRot, p);
            yield return null;
        }
        mainCamera.transform.position = _originalCamPos;
        mainCamera.transform.rotation = _originalCamRot;

        if (_cameraController != null) _cameraController.enabled = true;
        _currentHabitat = null;
        _state = State.Idle;
    }

    IEnumerator ReturnToCardFromInspect()
    {
        if (inspectionManager != null) inspectionManager.StopInspection();

        Vector3 targetPos = _currentHabitat.HasCamView ? _currentHabitat.CamViewPosition : _originalCamPos;
        Quaternion targetRot = _currentHabitat.HasCamView ? _currentHabitat.CamViewRotation : _originalCamRot;

        Vector3 startPos = mainCamera.transform.position;
        Quaternion startRot = mainCamera.transform.rotation;
        float t = 0f, dur = 0.7f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / dur));
            mainCamera.transform.position = Vector3.Lerp(startPos, targetPos, p);
            mainCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, p);
            yield return null;
        }
        mainCamera.transform.position = targetPos;
        mainCamera.transform.rotation = targetRot;

        _state = State.ShowingCard;
        ShowCard(_currentHabitat);
    }

    void ShowCard(InspectableHabitat habitat)
    {
        DestroyCard();

        var canvasObj = new GameObject("HabitatCardCanvas");
        _cardCanvas = canvasObj;
        var cv = canvasObj.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 10;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();
        EnsureEventSystem();

        var card = new GameObject("NamePanel");
        card.transform.SetParent(canvasObj.transform, false);
        var cardRt = card.AddComponent<RectTransform>();
        cardRt.anchorMin = new Vector2(0.5f, 1f); cardRt.anchorMax = new Vector2(0.5f, 1f);
        cardRt.pivot = new Vector2(0.5f, 1f);
        cardRt.anchoredPosition = namePanelPos;
        cardRt.sizeDelta = namePanelSize;
        var cardImg = card.AddComponent<Image>();
        if (namePanelSprite != null)
        {
            cardImg.sprite = namePanelSprite;
            cardImg.type = Image.Type.Simple;
            cardImg.preserveAspect = false;
            cardImg.color = new Color(1f, 1f, 1f, namePanelOpacity);
        }
        else cardImg.color = new Color(0.08f, 0.12f, 0.20f, 0.92f);
        cardImg.raycastTarget = false;

        var nameObj = new GameObject("NameText");
        nameObj.transform.SetParent(card.transform, false);
        var nrt = nameObj.AddComponent<RectTransform>();
        nrt.anchorMin = Vector2.zero; nrt.anchorMax = Vector2.one;
        nrt.offsetMin = new Vector2(nameTextPadLeft, nameTextPadBottom);
        nrt.offsetMax = new Vector2(-nameTextPadRight, -nameTextPadTop);
        var nameTxt = nameObj.AddComponent<Text>();
        nameTxt.font = GetFont(); nameTxt.fontSize = nameTextSize; nameTxt.fontStyle = FontStyle.Bold;
        nameTxt.alignment = nameTextAlignment; nameTxt.color = nameTextColor; nameTxt.raycastTarget = false;
        nameTxt.horizontalOverflow = HorizontalWrapMode.Wrap; nameTxt.verticalOverflow = VerticalWrapMode.Overflow;
        nameTxt.text = SafeGet(LanguageManager.Instance, habitat.AnimalNameKey, "Verblijf");

        LanguageManager.OnLanguageChanged refresh = () =>
        {
            var lm = LanguageManager.Instance;
            if (nameTxt != null) nameTxt.text = SafeGet(lm, habitat.AnimalNameKey, "Verblijf");
        };
        if (LanguageManager.Instance != null) LanguageManager.Instance.LanguageChanged += refresh;
        var deathHook = canvasObj.AddComponent<OnDestroyHook>();
        deathHook.OnDestroyAction = () =>
        {
            if (LanguageManager.Instance != null) LanguageManager.Instance.LanguageChanged -= refresh;
        };

        float rowX = buttonRowOrigin.x;
        float rowY = buttonRowOrigin.y;

        var backBtn = MakeIconButton(canvasObj.transform, backSprite, new Vector2(rowX, rowY), buttonHeight, out float wBack);
        rowX += wBack + buttonSpacing;

        var inspBtn = MakeIconButton(canvasObj.transform, inspectSprite, new Vector2(rowX, rowY), buttonHeight, out float wInsp);
        rowX += wInsp + buttonSpacing;

        var infoBtn = MakeIconButton(canvasObj.transform, infoSprite, new Vector2(rowX, rowY), buttonHeight, out float wInfo);
        rowX += wInfo + buttonSpacing;

        Button mgBtn = null;
        if (habitat.HasMinigame)
        {
            mgBtn = MakeIconButton(canvasObj.transform, playSprite, new Vector2(rowX, rowY), buttonHeight, out float wMg);
            rowX += wMg + buttonSpacing;
        }

        backBtn.onClick.AddListener(CloseHabitat);

        infoBtn.onClick.AddListener(() => ShowInfoPopup(habitat));

        inspBtn.onClick.AddListener(() =>
        {
            if (inspectionManager == null)
            {
                Debug.LogWarning("[HabitatInteractionController] No HabitatInspectionManager assigned.");
                return;
            }
            inspectionManager.StartInspection(habitat);
            DestroyCard();
            _state = State.Inspecting;

            ShowInspectBackButton();
        });

        if (mgBtn != null)
        {
            mgBtn.onClick.AddListener(() =>
            {
                MinigamePressed?.Invoke(habitat);
                SceneManager.LoadScene(habitat.MinigameScene);
            });
        }

        CardShown?.Invoke(backBtn, inspBtn, mgBtn, habitat);
    }

    void ShowInspectBackButton()
    {
        var canvasObj = new GameObject("InspectBackCanvas");
        _cardCanvas = canvasObj;
        var cv = canvasObj.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 10;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();
        EnsureEventSystem();

        var btn = MakeIconButton(canvasObj.transform, backSprite, buttonRowOrigin, buttonHeight, out _);
        btn.interactable = true;
        btn.onClick.AddListener(() =>
        {
            btn.interactable = false;
            DestroyCard();
            StartCoroutine(ReturnToCardFromInspect());
        });

        InspectBackButtonShown?.Invoke(btn);
    }

    void DestroyCard()
    {
        _infoPopup = null;
        _infoClosing = false;
        if (_cardCanvas != null) { Destroy(_cardCanvas); _cardCanvas = null; CardClosed?.Invoke(); }
    }

    Text MakeLabelSafe(Transform parent, string key, string fallback, int size, FontStyle style, Color color, Vector2 pos, Vector2 sz)
    {
        var obj = new GameObject("Label");
        obj.transform.SetParent(parent, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f); rt.anchoredPosition = pos; rt.sizeDelta = sz;
        var t = obj.AddComponent<Text>();
        t.text = SafeGet(LanguageManager.Instance, key, fallback);
        t.font = GetFont(); t.fontSize = size; t.fontStyle = style;
        t.alignment = TextAnchor.MiddleCenter; t.color = color; t.raycastTarget = false;
        return t;
    }

    void ShowInfoPopup(InspectableHabitat habitat)
    {
        if (_cardCanvas == null) return;
        if (_infoPopup != null) Destroy(_infoPopup);
        _infoClosing = false;

        var root = new GameObject("InfoPopup");
        root.transform.SetParent(_cardCanvas.transform, false);
        var rrt = root.AddComponent<RectTransform>();
        rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
        rrt.offsetMin = rrt.offsetMax = Vector2.zero;
        _infoPopup = root;

        // Dim background; tap anywhere to close.
        var dim = root.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, infoDimOpacity);
        var dimBtn = root.AddComponent<Button>();
        dimBtn.transition = Selectable.Transition.None;
        dimBtn.targetGraphic = dim;
        dimBtn.onClick.AddListener(BeginCloseInfoPopup);

        // Panel
        var panel = new GameObject("Panel");
        panel.transform.SetParent(root.transform, false);
        var prt = panel.AddComponent<RectTransform>();
        prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(0.5f, 0.5f);
        prt.anchoredPosition = infoPanelPos; prt.sizeDelta = infoPanelSize;
        prt.localScale = Vector3.zero;
        _infoPanelRt = prt;
        var pImg = panel.AddComponent<Image>();
        if (infoPanelSprite != null)
        {
            pImg.sprite = infoPanelSprite;
            pImg.type = Image.Type.Simple;
            pImg.preserveAspect = false;
            pImg.color = new Color(1f, 1f, 1f, infoPanelOpacity);
        }
        else pImg.color = new Color(0.08f, 0.12f, 0.20f, 0.97f);
        pImg.raycastTarget = false;

        var descTxt = MakePopupText(panel.transform, infoDescPos, infoDescSize, infoDescFontSize, infoDescColor, infoDescAlignment);
        var factTxt = MakePopupText(panel.transform, infoFactPos, infoFactSize, infoFactFontSize, infoFactColor, infoFactAlignment);

        // Tap-to-continue indicator (same as the dialogue / how-to popups)
        var tapObj = new GameObject("TapToContinue");
        tapObj.transform.SetParent(panel.transform, false);
        var tapRt = tapObj.AddComponent<RectTransform>();
        tapRt.anchorMin = tapRt.anchorMax = tapRt.pivot = infoTapAnchor;
        tapRt.anchoredPosition = infoTapPosition;
        tapRt.sizeDelta = infoTapSize;
        var tapTxt = tapObj.AddComponent<Text>();
        tapTxt.font = GetFont(); tapTxt.fontSize = infoTapFontSize; tapTxt.fontStyle = FontStyle.Bold;
        tapTxt.alignment = infoTapAlignment; tapTxt.color = infoTapColor; tapTxt.raycastTarget = false;

        LanguageManager.OnLanguageChanged refresh = () =>
        {
            var lm = LanguageManager.Instance;
            if (descTxt != null) descTxt.text = SafeGet(lm, habitat.HabitatDescriptionKey, "Een verblijf voor dieren.");
            if (factTxt != null) factTxt.text = SafeGet(lm, habitat.EducationalFactKey, "Leuk weetje!");
            if (tapTxt != null) tapTxt.text = SafeGet(lm, "intro_tap_continue", "Tik om verder \u25B6");
        };
        refresh();
        if (LanguageManager.Instance != null) LanguageManager.Instance.LanguageChanged += refresh;
        var hook = root.AddComponent<OnDestroyHook>();
        hook.OnDestroyAction = () =>
        {
            if (LanguageManager.Instance != null) LanguageManager.Instance.LanguageChanged -= refresh;
        };

        StartCoroutine(InfoPopIn(prt));
    }

    void BeginCloseInfoPopup()
    {
        if (_infoClosing || _infoPopup == null) return;
        _infoClosing = true;
        StartCoroutine(InfoPopOut(_infoPopup, _infoPanelRt));
    }

    IEnumerator InfoPopIn(RectTransform rt)
    {
        float t = 0f;
        while (t < 0.3f && rt != null)
        {
            t += Time.deltaTime;
            float p = t / 0.3f;
            float overshoot = 1f + Mathf.Sin(p * Mathf.PI) * 0.15f;
            rt.localScale = Vector3.one * Mathf.SmoothStep(0f, 1f, p) * overshoot;
            yield return null;
        }
        if (rt != null) rt.localScale = Vector3.one;
    }

    IEnumerator InfoPopOut(GameObject root, RectTransform rt)
    {
        Vector3 start = rt != null ? rt.localScale : Vector3.one;
        float t = 0f;
        while (t < 0.15f && rt != null)
        {
            t += Time.deltaTime;
            rt.localScale = Vector3.Lerp(start, Vector3.zero, t / 0.15f);
            yield return null;
        }
        if (root != null) Destroy(root);
        if (_infoPopup == root) _infoPopup = null;
        _infoClosing = false;
    }

    void CloseInfoPopup()
    {
        if (_infoPopup != null) { Destroy(_infoPopup); _infoPopup = null; }
        _infoClosing = false;
    }

    Text MakePopupText(Transform parent, Vector2 pos, Vector2 size, int fontSize, Color color, TextAnchor align)
    {
        var obj = new GameObject("Text");
        obj.transform.SetParent(parent, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        var t = obj.AddComponent<Text>();
        t.font = GetFont(); t.fontSize = fontSize; t.fontStyle = FontStyle.Normal;
        t.alignment = align; t.color = color; t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    Button MakeIconButton(Transform parent, Sprite sprite, Vector2 pos, float height, out float width)
    {
        float aspect = (sprite != null && sprite.rect.height > 0.01f)
            ? sprite.rect.width / sprite.rect.height
            : 1f;
        width = height * aspect;

        var obj = new GameObject("IconBtn");
        obj.transform.SetParent(parent, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(width, height);

        var img = obj.AddComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Simple;
        img.preserveAspect = true;
        img.color = Color.white;

        var btn = obj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.colors = new ColorBlock
        {
            normalColor = Color.white,
            highlightedColor = new Color(0.88f, 0.88f, 0.88f),
            pressedColor = new Color(0.72f, 0.72f, 0.72f),
            selectedColor = Color.white,
            disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f),
            colorMultiplier = 1f,
            fadeDuration = 0.08f
        };

        return btn;
    }

    Button MakeButton(Transform parent, string label, Vector2 pos, Vector2 size, Color color)
    {
        var obj = new GameObject("Btn");
        obj.transform.SetParent(parent, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
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
            pressedColor = color * 0.75f,
            selectedColor = color,
            disabledColor = new Color(0.3f, 0.3f, 0.3f),
            colorMultiplier = 1f,
            fadeDuration = 0.08f
        };

        var lblObj = new GameObject("Label");
        lblObj.transform.SetParent(obj.transform, false);
        var lrt = lblObj.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        var txt = lblObj.AddComponent<Text>();
        txt.text = label; txt.font = GetFont(); txt.fontSize = 42; txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter; txt.color = Color.white; txt.raycastTarget = false;

        return btn;
    }

    static string SafeGet(LanguageManager lm, string key, string fallback)
    {
        if (lm == null || string.IsNullOrEmpty(key)) return fallback;
        var result = lm.Get(key);
        return (result == $"[{key}]") ? fallback : result;
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

public class OnDestroyHook : MonoBehaviour
{
    public System.Action OnDestroyAction;
    void OnDestroy() => OnDestroyAction?.Invoke();
}