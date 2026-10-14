using UnityEngine;
using System.Collections;
using static EnemyNavmesh;

public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    public float damage = 25f;
    private Vector3 direction;
    private ShootPool Shootpool;
    public void StartProjectile(Vector3 direction, ShootPool shooter)
    {
        this.direction = direction;
        this.Shootpool = shooter;
        StartCoroutine(Kill());
    }

    void Update()
    {
        transform.position += direction * speed * Time.deltaTime;    
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            EnemyHealth enemyHealth = collision.gameObject.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage);
            }
        }
        Shootpool.ReturnProjectile(gameObject);  
    }

    IEnumerator Kill() // Pra caso não acerte nada
    {
        yield return new WaitForSeconds(5f);
        Shootpool.ReturnProjectile(gameObject);
    }
}
