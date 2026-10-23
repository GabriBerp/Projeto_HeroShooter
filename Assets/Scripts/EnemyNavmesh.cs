using UnityEngine;
using UnityEngine.AI;

public class EnemyNavmesh : MonoBehaviour
{
    public Transform player;
    public EnemySpawner spawn;
    public NavMeshAgent agent;
    public Transform[] waypoints;
    public ShootPool shootPool;
    public MeleeAttack meleeAttack;
    public EnemyHealth enemyHealth;

    public float baseSpeed;
    public float losePlayerDistance = 5f;
    public float detectPlayerDistance = 20f;
    public float allyBackupDistance = 10f;
    public bool isRunningAway = false;

    public Light visor;
    public Color neutralColor = Color.white;
    public Color angryColor = Color.red;
    static Transform cachedPlayer;
    static Transform[] cachedWaypoints;

    private GhostCharacter ghostPlayer;

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
        // Estado limpo, já que o objeto está sendo reaproveitado
        currentState = EnemyState.WayPatrol;
        enemyHealth.currentHealth = enemyHealth.maxHealth;
        isRunningAway = false;
        inSmoke = false;
        smokeTimer = 0f;
        enemyHealth.father = spawn;

        if (visor != null) visor.color = neutralColor;

        // enemyType aleatório
        enemyType = (EnemyType)Random.Range(0, System.Enum.GetValues(typeof(EnemyType)).Length);

        // layerMask para a layer "Player"
        int playerLayer = LayerMask.NameToLayer("Player");
        layerMask = 1 << playerLayer;

        // player: objeto da cena na layer "Player"
        if (cachedPlayer == null)
        {
            foreach (Transform t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (t.gameObject.layer == playerLayer && t.GetComponent<CharacterControll>() != null)
                {
                    cachedPlayer = t;
                    break;
                }
            }
        }
        player = cachedPlayer;

        // agent
        agent = GetComponent<NavMeshAgent>();

        // waypoints: todos os objetos na layer "Waypoints"
        if (cachedWaypoints == null || cachedWaypoints.Length == 0)
        {
            int waypointLayer = LayerMask.NameToLayer("Waypoints");
            var list = new System.Collections.Generic.List<Transform>();
            foreach (Transform t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (t.gameObject.layer == waypointLayer)
                    list.Add(t);
            }
            cachedWaypoints = list.ToArray();
        }
        waypoints = cachedWaypoints;

        // ghostPlayer (fica null se não existir)
        ghostPlayer = player != null ? player.GetComponent<GhostCharacter>() : null;

        // shootPool + meleeAttack
        shootPool = GetComponent<ShootPool>();
        meleeAttack = GetComponent<MeleeAttack>();

        switch (enemyType)
        {
            case EnemyType.RangedEnemy:
                if (shootPool == null)
                    shootPool = gameObject.AddComponent<ShootPool>();

                shootPool.enabled = true;
                shootPool.isEnemy = true;
                shootPool.navmesh = this;
                shootPool.player = player;
                shootPool.layerMask = layerMask;

                // weaponType aleatório (SetWeapon já recria o pool e a munição)
                int weaponCount = System.Enum.GetValues(typeof(ShootPool.WeaponType)).Length;
                shootPool.SetWeapon((ShootPool.WeaponType)Random.Range(0, weaponCount));
                meleeAttack.enabled = false;
                break;
            case EnemyType.CloseCombatEnemy:
                if (meleeAttack == null)
                    meleeAttack = gameObject.AddComponent<MeleeAttack>();
                meleeAttack.enabled = true;
                meleeAttack.isEnemy = true;
                meleeAttack.navmesh = this;
                meleeAttack.player = player;
                meleeAttack.targetMask = layerMask;

                // meleeType aleatório
                int meleeCount = System.Enum.GetValues(typeof(MeleeAttack.MeleeType)).Length;
                meleeAttack.SetWeapon((MeleeAttack.MeleeType)Random.Range(0, meleeCount));
                shootPool.enabled = false;
                break;
        }
    }

    private void Start() {
        Init();
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
        bool ghost = ghostPlayer != null ? ghostPlayer.furtiveMode : false;
        if (inSmoke || ghost)
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
        bool ghost = ghostPlayer != null ? ghostPlayer.furtiveMode : false;
        if (isRunningAway || inSmoke || ghost)
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
        bool ghost = ghostPlayer != null ? ghostPlayer.furtiveMode : false;
        if (ghost)
        {
            return;
        }

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