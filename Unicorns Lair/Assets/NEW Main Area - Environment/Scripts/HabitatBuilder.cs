using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HabitatBuilder : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] public string itemId = "beaver_habitat";
    [SerializeField] private float totalBuildTime = 5f;

    public delegate void OnBuildComplete(string id);
    public event OnBuildComplete BuildComplete;

    private Vector3 _originalScale;
    private Bounds _worldBounds;
    private Canvas _uiCanvas;
    private RectTransform _progressBar;
    private Text _buildLabel;
    private List<GameObject> _clouds = new();

    void Awake()
    {
        _originalScale = transform.localScale;
    }

    public void HideAll()
    {
        foreach (var r in GetComponentsInChildren<Renderer>(true))
            r.enabled = false;
    }

    public void ShowAll()
    {
        foreach (var r in GetComponentsInChildren<Renderer>(true))
            r.enabled = true;
    }

    public void StartBuild()
    {
        _originalScale = transform.localScale;
        StartCoroutine(BuildSequence());
    }

    IEnumerator BuildSequence()
    {
        HideAll();
        _worldBounds = GetWorldBounds();

        Vector3 center = _worldBounds.center;
        float radius = _worldBounds.extents.magnitude;

        SetupBuildUI();
        SetBuildLabel(LanguageManager.Instance != null
            ? LanguageManager.Instance.Get("building_label")
            : "In aanbouw!");

        yield return StartCoroutine(Phase_CloudCover(center, radius));
        yield return StartCoroutine(Phase_Building(center, radius));
        yield return StartCoroutine(Phase_Reveal(center, radius));

        yield return StartCoroutine(ShowContinueButton());

        CleanupUI();
        transform.localScale = _originalScale;
        ShowAll();

        GameStateManager.Ensure();
        GameStateManager.Instance.NotifyItemBuilt(itemId);
        BuildComplete?.Invoke(itemId);
    }

    IEnumerator Phase_CloudCover(Vector3 center, float radius)
    {
        int cloudCount = 6;
        for (int i = 0; i < cloudCount; i++)
        {
            float angle = i * (360f / cloudCount) * Mathf.Deg2Rad;
            Vector3 pos = center + new Vector3(Mathf.Cos(angle) * radius * 0.5f, radius * 0.1f, Mathf.Sin(angle) * radius * 0.5f);
            var cloud = MakeCloud(pos, radius * 0.55f);
            _clouds.Add(cloud);
        }
        var centerCloud = MakeCloud(center + Vector3.up * radius * 0.15f, radius * 0.75f);
        _clouds.Add(centerCloud);

        float t = 0f;
        var finalScales = new List<Vector3>();
        foreach (var c in _clouds) finalScales.Add(c.transform.localScale);
        foreach (var c in _clouds) c.transform.localScale = Vector3.zero;

        while (t < 0.65f)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / 0.65f);
            for (int i = 0; i < _clouds.Count; i++)
                if (_clouds[i] != null)
                    _clouds[i].transform.localScale = finalScales[i] * p;
            yield return null;
        }

        foreach (var c in _clouds)
        {
            var captured = c;
            var fs = captured.transform.localScale;
            StartCoroutine(PulseCloud(captured.transform, fs));
        }

        SpawnStars(center, 10, radius * 0.5f);
        yield return new WaitForSeconds(0.3f);
    }

    IEnumerator Phase_Building(Vector3 center, float radius)
    {
        float interval = (totalBuildTime - 1.6f) / 10f;

        StartCoroutine(SpawnFlyingPlanks(center, radius, interval));
        StartCoroutine(SpawnRepeatingHammer(center, radius));
        StartCoroutine(SpawnSpinningWrench(center, radius));

        for (int i = 0; i < 10; i++)
        {
            yield return new WaitForSeconds(interval);
            SetProgress((float)(i + 1) / 10f);
            SpawnStars(center + Random.insideUnitSphere * radius * 0.35f + Vector3.up * radius * 0.2f, 6, radius * 0.4f);
            StartCoroutine(SpawnDebrisPuff(center + Random.insideUnitSphere * radius * 0.3f, radius));

            if (i % 2 == 0)
                StartCoroutine(CloudPuff(center + Random.insideUnitSphere * radius * 0.3f, radius * 0.3f));
        }

        yield return new WaitForSeconds(0.3f);
    }

    IEnumerator SpawnFlyingPlanks(Vector3 center, float radius, float interval)
    {
        Color[] cols = {
            new Color(0.76f, 0.52f, 0.22f), new Color(0.62f, 0.40f, 0.18f),
            new Color(0.88f, 0.68f, 0.35f), new Color(0.55f, 0.35f, 0.14f),
            new Color(0.95f, 0.75f, 0.40f),
        };

        float plankW = radius * 0.28f;
        float plankH = radius * 0.08f;
        int count = 16;

        for (int i = 0; i < count; i++)
        {
            yield return new WaitForSeconds((totalBuildTime - 1.6f) / count);

            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float launchDist = radius * Random.Range(2f, 3.5f);
            Vector3 launch = center + new Vector3(Mathf.Cos(angle) * launchDist, radius * Random.Range(0.05f, 0.35f), Mathf.Sin(angle) * launchDist);
            Vector3 land = center + new Vector3(Random.Range(-0.35f, 0.35f) * radius, Random.Range(0f, 0.2f) * radius, Random.Range(-0.35f, 0.35f) * radius);

            var plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(plank.GetComponent<Collider>());
            plank.transform.position = launch;
            plank.transform.localScale = new Vector3(plankW, plankH, plankH * 0.7f);
            plank.transform.rotation = Random.rotation;
            SetMat(plank, cols[Random.Range(0, cols.Length)]);

            StartCoroutine(FlyPlank(plank.transform, launch, land, radius));
        }
    }

    IEnumerator FlyPlank(Transform plank, Vector3 start, Vector3 end, float radius)
    {
        float dur = Random.Range(0.4f, 0.7f);
        float arc = radius * Random.Range(0.15f, 0.4f);
        float t = 0f;
        Vector3 startRot = new Vector3(Random.Range(0f, 360f), Random.Range(0f, 360f), 0f);
        Vector3 endRot = startRot + new Vector3(Random.Range(200f, 540f), Random.Range(200f, 540f), 0f);

        while (t < dur && plank != null)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / dur);
            plank.position = Vector3.Lerp(start, end, p) + Vector3.up * Mathf.Sin(p * Mathf.PI) * arc;
            plank.eulerAngles = Vector3.Lerp(startRot, endRot, p);
            yield return null;
        }
        if (plank == null) yield break;

        plank.position = end;
        StartCoroutine(PlankLandBounce(plank));
        StartCoroutine(SpawnDebrisPuff(end, radius));
        SpawnStars(end, 3, radius * 0.2f);
    }

    IEnumerator PlankLandBounce(Transform t)
    {
        if (t == null) yield break;
        Vector3 s = t.localScale;
        float dur = 0.28f, elapsed = 0f;
        while (elapsed < dur && t != null)
        {
            elapsed += Time.deltaTime;
            float p = elapsed / dur;
            float squash = Mathf.Sin(p * Mathf.PI * 2f) * 0.2f * Mathf.Lerp(1f, 0f, p);
            t.localScale = new Vector3(s.x * (1f + squash * 0.12f), s.y * (1f - squash * 0.18f), s.z);
            yield return null;
        }
        if (t != null)
        {
            t.localScale = s;
            StartCoroutine(ShrinkAndDestroy(t.gameObject, 0.8f));
        }
    }

    IEnumerator SpawnRepeatingHammer(Vector3 center, float radius)
    {
        float headSize = radius * 0.22f;
        float handleLen = radius * 0.5f;
        float waitTime = (totalBuildTime - 1.6f) / 5f;

        for (int i = 0; i < 5; i++)
        {
            yield return new WaitForSeconds(waitTime * 0.3f + i * waitTime * 0.15f);

            Vector3 side = center + new Vector3(Random.Range(-0.5f, 0.5f) * radius, radius * 0.25f, Random.Range(-0.5f, 0.5f) * radius);

            var head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(head.GetComponent<Collider>());
            head.transform.position = side;
            head.transform.localScale = new Vector3(headSize, headSize * 0.55f, headSize * 0.55f);
            SetMat(head, new Color(0.65f, 0.65f, 0.70f));

            var handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(handle.GetComponent<Collider>());
            handle.transform.SetParent(head.transform);
            handle.transform.localPosition = new Vector3(0f, -0.9f, 0f);
            handle.transform.localScale = new Vector3(0.18f, 1.6f, 0.18f);
            SetMat(handle, new Color(0.65f, 0.42f, 0.18f));

            StartCoroutine(HammerSwing(head.transform, side, radius));
        }
    }

    IEnumerator HammerSwing(Transform hammer, Vector3 pos, float radius)
    {
        Vector3 up = pos;
        Vector3 down = pos + Vector3.down * radius * 0.55f;
        float t = 0f;

        while (t < 0.22f && hammer != null)
        {
            t += Time.deltaTime;
            hammer.position = Vector3.Lerp(up, down, Mathf.SmoothStep(0f, 1f, t / 0.22f));
            yield return null;
        }

        SpawnStars(down, 6, radius * 0.25f);
        StartCoroutine(SpawnDebrisPuff(down, radius));

        t = 0f;
        while (t < 0.18f && hammer != null)
        {
            t += Time.deltaTime;
            hammer.position = Vector3.Lerp(down, up + Vector3.up * radius * 0.15f, Mathf.SmoothStep(0f, 1f, t / 0.18f));
            yield return null;
        }

        yield return new WaitForSeconds(0.3f);
        if (hammer != null) StartCoroutine(ShrinkAndDestroy(hammer.gameObject, 0.35f));
    }

    IEnumerator SpawnSpinningWrench(Vector3 center, float radius)
    {
        yield return new WaitForSeconds(totalBuildTime * 0.25f);

        float size = radius * 0.35f;

        var wrench = new GameObject("Wrench");
        wrench.transform.position = center + new Vector3(radius * 0.6f, radius * 0.25f, 0f);

        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(body.GetComponent<Collider>());
        body.transform.SetParent(wrench.transform);
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale = new Vector3(size * 0.18f, size, size * 0.18f);
        SetMat(body, new Color(0.72f, 0.72f, 0.76f));

        var headTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(headTop.GetComponent<Collider>());
        headTop.transform.SetParent(wrench.transform);
        headTop.transform.localPosition = new Vector3(0f, size * 0.55f, 0f);
        headTop.transform.localScale = new Vector3(size * 0.38f, size * 0.22f, size * 0.18f);
        SetMat(headTop, new Color(0.72f, 0.72f, 0.76f));

        var headBot = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(headBot.GetComponent<Collider>());
        headBot.transform.SetParent(wrench.transform);
        headBot.transform.localPosition = new Vector3(0f, -size * 0.55f, 0f);
        headBot.transform.localScale = new Vector3(size * 0.30f, size * 0.18f, size * 0.18f);
        SetMat(headBot, new Color(0.72f, 0.72f, 0.76f));

        float t = 0f;
        float dur = totalBuildTime * 0.5f;
        Vector3 startPos = wrench.transform.position;
        Vector3 endPos = center + new Vector3(-radius * 0.6f, radius * 0.25f, 0f);
        float arcH = radius * 0.25f;

        while (t < dur && wrench != null)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / dur);
            wrench.transform.position = Vector3.Lerp(startPos, endPos, p) + Vector3.up * Mathf.Sin(p * Mathf.PI) * arcH;
            wrench.transform.Rotate(Vector3.forward * 180f * Time.deltaTime);
            yield return null;
        }

        if (wrench != null) StartCoroutine(ShrinkAndDestroy(wrench, 0.4f));
    }

    IEnumerator SpawnDebrisPuff(Vector3 pos, float radius)
    {
        float puffSize = radius * 0.12f;
        for (int i = 0; i < 5; i++)
        {
            var puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(puff.GetComponent<Collider>());
            puff.transform.position = pos + Random.insideUnitSphere * radius * 0.1f;
            puff.transform.localScale = Vector3.one * puffSize * Random.Range(0.5f, 1.2f);
            SetMatTransparent(puff, new Color(0.92f, 0.88f, 0.80f, 0.75f));
            StartCoroutine(DebrisFade(puff, radius));
        }
        yield return null;
    }

    IEnumerator DebrisFade(GameObject obj, float radius)
    {
        var r = obj.GetComponent<Renderer>();
        float t = 0f;
        Vector3 vel = (Random.onUnitSphere + Vector3.up).normalized * radius * 0.4f;
        while (t < 0.55f && obj != null)
        {
            t += Time.deltaTime;
            float p = t / 0.55f;
            obj.transform.position += vel * Time.deltaTime;
            obj.transform.localScale *= 1f + Time.deltaTime * 1.8f;
            vel += Vector3.down * radius * 0.3f * Time.deltaTime;
            if (r != null)
            {
                var c = r.material.color;
                r.material.color = new Color(c.r, c.g, c.b, Mathf.Lerp(0.75f, 0f, p * p));
            }
            yield return null;
        }
        if (obj != null) Destroy(obj);
    }

    IEnumerator ShrinkAndDestroy(GameObject obj, float dur)
    {
        if (obj == null) yield break;
        Vector3 s = obj.transform.localScale;
        float t = 0f;
        while (t < dur && obj != null)
        {
            t += Time.deltaTime;
            obj.transform.localScale = Vector3.Lerp(s, Vector3.zero, t / dur);
            yield return null;
        }
        if (obj != null) Destroy(obj);
    }

    IEnumerator Phase_Reveal(Vector3 center, float radius)
    {
        SpawnStars(center + Vector3.up * radius * 0.2f, 20, radius * 0.9f);

        var snapshot = new List<GameObject>(_clouds);
        _clouds.Clear();

        float t = 0f;
        while (t < 0.7f)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / 0.7f);
            foreach (var c in snapshot)
            {
                if (c == null) continue;
                c.transform.localScale *= 1f + Time.deltaTime * 3f;
                foreach (var r in c.GetComponentsInChildren<Renderer>())
                {
                    var col = r.material.color;
                    float a = Mathf.Lerp(col.a, 0f, p * 0.15f);
                    r.material.color = new Color(col.r, col.g, col.b, a);
                    if (r.material.HasProperty("_BaseColor"))
                        r.material.SetColor("_BaseColor", r.material.color);
                }
            }
            yield return null;
        }

        foreach (var c in snapshot) if (c != null) Destroy(c);

        ShowAll();
        transform.localScale = _originalScale * 1.15f;
        t = 0f;
        while (t < 0.35f)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(_originalScale * 1.15f, _originalScale, t / 0.35f);
            yield return null;
        }
        transform.localScale = _originalScale;

        SpawnConfetti();
        yield return new WaitForSeconds(0.5f);
    }

    IEnumerator ShowContinueButton()
    {
        bool pressed = false;

        var btnObj = new GameObject("ContinueBtn");
        btnObj.transform.SetParent(_uiCanvas.transform, false);
        var rt = btnObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, -80f);
        rt.sizeDelta = new Vector2(440f, 130f);
        rt.localScale = Vector3.zero;

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
            fadeDuration = 0.08f
        };
        btn.onClick.AddListener(() => pressed = true);

        var lblObj = new GameObject("Label");
        lblObj.transform.SetParent(btnObj.transform, false);
        var lrt = lblObj.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        var txt = lblObj.AddComponent<Text>();
        txt.text = LanguageManager.Instance != null ? LanguageManager.Instance.Get("btn_continue") : "Doorgaan";
        txt.font = GetFont();
        txt.fontSize = 52;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.raycastTarget = false;
        lblObj.AddComponent<Outline>().effectColor = new Color(0f, 0.2f, 0.1f, 0.6f);

        float t = 0f;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            float p = t / 0.3f;
            float overshoot = 1f + Mathf.Sin(p * Mathf.PI) * 0.15f;
            rt.localScale = Vector3.one * Mathf.SmoothStep(0f, 1f, p) * overshoot;
            yield return null;
        }
        rt.localScale = Vector3.one;

        while (!pressed)
            yield return null;

        t = 0f;
        while (t < 0.15f)
        {
            t += Time.deltaTime;
            rt.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t / 0.15f);
            yield return null;
        }

        Destroy(btnObj);
    }

    IEnumerator PulseCloud(Transform t, Vector3 baseScale)
    {
        float offset = Random.Range(0f, Mathf.PI * 2f);
        while (t != null && _clouds.Contains(t.gameObject))
        {
            float p = 1f + Mathf.Sin(Time.time * 1.8f + offset) * 0.06f;
            t.localScale = baseScale * p;
            yield return null;
        }
    }

    IEnumerator CloudPuff(Vector3 pos, float size)
    {
        var cloud = MakeCloud(pos, size);
        float t = 0f;
        while (t < 0.55f && cloud != null)
        {
            t += Time.deltaTime;
            float p = t / 0.55f;
            cloud.transform.localScale *= 1f + Time.deltaTime * 2.5f;
            cloud.transform.position += Vector3.up * Time.deltaTime * size * 0.4f;
            foreach (var r in cloud.GetComponentsInChildren<Renderer>())
            {
                var col = r.material.color;
                r.material.color = new Color(col.r, col.g, col.b, Mathf.Lerp(0.9f, 0f, p * p));
            }
            yield return null;
        }
        if (cloud != null) Destroy(cloud);
    }

    GameObject MakeCloud(Vector3 pos, float size)
    {
        var root = new GameObject("Cloud");
        root.transform.position = pos;

        float[] xOff = { 0f, 0.38f, -0.32f, 0.18f, -0.18f, 0.08f };
        float[] yOff = { 0f, 0.05f, 0.08f, -0.06f, 0.04f, 0.12f };
        float[] scales = { 1f, 0.72f, 0.68f, 0.55f, 0.52f, 0.45f };

        for (int i = 0; i < xOff.Length; i++)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(sphere.GetComponent<Collider>());
            sphere.transform.SetParent(root.transform);
            sphere.transform.localPosition = new Vector3(xOff[i] * size, yOff[i] * size, Random.Range(-0.08f, 0.08f) * size);
            sphere.transform.localScale = Vector3.one * size * scales[i];
            SetMatTransparent(sphere, new Color(1f, 1f, 1f, 0.92f));
        }

        return root;
    }

    void SpawnStars(Vector3 pos, int count, float spread)
    {
        Color[] cols = {
            new Color(1f, 0.95f, 0.15f), new Color(1f, 0.42f, 0.85f),
            new Color(0.3f, 0.88f, 1f),  Color.white, new Color(1f, 0.55f, 0.1f)
        };
        float starSize = _worldBounds.size.magnitude * 0.018f;
        for (int i = 0; i < count; i++)
        {
            var star = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(star.GetComponent<Collider>());
            star.transform.position = pos + Random.insideUnitSphere * spread * 0.12f;
            star.transform.localScale = Vector3.one * starSize;
            star.transform.rotation = Random.rotation;
            SetMat(star, cols[Random.Range(0, cols.Length)]);
            Vector3 vel = (Random.onUnitSphere + Vector3.up * 0.6f).normalized * spread;
            StartCoroutine(StarFly(star, vel, spread));
        }
    }

    IEnumerator StarFly(GameObject obj, Vector3 vel, float spread)
    {
        float t = 0f;
        Vector3 startScale = obj.transform.localScale;
        float gravity = spread * 2.2f;
        while (t < 0.55f && obj != null)
        {
            t += Time.deltaTime;
            vel += Vector3.down * gravity * Time.deltaTime;
            obj.transform.position += vel * Time.deltaTime;
            obj.transform.Rotate(Vector3.one * 200f * Time.deltaTime);
            obj.transform.localScale = startScale * Mathf.Lerp(1f, 0f, (t / 0.55f) * (t / 0.55f));
            yield return null;
        }
        if (obj != null) Destroy(obj);
    }

    void SpawnConfetti()
    {
        if (_uiCanvas == null) return;
        Color[] cols = {
            new Color(1f, 0.22f, 0.22f), new Color(1f, 0.88f, 0.1f),
            new Color(0.2f, 0.82f, 0.3f), new Color(0.2f, 0.5f, 1f),
            new Color(0.9f, 0.3f, 1f),   new Color(1f, 0.55f, 0.1f)
        };
        for (int i = 0; i < 55; i++)
        {
            var piece = new GameObject("Confetti");
            piece.transform.SetParent(_uiCanvas.transform, false);
            var rt = piece.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(Random.Range(16f, 36f), Random.Range(20f, 46f));
            rt.anchoredPosition = new Vector2(Random.Range(-500f, 500f), Random.Range(0f, 60f));
            var img = piece.AddComponent<Image>();
            img.color = cols[Random.Range(0, cols.Length)];
            img.raycastTarget = false;
            float spin = Random.Range(-280f, 280f);
            float fall = Random.Range(400f, 860f);
            float sway = Random.Range(0.8f, 2.2f);
            float amount = Random.Range(55f, 170f);
            float startX = rt.anchoredPosition.x;
            StartCoroutine(AnimConfetti(rt, img, spin, fall, sway, amount, startX));
        }
    }

    IEnumerator AnimConfetti(RectTransform rt, Image img, float spin, float fall, float sway, float amount, float startX)
    {
        float t = 0f;
        while (t < 2.6f && rt != null)
        {
            t += Time.deltaTime;
            rt.anchoredPosition = new Vector2(startX + Mathf.Sin(t * sway) * amount, rt.anchoredPosition.y - fall * Time.deltaTime);
            rt.localRotation = Quaternion.Euler(0f, 0f, rt.localEulerAngles.z + spin * Time.deltaTime);
            img.color = new Color(img.color.r, img.color.g, img.color.b, Mathf.Lerp(1f, 0f, Mathf.Pow(t / 2.6f, 2.5f)));
            yield return null;
        }
        if (rt != null) Destroy(rt.gameObject);
    }

    void SetupBuildUI()
    {
        EnsureEventSystem();

        var cObj = new GameObject("BuildCanvas");
        _uiCanvas = cObj.AddComponent<Canvas>();
        _uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _uiCanvas.sortingOrder = 10;
        var scaler = cObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        cObj.AddComponent<GraphicRaycaster>();

        var card = new GameObject("Card");
        card.transform.SetParent(cObj.transform, false);
        var cardRt = card.AddComponent<RectTransform>();
        cardRt.anchorMin = new Vector2(0.5f, 0f);
        cardRt.anchorMax = new Vector2(0.5f, 0f);
        cardRt.pivot = new Vector2(0.5f, 0f);
        cardRt.anchoredPosition = new Vector2(0f, 30f);
        cardRt.sizeDelta = new Vector2(460f, 130f);
        cardRt.localScale = Vector3.zero;
        var cardBg = card.AddComponent<Image>();
        cardBg.color = new Color(0.16f, 0.09f, 0.04f, 0.95f);
        cardBg.raycastTarget = false;

        var accent = new GameObject("AccentTop");
        accent.transform.SetParent(card.transform, false);
        var acRt = accent.AddComponent<RectTransform>();
        acRt.anchorMin = new Vector2(0f, 1f);
        acRt.anchorMax = new Vector2(1f, 1f);
        acRt.pivot = new Vector2(0.5f, 1f);
        acRt.anchoredPosition = Vector2.zero;
        acRt.sizeDelta = new Vector2(0f, 6f);
        accent.AddComponent<Image>().color = new Color(0.76f, 0.52f, 0.22f);

        var accentBot = new GameObject("AccentBot");
        accentBot.transform.SetParent(card.transform, false);
        var acBotRt = accentBot.AddComponent<RectTransform>();
        acBotRt.anchorMin = new Vector2(0f, 0f);
        acBotRt.anchorMax = new Vector2(1f, 0f);
        acBotRt.pivot = new Vector2(0.5f, 0f);
        acBotRt.anchoredPosition = Vector2.zero;
        acBotRt.sizeDelta = new Vector2(0f, 6f);
        accentBot.AddComponent<Image>().color = new Color(0.76f, 0.52f, 0.22f);

        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(card.transform, false);
        var titleRt = titleObj.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 1f);
        titleRt.anchorMax = new Vector2(0.5f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -10f);
        titleRt.sizeDelta = new Vector2(440f, 32f);
        var titleTxt = titleObj.AddComponent<Text>();
        titleTxt.text = LanguageManager.Instance != null
            ? LanguageManager.Instance.Get("building_title")
            : "Verblijf bouwen!";
        titleTxt.font = GetFont();
        titleTxt.fontSize = 22;
        titleTxt.fontStyle = FontStyle.Bold;
        titleTxt.alignment = TextAnchor.MiddleCenter;
        titleTxt.color = new Color(1f, 0.82f, 0.42f);
        titleTxt.raycastTarget = false;
        titleObj.AddComponent<Outline>().effectColor = new Color(0.3f, 0.1f, 0f, 0.8f);

        var tObj = new GameObject("LabelText");
        tObj.transform.SetParent(card.transform, false);
        var trt = tObj.AddComponent<RectTransform>();
        trt.anchorMin = new Vector2(0.5f, 0.5f);
        trt.anchorMax = new Vector2(0.5f, 0.5f);
        trt.pivot = new Vector2(0.5f, 0.5f);
        trt.anchoredPosition = new Vector2(0f, 2f);
        trt.sizeDelta = new Vector2(430f, 38f);
        _buildLabel = tObj.AddComponent<Text>();
        _buildLabel.font = GetFont();
        _buildLabel.fontSize = 24;
        _buildLabel.fontStyle = FontStyle.Bold;
        _buildLabel.alignment = TextAnchor.MiddleCenter;
        _buildLabel.color = Color.white;
        _buildLabel.raycastTarget = false;
        tObj.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);

        var barBg = new GameObject("ProgressBg");
        barBg.transform.SetParent(card.transform, false);
        var barBgRt = barBg.AddComponent<RectTransform>();
        barBgRt.anchorMin = new Vector2(0.5f, 0f);
        barBgRt.anchorMax = new Vector2(0.5f, 0f);
        barBgRt.pivot = new Vector2(0.5f, 0f);
        barBgRt.anchoredPosition = new Vector2(0f, 14f);
        barBgRt.sizeDelta = new Vector2(420f, 18f);
        barBg.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

        var barFill = new GameObject("ProgressFill");
        barFill.transform.SetParent(barBg.transform, false);
        _progressBar = barFill.AddComponent<RectTransform>();
        _progressBar.anchorMin = new Vector2(0f, 0f);
        _progressBar.anchorMax = new Vector2(0f, 1f);
        _progressBar.pivot = new Vector2(0f, 0.5f);
        _progressBar.offsetMin = new Vector2(3f, 3f);
        _progressBar.offsetMax = new Vector2(3f, -3f);
        _progressBar.sizeDelta = new Vector2(0f, 0f);
        barFill.AddComponent<Image>().color = new Color(0.76f, 0.52f, 0.22f);

        StartCoroutine(PopInCard(cardRt));
        StartCoroutine(CycleFunText());
    }

    private static readonly string[] _funTextKeys =
    {
        "building_fun_0",
        "building_fun_1",
        "building_fun_2",
        "building_fun_3",
        "building_fun_4",
        "building_fun_5",
        "building_fun_6",
        "building_fun_7",
        "building_fun_8",
        "building_fun_9",
    };

    IEnumerator CycleFunText()
    {
        int idx = 0;
        while (_buildLabel != null)
        {
            string text = (LanguageManager.Instance != null)
                ? LanguageManager.Instance.Get(_funTextKeys[idx % _funTextKeys.Length])
                : _funTextKeys[idx % _funTextKeys.Length];
            SetBuildLabel(text);
            idx++;
            yield return new WaitForSeconds(1.1f);
        }
    }

    IEnumerator PopInCard(RectTransform rt)
    {
        float t = 0f;
        while (t < 0.35f)
        {
            t += Time.deltaTime;
            float p = t / 0.35f;
            float overshoot = 1f + Mathf.Sin(p * Mathf.PI) * 0.15f;
            rt.localScale = Vector3.one * Mathf.SmoothStep(0f, 1f, p) * overshoot;
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    void SetBuildLabel(string text) { if (_buildLabel != null) _buildLabel.text = text; }

    void SetProgress(float p)
    {
        if (_progressBar == null) return;
        _progressBar.sizeDelta = new Vector2(414f * p, 0f);
    }

    void CleanupUI()
    {
        if (_uiCanvas != null) Destroy(_uiCanvas.gameObject);
    }

    Bounds GetWorldBounds()
    {
        var renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return new Bounds(transform.position, Vector3.one * 10f);

        var b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);
        return b;
    }

    void SetMat(GameObject obj, Color col)
    {
        var r = obj.GetComponent<Renderer>();
        if (r == null) return;
        var sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var m = new Material(sh);
        m.color = col;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
        r.material = m;
    }

    void SetMatTransparent(GameObject obj, Color col)
    {
        var r = obj.GetComponent<Renderer>();
        if (r == null) return;
        var sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var m = new Material(sh);
        m.color = col;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
        if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.renderQueue = 3000;
        r.material = m;
    }

    void EnsureEventSystem()
    {
        if (UnityEngine.EventSystems.EventSystem.current != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
    }

    static Font GetFont()
    {
        var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return f ?? Font.CreateDynamicFontFromOSFont("Arial", 24);
    }
}