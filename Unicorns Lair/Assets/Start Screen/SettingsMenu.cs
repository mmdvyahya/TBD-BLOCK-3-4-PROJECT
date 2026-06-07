using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

// Self-contained settings button + menu. Make this a prefab, configure it once,
// and it works in every scene. It builds its own canvas, so it does not depend on
// any other script. It is a persistent singleton: add the prefab once (e.g. in your
// first scene) and it follows you into every scene. You can also drop the prefab into
// every scene individually - the singleton guard prevents duplicates.
public class SettingsMenu : MonoBehaviour
{
    public static SettingsMenu Instance { get; private set; }

    [Header("Settings Button (PNG)")]
    [SerializeField] private Sprite settingsButtonSprite;
    [Tooltip("Optional gear symbol PNG overlaid on the settings button. If empty, no overlay.")]
    [SerializeField] private Sprite settingsButtonIconSprite;
    [SerializeField] private Vector2 settingsButtonPos = new Vector2(-30f, -30f);
    [SerializeField] private Vector2 settingsButtonSize = new Vector2(120f, 120f);
    [SerializeField] private Vector2 settingsButtonIconSize = new Vector2(90f, 90f);

    [Header("Settings Panel - Background (PNG)")]
    [SerializeField] private Sprite settingsPanelSprite;
    [SerializeField] private Vector2 settingsPanelPos = new Vector2(0f, 0f);
    [SerializeField] private Vector2 settingsPanelSize = new Vector2(980f, 660f);
    [Range(0f, 1f)] [SerializeField] private float settingsPanelOpacity = 1f;
    [Range(0f, 1f)] [SerializeField] private float settingsDimOpacity = 0.6f;

    [Header("Settings Panel - Title (PNG, top center)")]
    [SerializeField] private Sprite settingsTitleSprite;
    [SerializeField] private Vector2 settingsTitlePos = new Vector2(0f, 250f);
    [SerializeField] private Vector2 settingsTitleSize = new Vector2(380f, 110f);

    [Header("Settings Panel - Close Button (PNG)")]
    [SerializeField] private Sprite settingsCloseSprite;
    [SerializeField] private Vector2 settingsClosePos = new Vector2(410f, 250f);
    [SerializeField] private Vector2 settingsCloseSize = new Vector2(90f, 90f);

    [Header("Settings - Row Labels (left text)")]
    [SerializeField] private int rowLabelFontSize = 48;
    [SerializeField] private Color rowLabelColor = Color.white;
    [SerializeField] private Vector2 rowLabelSize = new Vector2(400f, 80f);
    [SerializeField] private Vector2 languageLabelPos = new Vector2(-300f, 150f);
    [SerializeField] private Vector2 audioLabelPos = new Vector2(-300f, 30f);
    [SerializeField] private Vector2 vibrationsLabelPos = new Vector2(-300f, -90f);
    [SerializeField] private Vector2 musicLabelPos = new Vector2(-300f, -210f);

    [Header("Settings - Selection Highlight (PNG behind selected)")]
    [SerializeField] private Sprite selectionHighlightSprite;

    [Header("Settings - Language Options (NED / ENG / DEU)")]
    [SerializeField] private string[] languageCodes = { "NED", "ENG", "DEU" };
    [SerializeField] private Vector2 langNedPos = new Vector2(70f, 150f);
    [SerializeField] private Vector2 langEngPos = new Vector2(250f, 150f);
    [SerializeField] private Vector2 langDeuPos = new Vector2(430f, 150f);
    [SerializeField] private Vector2 langOptionSize = new Vector2(170f, 80f);
    [SerializeField] private int langOptionFontSize = 48;
    [SerializeField] private Color langOptionColor = Color.white;
    [SerializeField] private Vector2 langHighlightSize = new Vector2(170f, 96f);

    [Header("Settings - Audio Mute Button (PNG)")]
    [SerializeField] private Sprite audioUnmutedSprite;
    [SerializeField] private Sprite audioMutedSprite;
    [SerializeField] private Vector2 mutePos = new Vector2(40f, 30f);
    [SerializeField] private Vector2 muteSize = new Vector2(90f, 90f);

    [Header("Settings - Volume Slider (PNG)")]
    [SerializeField] private Sprite sliderTrackSprite;
    [Tooltip("Optional fill PNG that grows with the value.")]
    [SerializeField] private Sprite sliderFillSprite;
    [SerializeField] private Sprite sliderKnobSprite;
    [SerializeField] private Vector2 sliderPos = new Vector2(320f, 30f);
    [SerializeField] private Vector2 sliderSize = new Vector2(420f, 40f);
    [SerializeField] private Vector2 sliderKnobSize = new Vector2(70f, 70f);

    [Header("Settings - ON / OFF Toggles (shared)")]
    [SerializeField] private Vector2 toggleOptionSize = new Vector2(160f, 80f);
    [SerializeField] private int toggleFontSize = 48;
    [SerializeField] private Color toggleColor = Color.white;
    [SerializeField] private Vector2 toggleHighlightSize = new Vector2(170f, 96f);
    [SerializeField] private string onLabel = "ON";
    [SerializeField] private string offLabel = "OFF";

    [Header("Settings - Vibrations ON/OFF positions")]
    [SerializeField] private Vector2 vibOnPos = new Vector2(110f, -90f);
    [SerializeField] private Vector2 vibOffPos = new Vector2(300f, -90f);

    [Header("Settings - Music ON/OFF positions")]
    [SerializeField] private Vector2 musicOnPos = new Vector2(110f, -210f);
    [SerializeField] private Vector2 musicOffPos = new Vector2(300f, -210f);

    [Header("Settings - Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string masterParameter = "Master";
    [SerializeField] private string musicParameter = "Music";

    [Header("Behaviour")]
    [Tooltip("Keep this menu alive across scene loads so you only need to add it once.")]
    [SerializeField] private bool persistAcrossScenes = true;
    [Tooltip("Sorting order of the settings canvas. Keep high so it sits above scene UI.")]
    [SerializeField] private int canvasSortingOrder = 200;

    // PlayerPrefs keys (shared with VolumeSettings)
    const string PP_MASTER_LEVEL = "MasterLevel";
    const string PP_MUTED = "audio_muted";
    const string PP_MUSIC_ON = "music_on";
    const string PP_VIB_ON = "vibrations_on";

    private Canvas _canvas;
    private Image _muteIconImg;
    private GameObject _langHighlightNed, _langHighlightEng, _langHighlightDeu;
    private GameObject _vibOnHl, _vibOffHl, _musicOnHl, _musicOffHl;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (persistAcrossScenes) DontDestroyOnLoad(gameObject);
    }

    void OnEnable()  { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    void OnSceneLoaded(Scene s, LoadSceneMode m) { EnsureEventSystem(); }

    void Start()
    {
        LanguageManager.Ensure();
        ApplyAudioToMixer();
        EnsureEventSystem();
        BuildButton();
    }

    void BuildButton()
    {
        var cObj = new GameObject("SettingsCanvas");
        cObj.transform.SetParent(transform, false); // child of this -> persists with the singleton
        _canvas = cObj.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = canvasSortingOrder;
        var scaler = cObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        cObj.AddComponent<GraphicRaycaster>();

        var obj = new GameObject("SettingsBtn");
        obj.transform.SetParent(_canvas.transform, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = settingsButtonPos;
        rt.sizeDelta = settingsButtonSize;

        var img = obj.AddComponent<Image>();
        Color baseCol;
        if (settingsButtonSprite != null)
        {
            img.sprite = settingsButtonSprite;
            img.preserveAspect = true;
            img.color = Color.white;
            baseCol = Color.white;
        }
        else { baseCol = new Color(0f, 0f, 0f, 0.45f); img.color = baseCol; }

        var btn = obj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.colors = new ColorBlock
        {
            normalColor = baseCol,
            highlightedColor = baseCol * 1.12f,
            pressedColor = baseCol * 0.8f,
            selectedColor = baseCol,
            disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.4f),
            colorMultiplier = 1f,
            fadeDuration = 0.08f
        };
        btn.onClick.AddListener(BuildSettingsPanel);

        if (settingsButtonIconSprite != null)
        {
            var ico = new GameObject("Icon");
            ico.transform.SetParent(obj.transform, false);
            var irt = ico.AddComponent<RectTransform>();
            irt.anchorMin = irt.anchorMax = irt.pivot = new Vector2(0.5f, 0.5f);
            irt.anchoredPosition = Vector2.zero;
            irt.sizeDelta = settingsButtonIconSize;
            var iImg = ico.AddComponent<Image>();
            iImg.sprite = settingsButtonIconSprite;
            iImg.preserveAspect = true;
            iImg.raycastTarget = false;
        }
        else
        {
            var lObj = new GameObject("Icon");
            lObj.transform.SetParent(obj.transform, false);
            var lrt = lObj.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            var lTxt = lObj.AddComponent<Text>();
            lTxt.text = "\u2699";
            lTxt.font = GetFont();
            lTxt.fontSize = 72;
            lTxt.alignment = TextAnchor.MiddleCenter;
            lTxt.color = Color.white;
            lTxt.raycastTarget = false;
        }
    }

    public void BuildSettingsPanel()
    {
        if (_canvas == null) return;
        var existing = _canvas.transform.Find("SettingsPanel");
        if (existing != null) { Destroy(existing.gameObject); return; }

        var panel = new GameObject("SettingsPanel");
        panel.transform.SetParent(_canvas.transform, false);
        var prt = panel.AddComponent<RectTransform>();
        prt.anchorMin = Vector2.zero;
        prt.anchorMax = Vector2.one;
        prt.offsetMin = prt.offsetMax = Vector2.zero;
        var overlay = panel.AddComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, settingsDimOpacity);
        overlay.raycastTarget = true;

        var box = MakePanelImage(panel.transform, "Box", settingsPanelSprite, settingsPanelPos, settingsPanelSize, settingsPanelOpacity,
            new Color(0.10f, 0.15f, 0.22f, 0.98f), false, false).gameObject;

        MakePanelImage(box.transform, "Title", settingsTitleSprite, settingsTitlePos, settingsTitleSize, 1f,
            new Color(0.22f, 0.30f, 0.44f), false, true);

        var closeImg = MakePanelImage(box.transform, "CloseBtn", settingsCloseSprite, settingsClosePos, settingsCloseSize, 1f,
            new Color(0.55f, 0.18f, 0.18f), true, true);
        var closeBtn = closeImg.gameObject.AddComponent<Button>();
        closeBtn.targetGraphic = closeImg;
        closeBtn.onClick.AddListener(() => Destroy(panel));

        var curLang = LanguageManager.Instance.CurrentLanguage;

        // Language row
        MakeRowLabel(box.transform, "settings_language", "Language:", languageLabelPos);
        _langHighlightNed = MakeSelectable(box.transform, languageCodes.Length > 0 ? languageCodes[0] : "NED", langNedPos, langOptionSize, langOptionFontSize, langOptionColor, langHighlightSize, curLang == Language.Nederlands, () => SelectLanguage(Language.Nederlands));
        _langHighlightEng = MakeSelectable(box.transform, languageCodes.Length > 1 ? languageCodes[1] : "ENG", langEngPos, langOptionSize, langOptionFontSize, langOptionColor, langHighlightSize, curLang == Language.English, () => SelectLanguage(Language.English));
        _langHighlightDeu = MakeSelectable(box.transform, languageCodes.Length > 2 ? languageCodes[2] : "DEU", langDeuPos, langOptionSize, langOptionFontSize, langOptionColor, langHighlightSize, curLang == Language.Deutsch, () => SelectLanguage(Language.Deutsch));

        // Audio row
        MakeRowLabel(box.transform, "settings_audio", "Audio:", audioLabelPos);
        MakeMuteButton(box.transform);
        BuildVolumeSlider(box.transform);

        // Vibrations row
        bool vibOn = PlayerPrefs.GetInt(PP_VIB_ON, 1) == 1;
        MakeRowLabel(box.transform, "settings_vibrations", "Vibrations:", vibrationsLabelPos);
        _vibOnHl = MakeSelectable(box.transform, onLabel, vibOnPos, toggleOptionSize, toggleFontSize, toggleColor, toggleHighlightSize, vibOn, () => SetVibrations(true));
        _vibOffHl = MakeSelectable(box.transform, offLabel, vibOffPos, toggleOptionSize, toggleFontSize, toggleColor, toggleHighlightSize, !vibOn, () => SetVibrations(false));

        // Music row
        bool musicOn = PlayerPrefs.GetInt(PP_MUSIC_ON, 1) == 1;
        MakeRowLabel(box.transform, "settings_music", "Music:", musicLabelPos);
        _musicOnHl = MakeSelectable(box.transform, onLabel, musicOnPos, toggleOptionSize, toggleFontSize, toggleColor, toggleHighlightSize, musicOn, () => SetMusic(true));
        _musicOffHl = MakeSelectable(box.transform, offLabel, musicOffPos, toggleOptionSize, toggleFontSize, toggleColor, toggleHighlightSize, !musicOn, () => SetMusic(false));
    }

    Image MakePanelImage(Transform parent, string name, Sprite sprite, Vector2 pos, Vector2 size, float opacity, Color fallback, bool raycast, bool keepAspect)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        var img = go.AddComponent<Image>();
        if (sprite != null)
        {
            img.sprite = sprite;
            img.type = Image.Type.Simple;
            img.preserveAspect = keepAspect;
            img.color = new Color(1f, 1f, 1f, opacity);
        }
        else img.color = fallback;
        img.raycastTarget = raycast;
        return img;
    }

    Text MakeRowLabel(Transform parent, string key, string fallback, Vector2 pos)
    {
        var go = new GameObject("Label_" + key);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = rowLabelSize;
        var txt = go.AddComponent<Text>();
        txt.font = GetFont(); txt.fontSize = rowLabelFontSize; txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleLeft; txt.color = rowLabelColor; txt.raycastTarget = false;
        var loc = go.AddComponent<LocalizedText>();
        loc.key = key; loc.Refresh();
        if (string.IsNullOrEmpty(txt.text) || txt.text == "[" + key + "]") txt.text = fallback;
        return txt;
    }

    GameObject MakeSelectable(Transform parent, string label, Vector2 pos, Vector2 optSize, int fontSize, Color color, Vector2 hlSize, bool selected, System.Action onClick)
    {
        var go = new GameObject("Option_" + label);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = optSize;

        var hit = go.AddComponent<Image>();
        hit.color = new Color(0f, 0f, 0f, 0f);
        hit.raycastTarget = true;

        GameObject hl = null;
        if (selectionHighlightSprite != null)
        {
            hl = new GameObject("Highlight");
            hl.transform.SetParent(go.transform, false);
            var hrt = hl.AddComponent<RectTransform>();
            hrt.anchorMin = hrt.anchorMax = hrt.pivot = new Vector2(0.5f, 0.5f);
            hrt.anchoredPosition = Vector2.zero; hrt.sizeDelta = hlSize;
            var hImg = hl.AddComponent<Image>();
            hImg.sprite = selectionHighlightSprite; hImg.type = Image.Type.Simple; hImg.raycastTarget = false;
            hl.transform.SetAsFirstSibling();
            hl.SetActive(selected);
        }

        var t = new GameObject("Text");
        t.transform.SetParent(go.transform, false);
        var trt = t.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = trt.offsetMax = Vector2.zero;
        var txt = t.AddComponent<Text>();
        txt.text = label; txt.font = GetFont(); txt.fontSize = fontSize; txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter; txt.color = color; txt.raycastTarget = false;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = hit;
        btn.transition = Selectable.Transition.None;
        btn.onClick.AddListener(() => onClick());

        return hl;
    }

    Button MakeMuteButton(Transform parent)
    {
        var go = new GameObject("MuteBtn");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = mutePos; rt.sizeDelta = muteSize;
        _muteIconImg = go.AddComponent<Image>();
        _muteIconImg.preserveAspect = true;
        bool muted = PlayerPrefs.GetInt(PP_MUTED, 0) == 1;
        _muteIconImg.sprite = muted ? audioMutedSprite : audioUnmutedSprite;
        _muteIconImg.color = (_muteIconImg.sprite == null) ? (muted ? new Color(0.7f, 0.2f, 0.2f) : Color.white) : Color.white;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = _muteIconImg;
        btn.onClick.AddListener(ToggleMute);
        return btn;
    }

    Slider BuildVolumeSlider(Transform parent)
    {
        var go = new GameObject("AudioSlider");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = sliderPos; rt.sizeDelta = sliderSize;
        var slider = go.AddComponent<Slider>();

        var bg = new GameObject("Track");
        bg.transform.SetParent(go.transform, false);
        var bgrt = bg.AddComponent<RectTransform>();
        bgrt.anchorMin = Vector2.zero; bgrt.anchorMax = Vector2.one; bgrt.offsetMin = bgrt.offsetMax = Vector2.zero;
        var bgImg = bg.AddComponent<Image>();
        if (sliderTrackSprite != null) { bgImg.sprite = sliderTrackSprite; bgImg.type = Image.Type.Simple; }
        else bgImg.color = new Color(1f, 1f, 1f, 0.3f);
        bgImg.raycastTarget = true;

        if (sliderFillSprite != null)
        {
            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(go.transform, false);
            var fart = fillArea.AddComponent<RectTransform>();
            fart.anchorMin = new Vector2(0f, 0.5f); fart.anchorMax = new Vector2(1f, 0.5f);
            fart.sizeDelta = new Vector2(0f, sliderSize.y);
            fart.anchoredPosition = Vector2.zero;
            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var frt = fill.AddComponent<RectTransform>();
            frt.anchorMin = new Vector2(0f, 0f); frt.anchorMax = new Vector2(1f, 1f); frt.offsetMin = frt.offsetMax = Vector2.zero;
            var fImg = fill.AddComponent<Image>(); fImg.sprite = sliderFillSprite; fImg.type = Image.Type.Simple; fImg.raycastTarget = false;
            slider.fillRect = frt;
        }

        var handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(go.transform, false);
        var hart = handleArea.AddComponent<RectTransform>();
        hart.anchorMin = Vector2.zero; hart.anchorMax = Vector2.one; hart.offsetMin = hart.offsetMax = Vector2.zero;
        var handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        var hrt = handle.AddComponent<RectTransform>();
        hrt.sizeDelta = sliderKnobSize;
        var hImg = handle.AddComponent<Image>();
        if (sliderKnobSprite != null) { hImg.sprite = sliderKnobSprite; hImg.preserveAspect = true; }
        else hImg.color = Color.white;
        slider.handleRect = hrt;
        slider.targetGraphic = hImg;

        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f; slider.maxValue = 100f; slider.wholeNumbers = false;
        slider.value = PlayerPrefs.GetFloat(PP_MASTER_LEVEL, 100f);
        slider.onValueChanged.AddListener(OnMasterSliderChanged);
        return slider;
    }

    void SelectLanguage(Language lang)
    {
        LanguageManager.Instance.SetLanguage(lang);
        var cur = LanguageManager.Instance.CurrentLanguage;
        if (_langHighlightNed != null) _langHighlightNed.SetActive(cur == Language.Nederlands);
        if (_langHighlightEng != null) _langHighlightEng.SetActive(cur == Language.English);
        if (_langHighlightDeu != null) _langHighlightDeu.SetActive(cur == Language.Deutsch);
    }

    void SetVibrations(bool on)
    {
        PlayerPrefs.SetInt(PP_VIB_ON, on ? 1 : 0);
        PlayerPrefs.Save();
        if (_vibOnHl != null) _vibOnHl.SetActive(on);
        if (_vibOffHl != null) _vibOffHl.SetActive(!on);
    }

    void SetMusic(bool on)
    {
        PlayerPrefs.SetInt(PP_MUSIC_ON, on ? 1 : 0);
        PlayerPrefs.Save();
        if (_musicOnHl != null) _musicOnHl.SetActive(on);
        if (_musicOffHl != null) _musicOffHl.SetActive(!on);
        ApplyAudioToMixer();
    }

    void ToggleMute()
    {
        bool muted = PlayerPrefs.GetInt(PP_MUTED, 0) == 1;
        muted = !muted;
        PlayerPrefs.SetInt(PP_MUTED, muted ? 1 : 0);
        PlayerPrefs.Save();
        if (_muteIconImg != null)
        {
            _muteIconImg.sprite = muted ? audioMutedSprite : audioUnmutedSprite;
            _muteIconImg.color = (_muteIconImg.sprite == null) ? (muted ? new Color(0.7f, 0.2f, 0.2f) : Color.white) : Color.white;
        }
        ApplyAudioToMixer();
    }

    void OnMasterSliderChanged(float v)
    {
        PlayerPrefs.SetFloat(PP_MASTER_LEVEL, v);
        PlayerPrefs.Save();
        ApplyAudioToMixer();
    }

    float LinearToDb(float lin) => lin > 0.001f ? Mathf.Log10(lin / 100f) * 20f : -80f;

    void ApplyAudioToMixer()
    {
        if (audioMixer == null) return;

        float level = PlayerPrefs.GetFloat(PP_MASTER_LEVEL, 100f);
        bool muted = PlayerPrefs.GetInt(PP_MUTED, 0) == 1;
        float master = muted ? 0f : level;
        audioMixer.SetFloat(masterParameter, LinearToDb(master));
        PlayerPrefs.SetFloat(masterParameter, master);

        bool musicOn = PlayerPrefs.GetInt(PP_MUSIC_ON, 1) == 1;
        float music = musicOn ? 100f : 0f;
        audioMixer.SetFloat(musicParameter, LinearToDb(music));
        PlayerPrefs.SetFloat(musicParameter, music);

        PlayerPrefs.Save();
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
