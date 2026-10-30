using System.Collections.Generic;
using UnityEngine;

public class ControlZone : MonoBehaviour
{
    public GameManager gm;
    public char zone; // Pode ser 'A', 'B' ou 'C'
    public float captureSpeed = 5f;      // pontos de porcentagem por segundo
    public float pointInterval = 1f;     // a cada quantos segundos dá 1 ponto

    // Collider -> tempo acumulado dentro da zona (desde o último ponto)
    private readonly Dictionary<Collider, float> playersInside = new Dictionary<Collider, float>();
    private readonly Dictionary<Collider, float> enemiesInside = new Dictionary<Collider, float>();

    // Lista temporária para evitar alterar o dicionário durante o foreach
    private readonly List<Collider> buffer = new List<Collider>();

    public bool PlayerInside => playersInside.Count > 0;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) playersInside[other] = 0f;
        else if (other.CompareTag("Enemy")) enemiesInside[other] = 0f;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) playersInside.Remove(other);
        else if (other.CompareTag("Enemy")) enemiesInside.Remove(other);
    }

    private void Update()
    {
        if (!GameManager.GameStarted || GameManager.MatchEnded) return;

        CleanInvalid(playersInside);
        CleanInvalid(enemiesInside);

        bool hasPlayer = playersInside.Count > 0;
        bool hasEnemy = enemiesInside.Count > 0;

        // Zona vazia ou disputada: nada muda
        if (hasPlayer == hasEnemy) return;

        // Só quem está sozinho na zona acumula tempo/pontos
        GrantPoints(hasPlayer ? playersInside : enemiesInside);

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

    private void CleanInvalid(Dictionary<Collider, float> dict)
    {
        buffer.Clear();
        foreach (var kv in dict)
        {
            if (kv.Key == null || !kv.Key.gameObject.activeInHierarchy)
                buffer.Add(kv.Key);
        }
        foreach (var c in buffer) dict.Remove(c);
    }

    // Quem está capturando sobe; o outro lado sempre fica com o que sobrou (soma = 100)
    private void Capture(ref float winner, ref float loser, float amount)
    {
        winner = Mathf.Min(100f, winner + amount);

        if (winner + loser > 100f)
            loser = 100f - winner;
    }

    private void GrantPoints(Dictionary<Collider, float> dict)
    {
        buffer.Clear();
        buffer.AddRange(dict.Keys);

        foreach (var c in buffer)
        {
            float t = dict[c] + Time.deltaTime;

            // while (e não if) garante que não perde ponto se o frame for muito longo
            while (t >= pointInterval)
            {
                t -= pointInterval;

                CharacterStats cs = c.GetComponent<CharacterStats>();
                if (cs != null) cs.AddPoint("control");
            }

            dict[c] = t;
        }
    }
}