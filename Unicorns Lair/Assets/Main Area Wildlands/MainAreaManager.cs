using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class MainAreaManager : MonoBehaviour
{
    [Header("Habitat Inspection")]
    [SerializeField] private HabitatInspectionManager habitatInspectionManager;
    [SerializeField] private HabitatInteractionMenu habitatInteractionMenu;
    [Header("Scene References")]
    [SerializeField] private GameObject habitatObject;
    [SerializeField] private Transform beaverBuyAnchor;
    [SerializeField] private Camera mainCamera;

    [Header("Habitat Settings")]
    [SerializeField] private string habitatItemId = "beaver_habitat";
    [SerializeField] private int habitatCost = 100;

    [Header("Buy Button")]
    [SerializeField] private float buttonScale = 0.006f;

    [Header("Post Build")]
    [SerializeField] private Sprite tapIconSprite;

    private static readonly Vector3 HabitatCamPos = new Vector3(1167.438f, 51.58662f, 1195.976f);
    private static readonly Quaternion HabitatCamRot = Quaternion.Euler(52.977f, 0.239f, -0.001f);

    private Vector3 _originalCamPos;
    private Quaternion _originalCamRot;
    private CameraSwipe _cameraSwipe;

    private enum SceneState
    {
        Idle,
        WaitingForBuy,
        WaitingForPlace,
        Building,
        Complete,
        InspectingHabitat
    }
    public void NotifyHabitatTapped(InspectableHabitat habitat)
    {
        if (_state != SceneState.Complete) return;
        if (habitat == null) return;

        if (_cameraSwipe != null)
            _cameraSwipe.enabled = false;

        if (habitatInteractionMenu != null)
            habitatInteractionMenu.OpenMenu(habitat);
    }

    public void NotifyInspectHabitat(InspectableHabitat habitat)
    {
        if (_state != SceneState.Complete) return;
        if (habitat == null) return;

        SetState(SceneState.InspectingHabitat);

        if (habitatInteractionMenu != null)
            habitatInteractionMenu.CloseMenu();

        if (habitatInspectionManager != null)
            habitatInspectionManager.OpenMinigame(habitat);
    }

    public void NotifyExitInspection()
    {
        if (_state != SceneState.InspectingHabitat) return;

        if (habitatInspectionManager != null)
            habitatInspectionManager.CloseInspectionMode();

        if (_cameraSwipe != null)
            _cameraSwipe.enabled = true;

        SetState(SceneState.Complete);
    }

    public void NotifyCloseHabitatMenu()
    {
        if (_state != SceneState.Complete) return;

        if (habitatInteractionMenu != null)
            habitatInteractionMenu.CloseMenu();

        if (_cameraSwipe != null)
            _cameraSwipe.enabled = true;
    }

    private SceneState _state = SceneState.Idle;
    private Canvas _worldCanvas;
    private GameObject _buyButtonObj;
    private Text _buyButtonLabel;
    private HabitatBuilder _builder;
    private Text _coinLabel;

    void Start()
    {
        GameStateManager.Ensure();
        LanguageManager.Ensure();

        if (mainCamera == null) mainCamera = Camera.main;

        _originalCamPos = mainCamera.transform.position;
        _originalCamRot = mainCamera.transform.rotation;
        _cameraSwipe = mainCamera.GetComponent<CameraSwipe>();

        SetupReferences();
        SetupCoinDisplay();
        SetupBuilder();
        SetState(DetermineInitialState());

        GameStateManager.Instance.CoinsChanged += OnCoinsChanged;
        LanguageManager.Instance.LanguageChanged += OnLanguageChanged;
    }

    void OnDestroy()
    {
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.CoinsChanged -= OnCoinsChanged;
        if (LanguageManager.Instance != null)
            LanguageManager.Instance.LanguageChanged -= OnLanguageChanged;
    }

    void SetupReferences()
    {
        if (habitatObject == null)
            habitatObject = GameObject.Find("Merged Animal Habitat Beever with tree trunks");

        if (habitatObject == null)
        {
            Debug.LogError("[MainAreaManager] Drag 'Merged Animal Habitat Beever with tree trunks' into Inspector.");
            return;
        }

        habitatObject.SetActive(true);

        if (beaverBuyAnchor == null)
        {
            var g = GameObject.Find("BeaverBuy");
            if (g != null) beaverBuyAnchor = g.transform;
        }

        if (beaverBuyAnchor == null)
            Debug.LogError("[MainAreaManager] Drag 'BeaverBuy' empty object into Inspector.");
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
            case SceneState.InspectingHabitat:
                SetBuyButtonActive(false);
                break;
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
                StartCoroutine(StartPostBuildEffects());
                break;

            case SceneState.Idle:
                break;
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
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
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
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = new Vector2(12f, 6f);
        lrt.offsetMax = new Vector2(-12f, -6f);
        _buyButtonLabel = labelObj.AddComponent<Text>();
        _buyButtonLabel.font = GetFont();
        _buyButtonLabel.fontSize = 48;
        _buyButtonLabel.fontStyle = FontStyle.Bold;
        _buyButtonLabel.alignment = TextAnchor.MiddleCenter;
        _buyButtonLabel.color = Color.white;
        _buyButtonLabel.raycastTarget = false;
        var lo = labelObj.AddComponent<Outline>();
        lo.effectColor = new Color(0f, 0.2f, 0.1f, 0.7f);
        lo.effectDistance = new Vector2(2f, -2f);

        StartCoroutine(TrackWorldPosition(rt));
        StartCoroutine(BobButton(rt));
    }

    IEnumerator TrackWorldPosition(RectTransform rt)
    {
        var canvasRt = _worldCanvas.GetComponent<RectTransform>();
        while (rt != null && _state != SceneState.Building && _state != SceneState.Complete)
        {
            Vector3 screen = mainCamera.WorldToScreenPoint(beaverBuyAnchor.position);
            if (screen.z > 0f)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRt, screen, null, out Vector2 local);
                rt.anchoredPosition = local;
                rt.gameObject.SetActive(true);
            }
            else
            {
                rt.gameObject.SetActive(false);
            }
            yield return null;
        }
    }

    IEnumerator BobButton(RectTransform rt)
    {
        float timeOffset = Random.Range(0f, Mathf.PI * 2f);
        while (rt != null && _state != SceneState.Building && _state != SceneState.Complete)
        {
            float bob = Mathf.Sin(Time.time * 2f + timeOffset) * 6f;
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, rt.anchoredPosition.y + bob * Time.deltaTime);
            yield return null;
        }
    }

    void EnsureEventSystem()
    {
        if (UnityEngine.EventSystems.EventSystem.current != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
    }

    void OnButtonPressed()
    {
        if (_state == SceneState.WaitingForBuy)
        {
            if (GameStateManager.Instance.Coins < habitatCost)
            {
                StartCoroutine(ShakeButton());
                return;
            }

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
            string name = LanguageManager.Instance.Get("habitat_beaver_name");
            string coins = LanguageManager.Instance.Get("shop_currency_short", habitatCost);
            _buyButtonLabel.text = $"{name}\n{coins}";
            _buyButtonLabel.fontSize = 36;
        }
        else if (_state == SceneState.WaitingForPlace)
        {
            _buyButtonLabel.text = LanguageManager.Instance.Get("btn_plaatsen");
            _buyButtonLabel.fontSize = 48;
        }
    }

    void SetBuyButtonActive(bool active)
    {
        if (_buyButtonObj != null) _buyButtonObj.SetActive(active);
    }

    void OnBuildComplete(string itemId)
    {
        SetState(SceneState.Complete);
    }

    private bool _habitatTapped;
    private bool _movingToHabitat;
    private GameObject _glowObj;
    private GameObject _tapIconObj;
    private Canvas _postBuildCanvas;

    IEnumerator StartPostBuildEffects()
    {
        yield return new WaitForSeconds(0.5f);

        SpawnGlow();
        SpawnTapIcon();
    }

    void SpawnGlow()
    {
        if (habitatObject == null) return;

        var bounds = GetHabitatBounds();

        var lightObj = new GameObject("HabitatGlowLight");
        lightObj.transform.position = bounds.center;
        var light = lightObj.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.88f, 0.35f);
        light.intensity = 0f;
        light.range = bounds.extents.magnitude * 3.5f;
        _glowObj = lightObj;

        var outlineRoot = new GameObject("HabitatOutline");
        outlineRoot.transform.SetParent(habitatObject.transform.parent);

        var sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
        var mat = new Material(sh);
        Color outlineCol = new Color(1f, 0.92f, 0.25f, 1f);
        mat.color = outlineCol;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", outlineCol);

        foreach (var mf in habitatObject.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.sharedMesh == null) continue;
            var copy = new GameObject($"Outline_{mf.gameObject.name}");
            copy.transform.SetParent(outlineRoot.transform);
            copy.transform.position = mf.transform.position;
            copy.transform.rotation = mf.transform.rotation;
            copy.transform.localScale = mf.transform.lossyScale * 1.04f;
            var mr = copy.AddComponent<MeshRenderer>();
            var copyMf = copy.AddComponent<MeshFilter>();
            copyMf.sharedMesh = mf.sharedMesh;
            mr.material = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        StartCoroutine(PulseGlow(lightObj, light, outlineRoot, mat, outlineCol));

        var colliderRoot = new GameObject("HabitatTapCollider");
        colliderRoot.transform.position = bounds.center;
        var sc = colliderRoot.AddComponent<SphereCollider>();
        sc.radius = bounds.extents.magnitude * 1.05f;
    }

    IEnumerator PulseGlow(GameObject lightObj, Light light, GameObject outlineRoot, Material mat, Color baseCol)
    {
        while (lightObj != null && !_habitatTapped)
        {
            float p = (Mathf.Sin(Time.time * 2.5f) + 1f) * 0.5f;
            light.intensity = Mathf.Lerp(0.4f, 1.8f, p);
            float alpha = Mathf.Lerp(0.5f, 1f, p);
            Color c = new Color(baseCol.r, baseCol.g, baseCol.b, alpha);
            mat.color = c;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            yield return null;
        }
        if (lightObj != null) Destroy(lightObj);
        if (outlineRoot != null) Destroy(outlineRoot);
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
            img.sprite = tapIconSprite;
        else
        {
            img.color = new Color(1f, 1f, 1f, 0.9f);
            var lblObj = new GameObject("FallbackLabel");
            lblObj.transform.SetParent(_tapIconObj.transform, false);
            var lrt = lblObj.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            var txt = lblObj.AddComponent<Text>();
            txt.text = "👆";
            txt.font = GetFont();
            txt.fontSize = 100;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.raycastTarget = false;
        }

        StartCoroutine(TrackHabitatIcon(rt));
        StartCoroutine(PulseTapIcon(_tapIconObj, img));
        StartCoroutine(DetectHabitatTap());
    }

    IEnumerator TrackHabitatIcon(RectTransform rt)
    {
        if (habitatObject == null) yield break;
        var bounds = GetHabitatBounds();
        Vector3 top = bounds.center + Vector3.up * bounds.extents.y * 1.3f;
        var canvasRt = _postBuildCanvas.GetComponent<RectTransform>();

        while (rt != null && !_habitatTapped)
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
        float offset = 0f;
        while (obj != null && !_habitatTapped)
        {
            float p = (Mathf.Sin(Time.time * 3f + offset) + 1f) * 0.5f;
            float s = Mathf.Lerp(0.85f, 1.15f, p);
            obj.transform.localScale = Vector3.one * s;
            if (img != null)
            {
                var c = img.color;
                img.color = new Color(c.r, c.g, c.b, Mathf.Lerp(0.6f, 1f, p));
            }
            yield return null;
        }
    }

    IEnumerator DetectHabitatTap()
    {
        while (!_habitatTapped)
        {
            bool clicked = false;
            Vector2 screenPos = Vector2.zero;

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
            {
                clicked = true;
                screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
            }
            else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                clicked = true;
                screenPos = Mouse.current.position.ReadValue();
            }

            if (clicked)
            {
                Ray ray = mainCamera.ScreenPointToRay(screenPos);
                if (Physics.Raycast(ray, out RaycastHit hit, 9999f))
                {
                    var root = hit.collider.transform;
                    while (root.parent != null) root = root.parent;
                    if (root.gameObject == habitatObject || hit.collider.gameObject.name == "HabitatTapCollider")
                    {
                        _habitatTapped = true;
                        if (_tapIconObj != null) Destroy(_tapIconObj.transform.parent.gameObject);
                        StartCoroutine(MoveCameraToHabitat());
                    }
                }
            }
            yield return null;
        }
    }

    IEnumerator MoveCameraToHabitat()
    {
        if (_cameraSwipe != null) _cameraSwipe.enabled = false;

        Vector3 startPos = mainCamera.transform.position;
        Quaternion startRot = mainCamera.transform.rotation;
        float t = 0f;
        float dur = 1.1f;

        while (t < dur)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / dur));
            mainCamera.transform.position = Vector3.Lerp(startPos, HabitatCamPos, p);
            mainCamera.transform.rotation = Quaternion.Slerp(startRot, HabitatCamRot, p);
            yield return null;
        }

        mainCamera.transform.position = HabitatCamPos;
        mainCamera.transform.rotation = HabitatCamRot;

        ShowBackButton();
    }

    void ShowBackButton()
    {
        var canvasObj = new GameObject("BackButtonCanvas");
        var cv = canvasObj.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 9;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        EnsureEventSystem();

        var btnObj = new GameObject("BackBtn");
        btnObj.transform.SetParent(canvasObj.transform, false);
        var rt = btnObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = new Vector2(30f, 30f);
        rt.sizeDelta = new Vector2(220f, 110f);

        var img = btnObj.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.55f);

        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.colors = new ColorBlock
        {
            normalColor = new Color(0f, 0f, 0f, 0.55f),
            highlightedColor = new Color(0.2f, 0.2f, 0.2f, 0.75f),
            pressedColor = new Color(0f, 0f, 0f, 0.85f),
            selectedColor = new Color(0f, 0f, 0f, 0.55f),
            disabledColor = new Color(0f, 0f, 0f, 0.25f),
            colorMultiplier = 1f,
            fadeDuration = 0.08f
        };
        btn.onClick.AddListener(() =>
        {
            Destroy(canvasObj);
            StartCoroutine(ReturnCamera());
        });

        var lblObj = new GameObject("Label");
        lblObj.transform.SetParent(btnObj.transform, false);
        var lrt = lblObj.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        var txt = lblObj.AddComponent<Text>();
        txt.text = "◀  Terug";
        txt.font = GetFont();
        txt.fontSize = 38;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.raycastTarget = false;
    }

    IEnumerator ReturnCamera()
    {
        Vector3 startPos = mainCamera.transform.position;
        Quaternion startRot = mainCamera.transform.rotation;
        float t = 0f;
        float dur = 1.0f;

        while (t < dur)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / dur));
            mainCamera.transform.position = Vector3.Lerp(startPos, _originalCamPos, p);
            mainCamera.transform.rotation = Quaternion.Slerp(startRot, _originalCamRot, p);
            yield return null;
        }

        mainCamera.transform.position = _originalCamPos;
        mainCamera.transform.rotation = _originalCamRot;

        if (_cameraSwipe != null) _cameraSwipe.enabled = true;
    }

    Bounds GetHabitatBounds()
    {
        var renderers = habitatObject.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return new Bounds(habitatObject.transform.position, Vector3.one * 10f);
        var b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);
        return b;
    }

    void OnCoinsChanged(int newAmount)
    {
        UpdateCoinDisplay(newAmount);
    }

    void OnLanguageChanged()
    {
        UpdateButtonLabel();
        UpdateCoinDisplay(GameStateManager.Instance.Coins);
    }

    void SetupCoinDisplay()
    {
        var cam = mainCamera != null ? mainCamera : Camera.main;
        if (cam == null) return;

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
        bgrt.anchorMin = new Vector2(0f, 1f);
        bgrt.anchorMax = new Vector2(0f, 1f);
        bgrt.pivot = new Vector2(0f, 1f);
        bgrt.anchoredPosition = new Vector2(20f, -20f);
        bgrt.sizeDelta = new Vector2(320f, 80f);

        var coinObj = new GameObject("CoinText");
        coinObj.transform.SetParent(coinBg.transform, false);
        var crt = coinObj.AddComponent<RectTransform>();
        crt.anchorMin = Vector2.zero;
        crt.anchorMax = Vector2.one;
        crt.offsetMin = new Vector2(10f, 0f);
        crt.offsetMax = new Vector2(-10f, 0f);
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

    static Font _font;
    static Font GetFont()
    {
        if (_font != null) return _font;
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (_font == null) _font = Font.CreateDynamicFontFromOSFont("Arial", 24);
        return _font;
    }
}