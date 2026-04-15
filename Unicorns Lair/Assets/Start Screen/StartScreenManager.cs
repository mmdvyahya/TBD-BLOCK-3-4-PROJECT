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

        LanguageManager.Ensure();

        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(_canvas.transform, false);
        var trt = titleObj.AddComponent<RectTransform>();
        trt.anchorMin        = new Vector2(0.5f, 0.5f);
        trt.anchorMax        = new Vector2(0.5f, 0.5f);
        trt.pivot            = new Vector2(0.5f, 0.5f);
        trt.anchoredPosition = new Vector2(0f, 260f);
        trt.sizeDelta        = new Vector2(960f, 220f);
        var titleTxt = titleObj.AddComponent<Text>();
        titleTxt.font          = GetFont();
        titleTxt.fontSize      = 100;
        titleTxt.fontStyle     = FontStyle.Bold;
        titleTxt.alignment     = TextAnchor.MiddleCenter;
        titleTxt.color         = Color.white;
        titleTxt.raycastTarget = false;
        var titleLoc = titleObj.AddComponent<LocalizedText>();
        titleLoc.key = "title_wildlands";
        titleLoc.Refresh();
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
        lTxt.font          = GetFont();
        lTxt.fontSize      = 64;
        lTxt.fontStyle     = FontStyle.Bold;
        lTxt.alignment     = TextAnchor.MiddleCenter;
        lTxt.color         = Color.white;
        lTxt.raycastTarget = false;
        var spelenLoc = lblObj.AddComponent<LocalizedText>();
        spelenLoc.key = "btn_spelen";
        spelenLoc.Refresh();
        var lo = lblObj.AddComponent<Outline>();
        lo.effectColor    = new Color(0f, 0.2f, 0.1f, 0.6f);
        lo.effectDistance = new Vector2(2f, -2f);

        MakeSettingsButton(_canvas.transform);

        StartCoroutine(PulseButton(brt));
    }

    void MakeSettingsButton(Transform parent)
    {
        var obj = new GameObject("SettingsBtn");
        obj.transform.SetParent(parent, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(1f, 1f);
        rt.anchorMax        = new Vector2(1f, 1f);
        rt.pivot            = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-30f, -30f);
        rt.sizeDelta        = new Vector2(120f, 120f);

        var img = obj.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.45f);

        var btn = obj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.colors = new ColorBlock
        {
            normalColor      = new Color(0f, 0f, 0f, 0.45f),
            highlightedColor = new Color(0.2f, 0.2f, 0.2f, 0.7f),
            pressedColor     = new Color(0f, 0f, 0f, 0.8f),
            selectedColor    = new Color(0f, 0f, 0f, 0.45f),
            disabledColor    = new Color(0f, 0f, 0f, 0.2f),
            colorMultiplier  = 1f,
            fadeDuration     = 0.08f
        };
        btn.onClick.AddListener(() => BuildSettingsPanel());

        var lObj = new GameObject("Icon");
        lObj.transform.SetParent(obj.transform, false);
        var lrt = lObj.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        var lTxt = lObj.AddComponent<Text>();
        lTxt.text          = "⚙";
        lTxt.font          = GetFont();
        lTxt.fontSize      = 72;
        lTxt.alignment     = TextAnchor.MiddleCenter;
        lTxt.color         = Color.white;
        lTxt.raycastTarget = false;
    }

    void BuildSettingsPanel()
    {
        var existing = _canvas.transform.Find("SettingsPanel");
        if (existing != null) { Destroy(existing.gameObject); return; }

        var panel = new GameObject("SettingsPanel");
        panel.transform.SetParent(_canvas.transform, false);
        var prt = panel.AddComponent<RectTransform>();
        prt.anchorMin = Vector2.zero;
        prt.anchorMax = Vector2.one;
        prt.offsetMin = prt.offsetMax = Vector2.zero;
        var overlay = panel.AddComponent<Image>();
        overlay.color         = new Color(0f, 0f, 0f, 0.78f);
        overlay.raycastTarget = true;

        var box = new GameObject("Box");
        box.transform.SetParent(panel.transform, false);
        var brt = box.AddComponent<RectTransform>();
        brt.anchorMin        = new Vector2(0.5f, 0.5f);
        brt.anchorMax        = new Vector2(0.5f, 0.5f);
        brt.pivot            = new Vector2(0.5f, 0.5f);
        brt.anchoredPosition = Vector2.zero;
        brt.sizeDelta        = new Vector2(720f, 760f);
        var boxImg = box.AddComponent<Image>();
        boxImg.color         = new Color(0.10f, 0.15f, 0.22f);
        boxImg.raycastTarget = false;

        MakePanelLabel(box.transform, "settings_title",    56, FontStyle.Bold,   Color.white,                    new Vector2(0f,  290f), new Vector2(680f, 90f));
        MakePanelLabel(box.transform, "settings_language", 36, FontStyle.Normal, new Color(0.72f, 0.85f, 1f),    new Vector2(0f,  190f), new Vector2(680f, 60f));

        var langs = new[] {
            ("Nederlands", Language.Nederlands),
            ("Deutsch",    Language.Deutsch),
            ("English",    Language.English),
        };

        float[] yPos = { 80f, -60f, -200f };

        for (int i = 0; i < langs.Length; i++)
        {
            var (name, lang) = langs[i];
            bool  active  = LanguageManager.Instance.CurrentLanguage == lang;
            Color btnCol  = active ? new Color(0.12f, 0.62f, 0.32f) : new Color(0.22f, 0.30f, 0.44f);

            var lbObj = new GameObject($"Lang_{name}");
            lbObj.transform.SetParent(box.transform, false);
            var lbrt = lbObj.AddComponent<RectTransform>();
            lbrt.anchorMin        = new Vector2(0.5f, 0.5f);
            lbrt.anchorMax        = new Vector2(0.5f, 0.5f);
            lbrt.pivot            = new Vector2(0.5f, 0.5f);
            lbrt.anchoredPosition = new Vector2(0f, yPos[i]);
            lbrt.sizeDelta        = new Vector2(620f, 110f);

            var lbImg = lbObj.AddComponent<Image>();
            lbImg.color = btnCol;
            var lbBtn = lbObj.AddComponent<Button>();
            lbBtn.targetGraphic = lbImg;
            lbBtn.colors = new ColorBlock
            {
                normalColor      = btnCol,
                highlightedColor = btnCol * 1.2f,
                pressedColor     = btnCol * 0.75f,
                selectedColor    = btnCol,
                disabledColor    = btnCol * 0.5f,
                colorMultiplier  = 1f,
                fadeDuration     = 0.08f
            };

            Language capturedLang  = lang;
            GameObject capturedPanel = panel;
            lbBtn.onClick.AddListener(() => {
                LanguageManager.Instance.SetLanguage(capturedLang);
                Destroy(capturedPanel);
                BuildSettingsPanel();
            });

            var llbObj = new GameObject("Label");
            llbObj.transform.SetParent(lbObj.transform, false);
            var llbrt = llbObj.AddComponent<RectTransform>();
            llbrt.anchorMin = Vector2.zero;
            llbrt.anchorMax = Vector2.one;
            llbrt.offsetMin = llbrt.offsetMax = Vector2.zero;
            var llbTxt = llbObj.AddComponent<Text>();
            llbTxt.text          = (active ? "✓  " : "       ") + name;
            llbTxt.font          = GetFont();
            llbTxt.fontSize      = 50;
            llbTxt.fontStyle     = FontStyle.Bold;
            llbTxt.alignment     = TextAnchor.MiddleCenter;
            llbTxt.color         = Color.white;
            llbTxt.raycastTarget = false;
        }

        var closeObj = new GameObject("CloseBtn");
        closeObj.transform.SetParent(box.transform, false);
        var crt = closeObj.AddComponent<RectTransform>();
        crt.anchorMin        = new Vector2(0.5f, 0.5f);
        crt.anchorMax        = new Vector2(0.5f, 0.5f);
        crt.pivot            = new Vector2(0.5f, 0.5f);
        crt.anchoredPosition = new Vector2(0f, -330f);
        crt.sizeDelta        = new Vector2(480f, 100f);
        var cImg = closeObj.AddComponent<Image>();
        cImg.color = new Color(0.55f, 0.18f, 0.18f);
        var cBtn = closeObj.AddComponent<Button>();
        cBtn.targetGraphic = cImg;
        cBtn.colors = new ColorBlock
        {
            normalColor      = new Color(0.55f, 0.18f, 0.18f),
            highlightedColor = new Color(0.72f, 0.25f, 0.25f),
            pressedColor     = new Color(0.38f, 0.10f, 0.10f),
            selectedColor    = new Color(0.55f, 0.18f, 0.18f),
            disabledColor    = new Color(0.3f,  0.3f,  0.3f),
            colorMultiplier  = 1f,
            fadeDuration     = 0.08f
        };
        cBtn.onClick.AddListener(() => Destroy(panel));

        var clObj = new GameObject("Label");
        clObj.transform.SetParent(closeObj.transform, false);
        var clrt = clObj.AddComponent<RectTransform>();
        clrt.anchorMin = Vector2.zero;
        clrt.anchorMax = Vector2.one;
        clrt.offsetMin = clrt.offsetMax = Vector2.zero;
        var clTxt = clObj.AddComponent<Text>();
        clTxt.font          = GetFont();
        clTxt.fontSize      = 46;
        clTxt.fontStyle     = FontStyle.Bold;
        clTxt.alignment     = TextAnchor.MiddleCenter;
        clTxt.color         = Color.white;
        clTxt.raycastTarget = false;
        var closeLoc = clObj.AddComponent<LocalizedText>();
        closeLoc.key = "btn_close";
        closeLoc.Refresh();
    }

    void MakePanelLabel(Transform parent, string key, int size, FontStyle style, Color color, Vector2 pos, Vector2 sizeDelta)
    {
        var obj = new GameObject(key);
        obj.transform.SetParent(parent, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta        = sizeDelta;
        var txt = obj.AddComponent<Text>();
        txt.font          = GetFont();
        txt.fontSize      = size;
        txt.fontStyle     = style;
        txt.alignment     = TextAnchor.MiddleCenter;
        txt.color         = color;
        txt.raycastTarget = false;
        var loc = obj.AddComponent<LocalizedText>();
        loc.key = key;
        loc.Refresh();
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
