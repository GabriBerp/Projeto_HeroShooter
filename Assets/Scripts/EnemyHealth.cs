using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public float maxHealth;
    public float currentHealth;
    public EnemySpawner father;

    public CharacterStats PlayerStats;
    public CharacterStats myStats;
    public EnemyNavmesh navmesh;

    public bool isDead { get; private set; }
    void Start() {
        Init();

        navmesh = GetComponent<EnemyNavmesh>();
        PlayerStats = navmesh.player.GetComponent<CharacterStats>();
    } 

    public void Init()
    {
        currentHealth = maxHealth;
        isDead = false;

    }
    
    public void TakeDamage(float amount, Vector3 attacker)
    {
        if (isDead) return;

        // Parte normal do projeto que foi o que o professor passou
        currentHealth -= amount;
        if (currentHealth <= 0f)
        {
            Die();
            isDead = true;
            return;
        }

        // Parte extra do projeto que eu fiz porque não tenho amor a mim mesmo e aparentemente gosto de sofrer
        float porc = currentHealth > 0f ? maxHealth / currentHealth : 0f;
        float rand = Random.value;
        if (rand >= porc) // na logica: quanto menor a vida, mais chances
        {
            navmesh.RunAway();
            navmesh.CallBackup(); // o cara com medo grita por socorro
            return;
        }
        if (!navmesh.isRunningAway) // prevenção caso seja atingido enquanto ja esta fugindo
        {
            navmesh.agent.SetDestination(attacker); // o cara puto vai ate onde tu tava
            navmesh.CallBackup(); // o cara puto chama a galera pra cima de ti
        }
    }

    void Die()
    {
        PlayerStats.AddKill();
        myStats.AddDeath();
        father.ReturnToPool(navmesh);
    }
}
