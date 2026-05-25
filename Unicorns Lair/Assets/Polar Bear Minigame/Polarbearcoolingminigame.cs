using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PolarBearCoolingMinigame : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private MicrophoneInput microphoneInput;
    [SerializeField] private Renderer polarBearRenderer;
    [SerializeField] private Transform polarBearTransform;
    [SerializeField] private Camera mainCamera;

    [Header("Particle Systems (optional)")]
    [Tooltip("Wind particles that emit when blowing - assign a ParticleSystem in the scene, or leave empty.")]
    [SerializeField] private ParticleSystem windParticles;
    [Tooltip("Burst particles played when minigame completes - assign a ParticleSystem in the scene, or leave empty.")]
    [SerializeField] private ParticleSystem successBurstParticles;
    [Tooltip("Optional ambient frost particles that always emit, intensifies as bear cools.")]
    [SerializeField] private ParticleSystem frostParticles;

    [Header("Cooling")]
    [SerializeField] private float secondsOfSoundNeeded = 5f;
    [SerializeField] private bool drainWhenSilent = false;
    [SerializeField] private float drainSpeed = 0.25f;
    [SerializeField] private float colorChangeSpeed = 2f;
    [SerializeField] private Color coolColor = new Color(0.55f, 0.82f, 1f);

    [Header("Camera Animation")]
    [SerializeField] private float celebrationZoomDistance = 1.5f;
    [SerializeField] private float celebrationZoomDuration = 0.6f;

    [Header("Reward")]
    [SerializeField] private int coinReward = 10;
    [SerializeField] private string returnSceneName = "MainArea";

    [Header("Settings")]
    [Tooltip("Show a kid-friendly 'How to Play' explanation before the game starts.")]
    [SerializeField] private bool showHowToPlay = true;

    private float _coolingProgress;
    private bool _completed;
    private bool _started;
    private bool _isBlowing;
    private Material _polarBearMaterial;
    private Color _originalColor;
    private string _colorProperty;
    private Vector3 _polarBearOriginPos;
    private Vector3 _cameraOriginPos;

    private Canvas _uiCanvas;
    private Image _coolingBar;
    private Text _instructionText;
    private Text _percentText;
    private Text _statusText;
    private GameObject _congratsCanvas;
    private GameObject _howToCanvas;
    private bool _hasStartedBefore;
    void Start()
    {
        LanguageManager.Ensure();
        GameStateManager.Ensure();

        if (microphoneInput == null) microphoneInput = FindFirstObjectByType<MicrophoneInput>();
        if (mainCamera == null) mainCamera = Camera.main;
        if (polarBearTransform == null && polarBearRenderer != null) polarBearTransform = polarBearRenderer.transform;

        if (polarBearRenderer != null)
        {
            _polarBearMaterial = polarBearRenderer.material;

            if (_polarBearMaterial.HasProperty("_Color")) _colorProperty = "_Color";
            else if (_polarBearMaterial.HasProperty("_BaseColor")) _colorProperty = "_BaseColor";
            else if (_polarBearMaterial.HasProperty("_MainColor")) _colorProperty = "_MainColor";
            else if (_polarBearMaterial.HasProperty("_TintColor")) _colorProperty = "_TintColor";
            else _colorProperty = null;

            if (_colorProperty != null) _originalColor = _polarBearMaterial.GetColor(_colorProperty);
            else Debug.LogWarning($"[PolarBearCoolingMinigame] Shader '{_polarBearMaterial.shader.name}' has no recognized color property - skipping color tint.");
        }
        if (polarBearTransform != null) _polarBearOriginPos = polarBearTransform.position;
        if (mainCamera != null) _cameraOriginPos = mainCamera.transform.position;

        SetWindEmission(0f);
        SetFrostEmission(2f);

        BuildUI();
        UpdateUI();

        if (showHowToPlay) ShowHowToPlay();
        else _started = true;

        LanguageManager.Instance.LanguageChanged += OnLanguageChanged;
    }

    void OnDestroy()
    {
        if (LanguageManager.Instance != null)
            LanguageManager.Instance.LanguageChanged -= OnLanguageChanged;
    }

    void OnLanguageChanged() => UpdateUI();

    void Update()
    {
        if (!_started || _completed) return;

        _isBlowing = microphoneInput != null && microphoneInput.WasBlowDetectedThisFrame;

        if (_isBlowing)
        {
            _coolingProgress += Time.deltaTime / secondsOfSoundNeeded;
            SetWindEmission(60f);
        }
        else
        {
            if (drainWhenSilent) _coolingProgress -= Time.deltaTime * drainSpeed;
            SetWindEmission(0f);
        }

        _coolingProgress = Mathf.Clamp01(_coolingProgress);
        SetFrostEmission(Mathf.Lerp(2f, 30f, _coolingProgress));

        ApplyCoolingVisual();
        UpdateUI();

        if (_coolingProgress >= 1f && !_completed)
            StartCoroutine(CompleteSequence());
    }

    void ApplyCoolingVisual()
    {
        if (_polarBearMaterial != null && _colorProperty != null)
        {
            Color current = _polarBearMaterial.GetColor(_colorProperty);
            Color target = Color.Lerp(_originalColor, coolColor, _coolingProgress);
            Color next = Color.Lerp(current, target, Time.deltaTime * colorChangeSpeed);
            _polarBearMaterial.SetColor(_colorProperty, next);
        }

        if (polarBearTransform != null)
        {
            if (_isBlowing && _coolingProgress > 0.05f)
            {
                float shake = 0.04f * _coolingProgress;
                Vector3 offset = new Vector3(
                    Mathf.Sin(Time.time * 28f) * shake,
                    0f,
                    Mathf.Cos(Time.time * 23f) * shake);
                polarBearTransform.position = _polarBearOriginPos + offset;
            }
            else
            {
                polarBearTransform.position = Vector3.Lerp(polarBearTransform.position, _polarBearOriginPos, Time.deltaTime * 6f);
            }
        }
    }

    void SetWindEmission(float rate)
    {
        if (windParticles == null) return;
        var em = windParticles.emission;
        em.rateOverTime = rate;
        if (rate > 0f && !windParticles.isPlaying) windParticles.Play();
    }

    void SetFrostEmission(float rate)
    {
        if (frostParticles == null) return;
        var em = frostParticles.emission;
        em.rateOverTime = rate;
        if (!frostParticles.isPlaying) frostParticles.Play();
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
        bg.color = new Color(0f, 0f, 0f, 0.78f);

        var card = new GameObject("Card");
        card.transform.SetParent(cObj.transform, false);
        var crt = card.AddComponent<RectTransform>();
        crt.anchorMin = crt.anchorMax = crt.pivot = new Vector2(0.5f, 0.5f);
        crt.anchoredPosition = Vector2.zero;
        crt.sizeDelta = new Vector2(920f, 700f);
        crt.localScale = Vector3.zero;
        var cImg = card.AddComponent<Image>();
        cImg.color = new Color(0.07f, 0.12f, 0.20f, 0.98f);

        var accent = new GameObject("Accent");
        accent.transform.SetParent(card.transform, false);
        var aRt = accent.AddComponent<RectTransform>();
        aRt.anchorMin = new Vector2(0f, 1f); aRt.anchorMax = new Vector2(1f, 1f);
        aRt.pivot = new Vector2(0.5f, 1f); aRt.anchoredPosition = Vector2.zero; aRt.sizeDelta = new Vector2(0f, 14f);
        accent.AddComponent<Image>().color = new Color(0.45f, 0.8f, 1f);

        MakeLabel(card.transform, SafeGet("minigame_polarbear_howto_title", "Hoe speel je?"),
            new Vector2(0f, -40f), new Vector2(840f, 80f), 54, FontStyle.Bold, new Color(0.7f, 0.92f, 1f), out _);

        MakeLabel(card.transform,
            SafeGet("minigame_polarbear_howto_intro", "De ijsbeer heeft het veel te warm! Help hem afkoelen."),
            new Vector2(0f, -150f), new Vector2(820f, 120f), 30, FontStyle.Normal, new Color(0.92f, 0.97f, 1f), out _);

        MakeHowToRow(card.transform, -300f,
            SafeGet("minigame_polarbear_howto_line1", "Blaas in de microfoon van je tablet, net als een koude wind."));
        MakeHowToRow(card.transform, -410f,
            SafeGet("minigame_polarbear_howto_line2", "Blijf blazen tot de ijsbeer helemaal is afgekoeld!"));

        var startBtn = MakeButton(card.transform, SafeGet("btn_lets_go", "Laten we beginnen!"),
            new Vector2(0f, 36f), new Vector2(520f, 120f), new Color(0.18f, 0.62f, 0.32f));
        var sbRt = startBtn.GetComponent<RectTransform>();
        sbRt.anchorMin = new Vector2(0.5f, 0f); sbRt.anchorMax = new Vector2(0.5f, 0f); sbRt.pivot = new Vector2(0.5f, 0f);
        startBtn.onClick.AddListener(() =>
        {
            if (_howToCanvas != null) Destroy(_howToCanvas);
            _howToCanvas = null;
            _started = true;
        });

        StartCoroutine(PopInCard(crt));
    }

    void MakeHowToRow(Transform parent, float y, string text)
    {
        var row = new GameObject("Row");
        row.transform.SetParent(parent, false);
        var rRt = row.AddComponent<RectTransform>();
        rRt.anchorMin = new Vector2(0.5f, 1f); rRt.anchorMax = new Vector2(0.5f, 1f);
        rRt.pivot = new Vector2(0.5f, 1f); rRt.anchoredPosition = new Vector2(0f, y); rRt.sizeDelta = new Vector2(820f, 90f);
        row.AddComponent<Image>().color = new Color(0.16f, 0.34f, 0.52f, 0.85f);

        var lObj = new GameObject("Label");
        lObj.transform.SetParent(row.transform, false);
        var lrt = lObj.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = new Vector2(20f, 0f); lrt.offsetMax = new Vector2(-20f, 0f);
        var t = lObj.AddComponent<Text>();
        t.text = text; t.font = GetFont(); t.fontSize = 28; t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter; t.color = Color.white; t.raycastTarget = false;
    }

    void BuildUI()
    {
        var cObj = new GameObject("CoolingCanvas");
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
        header.AddComponent<Image>().color = new Color(0.06f, 0.10f, 0.18f, 0.92f);

        MakeLabel(header.transform,
            SafeGet("minigame_polarbear_title", "IJsbeer Verkoeling"),
            new Vector2(0f, -16f), new Vector2(1000f, 70f), 50, FontStyle.Bold,
            new Color(0.85f, 0.95f, 1f), out _statusText);

        MakeLabel(header.transform,
            SafeGet("minigame_polarbear_instruction", "Blaas in de microfoon om de ijsbeer af te koelen!"),
            new Vector2(0f, -90f), new Vector2(1000f, 60f), 26, FontStyle.Normal,
            new Color(0.75f, 0.88f, 1f), out _instructionText);

        MakeLabel(header.transform, "0%",
            new Vector2(0f, -160f), new Vector2(400f, 50f), 32, FontStyle.Bold,
            new Color(0.55f, 0.85f, 1f), out _percentText);

        var barBg = new GameObject("BarBg");
        barBg.transform.SetParent(cObj.transform, false);
        var bgRt = barBg.AddComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0.5f, 1f); bgRt.anchorMax = new Vector2(0.5f, 1f);
        bgRt.pivot = new Vector2(0.5f, 1f);
        bgRt.anchoredPosition = new Vector2(0f, -250f);
        bgRt.sizeDelta = new Vector2(720f, 60f);
        barBg.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

        var barFillObj = new GameObject("BarFill");
        barFillObj.transform.SetParent(barBg.transform, false);
        var brt = barFillObj.AddComponent<RectTransform>();
        brt.anchorMin = new Vector2(0f, 0f); brt.anchorMax = new Vector2(0f, 1f);
        brt.pivot = new Vector2(0f, 0.5f);
        brt.anchoredPosition = new Vector2(8f, 0f);
        brt.sizeDelta = new Vector2(0f, -16f);
        _coolingBar = barFillObj.AddComponent<Image>();
        _coolingBar.color = new Color(0.50f, 0.85f, 1f);

        var stopBtn = MakeButton(cObj.transform, SafeGet("btn_back", "Stop"),
            new Vector2(30f, 30f), new Vector2(240f, 110f), new Color(0.55f, 0.18f, 0.18f));
        stopBtn.onClick.AddListener(ExitToMainArea);
    }

    void UpdateUI()
    {
        if (_coolingBar != null)
        {
            float maxWidth = 696f;
            var rt = _coolingBar.rectTransform;
            var sz = rt.sizeDelta;
            sz.x = Mathf.Lerp(rt.sizeDelta.x, maxWidth * _coolingProgress, Time.deltaTime * 6f);
            rt.sizeDelta = sz;
        }

        if (_percentText != null)
        {
            int percent = Mathf.RoundToInt(_coolingProgress * 100f);
            _percentText.text = percent + "%";
        }

        if (_statusText != null && !_completed)
        {
            string statusKey = _isBlowing ? "minigame_polarbear_blowing" : "minigame_polarbear_listening";
            string fb = _isBlowing ? "Blazen!" : "Aan het luisteren...";
            _statusText.text = SafeGet(statusKey, fb);
            _statusText.color = _isBlowing ? new Color(0.7f, 1f, 1f) : new Color(0.85f, 0.95f, 1f);
        }


        _hasStartedBefore = true;
        _completed = false;
    }

    IEnumerator CompleteSequence()
    {
        _completed = true;
        PlaytestLogger.Instance?.LogMinigameSuccess("PolarBear");
        SetWindEmission(0f);

        if (successBurstParticles != null) successBurstParticles.Play();
        if (frostParticles != null)
        {
            var em = frostParticles.emission;
            em.rateOverTime = 80f;
        }

        if (_statusText != null) _statusText.text = SafeGet("minigame_polarbear_success_title", "Lekker koel!");
        if (_instructionText != null) _instructionText.text = SafeGet("minigame_polarbear_success_desc", "De ijsbeer voelt zich weer happy!");

        if (mainCamera != null && polarBearTransform != null)
        {
            Vector3 startPos = mainCamera.transform.position;
            Vector3 toBear = (polarBearTransform.position - startPos).normalized;
            Vector3 zoomedPos = startPos + toBear * celebrationZoomDistance;
            float t = 0f;
            while (t < celebrationZoomDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / celebrationZoomDuration));
                mainCamera.transform.position = Vector3.Lerp(startPos, zoomedPos, p);
                yield return null;
            }
        }

        if (polarBearTransform != null)
        {
            float celebrateTime = 1.5f;
            float t = 0f;
            while (t < celebrateTime)
            {
                t += Time.deltaTime;
                float bob = Mathf.Sin(t * 8f) * 0.15f;
                polarBearTransform.position = _polarBearOriginPos + Vector3.up * Mathf.Abs(bob);
                yield return null;
            }
            polarBearTransform.position = _polarBearOriginPos;
        }

        DestroyMainUI();
        ShowCongrats();
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
        cImg.color = new Color(0.08f, 0.14f, 0.24f, 0.97f);

        var accent = new GameObject("Accent");
        accent.transform.SetParent(card.transform, false);
        var aRt = accent.AddComponent<RectTransform>();
        aRt.anchorMin = new Vector2(0f, 1f); aRt.anchorMax = new Vector2(1f, 1f);
        aRt.pivot = new Vector2(0.5f, 1f); aRt.anchoredPosition = Vector2.zero; aRt.sizeDelta = new Vector2(0f, 14f);
        accent.AddComponent<Image>().color = new Color(0.55f, 0.85f, 1f);

        MakeLabel(card.transform, SafeGet("minigame_complete", "Gefeliciteerd!"),
            new Vector2(0f, -55f), new Vector2(840f, 80f), 56, FontStyle.Bold, Color.white, out _);

        MakeLabel(card.transform, SafeGet("minigame_polarbear_success_title", "Lekker koel!"),
            new Vector2(0f, -150f), new Vector2(840f, 60f), 36, FontStyle.Normal, new Color(0.55f, 0.85f, 1f), out _);

        MakeLabel(card.transform,
            SafeGet("minigame_coins_earned", "Je hebt 100 munten verdiend!"),
            new Vector2(0f, -240f), new Vector2(840f, 60f), 38, FontStyle.Normal, new Color(0.35f, 1f, 0.55f), out _);

        MakeLabel(card.transform,
            SafeGet("minigame_polarbear_success_desc", "De ijsbeer voelt zich weer happy!"),
            new Vector2(0f, -310f), new Vector2(840f, 50f), 26, FontStyle.Normal, new Color(0.75f, 0.88f, 1f), out _);

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
    void ExitToMainArea()
    {
        if (!_completed)
        {
            PlaytestLogger.Instance?.LogMinigameFail(
                "PolarBear",
                "Player exited before completion"
            );
        }

        SceneManager.LoadScene(returnSceneName);
    }

    void DestroyMainUI()
    {
        if (_uiCanvas != null) Destroy(_uiCanvas.gameObject);
        _uiCanvas = null;
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