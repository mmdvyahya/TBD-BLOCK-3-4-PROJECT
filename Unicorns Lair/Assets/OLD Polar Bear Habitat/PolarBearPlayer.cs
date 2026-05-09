using System.Collections;
using UnityEngine;

public class PolarBearPlayer : MonoBehaviour
{
    [SerializeField] private float jumpDuration     = 0.22f;
    [SerializeField] private float jumpArcHeight    = 1.4f;
    [SerializeField] private float lateralHopSize   = 0.6f;
    [SerializeField] private float sheetHalfWidth   = 0.95f;

    private int   _lane;
    private float _lateralOffset;
    private bool  _isJumping;
    private bool  _isDead;

    private PolarBearGame _mgr;
    private Renderer      _rend;
    private Color         _baseColor;

    void Awake()
    {
        _rend      = GetComponent<Renderer>();
        _baseColor = _rend != null ? _rend.material.color : Color.white;
    }

    void Start()
    {
        _mgr = PolarBearGame.Instance;
    }

    void Update()
    {
        if (_isDead || _mgr == null) return;
        if (_mgr.CurrentState != GameState.Playing) return;

        if (Input.GetKeyDown(KeyCode.UpArrow)    || Input.GetKeyDown(KeyCode.W)) TryJumpForward();
        if (Input.GetKeyDown(KeyCode.LeftArrow)  || Input.GetKeyDown(KeyCode.A)) TryJumpLateral(-1);
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) TryJumpLateral(1);
        if (Input.GetKeyDown(KeyCode.Space)      || Input.GetKeyDown(KeyCode.B)) TryBlow();
    }

    void LateUpdate()
    {
        if (_isDead || _isJumping || _mgr == null) return;
        if (_mgr.CurrentState != GameState.Playing) return;

        TrackCurrentSheet();

        if (Mathf.Abs(transform.position.x) > _mgr.FallBoundaryX)
            Die();
    }

    public void TryJumpForward()
    {
        if (!CanAct()) return;

        int nextLane = _lane + 1;
        if (nextLane > _mgr.IceSheetCount + 1) return;

        if (nextLane == _mgr.IceSheetCount + 1)
        {
            float endZ = (_mgr.IceSheetCount + 1) * _mgr.LaneZSpacing;
            StartCoroutine(JumpToEnd(nextLane, endZ));
            return;
        }

        var targetSheet = _mgr.IceSheets[nextLane - 1];

        if (targetSheet.HasObstacle)
        {
            _mgr.ShowBlockedFeedback();
            StartCoroutine(BounceBack());
            return;
        }

        StartCoroutine(JumpToSheet(nextLane, targetSheet));
    }

    public void TryJumpLateral(int dir)
    {
        if (!CanAct()) return;
        if (_lane == 0 || _lane == _mgr.IceSheetCount + 1) return;

        _lateralOffset = Mathf.Clamp(_lateralOffset + dir * lateralHopSize, -0.85f, 0.85f);
    }

    public void TryBlow()
    {
        if (_isDead) return;
        if (_mgr.CurrentState != GameState.Playing) return;

        int nextLane = _lane + 1;
        if (nextLane < 1 || nextLane > _mgr.IceSheetCount)
        {
            _mgr.ShowBlowFeedback(false);
            return;
        }

        var sheet = _mgr.IceSheets[nextLane - 1];
        if (sheet.HasObstacle)
        {
            sheet.MeltObstacle();
            _mgr.ShowBlowFeedback(true);
        }
        else
        {
            _mgr.ShowBlowFeedback(false);
        }
    }

    public void ResetToStart()
    {
        StopAllCoroutines();
        _lane          = 0;
        _lateralOffset = 0f;
        _isJumping     = false;
        _isDead        = false;
        transform.position = new Vector3(0f, 1.1f, 0f);
        SetColor(_baseColor);
    }

    bool CanAct() =>
        !_isJumping && !_isDead && _mgr != null && _mgr.CurrentState == GameState.Playing;

    void TrackCurrentSheet()
    {
        if (_lane < 1 || _lane > _mgr.IceSheetCount) return;

        var sheet = _mgr.IceSheets[_lane - 1];
        Vector3 pos = transform.position;
        pos.x = sheet.transform.position.x + _lateralOffset;
        transform.position = pos;
    }

    void Die()
    {
        if (_isDead) return;
        _isDead = true;
        StartCoroutine(DeathSequence());
    }

    IEnumerator JumpToSheet(int targetLane, IceSheetController targetSheet)
    {
        _isJumping = true;

        float startX = transform.position.x;
        float startZ = transform.position.z;
        float targetZ = targetSheet.transform.position.z;
        float t = 0f;

        while (t < jumpDuration)
        {
            t += Time.deltaTime;
            float p    = Mathf.Clamp01(t / jumpDuration);
            float yArc = jumpArcHeight * 4f * p * (1f - p);

            transform.position = new Vector3(
                startX,
                1.1f + yArc,
                Mathf.Lerp(startZ, targetZ, p)
            );
            yield return null;
        }

        float landedX     = startX;
        float sheetXNow   = targetSheet.transform.position.x;
        float landedOffset = landedX - sheetXNow;

        transform.position = new Vector3(landedX, 1.1f, targetZ);
        _lane          = targetLane;
        _lateralOffset = landedOffset;
        _isJumping     = false;

        if (Mathf.Abs(landedOffset) > sheetHalfWidth)
        {
            _mgr.ShowMissedFeedback();
            Die();
        }
    }

    IEnumerator JumpToEnd(int targetLane, float endZ)
    {
        _isJumping = true;

        float startX = transform.position.x;
        float startZ = transform.position.z;
        float t = 0f;

        while (t < jumpDuration)
        {
            t += Time.deltaTime;
            float p    = Mathf.Clamp01(t / jumpDuration);
            float yArc = jumpArcHeight * 4f * p * (1f - p);
            transform.position = new Vector3(startX, 1.1f + yArc, Mathf.Lerp(startZ, endZ, p));
            yield return null;
        }

        _lane          = targetLane;
        _lateralOffset = 0f;
        _isJumping     = false;
        transform.position = new Vector3(0f, 1.1f, endZ);
        _mgr.SetState(GameState.ReachedEnd);
    }

    IEnumerator DeathSequence()
    {
        SetColor(new Color(1f, 0.3f, 0.3f));
        float t = 0f;
        Vector3 start = transform.position;
        while (t < 0.6f)
        {
            t += Time.deltaTime;
            transform.position = start + Vector3.down * (t / 0.6f) * 2.5f;
            yield return null;
        }
        _mgr.SetState(GameState.Dead);
    }

    IEnumerator BounceBack()
    {
        _isJumping = true;
        SetColor(new Color(1f, 0.55f, 0.2f));

        Vector3 start = transform.position;
        Vector3 nudge = start + new Vector3(0f, 0.35f, 0.3f);
        float   t     = 0f;

        while (t < 0.18f) { t += Time.deltaTime; transform.position = Vector3.Lerp(start, nudge, t / 0.18f); yield return null; }
        t = 0f;
        while (t < 0.18f) { t += Time.deltaTime; transform.position = Vector3.Lerp(nudge, start, t / 0.18f); yield return null; }

        transform.position = start;
        SetColor(_baseColor);
        _isJumping = false;
    }

    void SetColor(Color c)
    {
        if (_rend == null) return;
        _rend.material.color = c;
        if (_rend.material.HasProperty("_BaseColor")) _rend.material.SetColor("_BaseColor", c);
    }
}
