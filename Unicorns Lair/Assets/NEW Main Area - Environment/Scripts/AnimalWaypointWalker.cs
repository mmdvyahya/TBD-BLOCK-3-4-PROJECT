using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class AnimalWaypointWalker : MonoBehaviour
{
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform[] waypoints;

    [SerializeField] private string idleStateName = "Idle";
    [SerializeField] private string walkStateName = "Walk";

    [SerializeField] private float idleBeforeWalking = 2f;
    [SerializeField] private float waitAtEachPoint = 1f;
    [SerializeField] private float destinationReachedDistance = 0.35f;
    [SerializeField] private float transitionTime = 0.15f;

    private int currentWaypointIndex;

    private void Awake()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        Debug.Log($"{name}: Walker started.");

        if (agent == null)
        {
            Debug.LogError($"{name}: No NavMeshAgent.");
            return;
        }

        if (!agent.isOnNavMesh)
        {
            Debug.LogError($"{name}: Agent is NOT on NavMesh.");
            return;
        }

        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogError($"{name}: No waypoints assigned.");
            return;
        }

        Debug.Log($"{name}: Ready. Waypoints count: {waypoints.Length}");

        PlayIdle();
        StartCoroutine(WalkLoop());
    }

    private IEnumerator WalkLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(idleBeforeWalking);

            Transform target = waypoints[currentWaypointIndex];

            if (target == null)
            {
                Debug.LogWarning($"{name}: Waypoint {currentWaypointIndex} is NULL.");
                yield break;
            }

            Debug.Log($"{name}: Moving to {target.name}");

            agent.isStopped = false;
            agent.SetDestination(target.position);
            PlayWalk();

            while (agent.pathPending)
                yield return null;

            Debug.Log($"{name}: Path status: {agent.pathStatus}, distance: {agent.remainingDistance}");

            while (agent.hasPath && agent.remainingDistance > destinationReachedDistance)
                yield return null;

            Debug.Log($"{name}: Reached {target.name}");

            agent.ResetPath();
            PlayIdle();

            currentWaypointIndex++;
            if (currentWaypointIndex >= waypoints.Length)
                currentWaypointIndex = 0;

            yield return new WaitForSeconds(waitAtEachPoint);
        }
    }

    private void PlayIdle()
    {
        if (animator != null)
            animator.CrossFade(idleStateName, transitionTime);
    }

    private void PlayWalk()
    {
        if (animator != null)
            animator.CrossFade(walkStateName, transitionTime);
    }
}