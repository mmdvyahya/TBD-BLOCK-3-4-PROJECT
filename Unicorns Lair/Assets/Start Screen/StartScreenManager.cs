using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StartScreenManager : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float flyDuration  = 3.5f;
    [SerializeField] private float irisStartAt  = 0.72f;
    [SerializeField] private float irisDuration = 0.9f;

    private static readonly Vector3    EndPos = new Vector3(-125.4f, 26.1f, 1070.5f);
    private static readonly Quaternion EndRot = Quaternion.Euler(10.05f, 90.855f, 0f);

    private Camera  _cam;
    private Canvas  _canvas;
    private bool    _flying;

    void Start()
    {
        _cam = Camera.main;
        if (_cam == null)
        {
            var co = new GameObject("Main Camera");
            _cam   = co.AddComponent<Camera>();
            co.tag = "MainCamera";
        }

        EnsureEventSystem();
        BuildUI();
    }

    void BuildUI()
    {
        var cObj = new GameObject("StartCanvas");
        _canvas = cObj.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = cObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight  = 0.5f;

        cObj.AddComponent<GraphicRaycaster>();

        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(_canvas.transform, false);
        var trt = titleObj.AddComponent<RectTransform>();
        trt.anchorMin        = new Vector2(0.5f, 0.5f);
        trt.anchorMax        = new Vector2(0.5f, 0.5f);
        trt.pivot            = new Vector2(0.5f, 0.5f);
        trt.anchoredPosition = new Vector2(0f, 260f);
        trt.sizeDelta        = new Vector2(960f, 220f);
        var titleTxt = titleObj.AddComponent<Text>();
        titleTxt.text          = "Wildlands Game";
        titleTxt.font          = GetFont();
        titleTxt.fontSize      = 100;
        titleTxt.fontStyle     = FontStyle.Bold;
        titleTxt.alignment     = TextAnchor.MiddleCenter;
        titleTxt.color         = Color.white;
        titleTxt.raycastTarget = false;
        var to = titleObj.AddComponent<Outline>();
        to.effectColor    = new Color(0f, 0f, 0f, 0.65f);
        to.effectDistance = new Vector2(5f, -5f);

        var btnObj = new GameObject("SpelenBtn");
        btnObj.transform.SetParent(_canvas.transform, false);
        var brt = btnObj.AddComponent<RectTransform>();
        brt.anchorMin        = new Vector2(0.5f, 0.5f);
        brt.anchorMax        = new Vector2(0.5f, 0.5f);
        brt.pivot            = new Vector2(0.5f, 0.5f);
        brt.anchoredPosition = new Vector2(0f, -40f);
        brt.sizeDelta        = new Vector2(420f, 150f);

        var btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.12f, 0.72f, 0.36f);

        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        btn.colors = new ColorBlock
        {
            normalColor      = new Color(0.12f, 0.72f, 0.36f),
            highlightedColor = new Color(0.20f, 0.88f, 0.48f),
            pressedColor     = new Color(0.07f, 0.48f, 0.22f),
            selectedColor    = new Color(0.12f, 0.72f, 0.36f),
            disabledColor    = new Color(0.35f, 0.35f, 0.35f),
            colorMultiplier  = 1f,
            fadeDuration     = 0.1f
        };
        btn.onClick.AddListener(OnSpelen);

        var lblObj = new GameObject("Label");
        lblObj.transform.SetParent(btnObj.transform, false);
        var lrt = lblObj.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        var lTxt = lblObj.AddComponent<Text>();
        lTxt.text          = "Spelen  ▶";
        lTxt.font          = GetFont();
        lTxt.fontSize      = 64;
        lTxt.fontStyle     = FontStyle.Bold;
        lTxt.alignment     = TextAnchor.MiddleCenter;
        lTxt.color         = Color.white;
        lTxt.raycastTarget = false;
        var lo = lblObj.AddComponent<Outline>();
        lo.effectColor    = new Color(0f, 0.2f, 0.1f, 0.6f);
        lo.effectDistance = new Vector2(2f, -2f);

        StartCoroutine(PulseButton(brt));
    }

    IEnumerator PulseButton(RectTransform rt)
    {
        while (!_flying)
        {
            float s = 1f + Mathf.Sin(Time.time * 2.2f) * 0.04f;
            rt.localScale = Vector3.one * s;
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    void OnSpelen()
    {
        if (_flying) return;
        _flying = true;
        StartCoroutine(FlyAndWipe());
    }

    IEnumerator FlyAndWipe()
    {
        HideUI();

        Vector3    startPos = _cam.transform.position;
        Quaternion startRot = _cam.transform.rotation;

        bool      irisStarted   = false;
        Coroutine irisCoroutine = null;

        float t = 0f;
        while (t < flyDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / flyDuration));

            _cam.transform.position = Vector3.Lerp(startPos, EndPos, p);
            _cam.transform.rotation = Quaternion.Slerp(startRot, EndRot, p);

            if (!irisStarted && t / flyDuration >= irisStartAt)
            {
                irisStarted   = true;
                irisCoroutine = StartCoroutine(IrisClose(irisDuration));
            }

            yield return null;
        }

        _cam.transform.position = EndPos;
        _cam.transform.rotation = EndRot;

        if (irisCoroutine != null)
            yield return irisCoroutine;

        yield return new WaitForSeconds(0.2f);
        SceneManager.LoadScene("BuyHabitatScreen");
    }

    IEnumerator IrisClose(float duration)
    {
        int res     = 128;
        float cx    = res * 0.5f;
        float cy    = res * 0.5f;
        float aspect = (float)Screen.height / Mathf.Max(Screen.width, 1);

        float[] dists = new float[res * res];
        float maxDist = 0f;
        for (int i = 0; i < res * res; i++)
        {
            float nx = ((i % res) - cx) / res;
            float ny = ((i / res) - cy) / res * aspect;
            float d  = Mathf.Sqrt(nx * nx + ny * ny);
            dists[i] = d;
            if (d > maxDist) maxDist = d;
        }

        var tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode   = TextureWrapMode.Clamp;

        var irisObj = new GameObject("IrisWipe");
        irisObj.transform.SetParent(_canvas.transform, false);
        irisObj.transform.SetAsLastSibling();
        var irt = irisObj.AddComponent<RectTransform>();
        irt.anchorMin = Vector2.zero;
        irt.anchorMax = Vector2.one;
        irt.offsetMin = irt.offsetMax = Vector2.zero;
        var raw = irisObj.AddComponent<RawImage>();
        raw.texture       = tex;
        raw.raycastTarget = false;

        Color[] pixels  = new Color[res * res];
        float   feather = 0.018f;
        float   elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float p      = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            float radius = Mathf.Lerp(maxDist * 1.05f, 0f, p);

            for (int i = 0; i < pixels.Length; i++)
            {
                float alpha  = Mathf.Clamp01((dists[i] - radius) / feather + 0.5f);
                pixels[i]    = new Color(0f, 0f, 0f, alpha);
            }

            tex.SetPixels(pixels);
            tex.Apply(false);
            yield return null;
        }

        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.black;
        tex.SetPixels(pixels);
        tex.Apply(false);
    }

    void HideUI()
    {
        foreach (Transform child in _canvas.transform)
            child.gameObject.SetActive(false);
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
