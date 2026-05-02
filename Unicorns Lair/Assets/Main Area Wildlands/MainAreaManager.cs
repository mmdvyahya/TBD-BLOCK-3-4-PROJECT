using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class MainAreaManager : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private GameObject habitatObject;
    [SerializeField] private Transform beaverBuyAnchor;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private HabitatInspectionManager inspectionManager;
    [SerializeField] private BeaverBalanceMinigame beaverMinigame;

    [Header("Habitat Settings")]
    [SerializeField] private string habitatItemId = "beaver_habitat";
    [SerializeField] private int habitatCost = 100;

    [Header("Post Build")]
    [SerializeField] private Sprite tapIconSprite;

    private static readonly Vector3 HabitatCamPos = new Vector3(1167.438f, 51.58662f, 1195.976f);
    private static readonly Quaternion HabitatCamRot = Quaternion.Euler(52.977f, 0.239f, -0.001f);

    private enum SceneState { Idle, WaitingForBuy, WaitingForPlace, Building, Complete, Inspecting }

    private SceneState _state = SceneState.Idle;
    private CameraSwipe _cameraSwipe;
    private Vector3 _originalCamPos;
    private Quaternion _originalCamRot;

    private Canvas _worldCanvas;
    private GameObject _buyButtonObj;
    private Text _buyButtonLabel;
    private HabitatBuilder _builder;
    private Text _coinLabel;

    private bool _glowActive;
    private bool _postBuildSpawned;
    private GameObject _backButtonCanvas;
    private InspectableHabitat _currentHabitat;

    void Update()
    {
        if (_state != SceneState.Complete || !_glowActive) return;

        bool clicked = false;
        Vector2 screenPos = Vector2.zero;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            clicked = true;
            screenPos = Mouse.current.position.ReadValue();
        }
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            clicked = true;
            screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
        }

        if (!clicked) return;

        Ray ray = mainCamera.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 9999f))
        {
            Debug.Log($"[MainAreaManager] Hit: {hit.collider.gameObject.name}");
            var habitat = hit.collider.GetComponentInParent<InspectableHabitat>()
                       ?? hit.collider.GetComponent<InspectableHabitat>();
            if (habitat != null)
                StartCoroutine(EnterInspection(habitat));
        }
    }

    void Start()
    {
        GameStateManager.Ensure();
        LanguageManager.Ensure();

        if (mainCamera == null) mainCamera = Camera.main;
        _originalCamPos = mainCamera.transform.position;
        _originalCamRot = mainCamera.transform.rotation;
        _cameraSwipe = mainCamera.GetComponent<CameraSwipe>();

        EnsureEventSystem();
        SetupReferences();
        SetupCoinDisplay();
        SetupBuilder();
        SetState(DetermineInitialState());

        GameStateManager.Instance.CoinsChanged += OnCoinsChanged;
        LanguageManager.Instance.LanguageChanged += OnLanguageChanged;
    }

    void OnDestroy()
    {
        if (GameStateManager.Instance != null) GameStateManager.Instance.CoinsChanged -= OnCoinsChanged;
        if (LanguageManager.Instance != null) LanguageManager.Instance.LanguageChanged -= OnLanguageChanged;
    }

    void SetupReferences()
    {
        if (habitatObject == null)
            habitatObject = GameObject.Find("Merged Animal Habitat Beever with tree trunks");
        if (habitatObject == null) { Debug.LogError("[MainAreaManager] Habitat object not found!"); return; }

        habitatObject.SetActive(true);

        if (beaverBuyAnchor == null)
        {
            var g = GameObject.Find("BeaverBuy");
            if (g != null) beaverBuyAnchor = g.transform;
        }
    }

    void SetupBuilder()
    {
        if (habitatObject == null) return;
        _builder = habitatObject.GetComponent<HabitatBuilder>();
        if (_builder == null) _builder = habitatObject.AddComponent<HabitatBuilder>();
        _builder.BuildComplete += OnBuildComplete;
    }

    SceneState DetermineInitialState()
    {
        if (GameStateManager.Instance.IsBuilt(habitatItemId)) return SceneState.Complete;
        if (GameStateManager.Instance.IsBought(habitatItemId)) return SceneState.WaitingForPlace;
        return SceneState.WaitingForBuy;
    }

    void SetState(SceneState next)
    {
        _state = next;

        switch (_state)
        {
            case SceneState.WaitingForBuy:
                _builder?.HideAll();
                SpawnWorldButton();
                UpdateButtonLabel();
                break;

            case SceneState.WaitingForPlace:
                _builder?.HideAll();
                SpawnWorldButton();
                UpdateButtonLabel();
                break;

            case SceneState.Building:
                SetBuyButtonActive(false);
                _builder?.StartBuild();
                break;

            case SceneState.Complete:
                SetBuyButtonActive(false);
                _builder?.ShowAll();
                if (!_postBuildSpawned)
                {
                    _postBuildSpawned = true;
                    StartCoroutine(StartPostBuildEffects());
                }
                break;

            case SceneState.Inspecting:
                SetBuyButtonActive(false);
                break;
        }
    }

    void OnBuildComplete(string id) => SetState(SceneState.Complete);
    void OnCoinsChanged(int amount) => UpdateCoinDisplay(amount);
    void OnLanguageChanged() { UpdateButtonLabel(); UpdateCoinDisplay(GameStateManager.Instance.Coins); }

    public void NotifyHabitatTapped(InspectableHabitat habitat)
    {
        if (_state != SceneState.Complete || habitat == null) return;
        StartCoroutine(EnterInspection(habitat));
    }

    public void NotifyExitInspection()
    {
        if (_state != SceneState.Inspecting) return;
        StartCoroutine(ExitInspection());
    }

    public void NotifyMinigameComplete()
    {
        if (_currentHabitat != null)
            ShowInspectionUI(_currentHabitat);
    }

    IEnumerator EnterInspection(InspectableHabitat habitat)
    {
        SetState(SceneState.Inspecting);
        _currentHabitat = habitat;
        HideGlow();
        if (_cameraSwipe != null) _cameraSwipe.enabled = false;

        Vector3 startPos = mainCamera.transform.position;
        Quaternion startRot = mainCamera.transform.rotation;
        float t = 0f;
        while (t < 1.1f)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 1.1f));
            mainCamera.transform.position = Vector3.Lerp(startPos, HabitatCamPos, p);
            mainCamera.transform.rotation = Quaternion.Slerp(startRot, HabitatCamRot, p);
            yield return null;
        }
        mainCamera.transform.position = HabitatCamPos;
        mainCamera.transform.rotation = HabitatCamRot;

        ShowInspectionUI(habitat);
    }

    IEnumerator ExitInspection()
    {
        HideInspectionUI();

        if (inspectionManager != null) inspectionManager.CloseInspectionMode();

        Vector3 startPos = mainCamera.transform.position;
        Quaternion startRot = mainCamera.transform.rotation;
        float t = 0f;
        while (t < 1.0f)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 1.0f));
            mainCamera.transform.position = Vector3.Lerp(startPos, _originalCamPos, p);
            mainCamera.transform.rotation = Quaternion.Slerp(startRot, _originalCamRot, p);
            yield return null;
        }
        mainCamera.transform.position = _originalCamPos;
        mainCamera.transform.rotation = _originalCamRot;

        if (_cameraSwipe != null) _cameraSwipe.enabled = true;
        SetState(SceneState.Complete);
    }

    IEnumerator ReturnToCardView(System.Action onDone)
    {
        if (inspectionManager != null) inspectionManager.CloseInspectionMode();

        Vector3 startPos = mainCamera.transform.position;
        Quaternion startRot = mainCamera.transform.rotation;
        float t = 0f;
        while (t < 0.7f)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.7f));
            mainCamera.transform.position = Vector3.Lerp(startPos, HabitatCamPos, p);
            mainCamera.transform.rotation = Quaternion.Slerp(startRot, HabitatCamRot, p);
            yield return null;
        }
        mainCamera.transform.position = HabitatCamPos;
        mainCamera.transform.rotation = HabitatCamRot;

        onDone?.Invoke();
    }

    void ShowInspectionUI(InspectableHabitat habitat)
    {
        var canvasObj = new GameObject("InspectionCanvas");
        _backButtonCanvas = canvasObj;
        var cv = canvasObj.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 10;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        var lm = LanguageManager.Instance;

        string animalName = SafeGet(lm, habitat?.AnimalNameKey, "Bever Verblijf");
        string desc = SafeGet(lm, habitat?.HabitatDescriptionKey, "Een verblijf voor bevers.");
        string fact = SafeGet(lm, habitat?.EducationalFactKey, "Bevers bouwen dammen van takken!");
        string hintText = SafeGet(lm, "inspect_hint", "Kantel tablet om rond te kijken");
        string backText = SafeGet(lm, "btn_back", "◀  Terug");
        string inspText = SafeGet(lm, "btn_inspect", "🔍  Inspecteren");

        var card = new GameObject("InfoCard");
        card.transform.SetParent(canvasObj.transform, false);
        var cardRt = card.AddComponent<RectTransform>();
        cardRt.anchorMin = new Vector2(0.5f, 1f);
        cardRt.anchorMax = new Vector2(0.5f, 1f);
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

        var nameText = MakeCardLabelSafe(card.transform, habitat?.AnimalNameKey, "Bever Verblijf", 52, FontStyle.Bold, Color.white, new Vector2(0f, -24f), new Vector2(920f, 70f));
        var descText = MakeCardLabelSafe(card.transform, habitat?.HabitatDescriptionKey, "Een verblijf voor bevers.", 32, FontStyle.Normal, new Color(0.80f, 0.92f, 1f), new Vector2(0f, -102f), new Vector2(920f, 56f));
        var factText = MakeCardLabelSafe(card.transform, habitat?.EducationalFactKey, "Bevers bouwen dammen van takken!", 26, FontStyle.Normal, new Color(1f, 0.88f, 0.55f), new Vector2(0f, -160f), new Vector2(920f, 48f));
        var hintTextObj = MakeCardLabelSafe(card.transform, "inspect_hint", "Kantel tablet om rond te kijken", 22, FontStyle.Normal, new Color(0.55f, 0.65f, 0.75f), new Vector2(0f, -220f), new Vector2(920f, 36f));

        LanguageManager.OnLanguageChanged refreshCardTexts = () =>
        {
            var lmNow = LanguageManager.Instance;
            if (nameText != null) nameText.text = SafeGet(lmNow, habitat?.AnimalNameKey, "Bever Verblijf");
            if (descText != null) descText.text = SafeGet(lmNow, habitat?.HabitatDescriptionKey, "Een verblijf voor bevers.");
            if (factText != null) factText.text = SafeGet(lmNow, habitat?.EducationalFactKey, "Bevers bouwen dammen van takken!");
            if (hintTextObj != null) hintTextObj.text = SafeGet(lmNow, "inspect_hint", "Kantel tablet om rond te kijken");
        };

        if (LanguageManager.Instance != null)
            LanguageManager.Instance.LanguageChanged += refreshCardTexts;

        canvasObj.GetComponent<Canvas>();
        var destroyer = canvasObj.AddComponent<OnDestroyCallback>();
        destroyer.OnDestroyAction = () =>
        {
            if (LanguageManager.Instance != null)
                LanguageManager.Instance.LanguageChanged -= refreshCardTexts;
        };

        var backBtn = MakeButton(canvasObj.transform, backText, new Vector2(30f, 30f),
            new Vector2(220f, 120f), new Color(0.55f, 0.18f, 0.18f),
            new Vector2(0f, 0f), new Vector2(0f, 0f));

        var inspBtn = MakeButton(canvasObj.transform, inspText, new Vector2(265f, 30f),
            new Vector2(340f, 120f), new Color(0.12f, 0.48f, 0.78f),
            new Vector2(0f, 0f), new Vector2(0f, 0f));

        string minigameText = SafeGet(LanguageManager.Instance, "btn_minigame", "🎮  Minigame");
        var minigameBtn = MakeButton(canvasObj.transform, minigameText, new Vector2(620f, 30f),
            new Vector2(320f, 120f), new Color(0.55f, 0.18f, 0.65f),
            new Vector2(0f, 0f), new Vector2(0f, 0f));

        var backBtnComp = backBtn.GetComponent<Button>();

        backBtnComp.onClick.AddListener(NotifyExitInspection);

        inspBtn.GetComponent<Button>().onClick.AddListener(() =>
        {
            if (inspectionManager == null)
                inspectionManager = FindFirstObjectByType<HabitatInspectionManager>();

            if (inspectionManager == null || habitat == null)
            {
                Debug.LogError("[Inspect] Missing reference — assign HabitatInspectionManager in Inspector!");
                return;
            }

            inspectionManager.OpenMinigame(habitat);
            card.SetActive(false);
            inspBtn.SetActive(false);
            minigameBtn.SetActive(false);

            backBtnComp.onClick.RemoveAllListeners();
            backBtnComp.onClick.AddListener(() =>
            {
                backBtnComp.interactable = false;
                StartCoroutine(ReturnToCardView(() =>
                {
                    card.SetActive(true);
                    inspBtn.SetActive(true);
                    minigameBtn.SetActive(true);
                    backBtnComp.interactable = true;
                    backBtnComp.onClick.RemoveAllListeners();
                    backBtnComp.onClick.AddListener(NotifyExitInspection);
                }));
            });
        });

        minigameBtn.GetComponent<Button>().onClick.AddListener(() =>
        {
            if (beaverMinigame == null)
                beaverMinigame = FindFirstObjectByType<BeaverBalanceMinigame>();

            if (beaverMinigame != null)
            {
                HideInspectionUI();
                beaverMinigame.OpenMinigame();
            }
            else
                Debug.LogError("[Minigame] Assign BeaverBalanceMinigame in Inspector on _Manager!");
        });
    }

    static string SafeGet(LanguageManager lm, string key, string fallback)
    {
        if (lm == null || string.IsNullOrEmpty(key)) return fallback;
        var result = lm.Get(key);
        if (result == $"[{key}]")
        {
            Debug.LogWarning($"[SafeGet] Key not found in LanguageManager: '{key}' — update your LanguageManager.cs!");
            return fallback;
        }
        return result;
    }

    GameObject MakeButton(Transform parent, string label, Vector2 pos, Vector2 size,
                          Color col, Vector2 anchorMin, Vector2 anchorMax)
    {
        var obj = new GameObject("Btn");
        obj.transform.SetParent(parent, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var img = obj.AddComponent<Image>();
        img.color = col;

        var btn = obj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.colors = new ColorBlock
        {
            normalColor = col,
            highlightedColor = col * 1.2f,
            pressedColor = col * 0.75f,
            selectedColor = col,
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
        txt.text = label; txt.font = GetFont(); txt.fontSize = 42;
        txt.fontStyle = FontStyle.Bold; txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white; txt.raycastTarget = false;

        return obj;
    }

    void MakeCardLabel(Transform parent, string text, int size, FontStyle style, Color color, Vector2 pos, Vector2 sz)
    {
        var obj = new GameObject("Label");
        obj.transform.SetParent(parent, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f); rt.anchoredPosition = pos; rt.sizeDelta = sz;
        var t = obj.AddComponent<Text>();
        t.text = text; t.font = GetFont(); t.fontSize = size; t.fontStyle = style;
        t.alignment = TextAnchor.MiddleCenter; t.color = color; t.raycastTarget = false;
    }

    Text MakeCardLabelSafe(Transform parent, string key, string fallback, int size, FontStyle style, Color color, Vector2 pos, Vector2 sz)
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

    void MakeCardLabelLocalized(Transform parent, string key, string fallback, int size, FontStyle style, Color color, Vector2 pos, Vector2 sz)
    {
        var t = MakeCardLabelSafe(parent, key, fallback, size, style, color, pos, sz);
        if (!string.IsNullOrEmpty(key))
        {
            var loc = t.gameObject.AddComponent<LocalizedText>();
            loc.key = key;
        }
    }

    void HideInspectionUI()
    {
        if (_backButtonCanvas != null) { Destroy(_backButtonCanvas); _backButtonCanvas = null; }
    }

    IEnumerator StartPostBuildEffects()
    {
        yield return new WaitForSeconds(0.6f);
        SpawnGlow();
        SpawnTapIcon();
    }

    private Light _glowLight;
    private GameObject _outlineRoot;
    private Material _outlineMat;
    private Canvas _postBuildCanvas;
    private GameObject _tapIconObj;

    void SpawnGlow()
    {
        if (habitatObject == null) return;
        _glowActive = true;
        var bounds = GetHabitatBounds();

        var lightObj = new GameObject("HabitatGlowLight");
        lightObj.transform.position = bounds.center;
        _glowLight = lightObj.AddComponent<Light>();
        _glowLight.type = LightType.Point;
        _glowLight.color = new Color(1f, 0.88f, 0.35f);
        _glowLight.intensity = 0f;
        _glowLight.range = bounds.extents.magnitude * 3.5f;

        _outlineRoot = new GameObject("HabitatOutline");
        _outlineRoot.transform.SetParent(habitatObject.transform.parent);
        var sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
        _outlineMat = new Material(sh);
        Color outlineCol = new Color(1f, 0.92f, 0.25f, 1f);
        _outlineMat.color = outlineCol;
        if (_outlineMat.HasProperty("_BaseColor")) _outlineMat.SetColor("_BaseColor", outlineCol);

        foreach (var mf in habitatObject.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.sharedMesh == null) continue;
            var copy = new GameObject($"Outline_{mf.gameObject.name}");
            copy.transform.SetParent(_outlineRoot.transform);
            copy.transform.position = mf.transform.position;
            copy.transform.rotation = mf.transform.rotation;
            copy.transform.localScale = mf.transform.lossyScale * 1.04f;
            var mr = copy.AddComponent<MeshRenderer>();
            var cmf = copy.AddComponent<MeshFilter>();
            cmf.sharedMesh = mf.sharedMesh;
            mr.material = _outlineMat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        var inspectable = habitatObject.GetComponent<InspectableHabitat>();
        if (inspectable == null) inspectable = habitatObject.AddComponent<InspectableHabitat>();

        var existingCollider = habitatObject.GetComponent<SphereCollider>();
        if (existingCollider == null)
        {
            var sc = habitatObject.AddComponent<SphereCollider>();
            sc.center = habitatObject.transform.InverseTransformPoint(bounds.center);
            sc.radius = bounds.extents.magnitude * 1.05f / habitatObject.transform.lossyScale.x;
        }

        StartCoroutine(PulseGlow(outlineCol));
    }

    IEnumerator PulseGlow(Color baseCol)
    {
        while (_glowActive)
        {
            float p = (Mathf.Sin(Time.time * 2.5f) + 1f) * 0.5f;
            if (_glowLight != null) _glowLight.intensity = Mathf.Lerp(0.4f, 1.8f, p);
            if (_outlineMat != null)
            {
                Color c = new Color(baseCol.r, baseCol.g, baseCol.b, Mathf.Lerp(0.5f, 1f, p));
                _outlineMat.color = c;
                if (_outlineMat.HasProperty("_BaseColor")) _outlineMat.SetColor("_BaseColor", c);
            }
            yield return null;
        }
    }

    void HideGlow()
    {
        _glowActive = false;
        if (_glowLight != null) Destroy(_glowLight.gameObject);
        if (_outlineRoot != null) Destroy(_outlineRoot);
        if (_postBuildCanvas != null) Destroy(_postBuildCanvas.gameObject);
    }

    void SpawnTapIcon()
    {
        var canvasObj = new GameObject("PostBuildCanvas");
        _postBuildCanvas = canvasObj.AddComponent<Canvas>();
        _postBuildCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _postBuildCanvas.sortingOrder = 8;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        _tapIconObj = new GameObject("TapIcon");
        _tapIconObj.transform.SetParent(canvasObj.transform, false);
        var rt = _tapIconObj.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(200f, 200f);

        var img = _tapIconObj.AddComponent<Image>();
        img.raycastTarget = false;
        if (tapIconSprite != null)
        {
            img.sprite = tapIconSprite;
            img.color = Color.white;
        }
        else
        {
            img.color = new Color(1f, 1f, 1f, 0f);
            var lblObj = new GameObject("FallbackLabel");
            lblObj.transform.SetParent(_tapIconObj.transform, false);
            var lrt = lblObj.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            var txt = lblObj.AddComponent<Text>();
            txt.text = "👆"; txt.font = GetFont(); txt.fontSize = 100;
            txt.alignment = TextAnchor.MiddleCenter; txt.color = Color.white; txt.raycastTarget = false;
        }

        StartCoroutine(TrackHabitatIcon(rt));
        StartCoroutine(PulseTapIcon(_tapIconObj, img));
    }

    IEnumerator TrackHabitatIcon(RectTransform rt)
    {
        if (habitatObject == null) yield break;
        var bounds = GetHabitatBounds();
        Vector3 top = bounds.center + Vector3.up * bounds.extents.y * 1.3f;
        var canvasRt = _postBuildCanvas.GetComponent<RectTransform>();

        while (rt != null && _glowActive)
        {
            Vector3 screen = mainCamera.WorldToScreenPoint(top);
            if (screen.z > 0f)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screen, null, out Vector2 local);
                rt.anchoredPosition = local;
                rt.gameObject.SetActive(true);
            }
            else rt.gameObject.SetActive(false);
            yield return null;
        }
    }

    IEnumerator PulseTapIcon(GameObject obj, Image img)
    {
        while (obj != null && _glowActive)
        {
            float p = (Mathf.Sin(Time.time * 3f) + 1f) * 0.5f;
            obj.transform.localScale = Vector3.one * Mathf.Lerp(0.85f, 1.15f, p);
            if (img != null) { var c = img.color; img.color = new Color(c.r, c.g, c.b, Mathf.Lerp(0.6f, 1f, p)); }
            yield return null;
        }
    }

    void SpawnWorldButton()
    {
        if (beaverBuyAnchor == null) return;
        if (_worldCanvas != null) return;

        EnsureEventSystem();

        var canvasObj = new GameObject("BeaverBuyCanvas");
        _worldCanvas = canvasObj.AddComponent<Canvas>();
        _worldCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _worldCanvas.sortingOrder = 5;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        var btnObj = new GameObject("BuyBtn");
        btnObj.transform.SetParent(canvasObj.transform, false);
        var rt = btnObj.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(420f, 160f);

        var img = btnObj.AddComponent<Image>();
        img.color = new Color(0.12f, 0.68f, 0.34f);

        var btn = btnObj.AddComponent<Button>();
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
        _buyButtonObj = btnObj;

        var labelObj = new GameObject("Label");
        labelObj.transform.SetParent(btnObj.transform, false);
        var lrt = labelObj.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = new Vector2(12f, 6f); lrt.offsetMax = new Vector2(-12f, -6f);
        _buyButtonLabel = labelObj.AddComponent<Text>();
        _buyButtonLabel.font = GetFont();
        _buyButtonLabel.fontSize = 48;
        _buyButtonLabel.fontStyle = FontStyle.Bold;
        _buyButtonLabel.alignment = TextAnchor.MiddleCenter;
        _buyButtonLabel.color = Color.white;
        _buyButtonLabel.raycastTarget = false;
        labelObj.AddComponent<Outline>().effectColor = new Color(0f, 0.2f, 0.1f, 0.7f);

        StartCoroutine(TrackWorldPosition(rt));
    }

    IEnumerator TrackWorldPosition(RectTransform rt)
    {
        var canvasRt = _worldCanvas.GetComponent<RectTransform>();
        float offset = Random.Range(0f, Mathf.PI * 2f);
        while (rt != null && _state != SceneState.Building && _state != SceneState.Complete)
        {
            Vector3 screen = mainCamera.WorldToScreenPoint(beaverBuyAnchor.position);
            if (screen.z > 0f)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screen, null, out Vector2 local);
                local.y += Mathf.Sin(Time.time * 2f + offset) * 8f;
                rt.anchoredPosition = local;
                rt.gameObject.SetActive(true);
            }
            else rt.gameObject.SetActive(false);
            yield return null;
        }
    }

    void OnButtonPressed()
    {
        if (_state == SceneState.WaitingForBuy)
        {
            if (GameStateManager.Instance.Coins < habitatCost) { StartCoroutine(ShakeButton()); return; }
            GameStateManager.Instance.TrySpendCoins(habitatCost);
            GameStateManager.Instance.NotifyItemBought(habitatItemId);
            SetState(SceneState.WaitingForPlace);
        }
        else if (_state == SceneState.WaitingForPlace)
        {
            SetState(SceneState.Building);
        }
    }

    void UpdateButtonLabel()
    {
        if (_buyButtonLabel == null) return;
        if (_state == SceneState.WaitingForBuy)
        {
            _buyButtonLabel.text = $"{LanguageManager.Instance.Get("habitat_beaver_name")}\n{LanguageManager.Instance.Get("shop_currency_short", habitatCost)}";
            _buyButtonLabel.fontSize = 36;
        }
        else if (_state == SceneState.WaitingForPlace)
        {
            _buyButtonLabel.text = LanguageManager.Instance.Get("btn_plaatsen");
            _buyButtonLabel.fontSize = 48;
        }
    }

    void SetBuyButtonActive(bool active) { if (_buyButtonObj != null) _buyButtonObj.SetActive(active); }

    void SetupCoinDisplay()
    {
        var canvasObj = new GameObject("CoinCanvas");
        var cv = canvasObj.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        var coinBg = new GameObject("CoinBg");
        coinBg.transform.SetParent(canvasObj.transform, false);
        var bgImg = coinBg.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.45f);
        bgImg.raycastTarget = false;
        var bgrt = coinBg.GetComponent<RectTransform>();
        bgrt.anchorMin = new Vector2(0f, 1f); bgrt.anchorMax = new Vector2(0f, 1f);
        bgrt.pivot = new Vector2(0f, 1f); bgrt.anchoredPosition = new Vector2(20f, -20f);
        bgrt.sizeDelta = new Vector2(320f, 80f);

        var coinObj = new GameObject("CoinText");
        coinObj.transform.SetParent(coinBg.transform, false);
        var crt = coinObj.AddComponent<RectTransform>();
        crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
        crt.offsetMin = new Vector2(10f, 0f); crt.offsetMax = new Vector2(-10f, 0f);
        _coinLabel = coinObj.AddComponent<Text>();
        _coinLabel.font = GetFont();
        _coinLabel.fontSize = 44;
        _coinLabel.fontStyle = FontStyle.Bold;
        _coinLabel.alignment = TextAnchor.MiddleCenter;
        _coinLabel.color = Color.white;
        _coinLabel.raycastTarget = false;
        coinObj.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.6f);

        UpdateCoinDisplay(GameStateManager.Instance.Coins);
    }

    void UpdateCoinDisplay(int amount)
    {
        if (_coinLabel == null) return;
        _coinLabel.text = LanguageManager.Instance.Get("shop_currency", amount);
    }

    IEnumerator ShakeButton()
    {
        if (_buyButtonObj == null) yield break;
        var rt = _buyButtonObj.GetComponent<RectTransform>();
        var origin = rt.anchoredPosition;
        foreach (float off in new[] { -18f, 18f, -14f, 14f, -8f, 8f, 0f })
        {
            rt.anchoredPosition = origin + new Vector2(off, 0f);
            yield return new WaitForSeconds(0.04f);
        }
        rt.anchoredPosition = origin;
    }

    Bounds GetHabitatBounds()
    {
        var renderers = habitatObject.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return new Bounds(habitatObject.transform.position, Vector3.one * 10f);
        var b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);
        return b;
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

public class OnDestroyCallback : MonoBehaviour
{
    public System.Action OnDestroyAction;
    void OnDestroy() => OnDestroyAction?.Invoke();
}