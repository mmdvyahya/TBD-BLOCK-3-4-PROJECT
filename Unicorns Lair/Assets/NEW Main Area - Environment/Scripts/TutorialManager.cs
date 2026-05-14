using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }
    public static event System.Action UnlockChanged;

    enum SubState
    {
        WaitingForBuy = 0,
        Building = 1,
        WaitingForTap = 2,
        WaitingForInspect = 3,
        Inspecting = 4,
        WaitingForBack = 5,
        WaitingForMinigame = 6,
        WaitingForMinigameReturn = 7,
        Complete = 99
    }

    [Header("Habitat Build Order")]
    [SerializeField]
    private string[] habitatOrder =
    {
        "beaver_habitat","polarbear_habitat","racoon_habitat","prairiedog_habitat","baboon_habitat","hippo_habitat",
    };

    [Header("Visuals")]
    [Tooltip("Optional. Sprite shown hovering above the habitat when player needs to tap on it.")]
    [SerializeField] private Sprite tapIcon;
    [SerializeField] private Color glowColor = new Color(1f, 0.85f, 0.2f);
    [SerializeField] private float glowGroundOffset = 0.05f;
    [SerializeField] private float inspectionPromptSeconds = 5f;

    [Header("Camera")]
    [SerializeField] private Camera mainCamera;

    [System.Serializable]
    public struct DialogueLine
    {
        public string localizationKey;
        [TextArea(2, 4)] public string fallbackText;
    }

    [Header("Intro Dialogue")]
    [SerializeField] private string speakerNameKey = "intro_speaker";
    [SerializeField] private string speakerNameFallback = "Boswachter";
    [Tooltip("Characters per second typed out.")]
    [SerializeField] private float charsPerSecond = 38f;
    [SerializeField]
    private DialogueLine[] introDialogue = new DialogueLine[]
    {
        new DialogueLine { localizationKey = "intro_dlg_0", fallbackText = "Hé daar, kleine ontdekker!" },
        new DialogueLine { localizationKey = "intro_dlg_1", fallbackText = "Welkom in Wildlands... gelukkig ben jij er!" },
        new DialogueLine { localizationKey = "intro_dlg_2", fallbackText = "Oh nee, we hebben jouw hulp echt nodig!" },
        new DialogueLine { localizationKey = "intro_dlg_3", fallbackText = "De dieren hebben nog geen plek om te wonen..." },
        new DialogueLine { localizationKey = "intro_dlg_4", fallbackText = "Help jij ons om een dierentuin voor ze te bouwen?" },
        new DialogueLine { localizationKey = "intro_dlg_5", fallbackText = "Speel leuke minigames met de dieren om munten te verdienen!" },
        new DialogueLine { localizationKey = "intro_dlg_6", fallbackText = "Bouw met die munten gloednieuwe verblijven!" },
        new DialogueLine { localizationKey = "intro_dlg_7", fallbackText = "Klaar om de allerbeste dierenheld te worden? Tik om te beginnen!" },
    };

    private bool _introActive;
    private int _dialogueIndex;
    private bool _typing;
    private bool _skipTyping;
    private GameObject _dialogueOverlay;
    private GameObject _dialogueBox;
    private Text _dialogueText;
    private Text _dialogueSpeaker;
    private GameObject _continueIndicator;
    private Coroutine _typeCoroutine;
    private Coroutine _indicatorPulseCoroutine;

    private int _currentStep;
    private bool _hasSeenIntro;
    private bool _tutorialFinished;
    private SubState _sub;
    private float _inspectTimer;

    private Canvas _tutorialCanvas;
    private GameObject _bannerObj;
    private Text _bannerText;
    private GameObject _pointerObj;
    private RectTransform _pointerRt;
    private Image _pointerImage;
    private Text _pointerFallbackText;
    private Transform _pointerTarget;
    private GameObject _habitatGlowObj;
    private Material _glowMat;
    private Vector3 _originalGlowScale = Vector3.one;

    private Button _pulsingCardBack;
    private Button _pulsingCardInspect;
    private Button _pulsingCardMinigame;
    private Button _pulsingInspectBack;
    private Coroutine _pulseCoroutine;

    const string PREF_STEP = "tutorial_step";
    const string PREF_INTRO = "tutorial_intro_seen";
    const string PREF_SUB = "tutorial_substate";
    const string PREF_COINS = "tutorial_coins_before_minigame";

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        LanguageManager.Ensure();
        GameStateManager.Ensure();

        _currentStep = PlayerPrefs.GetInt(PREF_STEP, 0);
        _hasSeenIntro = PlayerPrefs.GetInt(PREF_INTRO, 0) == 1;
        _sub = (SubState)PlayerPrefs.GetInt(PREF_SUB, (int)SubState.WaitingForBuy);
        _tutorialFinished = _currentStep >= habitatOrder.Length;

        BuildTutorialCanvas();
        ApplyUnlocks();

        GameStateManager.Instance.ItemBuilt += OnItemBuilt;
        LanguageManager.Instance.LanguageChanged += OnLanguageChanged;
        HabitatInteractionController.CardShown += OnCardShown;
        HabitatInteractionController.CardClosed += OnCardClosed;
        HabitatInteractionController.InspectBackButtonShown += OnInspectBackShown;
        HabitatInteractionController.MinigamePressed += OnMinigamePressed;

        if (!_hasSeenIntro)
        {
            _introActive = true;
            ApplyUnlocks();
            HidePointer();
            HideBanner();
            ShowIntroDialogue();
        }
        else ResumeFromState();
    }

    void OnDestroy()
    {
        if (GameStateManager.Instance != null) GameStateManager.Instance.ItemBuilt -= OnItemBuilt;
        if (LanguageManager.Instance != null) LanguageManager.Instance.LanguageChanged -= OnLanguageChanged;
        HabitatInteractionController.CardShown -= OnCardShown;
        HabitatInteractionController.CardClosed -= OnCardClosed;
        HabitatInteractionController.InspectBackButtonShown -= OnInspectBackShown;
        HabitatInteractionController.MinigamePressed -= OnMinigamePressed;
        if (Instance == this) Instance = null;
    }

    public bool IsHabitatUnlocked(string habitatId)
    {
        if (_introActive) return false;
        if (_tutorialFinished) return true;
        int idx = System.Array.IndexOf(habitatOrder, habitatId);
        if (idx < 0) return true;
        return idx <= _currentStep;
    }

    void ApplyUnlocks() => UnlockChanged?.Invoke();

    void ResumeFromState()
    {
        if (_tutorialFinished) { HidePointer(); HideBanner(); HideGlow(); return; }

        if (_sub == SubState.WaitingForMinigameReturn)
        {
            int coinsBefore = PlayerPrefs.GetInt(PREF_COINS, GameStateManager.Instance.Coins);
            if (GameStateManager.Instance.Coins > coinsBefore) { AdvanceToNextHabitatStep(); return; }
            EnterSubState(SubState.WaitingForTap);
            return;
        }

        switch (_sub)
        {
            case SubState.WaitingForBuy: EnterSubState(SubState.WaitingForBuy); break;
            case SubState.Building: EnterSubState(SubState.WaitingForBuy); break;
            case SubState.WaitingForTap: EnterSubState(SubState.WaitingForTap); break;
            default: EnterSubState(SubState.WaitingForTap); break;
        }
    }

    void EnterSubState(SubState next)
    {
        _sub = next;
        PlayerPrefs.SetInt(PREF_SUB, (int)_sub);
        PlayerPrefs.Save();

        StopPulsing();
        HideGlow();
        _pointerTarget = null;

        if (_tutorialFinished || _currentStep >= habitatOrder.Length) { HidePointer(); HideBanner(); return; }

        string currentId = habitatOrder[_currentStep];
        var habitat = FindHabitat(currentId);

        switch (next)
        {
            case SubState.WaitingForBuy:
                if (habitat != null) _pointerTarget = habitat.GetButtonAnchor();
                ShowPointer(useTapIcon: false);
                ShowBanner(_currentStep == 0
                    ? SafeGet("tutorial_first_buy", "Tik op de groene knop om je eerste verblijf te bouwen!")
                    : SafeGet("tutorial_next_buy", "Goed gedaan! Bouw nu het volgende verblijf!"));
                break;

            case SubState.Building:
                HidePointer();
                ShowBanner(SafeGet("tutorial_building", "Even wachten... je verblijf wordt gebouwd!"));
                break;

            case SubState.WaitingForTap:
                if (habitat != null)
                {
                    _pointerTarget = habitat.GetButtonAnchor();
                    SpawnGlowAroundHabitat(habitat);
                }
                ShowPointer(useTapIcon: true);
                ShowBanner(SafeGet("tutorial_tap_habitat", "Tik op het verblijf om het dier te ontmoeten!"));
                break;

            case SubState.WaitingForInspect:
                HidePointer();
                ShowBanner(SafeGet("tutorial_press_inspect", "Druk op Inspecteren om rond te kijken!"));
                StartPulsing(_pulsingCardInspect);
                break;

            case SubState.Inspecting:
                HidePointer();
                ShowBanner(SafeGet("tutorial_inspecting", "Kijk goed rond! Kantel de tablet of gebruik WASD."));
                _inspectTimer = inspectionPromptSeconds;
                break;

            case SubState.WaitingForBack:
                HidePointer();
                ShowBanner(SafeGet("tutorial_press_back", "Druk op Terug om verder te gaan!"));
                StartPulsing(_pulsingInspectBack);
                break;

            case SubState.WaitingForMinigame:
                HidePointer();
                ShowBanner(SafeGet("tutorial_press_minigame", "Speel een minigame om munten te verdienen!"));
                StartPulsing(_pulsingCardMinigame);
                break;
        }
    }

    void OnItemBuilt(string itemId)
    {
        if (_tutorialFinished) return;
        if (_currentStep >= habitatOrder.Length) return;
        if (habitatOrder[_currentStep] != itemId) return;
        EnterSubState(SubState.WaitingForTap);
    }

    void OnCardShown(Button back, Button inspect, Button minigame, InspectableHabitat habitat)
    {
        _pulsingCardBack = back;
        _pulsingCardInspect = inspect;
        _pulsingCardMinigame = minigame;

        if (_sub == SubState.WaitingForTap) EnterSubState(SubState.WaitingForInspect);
        else if (_sub == SubState.WaitingForBack) EnterSubState(SubState.WaitingForMinigame);
    }

    void OnCardClosed()
    {
        _pulsingCardBack = _pulsingCardInspect = _pulsingCardMinigame = null;
        StopPulsing();
    }

    void OnInspectBackShown(Button back)
    {
        _pulsingInspectBack = back;
        if (_sub == SubState.WaitingForInspect) EnterSubState(SubState.Inspecting);
    }

    void OnMinigamePressed(InspectableHabitat habitat)
    {
        if (_tutorialFinished) return;
        PlayerPrefs.SetInt(PREF_COINS, GameStateManager.Instance.Coins);
        EnterSubState(SubState.WaitingForMinigameReturn);
    }

    void AdvanceToNextHabitatStep()
    {
        _currentStep++;
        PlayerPrefs.SetInt(PREF_STEP, _currentStep);

        if (_currentStep >= habitatOrder.Length)
        {
            _tutorialFinished = true;
            ShowBanner(SafeGet("tutorial_complete", "Geweldig! Alle dieren hebben een thuis. Speel verder!"));
            StartCoroutine(HideBannerAfter(4f));
            HidePointer(); HideGlow();
            ApplyUnlocks();
            PlayerPrefs.SetInt(PREF_SUB, (int)SubState.Complete);
            PlayerPrefs.Save();
            return;
        }
        ApplyUnlocks();
        EnterSubState(SubState.WaitingForBuy);
    }

    void Update()
    {
        if (_sub == SubState.Inspecting && _inspectTimer > 0f)
        {
            _inspectTimer -= Time.deltaTime;
            if (_inspectTimer <= 0f) EnterSubState(SubState.WaitingForBack);
        }

        if (_pointerObj != null && _pointerObj.activeSelf && _pointerTarget != null)
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera != null)
            {
                Vector3 screen = mainCamera.WorldToScreenPoint(_pointerTarget.position);
                if (screen.z <= 0f) _pointerObj.SetActive(false);
                else
                {
                    _pointerObj.SetActive(true);
                    var canvasRt = _tutorialCanvas.GetComponent<RectTransform>();
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screen, null, out Vector2 local);
                    float bob = Mathf.Sin(Time.time * 4f) * 18f;
                    local.y += 110f + bob;
                    _pointerRt.anchoredPosition = local;
                    float pulse = 1f + Mathf.Sin(Time.time * 5f) * 0.08f;
                    _pointerRt.localScale = Vector3.one * pulse;
                }
            }
        }

        if (_habitatGlowObj != null && _glowMat != null)
        {
            float a = 0.35f + Mathf.Sin(Time.time * 3f) * 0.25f;
            var c = glowColor; c.a = a;
            _glowMat.color = c;
            if (_glowMat.HasProperty("_BaseColor")) _glowMat.SetColor("_BaseColor", c);
            float s = 1f + Mathf.Sin(Time.time * 3f) * 0.05f;
            _habitatGlowObj.transform.localScale = new Vector3(_originalGlowScale.x * s, _originalGlowScale.y, _originalGlowScale.z * s);
        }
    }

    void SpawnGlowAroundHabitat(Habitat habitat)
    {
        HideGlow();
        if (habitat == null) return;

        Bounds? combined = null;
        var renderers = habitat.GetComponentsInChildren<Renderer>(false);
        foreach (var r in renderers)
        {
            if (combined == null) combined = r.bounds;
            else { var b = combined.Value; b.Encapsulate(r.bounds); combined = b; }
        }
        if (combined == null) return;

        var b2 = combined.Value;
        Vector3 groundCenter = new Vector3(b2.center.x, b2.min.y + glowGroundOffset, b2.center.z);
        float size = Mathf.Max(b2.size.x, b2.size.z) * 1.3f;

        _habitatGlowObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        _habitatGlowObj.name = "TutorialHabitatGlow";
        var col = _habitatGlowObj.GetComponent<Collider>();
        if (col != null) Destroy(col);

        _habitatGlowObj.transform.position = groundCenter;
        _habitatGlowObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        _habitatGlowObj.transform.localScale = new Vector3(size, size, 1f);
        _originalGlowScale = _habitatGlowObj.transform.localScale;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Transparent");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        _glowMat = new Material(shader);
        var c = glowColor; c.a = 0.45f;
        _glowMat.color = c;
        if (_glowMat.HasProperty("_BaseColor")) _glowMat.SetColor("_BaseColor", c);
        if (_glowMat.HasProperty("_Surface")) _glowMat.SetFloat("_Surface", 1f);
        if (_glowMat.HasProperty("_Blend")) _glowMat.SetFloat("_Blend", 0f);
        _glowMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        _glowMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        _glowMat.SetInt("_ZWrite", 0);
        _glowMat.renderQueue = 3000;
        _glowMat.mainTexture = MakeRingTexture(256);
        _habitatGlowObj.GetComponent<Renderer>().material = _glowMat;
    }

    void HideGlow()
    {
        if (_habitatGlowObj != null) Destroy(_habitatGlowObj);
        _habitatGlowObj = null;
        _glowMat = null;
    }

    Texture2D MakeRingTexture(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float c = size * 0.5f;
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - c) / c;
                float dy = (y - c) / c;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a;
                if (d > 1f) a = 0f;
                else if (d > 0.85f) a = 1f - (d - 0.85f) / 0.15f;
                else if (d > 0.65f) a = 1f;
                else if (d > 0.45f) a = (d - 0.45f) / 0.2f;
                else a = 0f;
                pixels[y * size + x] = new Color(1f, 1f, 1f, a);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    void StartPulsing(Button btn)
    {
        if (btn == null) return;
        StopPulsing();
        _pulseCoroutine = StartCoroutine(PulseButton(btn));
    }

    void StopPulsing()
    {
        if (_pulseCoroutine != null) StopCoroutine(_pulseCoroutine);
        _pulseCoroutine = null;
    }

    IEnumerator PulseButton(Button btn)
    {
        if (btn == null) yield break;
        var rt = btn.GetComponent<RectTransform>();
        Vector3 baseScale = rt.localScale;
        while (btn != null && rt != null)
        {
            float pulse = 1f + Mathf.Sin(Time.time * 5f) * 0.10f;
            rt.localScale = baseScale * pulse;
            yield return null;
        }
    }

    Habitat FindHabitat(string id)
    {
        var all = FindObjectsByType<Habitat>(FindObjectsSortMode.None);
        foreach (var h in all) if (h != null && h.HabitatId == id) return h;
        return null;
    }

    void BuildTutorialCanvas()
    {
        var cObj = new GameObject("TutorialCanvas");
        _tutorialCanvas = cObj.AddComponent<Canvas>();
        _tutorialCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _tutorialCanvas.sortingOrder = 30;
        var scaler = cObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        cObj.AddComponent<GraphicRaycaster>();
        EnsureEventSystem();

        _bannerObj = new GameObject("Banner");
        _bannerObj.transform.SetParent(cObj.transform, false);
        var brt = _bannerObj.AddComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.5f, 1f); brt.anchorMax = new Vector2(0.5f, 1f);
        brt.pivot = new Vector2(0.5f, 1f);
        brt.anchoredPosition = new Vector2(0f, -140f);
        brt.sizeDelta = new Vector2(900f, 120f);
        var bImg = _bannerObj.AddComponent<Image>();
        bImg.color = new Color(0.10f, 0.45f, 0.18f, 0.92f);
        bImg.raycastTarget = false;
        var bTxtObj = new GameObject("Text");
        bTxtObj.transform.SetParent(_bannerObj.transform, false);
        var btrt = bTxtObj.AddComponent<RectTransform>();
        btrt.anchorMin = Vector2.zero; btrt.anchorMax = Vector2.one;
        btrt.offsetMin = new Vector2(18f, 6f); btrt.offsetMax = new Vector2(-18f, -6f);
        _bannerText = bTxtObj.AddComponent<Text>();
        _bannerText.font = GetFont(); _bannerText.fontSize = 32; _bannerText.fontStyle = FontStyle.Bold;
        _bannerText.alignment = TextAnchor.MiddleCenter; _bannerText.color = Color.white; _bannerText.raycastTarget = false;
        bTxtObj.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.7f);
        _bannerObj.SetActive(false);

        _pointerObj = new GameObject("Pointer");
        _pointerObj.transform.SetParent(cObj.transform, false);
        _pointerRt = _pointerObj.AddComponent<RectTransform>();
        _pointerRt.anchorMin = _pointerRt.anchorMax = _pointerRt.pivot = new Vector2(0.5f, 0.5f);
        _pointerRt.sizeDelta = new Vector2(140f, 140f);

        _pointerFallbackText = _pointerObj.AddComponent<Text>();
        _pointerFallbackText.text = "▼";
        _pointerFallbackText.font = GetFont();
        _pointerFallbackText.fontSize = 100; _pointerFallbackText.fontStyle = FontStyle.Bold;
        _pointerFallbackText.alignment = TextAnchor.MiddleCenter;
        _pointerFallbackText.color = glowColor;
        _pointerFallbackText.raycastTarget = false;
        var pOutline = _pointerObj.AddComponent<Outline>();
        pOutline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        pOutline.effectDistance = new Vector2(3f, -3f);

        if (tapIcon != null)
        {
            var iconObj = new GameObject("TapIconImage");
            iconObj.transform.SetParent(_pointerObj.transform, false);
            var irt = iconObj.AddComponent<RectTransform>();
            irt.anchorMin = Vector2.zero; irt.anchorMax = Vector2.one;
            irt.offsetMin = irt.offsetMax = Vector2.zero;
            _pointerImage = iconObj.AddComponent<Image>();
            _pointerImage.sprite = tapIcon;
            _pointerImage.color = Color.white;
            _pointerImage.raycastTarget = false;
            _pointerImage.preserveAspect = true;
            _pointerImage.enabled = false;
        }
        _pointerObj.SetActive(false);
    }

    void ShowPointer(bool useTapIcon)
    {
        if (_pointerObj == null) return;
        _pointerObj.SetActive(true);
        bool hasIcon = _pointerImage != null;
        if (_pointerImage != null) _pointerImage.enabled = useTapIcon && hasIcon;
        if (_pointerFallbackText != null) _pointerFallbackText.enabled = !(useTapIcon && hasIcon);
    }

    void HidePointer()
    {
        if (_pointerObj != null) _pointerObj.SetActive(false);
    }

    void ShowIntroDialogue()
    {
        _dialogueIndex = 0;
        _typing = false;
        _skipTyping = false;

        _dialogueOverlay = new GameObject("IntroDialogueOverlay");
        _dialogueOverlay.transform.SetParent(_tutorialCanvas.transform, false);
        var ort = _dialogueOverlay.AddComponent<RectTransform>();
        ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one;
        ort.offsetMin = ort.offsetMax = Vector2.zero;

        var dim = _dialogueOverlay.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.55f);
        var dimBtn = _dialogueOverlay.AddComponent<Button>();
        dimBtn.targetGraphic = dim;
        dimBtn.transition = Selectable.Transition.None;
        dimBtn.onClick.AddListener(OnDialogueTapped);

        _dialogueBox = new GameObject("DialogueBox");
        _dialogueBox.transform.SetParent(_dialogueOverlay.transform, false);
        var brt = _dialogueBox.AddComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.5f, 0f); brt.anchorMax = new Vector2(0.5f, 0f);
        brt.pivot = new Vector2(0.5f, 0f);
        brt.anchoredPosition = new Vector2(0f, 100f);
        brt.sizeDelta = new Vector2(1000f, 480f);
        var bImg = _dialogueBox.AddComponent<Image>();
        bImg.color = new Color(0.08f, 0.13f, 0.24f, 0.97f);
        bImg.raycastTarget = false;

        var accentTop = new GameObject("AccentTop");
        accentTop.transform.SetParent(_dialogueBox.transform, false);
        var atRt = accentTop.AddComponent<RectTransform>();
        atRt.anchorMin = new Vector2(0f, 1f); atRt.anchorMax = new Vector2(1f, 1f);
        atRt.pivot = new Vector2(0.5f, 1f); atRt.anchoredPosition = Vector2.zero; atRt.sizeDelta = new Vector2(0f, 10f);
        accentTop.AddComponent<Image>().color = new Color(0.3f, 0.75f, 1f);

        var accentLeft = new GameObject("AccentLeft");
        accentLeft.transform.SetParent(_dialogueBox.transform, false);
        var alRt = accentLeft.AddComponent<RectTransform>();
        alRt.anchorMin = new Vector2(0f, 0f); alRt.anchorMax = new Vector2(0f, 1f);
        alRt.pivot = new Vector2(0f, 0.5f); alRt.anchoredPosition = Vector2.zero; alRt.sizeDelta = new Vector2(8f, 0f);
        accentLeft.AddComponent<Image>().color = new Color(0.3f, 0.75f, 1f);

        var nameTag = new GameObject("SpeakerTag");
        nameTag.transform.SetParent(_dialogueBox.transform, false);
        var ntRt = nameTag.AddComponent<RectTransform>();
        ntRt.anchorMin = new Vector2(0f, 1f); ntRt.anchorMax = new Vector2(0f, 1f);
        ntRt.pivot = new Vector2(0f, 0f);
        ntRt.anchoredPosition = new Vector2(36f, 6f);
        ntRt.sizeDelta = new Vector2(320f, 70f);
        var ntImg = nameTag.AddComponent<Image>();
        ntImg.color = new Color(0.3f, 0.75f, 1f);
        ntImg.raycastTarget = false;

        var nameTxtObj = new GameObject("Text");
        nameTxtObj.transform.SetParent(nameTag.transform, false);
        var nameTxtRt = nameTxtObj.AddComponent<RectTransform>();
        nameTxtRt.anchorMin = Vector2.zero; nameTxtRt.anchorMax = Vector2.one;
        nameTxtRt.offsetMin = new Vector2(18f, 0f); nameTxtRt.offsetMax = new Vector2(-18f, 0f);
        _dialogueSpeaker = nameTxtObj.AddComponent<Text>();
        _dialogueSpeaker.text = SafeGet(speakerNameKey, speakerNameFallback);
        _dialogueSpeaker.font = GetFont();
        _dialogueSpeaker.fontSize = 36;
        _dialogueSpeaker.fontStyle = FontStyle.Bold;
        _dialogueSpeaker.alignment = TextAnchor.MiddleLeft;
        _dialogueSpeaker.color = new Color(0.08f, 0.13f, 0.24f);
        _dialogueSpeaker.raycastTarget = false;

        var txtObj = new GameObject("DialogueText");
        txtObj.transform.SetParent(_dialogueBox.transform, false);
        var txtRt = txtObj.AddComponent<RectTransform>();
        txtRt.anchorMin = new Vector2(0f, 0f); txtRt.anchorMax = new Vector2(1f, 1f);
        txtRt.offsetMin = new Vector2(48f, 90f); txtRt.offsetMax = new Vector2(-48f, -50f);
        _dialogueText = txtObj.AddComponent<Text>();
        _dialogueText.text = "";
        _dialogueText.font = GetFont();
        _dialogueText.fontSize = 42;
        _dialogueText.fontStyle = FontStyle.Normal;
        _dialogueText.alignment = TextAnchor.UpperLeft;
        _dialogueText.color = new Color(0.95f, 0.97f, 1f);
        _dialogueText.raycastTarget = false;
        _dialogueText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _dialogueText.verticalOverflow = VerticalWrapMode.Truncate;
        txtObj.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.7f);

        _continueIndicator = new GameObject("ContinueIndicator");
        _continueIndicator.transform.SetParent(_dialogueBox.transform, false);
        var ciRt = _continueIndicator.AddComponent<RectTransform>();
        ciRt.anchorMin = new Vector2(1f, 0f); ciRt.anchorMax = new Vector2(1f, 0f);
        ciRt.pivot = new Vector2(1f, 0f);
        ciRt.anchoredPosition = new Vector2(-30f, 18f);
        ciRt.sizeDelta = new Vector2(280f, 44f);
        var ciTxt = _continueIndicator.AddComponent<Text>();
        ciTxt.text = SafeGet("intro_tap_continue", "Tik om verder ▶");
        ciTxt.font = GetFont(); ciTxt.fontSize = 24; ciTxt.fontStyle = FontStyle.Bold;
        ciTxt.alignment = TextAnchor.MiddleRight;
        ciTxt.color = new Color(0.3f, 0.75f, 1f);
        ciTxt.raycastTarget = false;
        _continueIndicator.SetActive(false);

        StartCoroutine(SlideInDialogue(brt));
    }

    IEnumerator SlideInDialogue(RectTransform boxRt)
    {
        Vector2 endPos = boxRt.anchoredPosition;
        Vector2 startPos = endPos + new Vector2(0f, -boxRt.sizeDelta.y - 200f);
        boxRt.anchoredPosition = startPos;
        float t = 0f, dur = 0.45f;
        while (t < dur)
        {
            t += Time.deltaTime;
            if (boxRt == null) yield break;
            float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / dur));
            boxRt.anchoredPosition = Vector2.Lerp(startPos, endPos, p);
            yield return null;
        }
        if (boxRt != null) boxRt.anchoredPosition = endPos;
        StartTypingCurrentLine();
    }

    void StartTypingCurrentLine()
    {
        if (_typeCoroutine != null) StopCoroutine(_typeCoroutine);
        _typeCoroutine = StartCoroutine(TypeLine(_dialogueIndex));
    }

    IEnumerator TypeLine(int index)
    {
        if (index < 0 || index >= introDialogue.Length) yield break;
        HideContinueIndicator();
        _typing = true;
        _skipTyping = false;

        var line = introDialogue[index];
        string content = !string.IsNullOrEmpty(line.localizationKey)
            ? SafeGet(line.localizationKey, line.fallbackText)
            : line.fallbackText;
        if (string.IsNullOrEmpty(content)) content = "";

        _dialogueText.text = "";
        float delay = 1f / Mathf.Max(1f, charsPerSecond);
        for (int i = 0; i < content.Length; i++)
        {
            if (_skipTyping) { _dialogueText.text = content; break; }
            _dialogueText.text = content.Substring(0, i + 1);
            char c = content[i];
            float wait = delay;
            if (c == '.' || c == '!' || c == '?') wait = delay * 7f;
            else if (c == ',' || c == ';' || c == ':') wait = delay * 3f;
            yield return new WaitForSeconds(wait);
        }
        _typing = false;
        _skipTyping = false;
        ShowContinueIndicator();
    }

    void OnDialogueTapped()
    {
        if (_dialogueOverlay == null) return;
        if (_typing) { _skipTyping = true; return; }
        AdvanceDialogue();
    }

    void AdvanceDialogue()
    {
        _dialogueIndex++;
        if (_dialogueIndex >= introDialogue.Length)
        {
            StartCoroutine(CloseIntroDialogue());
        }
        else
        {
            StartTypingCurrentLine();
        }
    }

    void ShowContinueIndicator()
    {
        if (_continueIndicator == null) return;
        _continueIndicator.SetActive(true);
        if (_indicatorPulseCoroutine != null) StopCoroutine(_indicatorPulseCoroutine);
        _indicatorPulseCoroutine = StartCoroutine(PulseIndicator());
    }

    void HideContinueIndicator()
    {
        if (_continueIndicator != null) _continueIndicator.SetActive(false);
        if (_indicatorPulseCoroutine != null) { StopCoroutine(_indicatorPulseCoroutine); _indicatorPulseCoroutine = null; }
    }

    IEnumerator PulseIndicator()
    {
        while (_continueIndicator != null && _continueIndicator.activeSelf)
        {
            float t = (Mathf.Sin(Time.time * 4f) + 1f) * 0.5f;
            float scale = Mathf.Lerp(0.92f, 1.08f, t);
            _continueIndicator.transform.localScale = new Vector3(scale, scale, 1f);
            var txt = _continueIndicator.GetComponent<Text>();
            if (txt != null)
            {
                var c = txt.color; c.a = Mathf.Lerp(0.55f, 1f, t); txt.color = c;
            }
            yield return null;
        }
    }

    IEnumerator CloseIntroDialogue()
    {
        if (_typeCoroutine != null) StopCoroutine(_typeCoroutine);
        HideContinueIndicator();

        var brt = _dialogueBox != null ? _dialogueBox.GetComponent<RectTransform>() : null;
        if (brt != null)
        {
            Vector2 startPos = brt.anchoredPosition;
            Vector2 endPos = startPos + new Vector2(0f, -brt.sizeDelta.y - 200f);
            float t = 0f, dur = 0.35f;
            while (t < dur)
            {
                t += Time.deltaTime;
                if (brt == null) break;
                float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / dur));
                brt.anchoredPosition = Vector2.Lerp(startPos, endPos, p);
                if (_dialogueOverlay != null)
                {
                    var dim = _dialogueOverlay.GetComponent<Image>();
                    if (dim != null) { var c = dim.color; c.a = Mathf.Lerp(0.55f, 0f, p); dim.color = c; }
                }
                yield return null;
            }
        }

        if (_dialogueOverlay != null) Destroy(_dialogueOverlay);
        _dialogueOverlay = null;
        _dialogueBox = null;
        _dialogueText = null;
        _dialogueSpeaker = null;
        _continueIndicator = null;

        PlayerPrefs.SetInt(PREF_INTRO, 1);
        PlayerPrefs.Save();
        _hasSeenIntro = true;
        _introActive = false;

        ApplyUnlocks();
        ResumeFromState();
    }

    void OnLanguageChanged() { EnterSubState(_sub); }

    void ShowBanner(string text)
    {
        if (_bannerObj == null) return;
        _bannerObj.SetActive(true);
        _bannerText.text = text;
    }

    void HideBanner()
    {
        if (_bannerObj != null) _bannerObj.SetActive(false);
    }

    IEnumerator HideBannerAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        HideBanner();
    }

    Button MakeButton(Transform parent, string label, Vector2 pos, Vector2 size, Color color)
    {
        var obj = new GameObject("Btn");
        obj.transform.SetParent(parent, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f); rt.anchorMax = new Vector2(0.5f, 0f); rt.pivot = new Vector2(0.5f, 0f);
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