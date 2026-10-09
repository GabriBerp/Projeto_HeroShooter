using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.InputSystem;
using System.Collections;

public class ShootPool : MonoBehaviour
{
    [Header("General Section")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public int poolSize = 20;
    private int activeProjectiles = 0;
    private ObjectPool<GameObject> pool;
    private bool reloading = false;
    public int currentAmmo;

    [Header("Enemy Section")]
    public bool isEnemy = false;
    private bool canShoot = false;
    private float fireRate = 0.5f;
    private float nextFireTime = 0f;
    public LayerMask layerMask;
    public Transform player;

    void Awake()
    {
        currentAmmo = poolSize;
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
        if (Mouse.current.leftButton.wasPressedThisFrame && !isEnemy)
        {
            Shoot();
        }
        if (Keyboard.current[Key.R].wasPressedThisFrame && currentAmmo <= poolSize && !reloading && !isEnemy)
        {
            StartCoroutine(Reload());
        }

        // Enemy Section
        if (isEnemy && canShoot && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }
        if (isEnemy && currentAmmo <= 0 && !reloading)
        {
            StartCoroutine(Reload());
        }

    }

    void FixedUpdate()
    {
        if (isEnemy)
        {
            canShoot = false;
            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, Mathf.Infinity, layerMask))
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

    void Shoot()
    {
        if (currentAmmo <= 0 || activeProjectiles >= poolSize)
        {
            return;
        }

        GameObject projectile = pool.Get();

        currentAmmo--;
        activeProjectiles++;

        projectile.transform.SetPositionAndRotation(firePoint.position, firePoint.rotation);
        projectile.GetComponent<Projectile>().StartProjectile(firePoint.forward, this);
    }

    public void ReturnProjectile(GameObject projectile)
    {
        activeProjectiles--;
        pool.Release(projectile);
    }

    IEnumerator Reload()
    {
        reloading = true;
        yield return new WaitForSeconds(2f);
        currentAmmo = poolSize;
        reloading = false;
    }
}
