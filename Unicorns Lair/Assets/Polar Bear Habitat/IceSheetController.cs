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
        _obstacleObj.transform.localPosition = Vector3.zero;

        Color snowBase   = new Color(0.92f, 0.96f, 1.00f);
        Color snowShadow = new Color(0.78f, 0.88f, 0.97f);

        MakeSnowball(_obstacleObj.transform, new Vector3(-0.18f, 0.38f,  0.10f), 0.42f, snowShadow);
        MakeSnowball(_obstacleObj.transform, new Vector3( 0.16f, 0.36f, -0.08f), 0.40f, snowShadow);
        MakeSnowball(_obstacleObj.transform, new Vector3( 0.00f, 0.38f,  0.00f), 0.48f, snowBase);
        MakeSnowball(_obstacleObj.transform, new Vector3(-0.10f, 0.70f,  0.05f), 0.30f, snowBase);
        MakeSnowball(_obstacleObj.transform, new Vector3( 0.08f, 0.72f, -0.04f), 0.28f, snowShadow);
        MakeSnowball(_obstacleObj.transform, new Vector3( 0.00f, 0.96f,  0.00f), 0.22f, snowBase);
    }

    void MakeSnowball(Transform parent, Vector3 localPos, float radius, Color col)
    {
        var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        s.transform.SetParent(parent);
        s.transform.localPosition = localPos;
        s.transform.localScale    = Vector3.one * radius;
        PolarBearGame.ApplyMaterial(s, col);
        Destroy(s.GetComponent<Collider>());
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
