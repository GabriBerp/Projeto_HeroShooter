using UnityEngine;

public class NoiseMaker : MonoBehaviour
{
    public Rigidbody rb;
    public float lifetime = 10f;
    public float enemyAtractRange = 10f;

    float timer;
    bool active;

    public void Launch(Vector3 position, Vector3 velocity)
    {
        transform.position = position;
        gameObject.SetActive(true);

        rb.linearVelocity = velocity;   // use rb.velocity em versões antigas do Unity
        rb.angularVelocity = Random.insideUnitSphere * 5f;

        timer = lifetime;
        active = true;
    }

    void Update()
    {
        if (!active) return;

        Atract();

        timer -= Time.deltaTime;
        if (timer <= 0f) Deactivate();
    }

    void Deactivate()
    {
        active = false;
        // para de emitir, mas deixa as partículas restantes sumirem naturalmente
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;
        Hide();
    }

    void Hide()
    {
        rb.isKinematic = false;
        gameObject.SetActive(false);
    }

    void Atract()
    {
        EnemyNavmesh[] allEnemys = FindObjectsByType<EnemyNavmesh>();
        foreach (EnemyNavmesh enemy in allEnemys)
        {
            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist < enemyAtractRange && !enemy.isRunningAway && (enemy.currentState != EnemyNavmesh.EnemyState.Pursuit))
            {
                if (enemy.currentState == EnemyNavmesh.EnemyState.LookAround)
                    enemy.ChangeState(EnemyNavmesh.EnemyState.WayPatrol);

                enemy.agent.SetDestination(transform.position);
            }
        }
    }
}