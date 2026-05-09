using UnityEngine;

public class RainRipple : MonoBehaviour
{
    [SerializeField] private float lifeTime = 0.45f;
    [SerializeField] private float startScale = 0.15f;
    [SerializeField] private float endScale = 0.65f;

    private Material mat;
    private Color startColor;
    private float timer;

    private void Awake()
    {
        Renderer r = GetComponent<Renderer>();
        mat = r.material;
        startColor = mat.color;

        transform.localScale = Vector3.one * startScale;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / lifeTime);

        transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, t);

        Color c = startColor;
        c.a = Mathf.Lerp(startColor.a, 0f, t);
        mat.color = c;

        if (t >= 1f)
            Destroy(gameObject);
    }
}