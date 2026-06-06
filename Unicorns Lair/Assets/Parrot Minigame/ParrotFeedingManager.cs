using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ParrotFeedingManager : MonoBehaviour
{
    private enum ParrotFeedingState
    {
        WaitingToStart,
        Playing,
        Checking,
        Complete
    }

    [Header("References")]
    [SerializeField] private ParrotTiltPourInput tiltInput;
    [SerializeField] private FeedingTray feedingTray;

    [Header("Scene Objects")]
    [SerializeField] private Transform seedSack;
    [SerializeField] private Transform seedSpawnPoint;
    [SerializeField] private GameObject seedPrefab;
    [SerializeField] private Transform parrotVisual;

    [Header("Sack Movement")]
    [SerializeField] private float moveSpeed = 2.2f;
    [SerializeField] private float moveRange = 3.2f;
    [Tooltip("Direction the sack travels (and bounces back along). Set to (-1,0,0) to flip, or (0,0,1) to move along Z, etc.")]
    [SerializeField] private Vector3 moveDirection = Vector3.right;

    [Header("Pour Target (win zone)")]
    [Tooltip("Place an empty above the correct spot (over the tray). Pouring while the sack is near this point fills the tray. If left empty, every poured seed counts.")]
    [SerializeField] private Transform pourTarget;
    [Tooltip("How close (horizontal world distance) the pour must be to the target to count.")]
    [SerializeField] private float pourTargetRadius = 1.5f;

    [Header("Seeds")]
    [SerializeField] private int maxSeedsToDrop = 25;
    [SerializeField] private float seedSpawnInterval = 0.08f;
    [SerializeField] private float seedSpread = 0.45f;
    [Tooltip("Each seed is uniformly scaled so its largest dimension equals this many world units. Lower if seeds look too big.")]
    [SerializeField] private float seedSize = 0.25f;
    [SerializeField] private float seedFallSpeed = 5f;
    [SerializeField] private float seedLifeTime = 4f;

    [Header("Timing")]
    [SerializeField] private float maxDuration = 20f;
    [SerializeField] private float finalSeedCheckDelay = 2f;

    [Header("Settings")]
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

    [Header("How To Play Voice")]
    [SerializeField] private LocalizedSoundData howToPlayLocalized;

    [Header("How To Play - Lets Go Button (PNG)")]
    [SerializeField] private Sprite letsGoButtonSprite;
    [SerializeField] private Vector2 letsGoButtonPos = new Vector2(0f, -760f);
    [SerializeField] private Vector2 letsGoButtonSize = new Vector2(480f, 170f);

    [Header("How To Play - Background")]
    [Range(0f, 1f)]
    [SerializeField] private float howToDimOpacity = 0.78f;

    [Header("Reward")]
    [Tooltip("How many coins the player earns when they win.")]
    [SerializeField] private int coinReward = 10;
    [Tooltip("Name of the scene to load when the minigame finishes or is closed.")]
    [SerializeField] private string returnSceneName = "MainArea";

    private ParrotFeedingState currentState;
    private Vector3 sackStartPosition;
    private float direction = 1f;
    private Vector3 _moveDirNorm = Vector3.right;
    private float seedSpawnTimer;
    private float gameTimer;
    private bool hasPouredAtLeastOnce;
    private int seedsDropped;

    private class ActiveSeed { public Transform tr; public float life; }
    private readonly List<ActiveSeed> _activeSeeds = new();

    private Vector3 _parrotBaseLocalPos;
    private bool _parrotBaseCaptured;

    private Canvas _uiCanvas;
    private Text _titleText;
    private Text _instructionText;
    private Text _feedbackText;
    private GameObject _howToCanvas;
    private (string key, string fallback)[] _htLines;
    private int _htPage;
    private int _htLineCount;
    private Text _htText;
    private Image _htImage;
    private GameObject _htTapIndicator;
    private Button _htLetsGoBtn;
    private GameObject _congratsCanvas;

    private void Start()
    {
        LanguageManager.Ensure();
        GameStateManager.Ensure();

        if (tiltInput == null) tiltInput = FindFirstObjectByType<ParrotTiltPourInput>();
        if (feedingTray == null) feedingTray = FindFirstObjectByType<FeedingTray>();
        if (seedSack != null) sackStartPosition = seedSack.position;

        if (parrotVisual != null)
        {
            _parrotBaseLocalPos = parrotVisual.localPosition;
            _parrotBaseCaptured = true;
        }

        _moveDirNorm = moveDirection.sqrMagnitude > 0.0001f ? moveDirection.normalized : Vector3.right;

        currentState = ParrotFeedingState.WaitingToStart;

        BuildUI();

        if (showHowToPlay) ShowHowToPlay();
        else StartMinigame();

        LanguageManager.Instance.LanguageChanged += OnLanguageChanged;
    }

    private void OnDestroy()
    {
        if (LanguageManager.Instance != null)
            LanguageManager.Instance.LanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged() => RefreshStaticTexts();

    private void Update()
    {
        UpdateSeeds();

        if (currentState != ParrotFeedingState.Playing)
            return;

        gameTimer += Time.deltaTime;

        bool isPouring = tiltInput != null && tiltInput.IsPouring;

        if (isPouring && seedsDropped < maxSeedsToDrop)
        {
            hasPouredAtLeastOnce = true;
            SetInstruction(IsPourOnTarget()
                ? SafeGet("minigame_parrot_pour", "Strooien!")
                : SafeGet("minigame_parrot_aim", "Mik op de bak!"));
            SpawnSeedsOverTime();
        }
        else
        {
            if (seedsDropped >= maxSeedsToDrop)
                SetInstruction(SafeGet("minigame_parrot_no_seeds", "Geen zaadjes meer!"));
            else
                SetInstruction(SafeGet("minigame_parrot_instruction", "Kantel om zaadjes te strooien!"));

            MoveSack();
        }

        AnimateParrot();

        if (feedingTray != null && feedingTray.IsFull)
        {
            CompleteMinigame();
            return;
        }

        if (seedsDropped >= maxSeedsToDrop && feedingTray != null && !feedingTray.IsFull)
        {
            StartCoroutine(CheckFinalSeedsAfterDelay());
            return;
        }

        if (gameTimer >= maxDuration)
        {
            if (!hasPouredAtLeastOnce)
                StartCoroutine(HandleFailure(SafeGet("minigame_parrot_retry_tilt", "Probeer opnieuw! Kantel de tablet!")));
            else
                StartCoroutine(CheckFinalSeedsAfterDelay());

            return;
        }
    }

    public void StartMinigame()
    {
        currentState = ParrotFeedingState.Playing;

        gameTimer = 0f;
        seedSpawnTimer = 0f;
        hasPouredAtLeastOnce = false;
        seedsDropped = 0;

        if (feedingTray != null) feedingTray.ResetTray();
        if (seedSack != null) seedSack.position = sackStartPosition;

        for (int i = _activeSeeds.Count - 1; i >= 0; i--)
            if (_activeSeeds[i].tr != null) Destroy(_activeSeeds[i].tr.gameObject);
        _activeSeeds.Clear();

        SetInstruction(SafeGet("minigame_parrot_instruction", "Kantel om zaadjes te strooien!"));
        SetFeedback("");
    }

    private void MoveSack()
    {
        if (seedSack == null) return;

        seedSack.position += _moveDirNorm * direction * moveSpeed * Time.deltaTime;

        float dist = Vector3.Dot(seedSack.position - sackStartPosition, _moveDirNorm);

        if (Mathf.Abs(dist) >= moveRange)
        {
            direction *= -1f;
            seedSack.position = sackStartPosition + _moveDirNorm * (Mathf.Sign(dist) * moveRange);
        }
    }

    private void SpawnSeedsOverTime()
    {
        seedSpawnTimer -= Time.deltaTime;
        if (seedSpawnTimer > 0f) return;
        seedSpawnTimer = seedSpawnInterval;
        SpawnSeed();
    }

    private void SpawnSeed()
    {
        if (seedPrefab == null || seedSpawnPoint == null) return;
        if (seedsDropped >= maxSeedsToDrop) return;

        Vector3 spawnPos = seedSpawnPoint.position;
        spawnPos.x += Random.Range(-seedSpread, seedSpread);
        spawnPos.z += Random.Range(-seedSpread * 0.35f, seedSpread * 0.35f);

        var seed = Instantiate(seedPrefab, spawnPos, Quaternion.identity);
        NormalizeSeedSize(seed, seedSize);
        _activeSeeds.Add(new ActiveSeed { tr = seed.transform, life = seedLifeTime });
        seedsDropped++;

        // Counting is proximity-based: a poured seed fills the tray only if the
        // sack is near the target (or always, if no target is assigned).
        if (feedingTray != null && IsPourOnTarget())
            feedingTray.NotifySeedLanded();
    }

    private bool IsPourOnTarget()
    {
        if (pourTarget == null) return true;
        Vector3 from = seedSpawnPoint != null ? seedSpawnPoint.position : transform.position;
        float dx = from.x - pourTarget.position.x;
        float dz = from.z - pourTarget.position.z;
        return Mathf.Sqrt(dx * dx + dz * dz) <= pourTargetRadius;
    }

    private void UpdateSeeds()
    {
        if (_activeSeeds.Count == 0) return;

        for (int i = _activeSeeds.Count - 1; i >= 0; i--)
        {
            var s = _activeSeeds[i];
            if (s.tr == null) { _activeSeeds.RemoveAt(i); continue; }

            s.tr.position += Vector3.down * seedFallSpeed * Time.deltaTime;
            s.life -= Time.deltaTime;

            if (s.life <= 0f)
            {
                Destroy(s.tr.gameObject);
                _activeSeeds.RemoveAt(i);
            }
        }
    }

    private void NormalizeSeedSize(GameObject obj, float targetSize)
    {
        if (obj == null || targetSize <= 0f) return;

        var renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Quaternion originalRot = obj.transform.rotation;
        obj.transform.rotation = Quaternion.identity;

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            b.Encapsulate(renderers[i].bounds);

        obj.transform.rotation = originalRot;

        float largest = Mathf.Max(b.size.x, b.size.y, b.size.z);
        if (largest <= 0.0001f) return;

        obj.transform.localScale *= targetSize / largest;
    }

    private void AnimateParrot()
    {
        if (parrotVisual == null || feedingTray == null) return;
        if (!_parrotBaseCaptured) return;

        float excitement = feedingTray.FillPercent;
        float bounce = Mathf.Sin(Time.time * 8f) * 0.06f * excitement;

        Vector3 pos = _parrotBaseLocalPos;
        pos.y = _parrotBaseLocalPos.y + bounce;
        parrotVisual.localPosition = pos;
    }

    private void CompleteMinigame()
    {
        if (currentState == ParrotFeedingState.Complete) return;
        currentState = ParrotFeedingState.Complete;

        SetInstruction("");
        SetFeedback(SafeGet("minigame_complete", "Gefeliciteerd!"));

        StartCoroutine(WinSequence());
    }

    private IEnumerator WinSequence()
    {
        yield return StartCoroutine(ParrotHappySequence());
        DestroyMainUI();
        ShowCongrats();
    }

    private IEnumerator CheckFinalSeedsAfterDelay()
    {
        if (currentState == ParrotFeedingState.Complete || currentState == ParrotFeedingState.Checking)
            yield break;

        currentState = ParrotFeedingState.Checking;
        SetInstruction(SafeGet("minigame_parrot_checking", "Zaadjes tellen..."));

        yield return new WaitForSeconds(finalSeedCheckDelay);

        if (feedingTray != null && feedingTray.IsFull)
        {
            CompleteMinigame();
        }
        else
        {
            SetInstruction("");
            SetFeedback(SafeGet("minigame_parrot_retry_aim", "Probeer opnieuw! Mik op de bak!"));
            yield return new WaitForSeconds(2f);
            StartMinigame();
        }
    }

    private IEnumerator HandleFailure(string message)
    {
        if (currentState == ParrotFeedingState.Complete || currentState == ParrotFeedingState.Checking)
            yield break;

        currentState = ParrotFeedingState.Checking;
        SetInstruction("");
        SetFeedback(message);

        yield return new WaitForSeconds(2f);
        StartMinigame();
    }

    private IEnumerator ParrotHappySequence()
    {
        if (parrotVisual == null) yield break;

        Vector3 baseScale = parrotVisual.localScale;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime;
            float s = 1f + Mathf.Sin(t * 25f) * 0.08f;
            parrotVisual.localScale = baseScale * s;
            yield return null;
        }
        parrotVisual.localScale = baseScale;
    }

    private void OnDrawGizmosSelected()
    {
        if (pourTarget == null) return;
        Gizmos.color = new Color(0.4f, 0.9f, 0.4f, 0.9f);
        Gizmos.DrawWireSphere(pourTarget.position, pourTargetRadius);
    }

    // ---------- UI ----------

    private void BuildUI()
    {
        var cObj = new GameObject("ParrotCanvas");
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
        header.AddComponent<Image>().color = new Color(0.10f, 0.13f, 0.08f, 0.92f);

        MakeLabel(header.transform,
            SafeGet("minigame_parrot_title", "Papegaai Voeren"),
            new Vector2(0f, -16f), new Vector2(1000f, 70f), 50, FontStyle.Bold,
            new Color(0.7f, 1f, 0.75f), out _titleText);

        MakeLabel(header.transform,
            SafeGet("minigame_parrot_instruction", "Kantel om zaadjes te strooien!"),
            new Vector2(0f, -100f), new Vector2(1000f, 60f), 30, FontStyle.Normal,
            new Color(0.92f, 1f, 0.9f), out _instructionText);

        MakeLabel(header.transform, "",
            new Vector2(0f, -170f), new Vector2(1000f, 50f), 30, FontStyle.Bold,
            Color.white, out _feedbackText);

        var stopBtn = MakeSpriteButton(cObj.transform, backButtonSprite, null, backButtonPos, backButtonSize);
        stopBtn.onClick.AddListener(ExitToMainArea);
    }

    private void ShowHowToPlay()
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
            ("minigame_parrot_howto_intro", "De papegaai heeft honger! Vul de bak met lekkere zaadjes."),
            ("minigame_parrot_howto_line1", "Kantel je tablet om zaadjes uit de zak te strooien."),
            ("minigame_parrot_howto_line2", "Mik de zaadjes in de bak eronder om hem te vullen!"),
        };
        _htLineCount = _htLines.Length;
        _htPage = 0;

        ShowHowToPage(0);
    }

    private void AdvanceHowTo()
    {
        if (_htLines == null) return;
        if (_htPage >= _htLineCount - 1) return;
        ShowHowToPage(_htPage + 1);
    }

    private void ShowHowToPage(int index)
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

    private void ShowCongrats()
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
        accent.AddComponent<Image>().color = new Color(0.5f, 0.9f, 0.4f);

        MakeLabel(card.transform, SafeGet("minigame_complete", "Gefeliciteerd!"),
            new Vector2(0f, -55f), new Vector2(840f, 80f), 56, FontStyle.Bold, Color.white, out _);

        MakeLabel(card.transform, SafeGet("minigame_parrot_success_title", "Lekkere zaadjes!"),
            new Vector2(0f, -150f), new Vector2(840f, 60f), 36, FontStyle.Normal, Color.white, out _);

        MakeLabel(card.transform,
            SafeGet("minigame_coins_earned", $"Je hebt {coinReward} munten verdiend!"),
            new Vector2(0f, -240f), new Vector2(840f, 60f), 38, FontStyle.Normal, Color.white, out _);

        MakeLabel(card.transform,
            SafeGet("minigame_parrot_success_desc", "De buik van de papegaai is vol en blij!"),
            new Vector2(0f, -310f), new Vector2(840f, 50f), 26, FontStyle.Normal, Color.white, out _);

        var continueBtn = MakeSpriteButton(card.transform, continueButtonSprite, SafeGet("btn_continue", "Doorgaan"), continueButtonPos, continueButtonSize);
        var cbRt = continueBtn.GetComponent<RectTransform>();
        cbRt.anchorMin = new Vector2(0.5f, 0f); cbRt.anchorMax = new Vector2(0.5f, 0f); cbRt.pivot = new Vector2(0.5f, 0f);
        continueBtn.onClick.AddListener(OnContinue);

        StartCoroutine(PopInCard(crt));
    }

    private IEnumerator PopInCard(RectTransform rt)
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

    private void OnContinue()
    {
        GameStateManager.Instance.AddCoins(coinReward);
        SceneManager.LoadScene(returnSceneName);
    }

    private void ExitToMainArea() => SceneManager.LoadScene(returnSceneName);

    private void DestroyMainUI()
    {
        if (_uiCanvas != null) Destroy(_uiCanvas.gameObject);
        _uiCanvas = null;
    }

    private void RefreshStaticTexts()
    {
        if (_titleText != null) _titleText.text = SafeGet("minigame_parrot_title", "Papegaai Voeren");
        // Instruction/feedback are dynamic; the next Update frame re-applies the right text.
    }

    private void SetInstruction(string text)
    {
        if (_instructionText != null) _instructionText.text = text;
    }

    private void SetFeedback(string text)
    {
        if (_feedbackText != null) _feedbackText.text = text;
    }

    private void MakeLabel(Transform parent, string text, Vector2 pos, Vector2 size, int fontSize, FontStyle style, Color color, out Text refOut)
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

    private Button MakeSpriteButton(Transform parent, Sprite sprite, string label, Vector2 pos, Vector2 size)
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

    private Button MakeButton(Transform parent, string label, Vector2 pos, Vector2 size, Color color)
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

    private string SafeGet(string key, string fallback)
    {
        var lm = LanguageManager.Instance;
        if (lm == null) return fallback;
        var result = lm.Get(key);
        return result == $"[{key}]" ? fallback : result;
    }

    private void EnsureEventSystem()
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