using UnityEngine;

public class TrayCatchZone : MonoBehaviour
{
    [SerializeField] private FeedingTray feedingTray;

    private void Awake()
    {
        if (feedingTray == null)
            feedingTray = GetComponentInParent<FeedingTray>();
    }

    private void OnTriggerEnter(Collider other)
    {
        SeedDrop seed = other.GetComponent<SeedDrop>();

        if (seed == null || feedingTray == null)
            return;

        Debug.Log("Seed landed in tray");

        feedingTray.NotifySeedLanded();
        Destroy(seed.gameObject);
    }
}