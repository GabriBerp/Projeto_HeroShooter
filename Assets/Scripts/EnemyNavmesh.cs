using UnityEngine;
using UnityEngine.AI;

public class EnemyNavmesh : MonoBehaviour
{
    public Transform player;
    public NavMeshAgent agent;
    public Transform[] waypoints;

    public float baseSpeed;
    public float losePlayerDistance = 5f;
    public float detectPlayerDistance = 20f;
    public float allyBackupDistance = 10f;
    public bool isRunningAway = false;

    public Light visor;
    public Color neutralColor = Color.white;
    public Color angryColor = Color.red;

    public enum EnemyState
    {
        WayPatrol,
        Pursuit
    }

    public enum EnemyType
    {
        RangedEnemy,
        CloseCombatEnemy,
    }

    public EnemyState currentState = EnemyState.WayPatrol;
    public EnemyType enemyType = EnemyType.CloseCombatEnemy;
    public LayerMask layerMask;
    public bool inSmoke = false;
    public float smokeMemory = 0.3f;   // tempo "cego" depois do último contato
    float smokeTimer;

    public void Init()
    {
        
    }

    public void MarkInSmoke()
    {
        inSmoke = true;
        smokeTimer = smokeMemory;
    }

    void UpdateSmoke()
    {
        if (!inSmoke) return;
        smokeTimer -= Time.deltaTime;
        if (smokeTimer <= 0f) inSmoke = false;
    }

    void Update()
    {
        UpdateSmoke();
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
        if (inSmoke)
        {
            agent.ResetPath();
            if (visor != null) visor.color = neutralColor;
            ChangeState(EnemyState.WayPatrol);
            return;
        }
        
        if (player != null || !isRunningAway)
        {
            agent.speed = enemyType == EnemyType.CloseCombatEnemy ? baseSpeed * 1.5f : baseSpeed;
            agent.stoppingDistance = enemyType == EnemyType.CloseCombatEnemy ? 0f : 6f;
            agent.SetDestination(player.position);
            transform.LookAt(player);

            float dist = Vector3.Distance(transform.position, player.position);
            if (dist >= losePlayerDistance)
            {
                agent.ResetPath();
                if (visor != null)
                {
                    visor.color = neutralColor;
                }
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
        if (isRunningAway || inSmoke)
        {
            return;
        }

        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, detectPlayerDistance, layerMask) || Vector3.Distance(transform.position, player.position) <= detectPlayerDistance / 2)
        {
            if (visor != null)
            {
                visor.color = angryColor;
            }
            ChangeState(EnemyState.Pursuit);
            CallBackup();
        }
    }

    public void CallBackup()
    {
        EnemyNavmesh[] todosInimigos = FindObjectsByType<EnemyNavmesh>();
        foreach (EnemyNavmesh aliado in todosInimigos)
        {
            float dist = Vector3.Distance(transform.position, aliado.transform.position);
            if (dist < allyBackupDistance && !aliado.isRunningAway && aliado != this) // se o caba ta com medo, ele caga pros aliados dele
            {
                aliado.agent.SetDestination(transform.position);
            }
        }
    }

    public void RunAway()
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
        if (visor != null)
        {
            visor.color = neutralColor;
        }
        ChangeState(EnemyState.WayPatrol);
    }
}