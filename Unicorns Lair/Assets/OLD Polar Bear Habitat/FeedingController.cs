using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FeedingController : MonoBehaviour
{
    [SerializeField] private float zoomDuration = 1.0f;

    private Camera     _cam;
    private GameObject _feedButton;

    void Start()
    {
        _cam = Camera.main;
        LanguageManager.Ensure();

        var bob = GetComponent<FishBob>();
        if (bob != null) bob.enabled = false;

        transform.position = new Vector3(transform.position.x, 2.6f, transform.position.z);

        StartCoroutine(ZoomThenShowButton());
    }

    IEnumerator ZoomThenShowButton()
    {
        Vector3    startPos = _cam.transform.position;
        Quaternion startRot = _cam.transform.rotation;
        float      startFOV = _cam.fieldOfView;

        float      endZWorld = (PolarBearGame.Instance.IceSheetCount + 1) * PolarBearGame.Instance.LaneZSpacing;
        Vector3    endPos    = new Vector3(0f, 5.5f, endZWorld - 4.5f);
        Quaternion endRot    = Quaternion.Euler(32f, 0f, 0f);
        float      endFOV    = 38f;

        float t = 0f;
        while (t < zoomDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / zoomDuration);
            _cam.transform.position = Vector3.Lerp(startPos, endPos, p);
            _cam.transform.rotation = Quaternion.Slerp(startRot, endRot, p);
            _cam.fieldOfView        = Mathf.Lerp(startFOV, endFOV, p);
            yield return null;
        }

        _cam.transform.position = endPos;
        _cam.transform.rotation = endRot;
        _cam.fieldOfView        = endFOV;

        ShowFeedButton();
    }

    void ShowFeedButton()
    {
        var canvasObj = new GameObject("FeedCanvas");
        var canvas    = canvasObj.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        canvasObj.AddComponent<GraphicRaycaster>();

        _feedButton = new GameObject("BtnFeed");
        _feedButton.transform.SetParent(canvasObj.transform, false);

        var rt = _feedButton.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, -200f);
        rt.sizeDelta        = new Vector2(420f, 130f);

        var img = _feedButton.AddComponent<Image>();
        img.color = new Color(0.22f, 0.78f, 0.35f);

        var btn = _feedButton.AddComponent<Button>();
        btn.targetGraphic = img;
        var cb = new ColorBlock
        {
            normalColor      = new Color(0.22f, 0.78f, 0.35f),
            highlightedColor = new Color(0.30f, 0.90f, 0.45f),
            pressedColor     = new Color(0.15f, 0.60f, 0.25f),
            selectedColor    = new Color(0.22f, 0.78f, 0.35f),
            disabledColor    = new Color(0.5f, 0.5f, 0.5f),
            colorMultiplier  = 1f,
            fadeDuration     = 0.08f
        };
        btn.colors = cb;
        btn.onClick.AddListener(OnFeedPressed);

        var labelObj = new GameObject("Label");
        labelObj.transform.SetParent(_feedButton.transform, false);
        var lrt = labelObj.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        var txt = labelObj.AddComponent<Text>();
        txt.text      = LanguageManager.Instance.Get("pb_feed_btn");
        txt.font      = PolarBearGame.GetFont();
        txt.fontSize  = 52;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color     = Color.white;
        var outline = labelObj.AddComponent<Outline>();
        outline.effectColor    = new Color(0f, 0.3f, 0.1f, 0.7f);
        outline.effectDistance = new Vector2(2f, -2f);
    }

    void OnFeedPressed()
    {
        if (_feedButton != null) _feedButton.SetActive(false);
        StartCoroutine(FlyFishToBear());
    }

    IEnumerator FlyFishToBear()
    {
        Vector3 bearPos = PolarBearGame.Instance.Player.transform.position + Vector3.up * 0.5f;
        Vector3 start   = transform.position;
        float   t       = 0f;
        float   dur     = 0.55f;

        while (t < dur)
        {
            t += Time.deltaTime;
            float p   = Mathf.SmoothStep(0f, 1f, t / dur);
            float arc = Mathf.Sin(p * Mathf.PI) * 1.5f;
            transform.position = Vector3.Lerp(start, bearPos, p) + Vector3.up * arc;
            transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, p);
            yield return null;
        }

        gameObject.SetActive(false);

        var bear = PolarBearGame.Instance.Player;
        var rend = bear.GetComponent<Renderer>();
        if (rend != null)
        {
            Color happy = new Color(1f, 0.92f, 0.35f);
            rend.material.color = happy;
            if (rend.material.HasProperty("_BaseColor")) rend.material.SetColor("_BaseColor", happy);
        }

        yield return new WaitForSeconds(0.4f);
        PolarBearGame.Instance.SetState(GameState.Complete);
    }
}
