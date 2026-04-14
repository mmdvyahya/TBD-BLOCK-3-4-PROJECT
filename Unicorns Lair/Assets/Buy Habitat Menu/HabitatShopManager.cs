using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HabitatShopManager : MonoBehaviour
{
    [SerializeField] private int startingCurrency = 500;

    private static readonly float HEADER_H   = 160f;
    private static readonly float CURRENCY_H = 90f;
    private static readonly float CARD_H     = 520f;
    private static readonly float CARD_GAP   = 24f;
    private static readonly float CARD_PAD   = 20f;

    private int    _currency;
    private Canvas _canvas;
    private Text   _currencyText;
    private ScrollRect _scrollRect;

    private readonly List<HabitatEntry> _habitats = new()
    {
        new HabitatEntry("Bever Verblijf",   "Een rustige waterplas\nvoor vrolijke bevers.", "🦫",     250, new Color(0.45f, 0.28f, 0.10f)),
        new HabitatEntry("IJsbeer Verblijf", "Een ijskoud paradijs\nvoor de ijsbeer.",       "🐻\u200d❄️", 250, new Color(0.18f, 0.48f, 0.78f)),
    };

    private readonly List<CardRefs> _cardRefs = new();

    void Start()
    {
        _currency = startingCurrency;

        var cam = Camera.main;
        if (cam == null)
        {
            var co = new GameObject("Main Camera");
            cam    = co.AddComponent<Camera>();
            co.tag = "MainCamera";
        }
        cam.backgroundColor = new Color(0.13f, 0.19f, 0.29f);
        cam.clearFlags      = CameraClearFlags.SolidColor;

        BuildUI();
    }

    void BuildUI()
    {
        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            var esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        var cObj = new GameObject("Canvas");
        _canvas = cObj.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = cObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight  = 0.5f;

        cObj.AddComponent<GraphicRaycaster>();

        var root = cObj.transform;

        MakeBg("BgFill", root, new Color(0.13f, 0.19f, 0.29f));
        MakeHeader(root);
        MakeCurrencyBar(root);
        MakeScrollArea(root);
        MakeScrollButtons(root);

        RefreshButtons();
    }

    void MakeHeader(Transform root)
    {
        var obj = new GameObject("Header");
        obj.transform.SetParent(root, false);
        MakeBgImage(obj, new Color(0.08f, 0.12f, 0.20f));

        var rt = obj.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(1f, 1f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta        = new Vector2(0f, HEADER_H);

        var lrt = MakeLabel("Title", obj.transform, "🏗  Koop een Verblijf", 54, FontStyle.Bold, Color.white);
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = new Vector2(20f, 8f);
        lrt.offsetMax = new Vector2(-20f, -8f);
        lrt.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
    }

    void MakeCurrencyBar(Transform root)
    {
        var obj = new GameObject("CurrencyBar");
        obj.transform.SetParent(root, false);
        MakeBgImage(obj, new Color(0.06f, 0.42f, 0.22f));

        var rt = obj.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(1f, 1f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -HEADER_H);
        rt.sizeDelta        = new Vector2(0f, CURRENCY_H);

        var lrt = MakeLabel("CurrencyText", obj.transform, $"💰  {_currency} munten", 40, FontStyle.Bold, Color.white);
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = new Vector2(20f, 0f);
        lrt.offsetMax = new Vector2(-20f, 0f);
        lrt.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
        _currencyText = lrt.GetComponent<Text>();
    }

    void MakeScrollArea(Transform root)
    {
        float topOffset = HEADER_H + CURRENCY_H;

        var scrollObj = new GameObject("ScrollView");
        scrollObj.transform.SetParent(root, false);

        var scrollRT = scrollObj.AddComponent<RectTransform>();
        scrollRT.anchorMin  = new Vector2(0f, 0f);
        scrollRT.anchorMax  = new Vector2(1f, 1f);
        scrollRT.offsetMin  = new Vector2(0f, 0f);
        scrollRT.offsetMax  = new Vector2(0f, -topOffset);

        scrollObj.AddComponent<RectMask2D>();

        _scrollRect = scrollObj.AddComponent<ScrollRect>();
        _scrollRect.horizontal        = false;
        _scrollRect.vertical          = true;
        _scrollRect.scrollSensitivity = 60f;
        _scrollRect.movementType      = ScrollRect.MovementType.Elastic;
        _scrollRect.elasticity        = 0.1f;
        _scrollRect.inertia           = true;
        _scrollRect.decelerationRate  = 0.135f;

        var contentObj = new GameObject("Content");
        contentObj.transform.SetParent(scrollObj.transform, false);

        var contentRT = contentObj.AddComponent<RectTransform>();
        contentRT.anchorMin        = new Vector2(0f, 1f);
        contentRT.anchorMax        = new Vector2(1f, 1f);
        contentRT.pivot            = new Vector2(0.5f, 1f);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta        = Vector2.zero;

        var vlg = contentObj.AddComponent<VerticalLayoutGroup>();
        vlg.padding              = new RectOffset((int)CARD_PAD, (int)CARD_PAD, (int)CARD_PAD, (int)CARD_PAD);
        vlg.spacing              = CARD_GAP;
        vlg.childAlignment       = TextAnchor.UpperCenter;
        vlg.childControlWidth    = true;
        vlg.childControlHeight   = false;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        var csf = contentObj.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _scrollRect.content = contentRT;

        for (int i = 0; i < _habitats.Count; i++)
            MakeCard(contentObj.transform, i);
    }

    void MakeCard(Transform parent, int index)
    {
        var h = _habitats[index];

        var cardObj = new GameObject($"Card_{index}");
        cardObj.transform.SetParent(parent, false);
        MakeBgImage(cardObj, new Color(0.18f, 0.26f, 0.40f));

        var crt = cardObj.GetComponent<RectTransform>();
        crt.sizeDelta = new Vector2(0f, CARD_H);

        var le = cardObj.AddComponent<LayoutElement>();
        le.preferredHeight = CARD_H;
        le.flexibleWidth   = 1f;

        var topBar = new GameObject("TopBar");
        topBar.transform.SetParent(cardObj.transform, false);
        MakeBgImage(topBar, h.Color);
        var tbrt = topBar.GetComponent<RectTransform>();
        tbrt.anchorMin        = new Vector2(0f, 1f);
        tbrt.anchorMax        = new Vector2(1f, 1f);
        tbrt.pivot            = new Vector2(0.5f, 1f);
        tbrt.anchoredPosition = Vector2.zero;
        tbrt.sizeDelta        = new Vector2(0f, 14f);

        var emojiBox = new GameObject("EmojiBox");
        emojiBox.transform.SetParent(cardObj.transform, false);
        MakeBgImage(emojiBox, new Color(h.Color.r, h.Color.g, h.Color.b, 0.30f));
        var ert = emojiBox.GetComponent<RectTransform>();
        ert.anchorMin        = new Vector2(0f, 1f);
        ert.anchorMax        = new Vector2(0f, 1f);
        ert.pivot            = new Vector2(0f, 1f);
        ert.anchoredPosition = new Vector2(28f, -28f);
        ert.sizeDelta        = new Vector2(190f, 190f);

        var eTxt = MakeLabel("EmojiText", emojiBox.transform, h.Emoji, 88, FontStyle.Normal, Color.white);
        eTxt.anchorMin = Vector2.zero;
        eTxt.anchorMax = Vector2.one;
        eTxt.offsetMin = eTxt.offsetMax = Vector2.zero;
        eTxt.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;

        var nameLrt = MakeLabel("Name", cardObj.transform, h.Name, 48, FontStyle.Bold, Color.white);
        nameLrt.anchorMin        = new Vector2(0f, 1f);
        nameLrt.anchorMax        = new Vector2(1f, 1f);
        nameLrt.pivot            = new Vector2(0f, 1f);
        nameLrt.anchoredPosition = new Vector2(240f, -34f);
        nameLrt.sizeDelta        = new Vector2(-268f, 64f);
        nameLrt.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;

        var descLrt = MakeLabel("Desc", cardObj.transform, h.Description, 30, FontStyle.Normal, new Color(0.75f, 0.88f, 1f));
        descLrt.anchorMin        = new Vector2(0f, 1f);
        descLrt.anchorMax        = new Vector2(1f, 1f);
        descLrt.pivot            = new Vector2(0f, 1f);
        descLrt.anchoredPosition = new Vector2(240f, -110f);
        descLrt.sizeDelta        = new Vector2(-268f, 110f);
        var descTxt = descLrt.GetComponent<Text>();
        descTxt.alignment   = TextAnchor.UpperLeft;
        descTxt.lineSpacing = 1.3f;

        var priceLrt = MakeLabel("Price", cardObj.transform, $"💰 {h.Price} munten", 36, FontStyle.Bold, new Color(0.35f, 1f, 0.60f));
        priceLrt.anchorMin        = new Vector2(0f, 1f);
        priceLrt.anchorMax        = new Vector2(1f, 1f);
        priceLrt.pivot            = new Vector2(0f, 1f);
        priceLrt.anchoredPosition = new Vector2(240f, -242f);
        priceLrt.sizeDelta        = new Vector2(-268f, 52f);
        priceLrt.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;

        bool hasPlaatsen = index == 0;

        var buyBtnObj = new GameObject("BuyBtn");
        buyBtnObj.transform.SetParent(cardObj.transform, false);
        var buyBtnImg = buyBtnObj.AddComponent<Image>();
        buyBtnImg.color = h.Color;
        var buyBrt = buyBtnObj.GetComponent<RectTransform>();
        buyBrt.anchorMin        = new Vector2(0f, 0f);
        buyBrt.anchorMax        = new Vector2(1f, 0f);
        buyBrt.pivot            = new Vector2(0f, 0f);
        buyBrt.anchoredPosition = new Vector2(28f, 26f);
        buyBrt.sizeDelta        = new Vector2(-56f, 106f);

        var buyBtn = buyBtnObj.AddComponent<Button>();
        buyBtn.targetGraphic = buyBtnImg;
        int ci = index;
        buyBtn.onClick.AddListener(() => OnBuy(ci));
        buyBtn.colors = MakeColorBlock(h.Color);

        var buyLabelRt = MakeLabel("BtnText", buyBtnObj.transform, $"Kopen voor 💰 {h.Price}", 38, FontStyle.Bold, Color.white);
        buyLabelRt.anchorMin = Vector2.zero;
        buyLabelRt.anchorMax = Vector2.one;
        buyLabelRt.offsetMin = buyLabelRt.offsetMax = Vector2.zero;
        buyLabelRt.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
        var buyLabelText = buyLabelRt.GetComponent<Text>();

        Button plaatsenBtn = null;
        Image  plaatsenImg = null;

        if (hasPlaatsen)
        {
            var plaatsenObj = new GameObject("PlaatsenBtn");
            plaatsenObj.transform.SetParent(cardObj.transform, false);
            plaatsenImg = plaatsenObj.AddComponent<Image>();
            plaatsenImg.color = new Color(0.28f, 0.65f, 0.38f);
            var pBrt = plaatsenObj.GetComponent<RectTransform>();
            pBrt.anchorMin        = new Vector2(1f, 0f);
            pBrt.anchorMax        = new Vector2(1f, 0f);
            pBrt.pivot            = new Vector2(1f, 0f);
            pBrt.anchoredPosition = new Vector2(-28f, 26f);
            pBrt.sizeDelta        = new Vector2(200f, 106f);

            plaatsenBtn = plaatsenObj.AddComponent<Button>();
            plaatsenBtn.targetGraphic = plaatsenImg;
            plaatsenBtn.colors        = MakeColorBlock(new Color(0.28f, 0.65f, 0.38f));
            plaatsenBtn.onClick.AddListener(() => UnityEngine.SceneManagement.SceneManager.LoadScene("PlaceHabitats"));

            var pLabelRt = MakeLabel("PlaatsenText", plaatsenObj.transform, "Plaatsen", 36, FontStyle.Bold, Color.white);
            pLabelRt.anchorMin = Vector2.zero;
            pLabelRt.anchorMax = Vector2.one;
            pLabelRt.offsetMin = pLabelRt.offsetMax = Vector2.zero;
            pLabelRt.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;

            plaatsenObj.SetActive(false);
        }

        _cardRefs.Add(new CardRefs
        {
            Btn         = buyBtn,
            BtnImg      = buyBtnImg,
            NormalColor = h.Color,
            BuyLabel    = buyLabelText,
            PlaatsenBtn = plaatsenBtn,
            PlaatsenImg = plaatsenImg,
        });
    }

    void OnBuy(int index)
    {
        var h    = _habitats[index];
        var refs = _cardRefs[index];

        if (refs.Purchased) return;

        if (_currency < h.Price)
        {
            StartCoroutine(ShakeCard(index));
            StartCoroutine(ShowToast("Niet genoeg munten! 💸", new Color(0.80f, 0.18f, 0.12f)));
            return;
        }

        _currency -= h.Price;
        _currencyText.text  = $"💰  {_currency} munten";
        refs.Purchased      = true;
        refs.BuyLabel.text  = "✓  Gekocht!";
        refs.BtnImg.color   = new Color(0.10f, 0.48f, 0.22f);
        refs.Btn.interactable = false;
        refs.Btn.colors     = MakeColorBlock(new Color(0.10f, 0.48f, 0.22f));

        if (refs.PlaatsenBtn != null)
        {
            refs.PlaatsenBtn.gameObject.SetActive(true);

            var buyRt = refs.BtnImg.GetComponent<RectTransform>();
            buyRt.sizeDelta = new Vector2(-264f, 106f);
        }

        RefreshButtons();
        StartCoroutine(ShowToast($"{h.Name} gekocht! 🎉", new Color(0.08f, 0.55f, 0.28f)));
        StartCoroutine(BounceCard(index));

        Debug.Log($"[HabitatShop] Gekocht: {h.Name} — open plaatsingsmodus hier.");
    }

    void MakeScrollButtons(Transform root)
    {
        float topOffset = HEADER_H + CURRENCY_H;
        Color btnCol    = new Color(0.22f, 0.32f, 0.50f);

        var upObj = new GameObject("ScrollUp");
        upObj.transform.SetParent(root, false);
        var upImg = upObj.AddComponent<Image>();
        upImg.color = btnCol;
        var upRt = upObj.GetComponent<RectTransform>();
        upRt.anchorMin        = new Vector2(1f, 1f);
        upRt.anchorMax        = new Vector2(1f, 1f);
        upRt.pivot            = new Vector2(1f, 1f);
        upRt.anchoredPosition = new Vector2(-16f, -(topOffset + 16f));
        upRt.sizeDelta        = new Vector2(90f, 90f);
        var upBtn = upObj.AddComponent<Button>();
        upBtn.targetGraphic = upImg;
        upBtn.colors        = MakeColorBlock(btnCol);
        upBtn.onClick.AddListener(() => StartCoroutine(SmoothScroll(1)));
        var upLrt = MakeLabel("UpArrow", upObj.transform, "▲", 44, FontStyle.Bold, Color.white);
        upLrt.anchorMin = Vector2.zero;
        upLrt.anchorMax = Vector2.one;
        upLrt.offsetMin = upLrt.offsetMax = Vector2.zero;
        upLrt.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;

        var downObj = new GameObject("ScrollDown");
        downObj.transform.SetParent(root, false);
        var downImg = downObj.AddComponent<Image>();
        downImg.color = btnCol;
        var downRt = downObj.GetComponent<RectTransform>();
        downRt.anchorMin        = new Vector2(1f, 1f);
        downRt.anchorMax        = new Vector2(1f, 1f);
        downRt.pivot            = new Vector2(1f, 1f);
        downRt.anchoredPosition = new Vector2(-16f, -(topOffset + 122f));
        downRt.sizeDelta        = new Vector2(90f, 90f);
        var downBtn = downObj.AddComponent<Button>();
        downBtn.targetGraphic = downImg;
        downBtn.colors        = MakeColorBlock(btnCol);
        downBtn.onClick.AddListener(() => StartCoroutine(SmoothScroll(-1)));
        var downLrt = MakeLabel("DownArrow", downObj.transform, "▼", 44, FontStyle.Bold, Color.white);
        downLrt.anchorMin = Vector2.zero;
        downLrt.anchorMax = Vector2.one;
        downLrt.offsetMin = downLrt.offsetMax = Vector2.zero;
        downLrt.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
    }

    IEnumerator SmoothScroll(int direction)
    {
        float start     = _scrollRect.verticalNormalizedPosition;
        float target    = Mathf.Clamp01(start + direction * 0.35f);
        float t         = 0f;
        float dur       = 0.25f;

        while (t < dur)
        {
            t += Time.deltaTime;
            _scrollRect.verticalNormalizedPosition = Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t / dur));
            yield return null;
        }
        _scrollRect.verticalNormalizedPosition = target;
    }

    void RefreshButtons()
    {
        foreach (var r in _cardRefs)
        {
            if (r.Purchased) continue;
            bool ok = _currency >= 250;
            r.Btn.interactable = ok;
            r.BtnImg.color     = ok ? r.NormalColor : new Color(0.30f, 0.30f, 0.33f);
            r.Btn.colors       = ok ? MakeColorBlock(r.NormalColor) : MakeColorBlock(new Color(0.30f, 0.30f, 0.33f));
        }
    }

    IEnumerator BounceCard(int index)
    {
        var rt = _cardRefs[index].BtnImg.GetComponent<RectTransform>();
        var s  = rt.localScale;
        float t = 0f;
        while (t < 0.10f) { t += Time.deltaTime; rt.localScale = Vector3.Lerp(s, s * 1.07f, t / 0.10f); yield return null; }
        t = 0f;
        while (t < 0.10f) { t += Time.deltaTime; rt.localScale = Vector3.Lerp(s * 1.07f, s, t / 0.10f); yield return null; }
    }

    IEnumerator ShakeCard(int index)
    {
        var rt     = _cardRefs[index].BtnImg.GetComponent<RectTransform>();
        var origin = rt.anchoredPosition;
        foreach (float off in new[] { -16f, 16f, -12f, 12f, -6f, 6f, 0f })
        {
            rt.anchoredPosition = origin + new Vector2(off, 0f);
            yield return new WaitForSeconds(0.04f);
        }
        rt.anchoredPosition = origin;
    }

    IEnumerator ShowToast(string msg, Color col)
    {
        var existing = _canvas.transform.Find("Toast");
        if (existing != null) Destroy(existing.gameObject);

        var obj = new GameObject("Toast");
        obj.transform.SetParent(_canvas.transform, false);
        var bg = obj.AddComponent<Image>();
        bg.color = new Color(col.r, col.g, col.b, 0f);

        var rt = obj.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 1f);
        rt.anchorMax        = new Vector2(0.5f, 1f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -(HEADER_H + CURRENCY_H + 20f));
        rt.sizeDelta        = new Vector2(860f, 110f);

        var lrt = MakeLabel("Msg", obj.transform, msg, 40, FontStyle.Bold, Color.white);
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = new Vector2(20f, 0f);
        lrt.offsetMax = new Vector2(-20f, 0f);
        lrt.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;

        float t = 0f;
        while (t < 0.15f) { t += Time.deltaTime; bg.color = new Color(col.r, col.g, col.b, Mathf.Lerp(0f,    0.95f, t / 0.15f)); yield return null; }
        yield return new WaitForSeconds(1.4f);
        t = 0f;
        while (t < 0.25f) { t += Time.deltaTime; bg.color = new Color(col.r, col.g, col.b, Mathf.Lerp(0.95f, 0f,    t / 0.25f)); yield return null; }
        Destroy(obj);
    }

    static ColorBlock MakeColorBlock(Color c) => new ColorBlock
    {
        normalColor      = c,
        highlightedColor = c * 1.18f,
        pressedColor     = c * 0.72f,
        selectedColor    = c,
        disabledColor    = new Color(0.30f, 0.30f, 0.33f),
        colorMultiplier  = 1f,
        fadeDuration     = 0.08f
    };

    static void MakeBgImage(GameObject obj, Color col)
    {
        var img = obj.AddComponent<Image>();
        img.color          = col;
        img.raycastTarget  = false;
    }

    void MakeBg(string name, Transform parent, Color col)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        MakeBgImage(obj, col);
        var rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    RectTransform MakeLabel(string name, Transform parent, string text, int size, FontStyle style, Color color)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rt = obj.AddComponent<RectTransform>();
        var t  = obj.AddComponent<Text>();
        t.text          = text;
        t.font          = GetFont();
        t.fontSize      = size;
        t.fontStyle     = style;
        t.color         = color;
        t.alignment     = TextAnchor.MiddleLeft;
        t.raycastTarget = false;
        return rt;
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

public class HabitatEntry
{
    public string Name;
    public string Description;
    public string Emoji;
    public int    Price;
    public Color  Color;

    public HabitatEntry(string name, string desc, string emoji, int price, Color color)
    {
        Name        = name;
        Description = desc;
        Emoji       = emoji;
        Price       = price;
        Color       = color;
    }
}

public class CardRefs
{
    public Button Btn;
    public Image  BtnImg;
    public Color  NormalColor;
    public Text   BuyLabel;
    public Button PlaatsenBtn;
    public Image  PlaatsenImg;
    public bool   Purchased;
}
