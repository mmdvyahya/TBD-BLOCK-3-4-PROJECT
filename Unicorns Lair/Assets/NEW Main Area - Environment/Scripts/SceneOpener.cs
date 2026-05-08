using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SceneOpener : MonoBehaviour
{
    [SerializeField] private float duration = 0.9f;

    void Start()
    {
        StartCoroutine(IrisOpen());
    }

    IEnumerator IrisOpen()
    {
        int res = 128;
        float cx = res * 0.5f;
        float cy = res * 0.5f;
        float aspect = (float)Screen.height / Mathf.Max(Screen.width, 1);

        float[] dists = new float[res * res];
        float maxDist = 0f;
        for (int i = 0; i < res * res; i++)
        {
            float nx = ((i % res) - cx) / res;
            float ny = ((i / res) - cy) / res * aspect;
            float d = Mathf.Sqrt(nx * nx + ny * ny);
            dists[i] = d;
            if (d > maxDist) maxDist = d;
        }

        var canvasObj = new GameObject("SceneOpenCanvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 99;
        canvasObj.AddComponent<CanvasScaler>();

        var texObj = new GameObject("IrisOpen");
        texObj.transform.SetParent(canvasObj.transform, false);
        var rt = texObj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        var raw = texObj.AddComponent<RawImage>();
        raw.texture = tex;
        raw.raycastTarget = false;

        Color[] pixels = new Color[res * res];
        float feather = 0.018f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            float radius = Mathf.Lerp(0f, maxDist * 1.05f, p);

            for (int i = 0; i < pixels.Length; i++)
            {
                float alpha = Mathf.Clamp01((radius - dists[i]) / feather + 0.5f);
                pixels[i] = new Color(0f, 0f, 0f, 1f - alpha);
            }

            tex.SetPixels(pixels);
            tex.Apply(false);
            yield return null;
        }

        Destroy(canvasObj);
        Destroy(this);
    }
}