using System.Collections;
using UnityEngine;

public class PlayerSpawnControl : MonoBehaviour
{
    public Transform[] spawns;
    public CharacterControll player;
    public ShootPool shootPool;
    [SerializeField] private float respawnDelay = 5f;

    private void Spawn()
    {
        Transform spawn = spawns[Random.Range(0, spawns.Length)];
        player.transform.SetPositionAndRotation(spawn.position, spawn.rotation);
        player.currentHealth = player.maxHealth;
        player.isDead = false;
        shootPool.Init();
        player.gameObject.SetActive(true);
    }

    // Chamado pelo player quando ele "morre"
    public void ReturnToPool(CharacterControll player)
    {
        player.gameObject.SetActive(false);
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);
        Spawn();
    }
}
