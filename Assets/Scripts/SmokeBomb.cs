using UnityEngine;

public class SmokeBomb : MonoBehaviour
{
    public Rigidbody rb;
    public ParticleSystem smoke;
    public float lifetime = 10f;

    float timer;
    bool active;

    public void Launch(Vector3 position, Vector3 velocity)
    {
        transform.position = position;
        gameObject.SetActive(true);

        rb.linearVelocity = velocity;   // use rb.velocity em versões antigas do Unity
        rb.angularVelocity = Random.insideUnitSphere * 5f;

        smoke.Clear();
        smoke.Play();

        timer = lifetime;
        active = true;
    }

    void Update()
    {
        if (!active) return;

        timer -= Time.deltaTime;
        if (timer <= 0f) Deactivate();
    }

    void Deactivate()
    {
        active = false;
        // para de emitir, mas deixa as partículas restantes sumirem naturalmente
        smoke.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;
        Invoke(nameof(Hide), smoke.main.startLifetime.constantMax);
    }

    void Hide()
    {
        rb.isKinematic = false;
        gameObject.SetActive(false);
    }
}