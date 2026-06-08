using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
public class CreditsManager : MonoBehaviour
{
    [Header("Credits")]
    [SerializeField] private float scrollSpeed = 80f;
    [SerializeField] private float endYPosition = 2600f;

    [Header("Scene Loading")]
    [SerializeField] private string returnSceneName = "MainArea";

    private RectTransform creditsRect;
    private bool isReturning;

    private void Start()
    {
        GameObject canvasObj = new GameObject("CreditsCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject bgObj = new GameObject("BlackBackground");
        bgObj.transform.SetParent(canvasObj.transform, false);

        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        Image bg = bgObj.AddComponent<Image>();
        bg.color = Color.black;

        GameObject creditsObj = new GameObject("CreditsText");
        creditsObj.transform.SetParent(canvasObj.transform, false);

        creditsRect = creditsObj.AddComponent<RectTransform>();
        creditsRect.anchorMin = new Vector2(0.5f, 0f);
        creditsRect.anchorMax = new Vector2(0.5f, 0f);
        creditsRect.pivot = new Vector2(0.5f, 0f);
        creditsRect.sizeDelta = new Vector2(1400f, 2600f);
        creditsRect.anchoredPosition = new Vector2(0f, -1800f);

        Text text = creditsObj.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 50;
        text.alignment = TextAnchor.UpperCenter;
        text.color = Color.white;

        text.text =
@"THANK YOU FOR PLAYING

WILDLANDS: LITTLE EXPLORERS

ASSEN INC.


LEAD PROGRAMMER / PRODUCT OWNER
Yahya Mammadov


SCRUM MASTER
TECHNICAL SYSTEMS DESIGN & DEVELOPMENT
UI DEVELOPMENT
3D CONCEPT ART
Allard van Bockel


3D ARTIST
VOICE ACTOR
Otis Hamelink


LEAD 3D ARTIST
TECHNICAL ARTIST
PROGRAMMER
COMPOSER
Lasse Kregmeier


GAME DESIGNER
3D DESIGNER
Muhammed Kaan Kalelioğlu


LEAD GAME DESIGN
UI DESIGN
Serkan


SPECIAL THANKS

Hanze University of Applied Sciences


THANK YOU FOR PLAYING";
    }

    private void Update()
    {
        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            SceneManager.LoadScene(returnSceneName);
            return;
        }

        if (Touchscreen.current != null &&
            Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            SceneManager.LoadScene(returnSceneName);
            return;
        }

        if (creditsRect == null || isReturning)
            return;

        creditsRect.anchoredPosition += Vector2.up * scrollSpeed * Time.deltaTime;

        if (creditsRect.anchoredPosition.y >= endYPosition)
        {
            isReturning = true;
            SceneManager.LoadScene(returnSceneName);
        }
    }
}