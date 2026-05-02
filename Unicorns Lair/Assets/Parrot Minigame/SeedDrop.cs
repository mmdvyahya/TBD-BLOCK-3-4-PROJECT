using UnityEngine;

public class SeedDrop : MonoBehaviour
{
    [SerializeField] private float fallSpeed = 5f;
    [SerializeField] private float lifeTime = 3f;

    private FeedingTray targetTray;
    private bool counted;

    private void Start()
    {
        targetTray = FindFirstObjectByType<FeedingTray>();
    }

    private void Update()
    {
        if (counted)
            return;

        transform.position += Vector3.down * fallSpeed * Time.deltaTime;

        if (targetTray != null && IsInsideTray())
        {
            counted = true;
            Debug.Log("Seed landed in tray");

            targetTray.NotifySeedLanded();
            Destroy(gameObject);
            return;
        }

        lifeTime -= Time.deltaTime;
        if (lifeTime <= 0f)
            Destroy(gameObject);
    }

    private bool IsInsideTray()
    {
        Vector3 seedPos = transform.position;
        Bounds trayBounds = targetTray.GetCatchBounds();

        return trayBounds.Contains(seedPos);
    }
}