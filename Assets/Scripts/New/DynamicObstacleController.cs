// Use English for comments in the code
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class DynamicObstacleController : MonoBehaviour
{
    [Header("Ping Pong Settings")]
    [SerializeField] private float travelDistance = 1.5f; // Distance to travel left and right from spawn point

    private NavMeshAgent agent;
    private Vector3 pointA;
    private Vector3 pointB;
    private Vector3 currentTarget;
    private bool pointsInitialized = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        InitializeLocalPingPong();
    }

    void Update()
    {
        if (!pointsInitialized) return;

        // Check if the obstacle has arrived near its local target point
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.15f)
        {
            // Swap between the two local boundary points
            currentTarget = (currentTarget == pointA) ? pointB : pointA;
            agent.SetDestination(currentTarget);
        }
    }

    private void InitializeLocalPingPong()
    {
        Vector3 spawnPosition = transform.position;

        // Create two local points based on the spawn position (moving along the X axis)
        Vector3 localLeft = spawnPosition + Vector3.left * travelDistance;
        Vector3 localRight = spawnPosition + Vector3.right * travelDistance;

        // Project the positions onto the NavMesh to ensure they stay within room limits
        pointA = GetValidNavMeshPoint(localLeft);
        pointB = GetValidNavMeshPoint(localRight);

        currentTarget = pointA;
        agent.SetDestination(currentTarget);
        pointsInitialized = true;
    }

    private Vector3 GetValidNavMeshPoint(Vector3 targetPos)
    {
        NavMeshHit hit;
        // 4.0f radius search to snap perfectly to the floor baked by your Runtime Builder
        if (NavMesh.SamplePosition(targetPos, out hit, 4.0f, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return targetPos;
    }
}