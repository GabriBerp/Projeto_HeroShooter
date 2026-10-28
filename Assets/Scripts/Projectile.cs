using UnityEngine;
using System.Collections;
using static EnemyNavmesh;

public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    public float damage;
    public float lifetime = 1f;

    [Header("Detecção de cenário")]
    public LayerMask obstacleLayers = ~0;   // o que para a bala. DESMARQUE a layer das balas e a do drone
    public float castRadius = 0.1f;         // "espessura" da bala na detecção

    [Header("Ricochete")]
    public bool canRicochet = true;
    public int maxRicochets = 1;
    public float ricochetLifetimeBonus = 1f;
    public float ricochetSpeedMultiplier = 1.15f;
    public LayerMask ricochetLayers = ~0;   // subconjunto de obstacleLayers que faz ricochetear
    public TrailRenderer trail;

    private Vector3 direction;
    private ShootPool Shootpool;

    private float baseSpeed;
    private int ricochetCount;
    private float remainingLifetime;
    private Coroutine killRoutine;
    private bool isActive;

    void Awake()
    {
        baseSpeed = speed;
    }

    public void StartProjectile(Vector3 direction, ShootPool shooter, float ltime, float damage)
    {
        trail.Clear();
        this.direction = direction.normalized;
        this.Shootpool = shooter;
        this.lifetime = ltime;
        this.damage = damage;

        speed = baseSpeed;
        ricochetCount = 0;
        remainingLifetime = lifetime;
        isActive = true;

        RestartKillTimer();
    }

    void Update()
    {
        if (!isActive) return;

        float distance = speed * Time.deltaTime;

        // Detecta cenário no caminho deste frame (não depende de Rigidbody/OnCollision)
        if (Physics.SphereCast(transform.position, castRadius, direction, out RaycastHit hit,
                               distance, obstacleLayers, QueryTriggerInteraction.Ignore))
        {
            if (!IsActorOrProjectile(hit.collider))
            {
                HandleSceneryHit(hit);
                return;
            }
        }

        transform.position += direction * distance;
    }

    // Player/inimigos/outras balas são tratados por OnCollisionEnter (ou ignorados aqui)
    bool IsActorOrProjectile(Collider col)
    {
        return col.CompareTag("Enemy")
            || col.CompareTag("Player")
            || col.GetComponentInParent<Projectile>() != null;
    }

    void HandleSceneryHit(RaycastHit hit)
    {
        bool layerAllowed = (ricochetLayers.value & (1 << hit.collider.gameObject.layer)) != 0;
        Debug.Log($"Colidiu com cenário: {hit.collider.name} | layerAllowed: {layerAllowed} | ricochetCount: {ricochetCount}");

        if (canRicochet && layerAllowed && ricochetCount < maxRicochets)
        {
            Ricochet(hit);
        }
        else
        {
            ReturnToPool();
        }
    }

    void Ricochet(RaycastHit hit)
    {
        ricochetCount++;
        Debug.Log(ricochetCount + "º vez ricocheteando!");

        direction = Vector3.Reflect(direction, hit.normal).normalized;

        // Posiciona no ponto de impacto, levemente afastado da parede
        transform.position = hit.point + hit.normal * (castRadius + 0.01f);

        speed *= ricochetSpeedMultiplier;

        remainingLifetime += ricochetLifetimeBonus;
        RestartKillTimer();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!isActive) return;

        GameObject other = collision.gameObject;

        if (other.CompareTag("Enemy"))
        {
            Debug.Log("Colidiu com inimigo.");
            EnemyHealth enemyHealth = other.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage, Shootpool.gameObject.transform.position);
            }
            ReturnToPool();
            return;
        }

        if (other.CompareTag("Player"))
        {
            Debug.Log("Colidiu com player.");
            CharacterControll playerHealth = other.GetComponent<CharacterControll>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(playerHealth.isResistent ? damage / 2 : damage);
            }
            ReturnToPool();
            return;
        }
        // Cenário é tratado no Update via SphereCast
    }

    void RestartKillTimer()
    {
        if (killRoutine != null)
        {
            StopCoroutine(killRoutine);
        }
        killRoutine = StartCoroutine(Kill());
    }

    IEnumerator Kill()
    {
        yield return new WaitForSeconds(remainingLifetime);
        ReturnToPool();
    }

    void ReturnToPool()
    {
        if (!isActive) return; // evita devolver duas vezes
        isActive = false;

        Debug.Log("Voltando pra Pool");
        if (killRoutine != null)
        {
            StopCoroutine(killRoutine);
            killRoutine = null;
        }
        Shootpool.ReturnProjectile(gameObject);
    }
}