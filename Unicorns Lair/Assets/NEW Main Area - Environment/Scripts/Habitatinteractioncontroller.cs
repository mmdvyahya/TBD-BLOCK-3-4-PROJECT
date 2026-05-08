using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HabitatInteractionController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private HabitatInspectionManager inspectionManager;

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

        var card = new GameObject("InfoCard");
        card.transform.SetParent(canvasObj.transform, false);
        var cardRt = card.AddComponent<RectTransform>();
        cardRt.anchorMin = new Vector2(0.5f, 1f); cardRt.anchorMax = new Vector2(0.5f, 1f);
        cardRt.pivot = new Vector2(0.5f, 1f);
        cardRt.anchoredPosition = new Vector2(0f, -30f);
        cardRt.sizeDelta = new Vector2(960f, 280f);
        var cardImg = card.AddComponent<Image>();
        cardImg.color = new Color(0.08f, 0.12f, 0.20f, 0.92f);
        cardImg.raycastTarget = false;

        var accent = new GameObject("Accent");
        accent.transform.SetParent(card.transform, false);
        var aRt = accent.AddComponent<RectTransform>();
        aRt.anchorMin = new Vector2(0f, 1f); aRt.anchorMax = new Vector2(1f, 1f);
        aRt.pivot = new Vector2(0.5f, 1f); aRt.anchoredPosition = Vector2.zero; aRt.sizeDelta = new Vector2(0f, 10f);
        accent.AddComponent<Image>().color = new Color(0.55f, 0.35f, 0.10f);

        var nameTxt = MakeLabelSafe(card.transform, habitat.AnimalNameKey, "Verblijf", 52, FontStyle.Bold, Color.white, new Vector2(0f, -24f), new Vector2(920f, 70f));
        var descTxt = MakeLabelSafe(card.transform, habitat.HabitatDescriptionKey, "Een verblijf voor dieren.", 32, FontStyle.Normal, new Color(0.80f, 0.92f, 1f), new Vector2(0f, -102f), new Vector2(920f, 56f));
        var factTxt = MakeLabelSafe(card.transform, habitat.EducationalFactKey, "Leuk weetje!", 26, FontStyle.Normal, new Color(1f, 0.88f, 0.55f), new Vector2(0f, -160f), new Vector2(920f, 48f));
        var hintTxt = MakeLabelSafe(card.transform, "inspect_hint", "Tilt to look around", 22, FontStyle.Normal, new Color(0.55f, 0.65f, 0.75f), new Vector2(0f, -220f), new Vector2(920f, 36f));

        LanguageManager.OnLanguageChanged refresh = () =>
        {
            var lm = LanguageManager.Instance;
            if (nameTxt != null) nameTxt.text = SafeGet(lm, habitat.AnimalNameKey, "Verblijf");
            if (descTxt != null) descTxt.text = SafeGet(lm, habitat.HabitatDescriptionKey, "Een verblijf voor dieren.");
            if (factTxt != null) factTxt.text = SafeGet(lm, habitat.EducationalFactKey, "Leuk weetje!");
            if (hintTxt != null) hintTxt.text = SafeGet(lm, "inspect_hint", "Tilt to look around");
        };
        if (LanguageManager.Instance != null) LanguageManager.Instance.LanguageChanged += refresh;
        var deathHook = canvasObj.AddComponent<OnDestroyHook>();
        deathHook.OnDestroyAction = () =>
        {
            if (LanguageManager.Instance != null) LanguageManager.Instance.LanguageChanged -= refresh;
        };

        string backText = SafeGet(LanguageManager.Instance, "btn_back", "Terug");
        string inspText = SafeGet(LanguageManager.Instance, "btn_inspect", "Inspecteren");
        string mgText = SafeGet(LanguageManager.Instance, "btn_minigame", "Minigame");

        var backBtn = MakeButton(canvasObj.transform, backText, new Vector2(30f, 30f), new Vector2(220f, 120f), new Color(0.55f, 0.18f, 0.18f));
        var inspBtn = MakeButton(canvasObj.transform, inspText, new Vector2(265f, 30f), new Vector2(340f, 120f), new Color(0.12f, 0.48f, 0.78f));

        Button mgBtn = null;
        if (habitat.HasMinigame)
            mgBtn = MakeButton(canvasObj.transform, mgText, new Vector2(620f, 30f), new Vector2(320f, 120f), new Color(0.55f, 0.18f, 0.65f));

        backBtn.onClick.AddListener(CloseHabitat);

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
            mgBtn.onClick.AddListener(() => SceneManager.LoadScene(habitat.MinigameScene));
        }
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

        string backText = SafeGet(LanguageManager.Instance, "btn_back", "Terug");
        var btn = MakeButton(canvasObj.transform, backText, new Vector2(30f, 30f), new Vector2(260f, 120f), new Color(0.55f, 0.18f, 0.18f));
        btn.interactable = true;
        btn.onClick.AddListener(() =>
        {
            btn.interactable = false;
            DestroyCard();
            StartCoroutine(ReturnToCardFromInspect());
        });
    }

    void DestroyCard()
    {
        if (_cardCanvas != null) { Destroy(_cardCanvas); _cardCanvas = null; }
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