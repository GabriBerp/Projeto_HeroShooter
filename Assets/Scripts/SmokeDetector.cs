using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class SmokeDetector : MonoBehaviour
{
    ParticleSystem ps;
    readonly List<ParticleSystem.Particle> inside = new List<ParticleSystem.Particle>();

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();

        var trigger = ps.trigger;
        trigger.enabled = true;
        trigger.inside = ParticleSystemOverlapAction.Callback;
        trigger.enter = ParticleSystemOverlapAction.Ignore;
        trigger.exit = ParticleSystemOverlapAction.Ignore;
        trigger.outside = ParticleSystemOverlapAction.Ignore;
    }

    void OnEnable()
    {
        var trigger = ps.trigger;
        foreach (var enemy in FindObjectsByType<EnemyNavmesh>(FindObjectsSortMode.None))
        {
            var col = enemy.GetComponent<Collider>();
            if (col != null) trigger.AddCollider(col);
        }
    }

    void OnParticleTrigger()
    {
        int count = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Inside, inside, out var colliderData);

        for (int i = 0; i < count; i++)
        {
            int colliderCount = colliderData.GetColliderCount(i);
            for (int c = 0; c < colliderCount; c++)
            {
                Component col = colliderData.GetCollider(i, c);
                if (col != null && col.TryGetComponent(out EnemyNavmesh enemy))
                {
                    enemy.MarkInSmoke();
                }
            }
        }
    }
}