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
        Pursuit,
        LookAround
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

    [Range(0f, 1f)] public float lookAroundChance = 0.4f;   // chance de parar ao chegar no ponto
    public Vector2 lookAroundTime = new Vector2(2f, 4f);     // duração mínima e máxima
    public float lookAroundSpeed = 90f;                      // graus por segundo

    float lookTimer;
    float lookDirection = 1f;   // 1 = gira pra direita, -1 = pra esquerda
    public bool isStunned { get; private set; }
    public bool isBeingPulled { get; private set; }
    float stunTimer;
    Coroutine pullRoutine;

    public void Init()
    {
        // Estado limpo, já que o objeto está sendo reaproveitado
        currentState = EnemyState.WayPatrol;
        enemyHealth.Init();
        isRunningAway = false;
        inSmoke = false;
        smokeTimer = 0f;
        lookTimer = 0f;
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

        // Estado limpo de stun/pull (objeto reaproveitado do pool)
        isStunned = false;
        isBeingPulled = false;
        stunTimer = 0f;
        pullRoutine = null;
        if (agent.isActiveAndEnabled && agent.isOnNavMesh) agent.isStopped = false;

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
        if (!GameManager.GameStarted) return;

        // Sendo puxado: o gancho controla o inimigo, a IA fica parada
        if (isBeingPulled) return;

        // Stunnado: só conta o tempo do stun
        if (isStunned)
        {
            UpdateStun();
            return;
        }

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
            case EnemyState.LookAround:
                LookAround();
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
            // se estava fugindo, não para pra olhar ao redor, só segue o patrulhamento
            bool wasRunningAway = isRunningAway;
            if (isRunningAway)
            {
                isRunningAway = false;
            }
            agent.speed = baseSpeed;

            if (!wasRunningAway && Random.value < lookAroundChance)
            {
                StartLookAround();
                return;
            }

            GoToRandomWaypoint();
        }
        SearchPlayer();
    }

    void GoToRandomWaypoint()
    {
        int randomIndex = Random.Range(0, waypoints.Length);
        agent.SetDestination(waypoints[randomIndex].position);
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

    void StartLookAround()
    {
        agent.ResetPath();
        lookTimer = Random.Range(lookAroundTime.x, lookAroundTime.y);
        lookDirection = Random.value < 0.5f ? 1f : -1f;
        ChangeState(EnemyState.LookAround);
    }

    void LookAround()
    {
        // gira no lugar
        transform.Rotate(0f, lookAroundSpeed * lookDirection * Time.deltaTime, 0f);

        // procura o player enquanto gira
        SearchPlayer();
        if (currentState != EnemyState.LookAround) return; // achou o player e virou Pursuit

        lookTimer -= Time.deltaTime;
        if (lookTimer <= 0f)
        {
            // acabou de olhar, escolhe outro ponto e volta a patrulhar
            agent.speed = baseSpeed;
            GoToRandomWaypoint();
            ChangeState(EnemyState.WayPatrol);
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
            if (dist < allyBackupDistance && !aliado.isRunningAway && aliado != this)
            {
                if (aliado.currentState == EnemyState.LookAround)
                    aliado.ChangeState(EnemyState.WayPatrol);

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

    // ===== STUN / PULL (habilidade "Get Over Here") =====

    public void Stun(float duration)
    {
        isStunned = true;
        stunTimer = duration;
        isRunningAway = false;

        agent.ResetPath();
        agent.isStopped = true;
        SetAttackEnabled(false);
    }

    void UpdateStun()
    {
        stunTimer -= Time.deltaTime;
        if (stunTimer > 0f) return;
        EndStun();
    }

    void EndStun()
    {
        isStunned = false;
        agent.isStopped = false;
        SetAttackEnabled(true);

        // acabou o stun: o inimigo fica puto e vai pra cima do player
        if (visor != null) visor.color = angryColor;
        ChangeState(EnemyState.Pursuit);
    }

    void SetAttackEnabled(bool on)
    {
        if (enemyType == EnemyType.RangedEnemy)
        {
            if (shootPool != null) shootPool.enabled = on;
        }
        else
        {
            if (meleeAttack != null) meleeAttack.enabled = on;
        }
    }

    // Puxa o inimigo até o alvo e aplica o stun. O timer do stun só começa a contar depois que a puxada termina.
    public void Pull(Transform target, float speed, float stopDistance, float stunDuration)
    {
        if (pullRoutine != null) StopCoroutine(pullRoutine);

        Stun(stunDuration);
        isBeingPulled = true;
        pullRoutine = StartCoroutine(PullRoutine(target, speed, stopDistance));
    }

    System.Collections.IEnumerator PullRoutine(Transform target, float speed, float stopDistance)
    {
        float safetyTime = 3f; // evita ficar preso caso algo bloqueie o caminho

        while (target != null && safetyTime > 0f)
        {
            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;
            float dist = toTarget.magnitude;
            if (dist <= stopDistance) break;

            float step = Mathf.Min(speed * Time.deltaTime, dist - stopDistance);
            agent.Move(toTarget.normalized * step); // respeita a NavMesh

            safetyTime -= Time.deltaTime;
            yield return null;
        }

        isBeingPulled = false;
        pullRoutine = null;
    }

    void OnDisable()
    {
        // se morrer/voltar pro pool no meio do stun ou da puxada
        isStunned = false;
        isBeingPulled = false;
        pullRoutine = null;
    }
}