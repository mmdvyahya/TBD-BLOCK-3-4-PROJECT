using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum GameState { Playing, Dead, ReachedEnd, Feeding, Complete }

public class PolarBearGame : MonoBehaviour
{
    public static PolarBearGame Instance { get; private set; }

    [SerializeField] private int   iceSheetCount = 10;
    [SerializeField] private float laneZSpacing  = 3f;
    [SerializeField] private float fallBoundaryX = 4.8f;
    [SerializeField] private int   randomSeed    = 42;

    [SerializeField] private float camHeight      = 10f;
    [SerializeField] private float camZOffset     = -5f;
    [SerializeField] private float camFollowSpeed = 5f;

    public PolarBearPlayer          Player    { get; private set; }
    public List<IceSheetController> IceSheets { get; private set; } = new();
    public GameObject               FishObject{ get; private set; }
    public int                      IceSheetCount => iceSheetCount;
    public float                    LaneZSpacing  => laneZSpacing;
    public float                    FallBoundaryX => fallBoundaryX;
    public GameState                CurrentState  { get; private set; }

    private Canvas     _canvas;
    private Text       _statusText;
    private GameObject _retryPanel;
    private Camera     _cam;

    void Awake() => Instance = this;

    void Start()
    {
        BuildScene();
        BuildUI();
        SetState(GameState.Playing);
    }

    void LateUpdate()
    {
        if (Player == null || _cam == null) return;
        if (CurrentState != GameState.Playing && CurrentState != GameState.Dead) return;

        Vector3 desired = new Vector3(0f, camHeight, Player.transform.position.z + camZOffset);
        _cam.transform.position = Vector3.Lerp(_cam.transform.position, desired, Time.deltaTime * camFollowSpeed);
    }

    void BuildScene()
    {
        EnsureLight();

        _cam = Camera.main;
        if (_cam == null)
        {
            var co = new GameObject("Main Camera");
            _cam   = co.AddComponent<Camera>();
            co.tag = "MainCamera";
        }
        _cam.transform.position = new Vector3(0f, camHeight, camZOffset);
        _cam.transform.rotation = Quaternion.Euler(52f, 0f, 0f);
        _cam.fieldOfView        = 55f;
        _cam.backgroundColor    = new Color(0.53f, 0.81f, 0.98f);

        float totalLen = (iceSheetCount + 2) * laneZSpacing;
        float midZ     = (iceSheetCount + 1) * laneZSpacing * 0.5f;

        var water = GameObject.CreatePrimitive(PrimitiveType.Plane);
        water.name = "Water";
        water.transform.position   = new Vector3(0f, -0.25f, midZ);
        water.transform.localScale = new Vector3(3f, 1f, totalLen * 0.1f);
        ApplyMaterial(water, new Color(0.15f, 0.42f, 0.82f));
        Destroy(water.GetComponent<Collider>());

        CreatePlatform("StartPlatform", Vector3.zero, new Color(0.72f, 0.92f, 1f));
        CreatePlatform("EndPlatform", new Vector3(0f, 0f, (iceSheetCount + 1) * laneZSpacing), new Color(0.72f, 0.92f, 1f));

        FishObject = BuildFishObject((iceSheetCount + 1) * laneZSpacing);
        FishObject.SetActive(false);

        Random.InitState(randomSeed);
        for (int i = 0; i < iceSheetCount; i++)
        {
            float z     = (i + 1) * laneZSpacing;
            bool  obs   = i >= 2 && i <= iceSheetCount - 3 && Random.value > 0.45f;
            float speed = Random.Range(0.6f, 1.7f) * (i % 2 == 0 ? 1f : -1f);
            float range = Random.Range(1.2f, 2.6f);
            IceSheets.Add(CreateIceSheet(i, z, speed, range, obs));
        }

        var bearObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        bearObj.name = "PolarBear";
        bearObj.transform.position   = new Vector3(0f, 1.1f, 0f);
        bearObj.transform.localScale = new Vector3(0.65f, 0.65f, 0.65f);
        ApplyMaterial(bearObj, new Color(0.95f, 0.97f, 1f));
        Destroy(bearObj.GetComponent<CapsuleCollider>());
        Player = bearObj.AddComponent<PolarBearPlayer>();
    }

    void EnsureLight()
    {
        if (FindFirstObjectByType<Light>() != null) return;
        var lo = new GameObject("DirectionalLight");
        var l  = lo.AddComponent<Light>();
        l.type            = LightType.Directional;
        l.intensity       = 1.2f;
        lo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    void CreatePlatform(string name, Vector3 pos, Color col)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.position   = pos;
        obj.transform.localScale = new Vector3(2.4f, 0.3f, 2.4f);
        ApplyMaterial(obj, col);
        Destroy(obj.GetComponent<Collider>());
    }

    IceSheetController CreateIceSheet(int idx, float z, float speed, float range, bool obstacle)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = $"IceSheet_{idx + 1:D2}";
        obj.transform.position   = new Vector3(0f, 0f, z);
        obj.transform.localScale = new Vector3(2.2f, 0.25f, 2.2f);
        Color iceCol = idx % 2 == 0 ? new Color(0.83f, 0.95f, 1f) : new Color(0.90f, 0.97f, 1f);
        ApplyMaterial(obj, iceCol);
        Destroy(obj.GetComponent<Collider>());

        var ctrl        = obj.AddComponent<IceSheetController>();
        ctrl.sheetIndex = idx;
        ctrl.moveSpeed  = speed;
        ctrl.moveRange  = range;
        if (obstacle) ctrl.SpawnObstacle();
        return ctrl;
    }

    GameObject BuildFishObject(float endZ)
    {
        var fish = new GameObject("Fish");
        fish.transform.position = new Vector3(0f, 2.6f, endZ);

        var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        body.name = "FishBody";
        body.transform.SetParent(fish.transform);
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale    = new Vector3(0.75f, 0.42f, 0.32f);
        ApplyMaterial(body, new Color(1f, 0.52f, 0.12f));
        Destroy(body.GetComponent<Collider>());

        var tail = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tail.name = "FishTail";
        tail.transform.SetParent(fish.transform);
        tail.transform.localPosition = new Vector3(-0.5f, 0f, 0f);
        tail.transform.localScale    = new Vector3(0.28f, 0.28f, 0.18f);
        tail.transform.localRotation = Quaternion.Euler(0f, 0f, 40f);
        ApplyMaterial(tail, new Color(0.95f, 0.42f, 0.08f));
        Destroy(tail.GetComponent<Collider>());

        var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eye.name = "FishEye";
        eye.transform.SetParent(fish.transform);
        eye.transform.localPosition = new Vector3(0.28f, 0.12f, 0.14f);
        eye.transform.localScale    = Vector3.one * 0.1f;
        ApplyMaterial(eye, Color.black);
        Destroy(eye.GetComponent<Collider>());

        fish.AddComponent<SphereCollider>().radius = 0.5f;
        fish.AddComponent<FishBob>();
        return fish;
    }

    public static void ApplyMaterial(GameObject obj, Color col)
    {
        var r = obj.GetComponent<Renderer>();
        if (r == null) return;

        Shader sh = Shader.Find("Universal Render Pipeline/Lit")
                 ?? Shader.Find("Standard")
                 ?? Shader.Find("Legacy Shaders/Diffuse");

        var mat = new Material(sh);
        mat.color = col;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
        r.material = mat;
    }

    void BuildUI()
    {
        var co     = new GameObject("UICanvas");
        _canvas    = co.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = co.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        co.AddComponent<GraphicRaycaster>();

        Color btnIce  = new Color(0.75f, 0.92f, 1f, 0.92f);
        Color btnBlow = new Color(0.55f, 0.82f, 1f, 0.92f);

        MakeCornerButton("BtnForward", "▲",       new Vector2(-90f, 200f), new Vector2(80f, 70f),  btnIce,  () => Player?.TryJumpForward());
        MakeCornerButton("BtnLeft",    "◀",        new Vector2(-150f, 120f), new Vector2(70f, 70f), btnIce,  () => Player?.TryJumpLateral(-1));
        MakeCornerButton("BtnRight",   "▶",        new Vector2(-30f,  120f), new Vector2(70f, 70f), btnIce,  () => Player?.TryJumpLateral(1));
        MakeCornerButton("BtnBlow",    "💨 Blow",  new Vector2(-90f,  35f),  new Vector2(130f, 70f), btnBlow, () => Player?.TryBlow());

        var statusObj = new GameObject("StatusText");
        statusObj.transform.SetParent(_canvas.transform, false);
        var srt = statusObj.AddComponent<RectTransform>();
        srt.anchorMin        = new Vector2(0.5f, 1f);
        srt.anchorMax        = new Vector2(0.5f, 1f);
        srt.pivot            = new Vector2(0.5f, 1f);
        srt.anchoredPosition = new Vector2(0f, -60f);
        srt.sizeDelta        = new Vector2(800f, 90f);
        _statusText           = statusObj.AddComponent<Text>();
        _statusText.font       = GetFont();
        _statusText.fontSize   = 46;
        _statusText.fontStyle  = FontStyle.Bold;
        _statusText.alignment  = TextAnchor.UpperCenter;
        _statusText.color      = Color.white;
        var so = statusObj.AddComponent<Outline>();
        so.effectColor    = new Color(0f, 0f, 0f, 0.7f);
        so.effectDistance = new Vector2(2f, -2f);

        _retryPanel = new GameObject("RetryPanel");
        _retryPanel.transform.SetParent(_canvas.transform, false);
        var bg  = _retryPanel.AddComponent<Image>();
        bg.color = new Color(0f, 0.1f, 0.3f, 0.72f);
        var brt = _retryPanel.GetComponent<RectTransform>();
        brt.anchorMin = Vector2.zero;
        brt.anchorMax = Vector2.one;
        brt.offsetMin = brt.offsetMax = Vector2.zero;

        var oops = new GameObject("OopsText");
        oops.transform.SetParent(_retryPanel.transform, false);
        var ort = oops.AddComponent<RectTransform>();
        ort.anchorMin = ort.anchorMax = ort.pivot = new Vector2(0.5f, 0.5f);
        ort.anchoredPosition = new Vector2(0f, 100f);
        ort.sizeDelta        = new Vector2(700f, 120f);
        var ot = oops.AddComponent<Text>();
        ot.text      = "Splash! 🌊\nTry again!";
        ot.font      = GetFont();
        ot.fontSize  = 56;
        ot.fontStyle = FontStyle.Bold;
        ot.alignment = TextAnchor.MiddleCenter;
        ot.color     = Color.white;

        MakeCenteredButton("BtnRetry", "🐻‍❄️  Let's go!", new Vector2(0f, -60f),
            new Vector2(360f, 120f), new Color(0.22f, 0.75f, 1f), OnRetryPressed, _retryPanel.transform);

        _retryPanel.SetActive(false);
    }

    void MakeCornerButton(string name, string label, Vector2 pos, Vector2 size,
                          Color bg, UnityEngine.Events.UnityAction cb)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(_canvas.transform, false);

        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(1f, 0f);
        rt.anchorMax        = new Vector2(1f, 0f);
        rt.pivot            = new Vector2(0.5f, 0f);
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;

        var img = obj.AddComponent<Image>();
        img.color = bg;

        var btn = obj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.colors        = MakeColorBlock(bg);
        btn.onClick.AddListener(cb);

        var tObj = new GameObject("Label");
        tObj.transform.SetParent(obj.transform, false);
        var trt = tObj.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        var t = tObj.AddComponent<Text>();
        t.text      = label;
        t.font      = GetFont();
        t.fontSize  = 30;
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.color     = new Color(0.08f, 0.1f, 0.3f);
    }

    void MakeCenteredButton(string name, string label, Vector2 pos, Vector2 size,
                             Color bg, UnityEngine.Events.UnityAction cb, Transform parent)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;

        var img = obj.AddComponent<Image>();
        img.color = bg;

        var btn = obj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.colors        = MakeColorBlock(bg);
        btn.onClick.AddListener(cb);

        var tObj = new GameObject("Label");
        tObj.transform.SetParent(obj.transform, false);
        var trt = tObj.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        var t = tObj.AddComponent<Text>();
        t.text      = label;
        t.font      = GetFont();
        t.fontSize  = 36;
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.color     = new Color(0.08f, 0.1f, 0.3f);
    }

    static ColorBlock MakeColorBlock(Color bg) => new ColorBlock
    {
        normalColor      = bg,
        highlightedColor = bg * 1.15f,
        pressedColor     = bg * 0.75f,
        selectedColor    = bg,
        disabledColor    = bg * 0.5f,
        colorMultiplier  = 1f,
        fadeDuration     = 0.08f
    };

    public void SetState(GameState next)
    {
        CurrentState = next;

        switch (next)
        {
            case GameState.Playing:
                _retryPanel.SetActive(false);
                SetStatus("");
                break;

            case GameState.Dead:
                SetStatus("Splash! 🌊");
                _retryPanel.SetActive(true);
                break;

            case GameState.ReachedEnd:
                SetStatus("You made it! 🐟 Tap the fish!");
                FishObject.SetActive(true);
                FishObject.AddComponent<FeedingController>();
                break;

            case GameState.Feeding:
                SetStatus("");
                break;

            case GameState.Complete:
                SetStatus("Yum yum! 🎉");
                StartCoroutine(CompleteSequence());
                break;
        }
    }

    IEnumerator CompleteSequence()
    {
        yield return new WaitForSeconds(2.5f);
        Debug.Log("[PolarBear] Minigame complete. Load next scene here.");
    }

    void OnRetryPressed()
    {
        Player.ResetToStart();
        foreach (var s in IceSheets) s.ResetSheet();
        SetState(GameState.Playing);
    }

    public void ShowBlowFeedback(bool hit)
    {
        SetStatus(hit ? "Poof! 💨 Ice melted!" : "Nothing to blow away!");
        StartCoroutine(ClearStatusAfter(1.6f));
    }

    public void ShowMissedFeedback()
    {
        SetStatus("Too far! ❄️ Wait for it...");
        StartCoroutine(ClearStatusAfter(1.6f));
    }

    public void ShowBlockedFeedback()
    {
        SetStatus("Blocked! 🧊  Blow it away first!");
        StartCoroutine(ClearStatusAfter(1.6f));
    }

    IEnumerator ClearStatusAfter(float t)
    {
        yield return new WaitForSeconds(t);
        if (CurrentState == GameState.Playing) SetStatus("");
    }

    void SetStatus(string msg)
    {
        if (_statusText) _statusText.text = msg;
    }

    static Font _cachedFont;
    public static Font GetFont()
    {
        if (_cachedFont != null) return _cachedFont;
        _cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (_cachedFont == null) _cachedFont = Font.CreateDynamicFontFromOSFont("Arial", 24);
        return _cachedFont;
    }
}

public class FishBob : MonoBehaviour
{
    private Vector3 _origin;
    void Start()  => _origin = transform.position;
    void Update() => transform.position = _origin + Vector3.up * (Mathf.Sin(Time.time * 2.2f) * 0.12f);
}
