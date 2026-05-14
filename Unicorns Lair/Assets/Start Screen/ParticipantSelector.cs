using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class ParticipantSelector : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private string nextSceneName = "StartScreen";
    [SerializeField] private int participantCount = 13;

    private Canvas canvas;

    private void Start()
    {
        EnsureCanvas();
        BuildUI();
    }

    private void EnsureCanvas()
    {
        GameObject canvasObj = new GameObject("ParticipantSelectorCanvas");

        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        EnsureEventSystem();
    }

    private void BuildUI()
    {
        CreateTitle();

        float startX = -260f;
        float startY = 360f;
        float gapX = 260f;
        float gapY = 150f;

        for (int i = 1; i <= participantCount; i++)
        {
            int index = i - 1;
            int row = index / 3;
            int col = index % 3;

            string id = "P" + i.ToString("00");

            Vector2 pos = new Vector2(
                startX + col * gapX,
                startY - row * gapY
            );

            CreateParticipantButton(id, pos);
        }
    }

    private void CreateTitle()
    {
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(canvas.transform, false);

        RectTransform rt = titleObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -160f);
        rt.sizeDelta = new Vector2(800f, 120f);

        TMP_Text text = titleObj.AddComponent<TextMeshProUGUI>();
        text.text = "Select participant";
        text.fontSize = 64;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
    }

    private void CreateParticipantButton(string id, Vector2 position)
    {
        GameObject btnObj = new GameObject("Button_" + id);
        btnObj.transform.SetParent(canvas.transform, false);

        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(220f, 100f);

        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.12f, 0.45f, 0.85f, 0.95f);

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => StartParticipant(id));

        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(btnObj.transform, false);

        RectTransform lrt = labelObj.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;

        TMP_Text label = labelObj.AddComponent<TextMeshProUGUI>();
        label.text = id;
        label.fontSize = 42;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
    }

    private void StartParticipant(string participantId)
    {
        if (PlaytestLogger.Instance == null)
        {
            GameObject loggerObj = new GameObject("PlaytestLogger");
            loggerObj.AddComponent<PlaytestLogger>();
        }

        PlaytestLogger.Instance.StartNewSession(participantId);

        SceneManager.LoadScene(nextSceneName);
    }

    private void EnsureEventSystem()
    {
        if (UnityEngine.EventSystems.EventSystem.current != null)
            return;

        GameObject es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
    }
}