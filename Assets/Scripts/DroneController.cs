using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class DroneController : MonoBehaviour
{
    [Header("Movimento")]
    public float moveSpeed = 8f;
    public float verticalSpeed = 5f;
    public float acceleration = 10f;
    [Tooltip("true = W vai pra onde está olhando (inclusive pra cima/baixo). false = só horizontal")]
    public bool moveAlongLook = false;

    [Header("Vida")]
    public float maxLife = 20f;
    public float drainPerSecond = 1f;
    public float lifeCostPerShot = 0.15f;

    [Header("Referencias")]
    public ShootPool shootPool;

    public float LifePercent => life / maxLife; // pra usar numa barra de UI

    private float life;
    private bool dying;
    private Rigidbody rb;
    private CharacterControll owner;
    private System.Action onDestroyed;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Init(CharacterControll owner, System.Action onDestroyed, Vector3 position)
    {
        transform.position = position;
        gameObject.SetActive(true);
        this.owner = owner;
        this.onDestroyed = onDestroyed;
        life = maxLife;

        shootPool.isEnemy = false;
        shootPool.infiniteAmmo = true;
        shootPool.convergeOnCrosshair = true;
        shootPool.aimCamera = Camera.main;
        shootPool.SetWeapon(ShootPool.WeaponType.Nailgun);
    }

    void Update()
    {
        if (dying) return;

        life -= drainPerSecond * Time.deltaTime;
        // Debug.Log(LifePercent);

        if (life <= 0f || (owner != null && owner.currentHealth <= 0f))
        {
            Die();
        }
    }

    void FixedUpdate()
    {
        if (dying) { rb.linearVelocity = Vector3.zero; return; }

        var kb = Keyboard.current;
        Vector3 forward = moveAlongLook
            ? transform.forward
            : Quaternion.Euler(0f, transform.eulerAngles.y, 0f) * Vector3.forward;
        Vector3 right = Quaternion.Euler(0f, transform.eulerAngles.y, 0f) * Vector3.right;

        Vector3 dir = Vector3.zero;
        if (kb[Key.W].isPressed) dir += forward;
        if (kb[Key.S].isPressed) dir -= forward;
        if (kb[Key.D].isPressed) dir += right;
        if (kb[Key.A].isPressed) dir -= right;
        dir = Vector3.ClampMagnitude(dir, 1f) * moveSpeed;

        if (kb[Key.Space].isPressed) dir.y += verticalSpeed;
        if (kb[Key.LeftShift].isPressed) dir.y -= verticalSpeed;

        float t = 1f - Mathf.Exp(-acceleration * Time.fixedDeltaTime);
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, dir, t);
    }

    public void Die()
    {
        if (dying) return;
        dying = true;

        shootPool.enabled = false;
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;

        onDestroyed?.Invoke(); // devolve a câmera e zera o cooldown

        gameObject.SetActive(false);
    }
}