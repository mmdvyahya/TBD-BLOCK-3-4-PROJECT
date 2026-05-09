using UnityEngine;

public class RainImpactSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject splashPrefab;

    [Header("World Spawn Area")]
    [SerializeField] private Vector3 areaCenter = Vector3.zero;
    [SerializeField] private Vector3 areaSize = new Vector3(80f, 30f, 80f);
    [SerializeField] private float spawnHeight = 40f;
    [SerializeField] private LayerMask groundMask;

    [Header("Timing")]
    [SerializeField] private float splashInterval = 0.02f;
    [SerializeField] private int splashesPerTick = 3;

    private bool isActive;
    private float timer;

    private void Update()
    {
        if (!isActive)
            return;

        timer -= Time.deltaTime;

        if (timer > 0f)
            return;

        timer = splashInterval;

        for (int i = 0; i < splashesPerTick; i++)
            SpawnSplash();
    }

    public void SetActiveRainImpacts(bool value)
    {
        isActive = value;
    }

    private void SpawnSplash()
    {
        if (splashPrefab == null)
            return;

        Vector3 randomPoint = areaCenter;
        randomPoint.x += Random.Range(-areaSize.x * 0.5f, areaSize.x * 0.5f);
        randomPoint.z += Random.Range(-areaSize.z * 0.5f, areaSize.z * 0.5f);
        randomPoint.y = areaCenter.y + spawnHeight;

        Ray ray = new Ray(randomPoint, Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hit, spawnHeight * 2f, groundMask))
        {
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, hit.normal);
            Instantiate(splashPrefab, hit.point + hit.normal * 0.03f, rot);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(areaCenter, areaSize);
    }
}