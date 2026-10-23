using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private EnemyNavmesh enemyPrefab;
    [SerializeField] private float respawnDelay = 5f;

    private EnemyNavmesh enemy;

    private void Awake()
    {
        // Cria o único inimigo do pool e já deixa guardado (desativado)
        enemy = Instantiate(enemyPrefab, transform.position, transform.rotation);
        enemy.spawn = this;
        enemy.Init();
        enemy.gameObject.SetActive(false);
    }

    private void Start()
    {
        Spawn();
    }

    private void Spawn()
    {
        enemy.transform.SetPositionAndRotation(transform.position, transform.rotation);
        enemy.gameObject.SetActive(true);
    }

    // Chamado pelo inimigo quando ele "morre"
    public void ReturnToPool(EnemyNavmesh returnedEnemy)
    {
        returnedEnemy.gameObject.SetActive(false);
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);
        Spawn();
    }
}