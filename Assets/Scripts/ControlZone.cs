using System.Collections.Generic;
using UnityEngine;

public class ControlZone : MonoBehaviour
{
    public GameManager gm;
    public char zone; // Pode ser 'A', 'B' ou 'C'
    public float captureSpeed = 5f; // pontos de porcentagem por segundo

    private readonly HashSet<Collider> playersInside = new HashSet<Collider>();
    private readonly HashSet<Collider> enemiesInside = new HashSet<Collider>();

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) playersInside.Add(other);
        else if (other.CompareTag("Enemy")) enemiesInside.Add(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) playersInside.Remove(other);
        else if (other.CompareTag("Enemy")) enemiesInside.Remove(other);
    }

    private void Update()
    {
        playersInside.RemoveWhere(c => c == null || !c.gameObject.activeInHierarchy);
        enemiesInside.RemoveWhere(c => c == null || !c.gameObject.activeInHierarchy);

        bool hasPlayer = playersInside.Count > 0;
        bool hasEnemy = enemiesInside.Count > 0;

        // Zona vazia ou disputada: nada muda
        if (hasPlayer == hasEnemy) return;

        float amount = Time.deltaTime * captureSpeed;

        switch (zone)
        {
            case 'A':
                if (hasPlayer) Capture(ref gm.zAPlayerPorc, ref gm.zAEnemyPorc, amount);
                else           Capture(ref gm.zAEnemyPorc, ref gm.zAPlayerPorc, amount);
                break;
            case 'B':
                if (hasPlayer) Capture(ref gm.zBPlayerPorc, ref gm.zBEnemyPorc, amount);
                else           Capture(ref gm.zBEnemyPorc, ref gm.zBPlayerPorc, amount);
                break;
            case 'C':
                if (hasPlayer) Capture(ref gm.zCPlayerPorc, ref gm.zCEnemyPorc, amount);
                else           Capture(ref gm.zCEnemyPorc, ref gm.zCPlayerPorc, amount);
                break;
        }
    }

    // Quem está capturando sobe; o outro lado sempre fica com o que sobrou (soma = 100)
    private void Capture(ref float winner, ref float loser, float amount)
    {
        winner = Mathf.Min(100f, winner + amount);

        if (winner + loser > 100f)
            loser = 100f - winner;
    }
}