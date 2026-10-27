using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.InputSystem;
using System.Collections;
using static EnemyNavmesh;

public class ShootPool : MonoBehaviour
{
    /*
    Mesma coisa que eu fiz no CharacterControll, porém para as armas.
    Isso permite com 1 script eu ter 3 armas diferentes, e isso ainda permite criar inimigos que atiram porém com 3 tipos de armas diferentes.
    Novamente, Pra evitar trabalho a gente faz mais trabalho erriessi    
    */
    public enum WeaponType
    {
        Shotgun,
        Silent_Revolver,
        Nailgun
    }

    private struct WeaponStats
    {
        public float damage;
        public int magazineSize;
        public float fireDelay;
        public float reloadTime;
        public bool automatic;
        public int pellets;
        public float spreadAngle;
    }

    private static WeaponStats GetStats(WeaponType type)
    {
        switch (type)
        {
            case WeaponType.Shotgun:
                return new WeaponStats
                {
                    damage = 3.5f,
                    magazineSize = 8,
                    fireDelay = 0.9f,
                    reloadTime = 2.5f,
                    automatic = false,
                    pellets = 6,
                    spreadAngle = 8f
                };
            case WeaponType.Silent_Revolver:
                return new WeaponStats
                {
                    damage = 8f,
                    magazineSize = 16,
                    fireDelay = 0.4f,
                    reloadTime = 2f,
                    automatic = false,
                    pellets = 1,
                    spreadAngle = 0f
                };
            case WeaponType.Nailgun:
            default:
                return new WeaponStats
                {
                    damage = 1f,
                    magazineSize = 32,
                    fireDelay = 0.1f,
                    reloadTime = 1.5f,
                    automatic = true,
                    pellets = 1,
                    spreadAngle = 1.5f
                };
        }
    }

    [Header("General Section")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public int currentAmmo;
    private int activeProjectiles = 0;
    private int poolSize;
    private ObjectPool<GameObject> pool;
    private bool reloading = false;
    private Coroutine reloadRoutine;
    private float nextFireTime = 0f;
    public bool infiniteAmmo = false;
    public System.Action OnShoot;

    [Header("Character Weapon Section")]
    public WeaponType weaponType = WeaponType.Shotgun;
    private WeaponStats stats;

    [Header("Enemy Section")]
    public bool isEnemy = false;
    public EnemyNavmesh navmesh;
    private bool canShoot = false;
    public float enemyFireDelayMultiplier = 1.5f;
    public LayerMask layerMask;
    public Transform player;
    [Header("Multi Fire Point / Mira (Drone)")]
    public Transform[] firePoints;
    public bool convergeOnCrosshair = false;
    public Camera aimCamera;
    public float aimMaxDistance = 100f;
    public LayerMask aimMask;
    private Transform[] activePoints;
    [Header("Mira Vertical")]
    public bool verticalAim = true;
    public float enemyAimHeight = 1f;
    public float enemyMaxPitch = 60f;

    void Awake()
    {
        if (isEnemy)
        {
           SetWeapon(weaponType); 
        }
    }

    public void SetWeapon(WeaponType newType)
    {
        weaponType = newType;
        stats = GetStats(weaponType);

        activePoints = (firePoints != null && firePoints.Length > 0)
            ? firePoints
            : new[] { firePoint };

        if (reloadRoutine != null)
        {
            StopCoroutine(reloadRoutine);
            reloadRoutine = null;
        }
        reloading = false;

        pool?.Clear();
        activeProjectiles = 0;

        poolSize = stats.magazineSize * stats.pellets * activePoints.Length;
        currentAmmo = stats.magazineSize;

        pool = new ObjectPool<GameObject>(
            () => Instantiate(projectilePrefab),
            projectile => projectile.SetActive(true),
            projectile => projectile.SetActive(false),
            projectile => Destroy(projectile),
            false,
            poolSize,
            poolSize
        );
    }

    void Update()
    {
        if (!GameManager.GameStarted) return;
        
        if (!isEnemy)
        {
            bool triggerPressed = stats.automatic
                ? Mouse.current.leftButton.isPressed
                : Mouse.current.leftButton.wasPressedThisFrame;

            if (triggerPressed)
            {
                TryShoot();
            }

            if (Keyboard.current[Key.R].wasPressedThisFrame
    && !infiniteAmmo && currentAmmo < stats.magazineSize && !reloading)
            {
                reloadRoutine = StartCoroutine(Reload());
            }
        }
        else
        {
            if (canShoot)
            {
                TryShoot();
            }
            if (currentAmmo <= 0 && !reloading)
            {
                reloadRoutine = StartCoroutine(Reload());
            }
        }
    }

    void FixedUpdate()
    {
        if (isEnemy)
        {
            canShoot = false;
            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, Mathf.Infinity, layerMask)
                && navmesh.currentState == EnemyState.Pursuit)
            {
                canShoot = true;
                Debug.DrawLine(transform.position, hit.point, Color.green);
            }
            else
            {
                Debug.DrawLine(transform.position, transform.position + transform.forward * 100f, Color.red);
            }
        }
    }

    void TryShoot()
    {
        if (reloading || Time.time < nextFireTime)
        {
            return;
        }

        if (currentAmmo <= 0)
        {
            // jogador sem munição tentando atirar: recarrega automático
            if (!isEnemy && !reloading)
            {
                reloadRoutine = StartCoroutine(Reload());
            }
            return;
        }

        Shoot();

        float delay = stats.fireDelay * (isEnemy ? enemyFireDelayMultiplier : 1f);
        nextFireTime = Time.time + delay;
    }

    void Shoot()
    {
        if (!infiniteAmmo) currentAmmo--;
        OnShoot?.Invoke();

        Camera cam = aimCamera != null ? aimCamera : Camera.main;

        // Drone (converge) OU jogador com mira vertical
        bool converge = cam != null && (convergeOnCrosshair || (!isEnemy && verticalAim));
        Vector3 aimPoint = converge ? GetAimPoint(cam) : Vector3.zero;

        float tim = 0f;
        switch (weaponType)
        {
            case WeaponType.Shotgun: tim = 0.5f; break;
            case WeaponType.Silent_Revolver: tim = 5f; break;
            case WeaponType.Nailgun: tim = 2.5f; break;
        }

        foreach (Transform fp in activePoints)
        {
            Quaternion baseRot = fp.rotation;

            if (converge)
            {
                Vector3 toAim = aimPoint - fp.position;
                if (toAim.sqrMagnitude > 0.01f)
                    baseRot = Quaternion.LookRotation(toAim);
            }
            else if (isEnemy)
            {
                baseRot = GetEnemyBaseRotation(fp);
            }

            for (int i = 0; i < stats.pellets; i++)
            {
                if (activeProjectiles >= poolSize) return;

                GameObject projectile = pool.Get();
                activeProjectiles++;

                Quaternion rotation = GetSpreadRotation(baseRot, stats.spreadAngle);
                Vector3 direction = rotation * Vector3.forward;

                projectile.transform.SetPositionAndRotation(fp.position, rotation);
                projectile.GetComponent<Projectile>().StartProjectile(direction, this, tim, stats.damage);
            }
        }
    }

    Quaternion GetEnemyBaseRotation(Transform fp)
    {
        if (player == null) return fp.rotation;

        Vector3 toTarget = (player.position + Vector3.up * enemyAimHeight) - fp.position;
        float flatDist = new Vector3(toTarget.x, 0f, toTarget.z).magnitude;

        // No Unity, X positivo = olhar pra baixo, então invertemos
        float pitch = -Mathf.Atan2(toTarget.y, flatDist) * Mathf.Rad2Deg;
        pitch = Mathf.Clamp(pitch, -enemyMaxPitch, enemyMaxPitch);

        return Quaternion.Euler(pitch, fp.eulerAngles.y, 0f);
    }

    Quaternion GetSpreadRotation(Quaternion baseRot, float angle)
    {
        if (angle <= 0f) return baseRot;

        Quaternion offset = Quaternion.Euler(
            Random.Range(-angle, angle),
            Random.Range(-angle, angle),
            0f
        );
        return baseRot * offset;
    }

    Vector3 GetAimPoint(Camera cam)
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, aimMaxDistance, aimMask, QueryTriggerInteraction.Ignore))
            return hit.point;
        return ray.GetPoint(aimMaxDistance);
    }

    public void ReturnProjectile(GameObject projectile)
    {
        activeProjectiles--;
        pool.Release(projectile);
    }

    IEnumerator Reload()
    {
        reloading = true;
        yield return new WaitForSeconds(stats.reloadTime);
        currentAmmo = stats.magazineSize;
        reloading = false;
        reloadRoutine = null;
    }

    public float GetMagazineSize()
    {
        return stats.magazineSize;
    }
    public bool IsReloading()
    {
        return reloading;
    }

    public void Init()
    {
        currentAmmo = stats.magazineSize;
        reloading = false;
        reloadRoutine = null;
    }
}