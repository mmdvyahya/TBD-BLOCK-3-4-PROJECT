using System.Collections;
using UnityEngine;

public class PrairieDogHole : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private int holeIndex;

    [Header("Visual References")]
    [SerializeField] private Transform prairieDogVisual;
    [SerializeField] private Transform holeVisual;

    [Header("Animation Settings")]
    [SerializeField] private float popHeight = 0.75f;
    [SerializeField] private float shakeAmount = 0.08f;
    [SerializeField] private float shakeDuration = 0.8f;

    private PrairieDogPeekManager manager;
    private Vector3 dogHiddenLocalPos;
    private Vector3 dogVisibleLocalPos;
    private Vector3 holeOriginalLocalPos;

    public int HoleIndex => holeIndex;

    private void Awake()
    {
        if (prairieDogVisual != null)
        {
            dogHiddenLocalPos = prairieDogVisual.localPosition;
            dogVisibleLocalPos = dogHiddenLocalPos + Vector3.up * popHeight;
            prairieDogVisual.gameObject.SetActive(false);
        }

        if (holeVisual != null)
            holeOriginalLocalPos = holeVisual.localPosition;
    }

    public void Initialize(PrairieDogPeekManager owner, int index)
    {
        manager = owner;
        holeIndex = index;
        HideDogInstant();
    }
    public void NotifyPressedFromRaycast()
    {
        manager?.NotifyHolePressed(this);
    }

    public IEnumerator PlayReveal(float visibleDuration, float popSpeed)
    {
        if (prairieDogVisual != null)
        {
            prairieDogVisual.gameObject.SetActive(true);
            yield return StartCoroutine(MoveDog(dogHiddenLocalPos, dogVisibleLocalPos, popSpeed));
            yield return new WaitForSeconds(visibleDuration);
            yield return StartCoroutine(MoveDog(dogVisibleLocalPos, dogHiddenLocalPos, popSpeed));
            prairieDogVisual.gameObject.SetActive(false);
        }
    }

    public IEnumerator PlayCorrectFeedback(float popSpeed)
    {
        if (prairieDogVisual == null)
            yield break;

        prairieDogVisual.gameObject.SetActive(true);
        yield return StartCoroutine(MoveDog(dogHiddenLocalPos, dogVisibleLocalPos, popSpeed));

        Vector3 baseScale = prairieDogVisual.localScale;
        float t = 0f;

        while (t < 0.6f)
        {
            t += Time.deltaTime;
            float s = 1f + Mathf.Sin(t * 20f) * 0.08f;
            prairieDogVisual.localScale = baseScale * s;
            yield return null;
        }

        prairieDogVisual.localScale = baseScale;
        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(MoveDog(dogVisibleLocalPos, dogHiddenLocalPos, popSpeed));
        prairieDogVisual.gameObject.SetActive(false);
    }

    public IEnumerator PlayWrongFeedback()
    {
        if (holeVisual == null)
            yield break;

        Vector3 baseScale = holeVisual.localScale;
        holeVisual.localScale = baseScale * 1.15f;
        yield return new WaitForSeconds(0.15f);
        holeVisual.localScale = baseScale;
    }

    public IEnumerator PlayHoleShake()
    {
        if (holeVisual == null)
            yield break;

        float t = 0f;

        while (t < shakeDuration)
        {
            t += Time.deltaTime;
            float x = Mathf.Sin(t * 40f) * shakeAmount;
            holeVisual.localPosition = holeOriginalLocalPos + new Vector3(x, 0f, 0f);
            yield return null;
        }

        holeVisual.localPosition = holeOriginalLocalPos;
    }

    private IEnumerator MoveDog(Vector3 from, Vector3 to, float speed)
    {
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * speed;
            prairieDogVisual.localPosition = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        prairieDogVisual.localPosition = to;
    }

    public void HideDogInstant()
    {
        if (prairieDogVisual == null)
            return;

        prairieDogVisual.localPosition = dogHiddenLocalPos;
        prairieDogVisual.gameObject.SetActive(false);
    }
}