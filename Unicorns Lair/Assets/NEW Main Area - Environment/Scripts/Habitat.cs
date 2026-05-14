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

    private enum HabitatState { NotPlaced, Building, Built }
    private HabitatState _state;

    private Canvas _worldCanvas;
    private GameObject _buttonObj;
    private Text _buttonLabel;
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
        rt.sizeDelta = new Vector2(420f, 160f);

        var img = _buttonObj.AddComponent<Image>();
        img.color = new Color(0.12f, 0.68f, 0.34f);

        var btn = _buttonObj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.colors = new ColorBlock
        {
            normalColor = new Color(0.12f, 0.68f, 0.34f),
            highlightedColor = new Color(0.20f, 0.85f, 0.46f),
            pressedColor = new Color(0.07f, 0.46f, 0.22f),
            selectedColor = new Color(0.12f, 0.68f, 0.34f),
            disabledColor = new Color(0.35f, 0.35f, 0.35f),
            colorMultiplier = 1f,
            fadeDuration = 0.1f
        };
        btn.onClick.AddListener(OnButtonPressed);

        var lblObj = new GameObject("Label");
        lblObj.transform.SetParent(_buttonObj.transform, false);
        var lrt = lblObj.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = new Vector2(12f, 6f); lrt.offsetMax = new Vector2(-12f, -6f);
        _buttonLabel = lblObj.AddComponent<Text>();
        _buttonLabel.font = GetFont();
        _buttonLabel.fontSize = 40;
        _buttonLabel.fontStyle = FontStyle.Bold;
        _buttonLabel.alignment = TextAnchor.MiddleCenter;
        _buttonLabel.color = Color.white;
        _buttonLabel.raycastTarget = false;
        lblObj.AddComponent<Outline>().effectColor = new Color(0f, 0.2f, 0.1f, 0.7f);

        StartCoroutine(TrackWorldPosition(rt));
        ApplyBuildModeVisibility();
    }

    void DestroyWorldButton()
    {
        if (_worldCanvas != null) { Destroy(_worldCanvas.gameObject); _worldCanvas = null; }
        _buttonObj = null;
        _buttonLabel = null;
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
        if (_buttonLabel == null) return;
        if (_state != HabitatState.NotPlaced) return;

        var lm = LanguageManager.Instance;
        string animalName = lm != null ? lm.Get(nameKey) : nameKey;
        if (animalName == $"[{nameKey}]") animalName = nameKey;

        string priceLine = lm != null ? lm.Get("shop_currency_short", cost) : cost.ToString();
        if (priceLine == "[shop_currency_short]") priceLine = cost + " coins";

        _buttonLabel.text = $"{animalName}\n{priceLine}";
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