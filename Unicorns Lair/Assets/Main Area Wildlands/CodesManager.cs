using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CodesManager : MonoBehaviour
{
    [System.Serializable]
    public class CodeEntry
    {
        public string code;
        public string skinName;
        public string rewardMessage;
    }

    [Header("Codes (4 digits each)")]
    [SerializeField]
    private List<CodeEntry> codes = new()
    {
        new CodeEntry { code = "1234", skinName = "Golden Beaver",   rewardMessage = "" },
        new CodeEntry { code = "0000", skinName = "Polar Explorer",  rewardMessage = "" },
        new CodeEntry { code = "4242", skinName = "Sunset Parrot",   rewardMessage = "" },
        new CodeEntry { code = "9999", skinName = "Desert Prairie",  rewardMessage = "" },
    };

    private const string SKIN_PREF_KEY = "unlocked_skins";

    private Canvas _canvas;
    private GameObject _panel;
    private Text _displayText;
    private Text _statusText;
    private Image _displayBg;
    private GameObject _skinDrawer;
    private string _currentInput = "";
    private bool _open;
    private bool _animating;

    private HashSet<string> _unlockedSkins = new();

    private static readonly Color DisplayNormal = new Color(0.05f, 0.10f, 0.18f, 0.95f);
    private static readonly Color DisplaySuccess = new Color(0.10f, 0.55f, 0.25f, 0.95f);
    private static readonly Color DisplayError = new Color(0.65f, 0.15f, 0.15f, 0.95f);

    void Start()
    {
        LanguageManager.Ensure();
        LoadUnlockedSkins();
        BuildButton();
    }

    void LoadUnlockedSkins()
    {
        _unlockedSkins.Clear();
        var saved = PlayerPrefs.GetString(SKIN_PREF_KEY, "");
        if (!string.IsNullOrEmpty(saved))
            foreach (var s in saved.Split('|'))
                if (!string.IsNullOrEmpty(s)) _unlockedSkins.Add(s);
    }

    void SaveUnlockedSkins()
    {
        PlayerPrefs.SetString(SKIN_PREF_KEY, string.Join("|", _unlockedSkins));
        PlayerPrefs.Save();
    }

    void BuildButton()
    {
        var cObj = new GameObject("CodesCanvas");
        _canvas = cObj.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 7;
        var scaler = cObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        cObj.AddComponent<GraphicRaycaster>();
        EnsureEventSystem();

        var btnObj = new GameObject("CodesBtn");
        btnObj.transform.SetParent(cObj.transform, false);
        var rt = btnObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-20f, 20f);
        rt.sizeDelta = new Vector2(220f, 100f);

        var img = btnObj.AddComponent<Image>();
        img.color = new Color(0.42f, 0.18f, 0.62f, 0.92f);

        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.colors = new ColorBlock
        {
            normalColor = new Color(0.42f, 0.18f, 0.62f, 0.92f),
            highlightedColor = new Color(0.55f, 0.25f, 0.78f, 1f),
            pressedColor = new Color(0.30f, 0.12f, 0.45f, 1f),
            selectedColor = new Color(0.42f, 0.18f, 0.62f, 0.92f),
            disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f),
            colorMultiplier = 1f,
            fadeDuration = 0.08f
        };
        btn.onClick.AddListener(OpenPanel);

        var lObj = new GameObject("Label");
        lObj.transform.SetParent(btnObj.transform, false);
        var lrt = lObj.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        var lTxt = lObj.AddComponent<Text>();
        lTxt.text = SafeGet("codes_title", "Codes");
        lTxt.font = GetFont();
        lTxt.fontSize = 38;
        lTxt.fontStyle = FontStyle.Bold;
        lTxt.alignment = TextAnchor.MiddleCenter;
        lTxt.color = Color.white;
        lTxt.raycastTarget = false;
        var loc = lObj.AddComponent<LocalizedText>();
        loc.key = "codes_title";
    }

    void OpenPanel()
    {
        if (_open || _animating) return;
        _open = true;
        _currentInput = "";

        var overlay = new GameObject("CodesPanel");
        overlay.transform.SetParent(_canvas.transform, false);
        _panel = overlay;

        var ort = overlay.AddComponent<RectTransform>();
        ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one;
        ort.offsetMin = ort.offsetMax = Vector2.zero;

        var bgImg = overlay.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.6f);

        BuildInfoPanel(overlay.transform);
        BuildCodeCard(overlay.transform);
    }

    void BuildInfoPanel(Transform parent)
    {
        var info = new GameObject("InfoPanel");
        info.transform.SetParent(parent, false);
        var iRt = info.AddComponent<RectTransform>();
        iRt.anchorMin = new Vector2(0f, 0.5f); iRt.anchorMax = new Vector2(0f, 0.5f);
        iRt.pivot = new Vector2(0f, 0.5f);
        iRt.anchoredPosition = new Vector2(40f, 0f);
        iRt.sizeDelta = new Vector2(380f, 800f);
        iRt.localScale = Vector3.zero;
        var iImg = info.AddComponent<Image>();
        iImg.color = new Color(0.10f, 0.14f, 0.22f, 0.95f);

        var iAccent = new GameObject("Accent");
        iAccent.transform.SetParent(info.transform, false);
        var iaRt = iAccent.AddComponent<RectTransform>();
        iaRt.anchorMin = new Vector2(0f, 1f); iaRt.anchorMax = new Vector2(1f, 1f);
        iaRt.pivot = new Vector2(0.5f, 1f); iaRt.anchoredPosition = Vector2.zero; iaRt.sizeDelta = new Vector2(0f, 8f);
        iAccent.AddComponent<Image>().color = new Color(1f, 0.85f, 0.25f);

        MakeLabel(info.transform, "Wildlands Tip!",
            new Vector2(0f, -25f), new Vector2(360f, 50f), 28, FontStyle.Bold,
            new Color(1f, 0.85f, 0.25f), withOutline: true);

        var bodyObj = new GameObject("Body");
        bodyObj.transform.SetParent(info.transform, false);
        var bRt = bodyObj.AddComponent<RectTransform>();
        bRt.anchorMin = new Vector2(0f, 0f); bRt.anchorMax = new Vector2(1f, 1f);
        bRt.offsetMin = new Vector2(20f, 80f); bRt.offsetMax = new Vector2(-20f, -90f);
        var bTxt = bodyObj.AddComponent<Text>();
        bTxt.text = SafeGet("codes_info_body",
            "Ga naar Wildlands Zoo om codes te vinden!\n\nBij elk dierenverblijf staat een infobord met een geheime code. Voer de code hier in om een nieuwe skin voor dat dier te ontgrendelen!");
        bTxt.font = GetFont(); bTxt.fontSize = 22; bTxt.fontStyle = FontStyle.Normal;
        bTxt.alignment = TextAnchor.UpperLeft; bTxt.color = new Color(0.85f, 0.92f, 1f);
        bTxt.raycastTarget = false;

        var infoLoc = bodyObj.AddComponent<LocalizedText>();
        infoLoc.key = "codes_info_body";

        StartCoroutine(PopInCard(iRt));
    }

    void BuildCodeCard(Transform parent)
    {
        var card = new GameObject("Card");
        card.transform.SetParent(parent, false);
        var cardRt = card.AddComponent<RectTransform>();
        cardRt.anchorMin = cardRt.anchorMax = cardRt.pivot = new Vector2(0.5f, 0.5f);
        cardRt.anchoredPosition = Vector2.zero;
        cardRt.sizeDelta = new Vector2(640f, 1100f);
        cardRt.localScale = Vector3.zero;
        var cardImg = card.AddComponent<Image>();
        cardImg.color = new Color(0.10f, 0.14f, 0.22f, 0.97f);
        cardImg.raycastTarget = true;

        var accent = new GameObject("Accent");
        accent.transform.SetParent(card.transform, false);
        var aRt = accent.AddComponent<RectTransform>();
        aRt.anchorMin = new Vector2(0f, 1f); aRt.anchorMax = new Vector2(1f, 1f);
        aRt.pivot = new Vector2(0.5f, 1f); aRt.anchoredPosition = Vector2.zero; aRt.sizeDelta = new Vector2(0f, 11f);
        accent.AddComponent<Image>().color = new Color(0.55f, 0.25f, 0.78f);

        MakeLabel(card.transform, "" + SafeGet("codes_title", "Codes"),
            new Vector2(0f, -22f), new Vector2(594f, 64f), 42, FontStyle.Bold, Color.white, withOutline: true);
        MakeLabel(card.transform, SafeGet("codes_subtitle", "Voer een 4-cijferige code in"),
            new Vector2(0f, -86f), new Vector2(594f, 40f), 22, FontStyle.Normal, new Color(0.75f, 0.85f, 1f));

        var displayObj = new GameObject("Display");
        displayObj.transform.SetParent(card.transform, false);
        var drt = displayObj.AddComponent<RectTransform>();
        drt.anchorMin = new Vector2(0.5f, 1f); drt.anchorMax = new Vector2(0.5f, 1f);
        drt.pivot = new Vector2(0.5f, 1f);
        drt.anchoredPosition = new Vector2(0f, -140f);
        drt.sizeDelta = new Vector2(560f, 100f);
        _displayBg = displayObj.AddComponent<Image>();
        _displayBg.color = DisplayNormal;

        var displayText = new GameObject("DisplayText");
        displayText.transform.SetParent(displayObj.transform, false);
        var dtRt = displayText.AddComponent<RectTransform>();
        dtRt.anchorMin = Vector2.zero; dtRt.anchorMax = Vector2.one;
        dtRt.offsetMin = new Vector2(20f, 0f); dtRt.offsetMax = new Vector2(-20f, 0f);
        _displayText = displayText.AddComponent<Text>();
        _displayText.text = "_ _ _ _";
        _displayText.font = GetFont();
        _displayText.fontSize = 66;
        _displayText.fontStyle = FontStyle.Bold;
        _displayText.alignment = TextAnchor.MiddleCenter;
        _displayText.color = Color.white;
        _displayText.raycastTarget = false;

        var statusObj = new GameObject("Status");
        statusObj.transform.SetParent(card.transform, false);
        var srt = statusObj.AddComponent<RectTransform>();
        srt.anchorMin = new Vector2(0.5f, 1f); srt.anchorMax = new Vector2(0.5f, 1f);
        srt.pivot = new Vector2(0.5f, 1f);
        srt.anchoredPosition = new Vector2(0f, -255f);
        srt.sizeDelta = new Vector2(594f, 48f);
        _statusText = statusObj.AddComponent<Text>();
        _statusText.text = "";
        _statusText.font = GetFont();
        _statusText.fontSize = 26;
        _statusText.fontStyle = FontStyle.Bold;
        _statusText.alignment = TextAnchor.MiddleCenter;
        _statusText.color = Color.white;
        _statusText.raycastTarget = false;

        BuildNumpad(card.transform);

        var enterBtn = MakeKey(card.transform, SafeGet("codes_enter", "Enter"), new Vector2(0f, -850f),
            new Vector2(560f, 86f), new Color(0.18f, 0.62f, 0.32f));
        enterBtn.onClick.AddListener(SubmitCode);

        var skinsBtn = MakeKey(card.transform, "" + SafeGet("codes_all_skins", "All Skins"), new Vector2(0f, -945f),
            new Vector2(560f, 70f), new Color(0.42f, 0.18f, 0.62f));
        skinsBtn.onClick.AddListener(ToggleSkinDrawer);

        var closeBtn = MakeKey(card.transform, SafeGet("btn_close", "Sluiten"), new Vector2(0f, -1030f),
            new Vector2(560f, 60f), new Color(0.55f, 0.18f, 0.18f));
        closeBtn.onClick.AddListener(ClosePanel);

        StartCoroutine(PopInCard(cardRt));
    }

    void ToggleSkinDrawer()
    {
        if (_skinDrawer != null)
        {
            StartCoroutine(SlideOutAndDestroy(_skinDrawer));
            _skinDrawer = null;
        }
        else
        {
            BuildSkinDrawer();
        }
    }

    void BuildSkinDrawer()
    {
        _skinDrawer = new GameObject("SkinDrawer");
        _skinDrawer.transform.SetParent(_panel.transform, false);
        var drt = _skinDrawer.AddComponent<RectTransform>();
        drt.anchorMin = new Vector2(1f, 0.5f); drt.anchorMax = new Vector2(1f, 0.5f);
        drt.pivot = new Vector2(1f, 0.5f);
        drt.sizeDelta = new Vector2(440f, 1000f);
        drt.anchoredPosition = new Vector2(440f, 0f);
        var img = _skinDrawer.AddComponent<Image>();
        img.color = new Color(0.10f, 0.14f, 0.22f, 0.97f);

        var accent = new GameObject("Accent");
        accent.transform.SetParent(_skinDrawer.transform, false);
        var aRt = accent.AddComponent<RectTransform>();
        aRt.anchorMin = new Vector2(0f, 1f); aRt.anchorMax = new Vector2(1f, 1f);
        aRt.pivot = new Vector2(0.5f, 1f); aRt.anchoredPosition = Vector2.zero; aRt.sizeDelta = new Vector2(0f, 10f);
        accent.AddComponent<Image>().color = new Color(0.55f, 0.25f, 0.78f);

        MakeLabel(_skinDrawer.transform, "" + SafeGet("codes_all_skins", "All Skins"),
            new Vector2(0f, -22f), new Vector2(420f, 56f), 32, FontStyle.Bold, Color.white, withOutline: true);

        int unlockedCount = 0;
        foreach (var c in codes) if (_unlockedSkins.Contains(c.skinName)) unlockedCount++;

        MakeLabel(_skinDrawer.transform,
            $"{unlockedCount} / {codes.Count} " + SafeGet("codes_unlocked", "ontgrendeld"),
            new Vector2(0f, -82f), new Vector2(420f, 36f), 22, FontStyle.Normal,
            new Color(0.75f, 0.85f, 1f));

        for (int i = 0; i < codes.Count; i++)
        {
            var entry = codes[i];
            bool isUnlocked = _unlockedSkins.Contains(entry.skinName);

            var row = new GameObject($"Skin_{i}");
            row.transform.SetParent(_skinDrawer.transform, false);
            var rrt = row.AddComponent<RectTransform>();
            rrt.anchorMin = new Vector2(0.5f, 1f); rrt.anchorMax = new Vector2(0.5f, 1f);
            rrt.pivot = new Vector2(0.5f, 1f);
            rrt.anchoredPosition = new Vector2(0f, -140f - i * 100f);
            rrt.sizeDelta = new Vector2(400f, 88f);

            var rImg = row.AddComponent<Image>();
            rImg.color = isUnlocked
                ? new Color(0.10f, 0.45f, 0.22f, 0.85f)
                : new Color(0.20f, 0.22f, 0.30f, 0.85f);

            var checkObj = new GameObject("Check");
            checkObj.transform.SetParent(row.transform, false);
            var crt = checkObj.AddComponent<RectTransform>();
            crt.anchorMin = new Vector2(0f, 0.5f); crt.anchorMax = new Vector2(0f, 0.5f);
            crt.pivot = new Vector2(0f, 0.5f); crt.anchoredPosition = new Vector2(14f, 0f);
            crt.sizeDelta = new Vector2(50f, 50f);
            var cTxt = checkObj.AddComponent<Text>();
            cTxt.text = isUnlocked ? "" : "";
            cTxt.font = GetFont();
            cTxt.fontSize = isUnlocked ? 44 : 30;
            cTxt.fontStyle = FontStyle.Bold;
            cTxt.alignment = TextAnchor.MiddleCenter;
            cTxt.color = isUnlocked ? new Color(0.5f, 1f, 0.6f) : new Color(0.6f, 0.6f, 0.7f);
            cTxt.raycastTarget = false;

            var nameObj = new GameObject("Name");
            nameObj.transform.SetParent(row.transform, false);
            var nrt = nameObj.AddComponent<RectTransform>();
            nrt.anchorMin = new Vector2(0f, 0f); nrt.anchorMax = new Vector2(1f, 1f);
            nrt.offsetMin = new Vector2(80f, 0f); nrt.offsetMax = new Vector2(-12f, 0f);
            var nTxt = nameObj.AddComponent<Text>();
            nTxt.text = entry.skinName;
            nTxt.font = GetFont();
            nTxt.fontSize = 26;
            nTxt.fontStyle = FontStyle.Bold;
            nTxt.alignment = TextAnchor.MiddleLeft;
            nTxt.color = isUnlocked ? Color.white : new Color(0.65f, 0.65f, 0.75f);
            nTxt.raycastTarget = false;
        }

        StartCoroutine(SlideInDrawer(drt));
    }

    IEnumerator SlideInDrawer(RectTransform rt)
    {
        float t = 0f;
        Vector2 startPos = new Vector2(440f, 0f);
        Vector2 endPos = new Vector2(-30f, 0f);
        while (t < 0.35f)
        {
            t += Time.deltaTime;
            if (rt == null) yield break;
            float p = Mathf.SmoothStep(0f, 1f, t / 0.35f);
            rt.anchoredPosition = Vector2.Lerp(startPos, endPos, p);
            yield return null;
        }
        if (rt != null) rt.anchoredPosition = endPos;
    }

    IEnumerator SlideOutAndDestroy(GameObject obj)
    {
        if (obj == null) yield break;
        var rt = obj.GetComponent<RectTransform>();
        Vector2 startPos = rt.anchoredPosition;
        Vector2 endPos = new Vector2(440f, 0f);
        float t = 0f;
        while (t < 0.3f && rt != null)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / 0.3f);
            rt.anchoredPosition = Vector2.Lerp(startPos, endPos, p);
            yield return null;
        }
        if (obj != null) Destroy(obj);
    }

    void BuildNumpad(Transform parent)
    {
        var keys = new[]
        {
            ("1", -360f), ("2", -360f), ("3", -360f),
            ("4", -480f), ("5", -480f), ("6", -480f),
            ("7", -600f), ("8", -600f), ("9", -600f),
            ("⌫", -720f),("0", -720f), ("✕", -720f),
        };
        float[] xPositions = { -188f, 0f, 188f };

        for (int i = 0; i < keys.Length; i++)
        {
            var (label, y) = keys[i];
            float x = xPositions[i % 3];
            Color color = new Color(0.18f, 0.24f, 0.36f);
            if (label == "⌫") color = new Color(0.45f, 0.30f, 0.18f);
            if (label == "✕") color = new Color(0.50f, 0.18f, 0.18f);

            var btn = MakeKey(parent, label, new Vector2(x, y), new Vector2(170f, 100f), color);
            string capturedLabel = label;
            btn.onClick.AddListener(() => HandleKey(capturedLabel));
        }
    }

    Button MakeKey(Transform parent, string label, Vector2 pos, Vector2 size, Color color)
    {
        var obj = new GameObject($"Key_{label}");
        obj.transform.SetParent(parent, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        var img = obj.AddComponent<Image>();
        img.color = color;
        var btn = obj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.colors = new ColorBlock
        {
            normalColor = color,
            highlightedColor = color * 1.25f,
            pressedColor = color * 0.65f,
            selectedColor = color,
            disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f),
            colorMultiplier = 1f,
            fadeDuration = 0.06f
        };
        var lblObj = new GameObject("Label");
        lblObj.transform.SetParent(obj.transform, false);
        var lrt = lblObj.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        var lTxt = lblObj.AddComponent<Text>();
        lTxt.text = label; lTxt.font = GetFont();
        lTxt.fontSize = label.Length > 1 ? 28 : 50;
        lTxt.fontStyle = FontStyle.Bold;
        lTxt.alignment = TextAnchor.MiddleCenter;
        lTxt.color = Color.white; lTxt.raycastTarget = false;
        btn.onClick.AddListener(() => StartCoroutine(KeyPressBounce(rt)));
        return btn;
    }

    void HandleKey(string label)
    {
        if (label == "⌫")
        {
            if (_currentInput.Length > 0) _currentInput = _currentInput.Substring(0, _currentInput.Length - 1);
        }
        else if (label == "✕")
        {
            _currentInput = ""; _statusText.text = "";
        }
        else if (_currentInput.Length < 4)
        {
            _currentInput += label; _statusText.text = "";
            _displayBg.color = DisplayNormal;
        }
        UpdateDisplay();
    }

    void UpdateDisplay()
    {
        if (_displayText == null) return;
        string s = "";
        for (int i = 0; i < 4; i++)
            s += (i < _currentInput.Length ? _currentInput[i].ToString() : "_") + (i < 3 ? "  " : "");
        _displayText.text = s;
    }

    void SubmitCode()
    {
        if (_animating) return;
        if (_currentInput.Length < 4) { ShowError(SafeGet("codes_too_short", "Code moet 4 cijfers zijn")); return; }

        foreach (var entry in codes)
        {
            if (entry.code == _currentInput)
            {
                if (_unlockedSkins.Contains(entry.skinName))
                {
                    ShowError(SafeGet("codes_already", "⚠️ Skin al ontgrendeld"));
                    return;
                }
                _unlockedSkins.Add(entry.skinName);
                SaveUnlockedSkins();
                ShowSuccess(entry.rewardMessage);
                return;
            }
        }

        ShowError(SafeGet("codes_invalid", "❌ Ongeldige code"));
    }

    void ShowSuccess(string message)
    {
        _animating = true;
        _displayBg.color = DisplaySuccess;
        _statusText.color = new Color(0.5f, 1f, 0.6f);
        _statusText.text = "✅  " + message;
        StartCoroutine(SuccessSequence());
    }

    void ShowError(string message)
    {
        _displayBg.color = DisplayError;
        _statusText.color = new Color(1f, 0.55f, 0.55f);
        _statusText.text = message;
        if (_displayBg != null)
            StartCoroutine(ShakeDisplay(_displayBg.GetComponent<RectTransform>()));
    }

    IEnumerator SuccessSequence()
    {
        if (_displayBg == null) { _animating = false; yield break; }
        var rt = _displayBg.GetComponent<RectTransform>();
        Vector3 baseScale = rt.localScale;
        float t = 0f;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            if (rt == null) { _animating = false; yield break; }
            float p = t / 0.3f;
            float pulse = 1f + Mathf.Sin(p * Mathf.PI) * 0.12f;
            rt.localScale = baseScale * pulse;
            yield return null;
        }
        if (rt != null) rt.localScale = baseScale;
        yield return new WaitForSeconds(2.5f);
        if (_displayText == null || _displayBg == null || _statusText == null) { _animating = false; yield break; }
        _currentInput = ""; UpdateDisplay();
        _displayBg.color = DisplayNormal;
        _statusText.text = "";
        _animating = false;

        if (_skinDrawer != null)
        {
            Destroy(_skinDrawer);
            _skinDrawer = null;
            BuildSkinDrawer();
        }
    }

    IEnumerator ShakeDisplay(RectTransform rt)
    {
        if (rt == null) yield break;
        Vector2 origin = rt.anchoredPosition;
        foreach (float off in new[] { -16f, 16f, -12f, 12f, -6f, 6f, 0f })
        {
            if (rt == null) yield break;
            rt.anchoredPosition = origin + new Vector2(off, 0f);
            yield return new WaitForSeconds(0.04f);
        }
        if (rt != null) rt.anchoredPosition = origin;
    }

    IEnumerator KeyPressBounce(RectTransform rt)
    {
        if (rt == null) yield break;
        Vector3 baseScale = Vector3.one;
        rt.localScale = baseScale * 0.92f;
        float t = 0f;
        while (t < 0.12f)
        {
            t += Time.deltaTime;
            if (rt == null) yield break;
            rt.localScale = Vector3.Lerp(baseScale * 0.92f, baseScale, t / 0.12f);
            yield return null;
        }
        if (rt != null) rt.localScale = baseScale;
    }

    IEnumerator PopInCard(RectTransform rt)
    {
        float t = 0f;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            if (rt == null) yield break;
            float p = t / 0.3f;
            float overshoot = 1f + Mathf.Sin(p * Mathf.PI) * 0.12f;
            rt.localScale = Vector3.one * Mathf.SmoothStep(0f, 1f, p) * overshoot;
            yield return null;
        }
        if (rt != null) rt.localScale = Vector3.one;
    }

    void ClosePanel()
    {
        if (_panel != null) Destroy(_panel);
        _panel = null;
        _displayText = null;
        _statusText = null;
        _displayBg = null;
        _skinDrawer = null;
        _currentInput = "";
        _open = false;
        _animating = false;
    }

    void MakeLabel(Transform parent, string text, Vector2 pos, Vector2 size,
                   int fontSize, FontStyle style, Color color, bool withOutline = false)
    {
        var obj = new GameObject("Label");
        obj.transform.SetParent(parent, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f); rt.anchoredPosition = pos; rt.sizeDelta = size;
        var t = obj.AddComponent<Text>();
        t.text = text; t.font = GetFont(); t.fontSize = fontSize;
        t.fontStyle = style; t.color = color;
        t.alignment = TextAnchor.MiddleCenter; t.raycastTarget = false;
        if (withOutline) obj.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.6f);
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