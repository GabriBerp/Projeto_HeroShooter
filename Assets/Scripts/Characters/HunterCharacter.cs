using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class HunterCharacter : CharacterAbility
{
    private CharacterControll controll;
    private CharacterMove move;
    public ShootPool shootPool;
    public MeleeAttack meleeAttack;
    private MouseOrbit orbit;
    private float baseShootDamage; // guarda o valor do dano da arma normal
    private float baseMeleeDamage; // guarda o valor do dano da arma corpo-a-corpo normal
    private float maxRageTime = 12f;
    private float rageTime = 0f;
    public bool rageMode = false;
    public float RagePercent => rageTime / maxRageTime; // pra usar numa barra de UI
    [Header("Get Over Here")]
    public float hookRange = 25f;
    public float hookRadius = 0.4f;          // "grossura" do gancho (facilita acertar)
    public float hookTravelSpeed = 60f;      // velocidade do gancho voando
    public float pullSpeed = 25f;            // velocidade que o inimigo é puxado
    public float pullStopDistance = 2f;      // distância final entre o inimigo e o player
    public float stunDuration = 2f;
    public Transform hookOrigin;             // opcional: mão/arma. Se vazio, usa o peito do player
    public LineRenderer hookLine;            // opcional: se vazio, é criado automaticamente
    bool isHooking;

    private void Cache()
    {
        if (controll != null) return;
        controll = GetComponent<CharacterControll>();
        shootPool = controll.shootPool;
        meleeAttack = controll.meleeAttack;
        move = GetComponent<CharacterMove>();
        orbit = Camera.main.GetComponent<MouseOrbit>();
    }

    protected override void Update()
    {
        base.Update();

        rageTime -= Time.deltaTime;

        if (rageTime <= 0 && rageMode)
        {
            EndRageMode();
        }
    }

    protected override void HandleAbility()
    {
        if (Keyboard.current == null) return;

        if (rageMode)
        {
            hab2Timer = 0f;
            return;
        }

        // Habilidade 1 - E
        if (Keyboard.current[Key.E].wasPressedThisFrame && hab1Timer >= hab1Cooldown)
        {
            Debug.Log("Habilidade 1 Usada!");
            ExecuteHab1();
            hab1Timer = 0f;
        }

        // Habilidade 2 - Q
        if (Keyboard.current[Key.Q].wasPressedThisFrame && hab2Timer >= hab2Cooldown)
        {
            Debug.Log("Habilidade 2 Usada!");
            ExecuteHab2();
        }
    }
    protected override void ExecuteHab1()
    {
        /*===
            CONCEITO: "Get over here!".
            O Jogador dispara um gancho que puxa o inimigo na direção dele, e deixa ele stunnado por um momento.
            ===*/
        Cache();
        if (isHooking) return;
        StartCoroutine(HookRoutine());
    }

    protected override void ExecuteHab2()
    {
        /*===
            CONCEITO: Ultimo Recurso.
            O Jogador entra em um modo de furia, causando mais dano, ganhando resistencia a dano inimigo, e ficando levemente mais rapido.
            ===*/
        Cache();
        rageMode = true;
        move.inRage = true;
        rageTime = maxRageTime;
        controll.isResistent = true;
    }

    void EndRageMode()
    {
        rageMode = false;
        move.inRage = false;
        controll.isResistent = false;
        hab2Timer = 0f;
    }

    IEnumerator HookRoutine()
    {
        isHooking = true;
        SetupHookLine();

        Vector3 origin = hookOrigin != null ? hookOrigin.position : transform.position + Vector3.up * 1.2f;

        // 1) Descobre pra onde a câmera está mirando
        Camera cam = Camera.main;
        Ray camRay = new Ray(cam.transform.position, cam.transform.forward);
        Vector3 aimPoint = TryGetHit(camRay, 200f, 0f, out RaycastHit camHit)
            ? camHit.point
            : camRay.GetPoint(100f);

        // 2) Dispara o gancho do player até o ponto mirado
        Vector3 dir = (aimPoint - origin).normalized;
        EnemyNavmesh enemy = null;
        Vector3 endPoint = origin + dir * hookRange;

        if (TryGetHit(new Ray(origin, dir), hookRange, hookRadius, out RaycastHit hit))
        {
            endPoint = hit.point;
            enemy = hit.collider.GetComponentInParent<EnemyNavmesh>();
            if (enemy != null && enemy.enemyHealth.isDead) enemy = null;
        }

        // 3) Animação do gancho indo até o alvo
        hookLine.enabled = true;
        float travelTime = Vector3.Distance(origin, endPoint) / hookTravelSpeed;
        float t = 0f;
        while (t < travelTime)
        {
            t += Time.deltaTime;
            Vector3 start = hookOrigin != null ? hookOrigin.position : transform.position + Vector3.up * 1.2f;
            hookLine.SetPosition(0, start);
            hookLine.SetPosition(1, Vector3.Lerp(start, endPoint, t / travelTime));
            yield return null;
        }

        // 4) Acertou um inimigo: puxa e stunna
        if (enemy != null && enemy.gameObject.activeInHierarchy)
        {
            enemy.Pull(transform, pullSpeed, pullStopDistance, stunDuration);

            while (enemy != null && enemy.gameObject.activeInHierarchy && enemy.isBeingPulled)
            {
                Vector3 start = hookOrigin != null ? hookOrigin.position : transform.position + Vector3.up * 1.2f;
                hookLine.SetPosition(0, start);
                hookLine.SetPosition(1, enemy.transform.position + Vector3.up * 1f);
                yield return null;
            }
        }
        else
        {
            // errou: pequena pausa antes de recolher
            yield return new WaitForSeconds(0.1f);
        }

        hookLine.enabled = false;
        isHooking = false;
    }

    // Raycast/SphereCast que ignora o próprio player e pega o colisor mais próximo
    bool TryGetHit(Ray ray, float range, float radius, out RaycastHit result)
    {
        RaycastHit[] hits = radius > 0f
            ? Physics.SphereCastAll(ray, radius, range, ~0, QueryTriggerInteraction.Ignore)
            : Physics.RaycastAll(ray, range, ~0, QueryTriggerInteraction.Ignore);

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var h in hits)
        {
            if (h.collider.transform.IsChildOf(transform)) continue; // ignora o próprio player
            result = h;
            return true;
        }

        result = default;
        return false;
    }

    void SetupHookLine()
    {
        if (hookLine != null) return;

        hookLine = gameObject.AddComponent<LineRenderer>();
        hookLine.positionCount = 2;
        hookLine.useWorldSpace = true;
        hookLine.startWidth = hookLine.endWidth = 0.08f;
        hookLine.material = new Material(Shader.Find("Sprites/Default"));
        hookLine.startColor = hookLine.endColor = Color.white;
        hookLine.enabled = false;
    }
}
