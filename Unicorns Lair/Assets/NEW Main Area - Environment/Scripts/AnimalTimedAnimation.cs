using System.Collections;
using UnityEngine;

public class AnimalTimedAnimation : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator animator;

    [Header("State Names")]
    [SerializeField] private string idleStateName = "Idle";
    [SerializeField] private string timedAnimationStateName = "Look Around";

    [Header("Timing")]
    [SerializeField] private float firstDelay = 3f;
    [SerializeField] private float repeatDelay = 10f;
    [SerializeField] private float transitionTime = 0.15f;

    private bool isPlayingTimedAnimation;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        animator.Play(idleStateName, 0, 0f);
        StartCoroutine(AnimationLoop());
    }

    private IEnumerator AnimationLoop()
    {
        yield return new WaitForSeconds(firstDelay);

        while (true)
        {
            yield return new WaitForSeconds(repeatDelay);

            if (!isPlayingTimedAnimation)
                yield return PlayTimedAnimation();
        }
    }

    private IEnumerator PlayTimedAnimation()
    {
        isPlayingTimedAnimation = true;

        animator.CrossFade(timedAnimationStateName, transitionTime, 0, 0f);

        yield return null;
        yield return new WaitForSeconds(transitionTime);

        float length = animator.GetCurrentAnimatorStateInfo(0).length;

        yield return new WaitForSeconds(length);

        animator.CrossFade(idleStateName, transitionTime);

        isPlayingTimedAnimation = false;
    }
}