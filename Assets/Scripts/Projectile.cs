using UnityEngine;
using System.Collections;
using static EnemyNavmesh;

public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    public float damage;
    public float lifetime = 1f;
    private Vector3 direction;
    private ShootPool Shootpool;
    public void StartProjectile(Vector3 direction, ShootPool shooter, float ltime, float damage)
    {
        this.direction = direction;
        this.Shootpool = shooter;
        this.lifetime = ltime;
        this.damage = damage;
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
        if (collision.gameObject.CompareTag("Player"))
        {
            CharacterControll playerHealth = collision.gameObject.GetComponent<CharacterControll>();
            if (playerHealth != null)
            {
                if (playerHealth.isResistent)
                {
                    playerHealth.TakeDamage(damage/2);
                }
                else
                {
                    playerHealth.TakeDamage(damage);
                }
            }
        }
        Shootpool.ReturnProjectile(gameObject);  
    }

    IEnumerator Kill() // Pra caso não acerte nada
    {
        yield return new WaitForSeconds(lifetime);
        Shootpool.ReturnProjectile(gameObject);
    }
}
