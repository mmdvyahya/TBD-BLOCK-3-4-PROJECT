using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlaceBeaverHabitatManager : MonoBehaviour
{
    [Header("Drag these in from the scene")]
    [SerializeField] private GameObject habitatObject;
    [SerializeField] private GameObject beaverPrefab;
    [SerializeField] private Transform  beaverAppears;
    [SerializeField] private Transform  pos1;
    [SerializeField] private Transform  pos2;
    [SerializeField] private Transform  pos3;

    [Header("Camera zoom")]
    [SerializeField] private float zoomDuration = 1.2f;
    [SerializeField] private float zoomFOV      = 50f;
    [SerializeField] private Vector3 endCamPos = new Vector3(-451f, 423f, 972f);

    private Camera  _cam;
    private Canvas  _canvas;
    private Text    _statusText;
    private bool    _placed;

    void Start()
    {
        _cam = Camera.main;

        EnsureLight();
        EnsureEventSystem();

        if (habitatObject == null)
            Debug.LogError("[PlaceBeaverHabitat] Drag 'Habitat Object' into the Inspector!");

        if (habitatObject != null)
            habitatObject.SetActive(false);

        if (pos1 == null) { var g = GameObject.Find("pos1"); if (g) pos1 = g.transform; }
        if (pos2 == null) { var g = GameObject.Find("pos2"); if (g) pos2 = g.transform; }
        if (pos3 == null) { var g = GameObject.Find("pos3"); if (g) pos3 = g.transform; }
        if (beaverAppears == null) { var g = GameObject.Find("BeaverAppears"); if (g) beaverAppears = g.transform; }

        var swimArea = GameObject.Find("SwimArea");
        if (swimArea != null)
        {
            swimArea.transform.position   = new Vector3(-484.6472f, 217.4f, 1152.296f);
            swimArea.transform.localScale = new Vector3(10.184f, 10.184f, 10.184f);
            var r = swimArea.GetComponent<Renderer>();
            if (r != null) r.enabled = false;
        }

        BuildUI();

        if (pos1) SpawnPlacementButton(pos1, "pos1");
        if (pos2) SpawnPlacementButton(pos2, "pos2");
        if (pos3) SpawnPlacementButton(pos3, "pos3");
    }

    void BuildUI()
    {
        EnsureEventSystem();

        var cObj = new GameObject("UICanvas");
        _canvas = cObj.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = cObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        cObj.AddComponent<GraphicRaycaster>();

        var statusObj = new GameObject("StatusText");
        statusObj.transform.SetParent(_canvas.transform, false);
        var srt = statusObj.AddComponent<RectTransform>();
        srt.anchorMin        = new Vector2(0.5f, 0.5f);
        srt.anchorMax        = new Vector2(0.5f, 0.5f);
        srt.pivot            = new Vector2(0.5f, 0.5f);
        srt.anchoredPosition = new Vector2(0f, 0f);
        srt.sizeDelta        = new Vector2(900f, 160f);
        var statusBg = statusObj.AddComponent<Image>();
        statusBg.color         = new Color(0f, 0f, 0f, 0f);
        statusBg.raycastTarget = false;

        var textObj = new GameObject("Label");
        textObj.transform.SetParent(statusObj.transform, false);
        var trt = textObj.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        _statusText              = textObj.AddComponent<Text>();
        _statusText.text         = "";
        _statusText.font         = GetFont();
        _statusText.fontSize     = 72;
        _statusText.fontStyle    = FontStyle.Bold;
        _statusText.alignment    = TextAnchor.MiddleCenter;
        _statusText.color        = Color.white;
        _statusText.raycastTarget = false;
        var outline = textObj.AddComponent<Outline>();
        outline.effectColor    = new Color(0f, 0f, 0f, 1f);
        outline.effectDistance = new Vector2(4f, -4f);

        StartCoroutine(FadeInStatusBg(statusBg));
    }

    void SpawnPlacementButton(Transform pos, string posName)
    {
        var btnObj = new GameObject($"PlaatsenBtn_{posName}");
        btnObj.transform.SetParent(_canvas.transform, false);

        var rt = btnObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(320f, 120f);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);

        var img = btnObj.AddComponent<Image>();
        img.color = new Color(0.10f, 0.62f, 0.32f);

        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.colors = new ColorBlock
        {
            normalColor      = new Color(0.10f, 0.62f, 0.32f),
            highlightedColor = new Color(0.18f, 0.80f, 0.42f),
            pressedColor     = new Color(0.06f, 0.42f, 0.22f),
            selectedColor    = new Color(0.10f, 0.62f, 0.32f),
            disabledColor    = new Color(0.4f,  0.4f,  0.4f),
            colorMultiplier  = 1f,
            fadeDuration     = 0.08f
        };

        Transform capturedPos = pos;
        btn.onClick.AddListener(() => OnPlaatsen(capturedPos));

        var labelObj = new GameObject("Label");
        labelObj.transform.SetParent(btnObj.transform, false);
        var lrt = labelObj.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        var txt = labelObj.AddComponent<Text>();
        txt.text          = "Plaatsen";
        txt.font          = GetFont();
        txt.fontSize      = 52;
        txt.fontStyle     = FontStyle.Bold;
        txt.alignment     = TextAnchor.MiddleCenter;
        txt.color         = Color.white;
        txt.raycastTarget = false;

        StartCoroutine(TrackWorldPos(rt, pos));
    }

    IEnumerator TrackWorldPos(RectTransform rt, Transform worldTarget)
    {
        while (rt != null && !_placed)
        {
            Vector3 screen = _cam.WorldToScreenPoint(worldTarget.position);
            if (screen.z > 0f)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvas.GetComponent<RectTransform>(),
                    screen, null, out Vector2 local);
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

    void OnPlaatsen(Transform pos)
    {
        if (_placed || habitatObject == null) return;
        _placed = true;

        foreach (Transform child in _canvas.transform)
            if (child.name.StartsWith("PlaatsenBtn_"))
                child.gameObject.SetActive(false);

        habitatObject.transform.position   = pos.position;
        habitatObject.transform.rotation   = Quaternion.Euler(0f, 180f, 0f);
        habitatObject.transform.localScale = new Vector3(313f, 313f, 313f);

        if (beaverAppears != null)
            beaverAppears.position = new Vector3(-468f, 213f, 1190f);
        habitatObject.SetActive(true);

        StartCoroutine(PlacementSequence(pos.position));
    }

    IEnumerator PlacementSequence(Vector3 habitatPos)
    {
        yield return StartCoroutine(PopIn(habitatObject));

        _statusText.text = "Goed gedaan! 🎉";

        yield return new WaitForSeconds(0.9f);

        yield return StartCoroutine(ZoomToHabitat(habitatPos));

        if (beaverPrefab != null && beaverAppears != null)
        {
            var swimAreaObj = GameObject.Find("SwimArea");
            if (swimAreaObj != null)
            {
                swimAreaObj.transform.position   = new Vector3(-484.6472f, 217.4f, 1152.296f);
                swimAreaObj.transform.localScale  = new Vector3(10.184f, 10.184f, 10.184f);
                var swimRend = swimAreaObj.GetComponent<Renderer>();
                if (swimRend != null) swimRend.enabled = false;
            }

            var beaver = Instantiate(beaverPrefab, beaverAppears.position, beaverAppears.rotation);
            beaver.transform.localScale = new Vector3(10f, 10f, 10f);
            var swimmer = beaver.AddComponent<BeaverSwimmer>();

            if (swimAreaObj != null)
            {
                swimmer.SetSwimArea(swimAreaObj.transform.position,
                    swimAreaObj.transform.localScale.x * 5f);
            }
        }

        SpawnConfetti();

        yield return new WaitForSeconds(3f);

        ShowDoorgaanButton();
    }

    IEnumerator FadeInStatusBg(Image bg)
    {
        while (_statusText == null || _statusText.text.Length == 0)
            yield return null;

        float t = 0f;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            bg.color = new Color(0f, 0f, 0f, Mathf.Lerp(0f, 0.55f, t / 0.2f));
            yield return null;
        }
    }

    IEnumerator PopIn(GameObject obj)
    {
        Vector3 targetScale = new Vector3(313f, 313f, 313f);
        obj.transform.localScale = Vector3.zero;
        float t = 0f;
        while (t < 0.45f)
        {
            t += Time.deltaTime;
            float p         = t / 0.45f;
            float overshoot = 1f + Mathf.Sin(p * Mathf.PI) * 0.15f;
            obj.transform.localScale = targetScale * Mathf.SmoothStep(0f, 1f, p) * overshoot;
            yield return null;
        }
        obj.transform.localScale = targetScale;
    }

    IEnumerator ZoomToHabitat(Vector3 target)
    {
        Vector3    startPos = _cam.transform.position;
        Quaternion startRot = _cam.transform.rotation;
        float      startFOV = _cam.fieldOfView;
        Vector3    endPos   = endCamPos;
        Quaternion endRot   = Quaternion.LookRotation((target - endPos).normalized);
        float      t        = 0f;

        while (t < zoomDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / zoomDuration);
            _cam.transform.position = Vector3.Lerp(startPos, endPos, p);
            _cam.transform.rotation = Quaternion.Slerp(startRot, endRot, p);
            _cam.fieldOfView        = Mathf.Lerp(startFOV, zoomFOV, p);
            yield return null;
        }
    }

    void ShowDoorgaanButton()
    {
        var btnObj = new GameObject("DoorgaanBtn");
        btnObj.transform.SetParent(_canvas.transform, false);

        var rt = btnObj.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, -130f);
        rt.sizeDelta        = new Vector2(380f, 120f);
        rt.localScale       = Vector3.zero;

        var img = btnObj.AddComponent<Image>();
        img.color = new Color(0.10f, 0.62f, 0.32f);

        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.colors = new ColorBlock
        {
            normalColor      = new Color(0.10f, 0.62f, 0.32f),
            highlightedColor = new Color(0.18f, 0.80f, 0.42f),
            pressedColor     = new Color(0.06f, 0.42f, 0.22f),
            selectedColor    = new Color(0.10f, 0.62f, 0.32f),
            disabledColor    = new Color(0.4f,  0.4f,  0.4f),
            colorMultiplier  = 1f,
            fadeDuration     = 0.08f
        };
        btn.onClick.AddListener(() => UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene"));

        var labelObj = new GameObject("Label");
        labelObj.transform.SetParent(btnObj.transform, false);
        var lrt = labelObj.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        var txt = labelObj.AddComponent<Text>();
        txt.text          = "Doorgaan";
        txt.font          = GetFont();
        txt.fontSize      = 52;
        txt.fontStyle     = FontStyle.Bold;
        txt.alignment     = TextAnchor.MiddleCenter;
        txt.color         = Color.white;
        txt.raycastTarget = false;
        var outline = labelObj.AddComponent<Outline>();
        outline.effectColor    = new Color(0f, 0.2f, 0.1f, 0.7f);
        outline.effectDistance = new Vector2(2f, -2f);

        StartCoroutine(PopInUI(rt));
    }

    IEnumerator PopInUI(RectTransform rt)
    {
        float t = 0f;
        while (t < 0.25f)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / 0.25f);
            float overshoot = 1f + Mathf.Sin(p * Mathf.PI) * 0.15f;
            rt.localScale = Vector3.one * p * overshoot;
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    void SpawnConfetti()
    {
        Color[] cols =
        {
            new Color(1f, 0.22f, 0.22f), new Color(1f, 0.85f, 0.1f),
            new Color(0.2f, 0.8f, 0.3f), new Color(0.2f, 0.5f, 1f),
            new Color(0.9f, 0.3f, 1f),   new Color(1f, 0.55f, 0.1f),
        };

        for (int i = 0; i < 60; i++)
        {
            var piece = new GameObject("Confetti");
            piece.transform.SetParent(_canvas.transform, false);

            var rt = piece.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(Random.Range(18f, 38f), Random.Range(22f, 48f));
            rt.anchoredPosition = new Vector2(Random.Range(-520f, 520f), Random.Range(0f, 80f));

            var img = piece.AddComponent<Image>();
            img.color = cols[Random.Range(0, cols.Length)];
            img.raycastTarget = false;

            float spinSpeed  = Random.Range(-280f, 280f);
            float fallSpeed  = Random.Range(420f, 900f);
            float swaySpeed  = Random.Range(0.8f, 2.2f);
            float swayAmount = Random.Range(60f, 180f);
            float startX     = rt.anchoredPosition.x;

            StartCoroutine(AnimateConfettiPiece(rt, img, spinSpeed, fallSpeed, swaySpeed, swayAmount, startX));
        }
    }

    IEnumerator AnimateConfettiPiece(RectTransform rt, Image img,
        float spinSpeed, float fallSpeed, float swaySpeed, float swayAmount, float startX)
    {
        float t       = 0f;
        float maxTime = 2.8f;

        while (t < maxTime && rt != null)
        {
            t += Time.deltaTime;
            float p = t / maxTime;

            rt.anchoredPosition = new Vector2(
                startX + Mathf.Sin(t * swaySpeed) * swayAmount,
                rt.anchoredPosition.y - fallSpeed * Time.deltaTime);

            rt.localRotation = Quaternion.Euler(0f, 0f, rt.localEulerAngles.z + spinSpeed * Time.deltaTime);

            img.color = new Color(img.color.r, img.color.g, img.color.b, Mathf.Lerp(1f, 0f, Mathf.Pow(p, 2.5f)));

            yield return null;
        }

        if (rt != null) Destroy(rt.gameObject);
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

public class BeaverSwimmer : MonoBehaviour
{
    private Vector3 _areaCenter;
    private float   _areaRadius = 25f;

    private Vector3 _currentTarget;
    private float   _swimSpeed    = 28f;
    private float   _bobSpeed     = 1.8f;
    private float   _bobHeight    = 3f;
    private float   _tiltAmount   = 10f;
    private float   _timeOffset;
    private float   _baseY;
    private float   _pickTargetTimer;
    private Quaternion _targetRot;

    public void SetSwimArea(Vector3 center, float radius)
    {
        _areaCenter = center;
        _areaRadius = radius;
    }

    void Start()
    {
        _areaCenter  = transform.position;
        _baseY       = transform.position.y;
        _timeOffset  = Random.Range(0f, Mathf.PI * 2f);
        _targetRot   = transform.rotation;
        PickNewTarget();
    }

    void Update()
    {
        _pickTargetTimer -= Time.deltaTime;
        if (_pickTargetTimer <= 0f)
            PickNewTarget();

        float bob = Mathf.Sin(Time.time * _bobSpeed + _timeOffset) * _bobHeight;
        Vector3 flatTarget = new Vector3(_currentTarget.x, _baseY + bob, _currentTarget.z);

        transform.position = Vector3.MoveTowards(transform.position, flatTarget, _swimSpeed * Time.deltaTime);

        Vector3 dir = (flatTarget - transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f)
        {
            float roll = Mathf.Sin(Time.time * _bobSpeed + _timeOffset) * _tiltAmount;
            _targetRot = Quaternion.LookRotation(dir.normalized) * Quaternion.Euler(0f, 0f, roll);
        }

        transform.rotation = Quaternion.Slerp(transform.rotation, _targetRot, Time.deltaTime * 3f);

        if (Vector3.Distance(transform.position, flatTarget) < 5f)
            PickNewTarget();
    }

    void PickNewTarget()
    {
        Vector2 rnd    = Random.insideUnitCircle * (_areaRadius * 0.85f);
        _currentTarget = new Vector3(_areaCenter.x + rnd.x, _baseY, _areaCenter.z + rnd.y);
        _pickTargetTimer = Random.Range(2.5f, 5f);
    }
}
