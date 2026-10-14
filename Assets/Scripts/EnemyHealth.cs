using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public float maxHealth = 10f;
    public float currentHealth;

    void Start() {
        currentHealth = maxHealth;
    } 
    
    public void TakeDamage(float amount)
    {
        // Parte normal do projeto que foi o que o professor passou
        currentHealth -= amount;
        if (currentHealth <= 0f)
        {
            Die();
            return;
        }

        // Parte extra do projeto que eu fiz porque não tenho amor a mim mesmo e aparentemente gosto de sofrer
        float porc = currentHealth > 0f ? maxHealth / currentHealth : 0f;
        float rand = Random.value;
        EnemyNavmesh navmesh = GetComponent<EnemyNavmesh>();
        if (rand >= porc) // na logica: quanto menor a vida, mais chances
        {
            navmesh.RunAway();
            navmesh.CallBackup(); // o cara com medo grita por socorro
            return;
        }
        if (!navmesh.isRunningAway) // prevenção caso seja atingido enquanto ja esta fugindo
        {
            if (navmesh.visor != null)
            {
                navmesh.visor.color = navmesh.angryColor;
            }
            navmesh.ChangeState(EnemyNavmesh.EnemyState.Pursuit);
            navmesh.CallBackup(); // o cara puto chama a galera pra cima de ti
        }
    }

    void Die()
    {
        gameObject.SetActive(false);
    }
}
