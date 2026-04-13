using System.Collections;
using UnityEngine;

public class IceSheetController : MonoBehaviour
{
    [SerializeField] public int   sheetIndex;
    [SerializeField] public float moveSpeed = 1f;
    [SerializeField] public float moveRange = 2f;

    public bool HasObstacle => _obstacleObj != null;

    private GameObject _obstacleObj;
    private float      _phase;
    private float      _startPhase;

    void Awake()
    {
        _phase = _startPhase = sheetIndex * 0.8f;
    }

    void Update()
    {
        _phase += moveSpeed * Time.deltaTime;
        float x = Mathf.Sin(_phase) * moveRange;
        transform.position = new Vector3(x, transform.position.y, transform.position.z);
    }

    public void SpawnObstacle()
    {
        if (_obstacleObj != null) return;

        _obstacleObj = new GameObject($"Obstacle_{sheetIndex}");
        _obstacleObj.transform.SetParent(transform);
        _obstacleObj.transform.localPosition = new Vector3(0f, 0.65f, 0f);

        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "ObstacleBody";
        body.transform.SetParent(_obstacleObj.transform);
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale    = new Vector3(0.68f, 0.88f, 0.68f);
        PolarBearGame.ApplyMaterial(body, new Color(0.95f, 0.32f, 0.15f));
        Destroy(body.GetComponent<Collider>());

        var cap = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cap.name = "ObstacleCap";
        cap.transform.SetParent(_obstacleObj.transform);
        cap.transform.localPosition = new Vector3(0f, 0.52f, 0f);
        cap.transform.localScale    = new Vector3(0.82f, 0.18f, 0.82f);
        PolarBearGame.ApplyMaterial(cap, new Color(1f, 0.55f, 0.2f));
        Destroy(cap.GetComponent<Collider>());

        var spike = GameObject.CreatePrimitive(PrimitiveType.Cube);
        spike.name = "ObstacleSpike";
        spike.transform.SetParent(_obstacleObj.transform);
        spike.transform.localPosition = new Vector3(0f, 0.78f, 0f);
        spike.transform.localScale    = new Vector3(0.22f, 0.35f, 0.22f);
        spike.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
        PolarBearGame.ApplyMaterial(spike, new Color(1f, 0.72f, 0.28f));
        Destroy(spike.GetComponent<Collider>());
    }

    public void MeltObstacle()
    {
        if (_obstacleObj == null) return;
        StartCoroutine(MeltRoutine(_obstacleObj));
        _obstacleObj = null;
    }

    public void ResetSheet()
    {
        _phase = _startPhase;
    }

    IEnumerator MeltRoutine(GameObject target)
    {
        float t = 0f;
        Vector3 startScale = target.transform.localScale;

        while (t < 0.35f)
        {
            t += Time.deltaTime;
            target.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t / 0.35f);
            yield return null;
        }

        Destroy(target);
    }
}
