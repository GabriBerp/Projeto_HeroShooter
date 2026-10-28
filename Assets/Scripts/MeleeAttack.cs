using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using static EnemyNavmesh;

public class MeleeAttack : MonoBehaviour
{
    /*
    Mesma lógica do ShootPool, só que para ataques corpo-a-corpo.
    1 script, vários tipos de arma branca, usado tanto pelo jogador quanto por inimigos.
    */
    public enum MeleeType
    {
        Knife,
        Bat,
        Axe
    }

    private struct MeleeStats
    {
        public float damage;
        public float attackDelay;   // tempo entre ataques
        public float windup;        // tempo entre apertar o botão e o golpe acertar de fato
        public float range;         // alcance do golpe
        public float angle;         // ângulo do arco de ataque (180 = meia-lua na frente)
        public float knockback;     // força de empurrão (0 = nenhum)
        public bool automatic;      // segurar o botão continua atacando
    }

    private static MeleeStats GetStats(MeleeType type)
    {
        switch (type)
        {
            case MeleeType.Knife:
                return new MeleeStats
                {
                    damage = 4f,
                    attackDelay = 0.35f,
                    windup = 0.05f,
                    range = 1.5f,
                    angle = 90f,
                    knockback = 0f,
                    automatic = true
                };
            case MeleeType.Bat:
                return new MeleeStats
                {
                    damage = 8f,
                    attackDelay = 0.8f,
                    windup = 0.2f,
                    range = 2f,
                    angle = 140f,
                    knockback = 8f,
                    automatic = false
                };
            case MeleeType.Axe:
            default:
                return new MeleeStats
                {
                    damage = 15f,
                    attackDelay = 1.4f,
                    windup = 0.4f,
                    range = 2.2f,
                    angle = 120f,
                    knockback = 5f,
                    automatic = false
                };
        }
    }

    [Header("General Section")]
    public Transform attackPoint;          // de onde o golpe sai (se vazio, usa o próprio transform)
    public LayerMask targetMask;           // quem pode ser atingido (inimigos para o jogador, jogador para o inimigo)
    public bool showGizmos = true;
    public System.Action OnAttack;         // use para tocar animação / som
    public System.Action<Collider> OnHit;  // use para efeito de impacto, hitmarker, etc.

    private bool attacking = false;
    private float nextAttackTime = 0f;
    private Coroutine attackRoutine;
    private readonly HashSet<Component> alreadyHit = new HashSet<Component>();
    private readonly Collider[] overlapBuffer = new Collider[16];

    [Header("Character Weapon Section")]
    public MeleeType meleeType = MeleeType.Knife;
    private MeleeStats stats;

    [Header("Enemy Section")]
    public bool isEnemy = false;
    public EnemyNavmesh navmesh;
    public Transform player;
    public float enemyAttackDelayMultiplier = 1.5f;

    void Awake()
    {
        if (attackPoint == null) attackPoint = transform;
        if (isEnemy)
        {
           SetWeapon(meleeType); 
        }
    }

    public void SetWeapon(MeleeType newType)
    {
        meleeType = newType;
        stats = GetStats(meleeType);

        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }
        attacking = false;
    }

    void Update()
    {
        if (!isEnemy)
        {
            bool triggerPressed = stats.automatic
                ? Mouse.current.leftButton.isPressed
                : Mouse.current.leftButton.wasPressedThisFrame;

            if (triggerPressed)
            {
                TryAttack();
            }
        }
        else
        {
            if (navmesh != null && player != null
                && navmesh.currentState == EnemyState.Pursuit
                && Vector3.Distance(transform.position, player.position) <= stats.range)
            {
                TryAttack();
            }
        }
    }

    void TryAttack()
    {
        if (attacking || Time.time < nextAttackTime)
        {
            return;
        }

        float delay = stats.attackDelay * (isEnemy ? enemyAttackDelayMultiplier : 1f);
        nextAttackTime = Time.time + delay;

        attackRoutine = StartCoroutine(AttackRoutine());
    }

    bool TryDamage(Collider col)
    {
        EnemyHealth enemy = col.GetComponentInParent<EnemyHealth>();
        if (enemy != null)
        {
            if (!alreadyHit.Add(enemy)) return false;
            enemy.TakeDamage(stats.damage, gameObject.transform.position);
            return true;
        }

        CharacterControll playerChar = col.GetComponentInParent<CharacterControll>();
        if (playerChar != null)
        {
            if (!alreadyHit.Add(playerChar)) return false;
            playerChar.TakeDamage(stats.damage);
            return true;
        }

        return false;
    }

    IEnumerator AttackRoutine()
    {
        attacking = true;
        OnAttack?.Invoke();

        // tempo de "preparação" do golpe (a animação de levantar a arma)
        if (stats.windup > 0f)
            yield return new WaitForSeconds(stats.windup);

        DoHit();

        attacking = false;
        attackRoutine = null;
    }

    void DoHit()
    {
        alreadyHit.Clear();

        Vector3 origin = attackPoint.position;
        int count = Physics.OverlapSphereNonAlloc(origin, stats.range, overlapBuffer, targetMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            Collider col = overlapBuffer[i];

            // ignora o próprio dono do ataque
            if (col.transform.root == transform.root) continue;

            // checa se o alvo está dentro do arco na frente do atacante
            Vector3 toTarget = col.bounds.center - origin;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.001f)
            {
                float angleToTarget = Vector3.Angle(transform.forward, toTarget);
                if (angleToTarget > stats.angle * 0.5f) continue;
            }

            if (!TryDamage(col)) continue;
            OnHit?.Invoke(col);

            // empurrão opcional
            if (stats.knockback > 0f)
            {
                Rigidbody rb = col.attachedRigidbody;
                if (rb != null)
                {
                    Vector3 dir = toTarget.sqrMagnitude > 0.001f ? toTarget.normalized : transform.forward;
                    rb.AddForce(dir * stats.knockback, ForceMode.Impulse);
                }
            }
        }
    }

    public bool IsAttacking()
    {
        return attacking;
    }

    public float GetRange()
    {
        return stats.range;
    }

    // Desenha o alcance e o arco do golpe no Scene View
    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        MeleeStats s = GetStats(meleeType);
        Transform origin = attackPoint != null ? attackPoint : transform;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(origin.position, s.range);

        Vector3 left = Quaternion.Euler(0f, -s.angle * 0.5f, 0f) * transform.forward;
        Vector3 right = Quaternion.Euler(0f, s.angle * 0.5f, 0f) * transform.forward;
        Gizmos.DrawLine(origin.position, origin.position + left * s.range);
        Gizmos.DrawLine(origin.position, origin.position + right * s.range);
    }
}