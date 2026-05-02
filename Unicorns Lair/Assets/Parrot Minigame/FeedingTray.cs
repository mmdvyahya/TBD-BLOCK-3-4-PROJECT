using UnityEngine;

public class FeedingTray : MonoBehaviour
{

    [Header("Fill Settings")]
    [SerializeField] private int seedsNeeded = 20;

    [Header("Visual")]
    [SerializeField] private Transform fillVisual;
    [SerializeField] private float maxFillHeight = 0.35f;

    private int currentSeeds;

    public float FillPercent => Mathf.Clamp01((float)currentSeeds / seedsNeeded);
    public bool IsFull => currentSeeds >= seedsNeeded;
    public Bounds GetCatchBounds()
    {
        Collider col = GetComponent<Collider>();

        if (col != null)
            return col.bounds;

        return new Bounds(transform.position, new Vector3(2f, 0.5f, 1f));
    }
    private void Start()
    {
        UpdateFillVisual();
    }

    public void NotifySeedLanded()
    {
        currentSeeds++;
        UpdateFillVisual();
    }

    private void UpdateFillVisual()
    {
        if (fillVisual == null)
            return;

        Vector3 scale = fillVisual.localScale;
        scale.y = Mathf.Lerp(0.02f, maxFillHeight, FillPercent);
        fillVisual.localScale = scale;
    }

    public void ResetTray()
    {
        currentSeeds = 0;
        UpdateFillVisual();
    }
}