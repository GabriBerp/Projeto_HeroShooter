using UnityEngine;
using UnityEngine.AI;

public class EnemyNavmesh : MonoBehaviour
{
    public Transform player;
    public NavMeshAgent agent;
    public Transform[] waypoints;

    public float baseSpeed;
    public int maxLife = 10;
    public int life;
    public float losePlayerDistance = 5f;
    public float detectPlayerDistance = 20f;
    public float allyBackupDistance = 10f;
    public bool isRunningAway = false;

    void Start()
    {
        life = maxLife;   
    }

    public enum EnemyState
    {
        WayPatrol,
        Pursuit
    }

    public EnemyState currentState = EnemyState.WayPatrol;
    public LayerMask layerMask;

    private void Update()
    {
        FiniteStateMachine();
    }

    public void ChangeState(EnemyState newState)
    {
        currentState = newState;
    }

    void FiniteStateMachine()
    {
        switch (currentState)
        {
            case EnemyState.WayPatrol:
                WayPatrol();
                break;
            case EnemyState.Pursuit:
                Pursuit();
                break;
            default:
                break;
        }
    }

    void Pursuit()
    {
        if (player != null || !isRunningAway)
        {
            agent.speed = baseSpeed;
            agent.stoppingDistance = 6f;
            agent.SetDestination(player.position);
            transform.LookAt(player);

            float dist = Vector3.Distance(transform.position, player.position);
            if (dist >= losePlayerDistance)
            {
                agent.ResetPath();
                ChangeState(EnemyState.WayPatrol);
            }
        }
    }

    void WayPatrol()
    {
        agent.stoppingDistance = 0f;
        if (waypoints.Length != 0 && !agent.pathPending && agent.remainingDistance < 0.5f)
        {
            if (isRunningAway)
            {
                isRunningAway = false;
            }
            agent.speed = baseSpeed;
            int randomIndex = Random.Range(0, waypoints.Length);
            agent.SetDestination(waypoints[randomIndex].position);
        }
        SearchPlayer();
    }

    void SearchPlayer()
    {
        if (isRunningAway)
        {
            return;
        }

        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, detectPlayerDistance, layerMask) || Vector3.Distance(transform.position, player.position) <= detectPlayerDistance / 2)
        {
            ChangeState(EnemyState.Pursuit);
            CallBackup();
        }
    }

    void CallBackup()
    {
        EnemyNavmesh[] todosInimigos = FindObjectsByType<EnemyNavmesh>();
        foreach (EnemyNavmesh aliado in todosInimigos)
        {
            float dist = Vector3.Distance(transform.position, aliado.transform.position);
            if (dist < allyBackupDistance || !aliado.isRunningAway)
            {
                aliado.ChangeState(EnemyState.Pursuit);
            }
        }
    }

    void RunAway()
    {
        isRunningAway = true;
        float maxDist = 0f;
        Transform highestWay = transform;
        foreach (Transform way in waypoints)
        {
            highestWay = Vector3.Distance(transform.position, way.position) >= maxDist ? way : highestWay;
            maxDist = Vector3.Distance(transform.position, way.position) >= maxDist ? Vector3.Distance(transform.position, way.position) : maxDist;
        }
        agent.speed = baseSpeed * 2f;
        agent.SetDestination(highestWay.position);
        ChangeState(EnemyState.WayPatrol);
    }
    
    // Chama saporra quando toma dano
    public void OnDamage()
    {
        life--;
        if (life <= maxLife / 2)
        {
            float rand = Random.value;
            if (rand <= 0.5f)
            {
                RunAway();
            }
        }
    }
}