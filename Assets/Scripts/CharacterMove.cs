using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterMove : MonoBehaviour
{
    private float speed = 4.5f;
    public Rigidbody rb;
    private Vector3 direction;
    private bool onGround = true;
    private float staminaMax = 100f;
    private float stamina;

    [Header("Hability Area")]
    public float hab1Cooldown = 8f;
    public float hab2Cooldown = 14f;
    public float hab3Cooldown = 25f;
    private float hab1Timer = 0f;
    private float hab2Timer = 0f;
    private float hab3Timer = 0f;

    void Start()
    {
        stamina = staminaMax;    
    }

    void Update()
    {
        transform.rotation = Quaternion.Euler(0, Camera.main.transform.eulerAngles.y, 0);
        direction = Vector3.zero;

        HandleMovement();
        HandleHability();
        HandleRun();

    }

    void FixedUpdate()
    {
        direction = Vector3.ClampMagnitude(direction, 1f);
        rb.linearVelocity = new Vector3(direction.x * speed, rb.linearVelocity.y, direction.z * speed);
    }

    void HandleMovement()
    {
        if (Keyboard.current[Key.W].isPressed)
        {
            direction += transform.forward;
        }
        if (Keyboard.current[Key.S].isPressed)
        {
            direction += transform.forward * -1;
        }
        if (Keyboard.current[Key.D].isPressed)
        {
            direction += transform.right;
        }else if (Keyboard.current[Key.A].isPressed)
        {
            direction += transform.right * -1;
        }
    }

    void HandleHability()
    {
        // Habilidade 1 - C
        if (Keyboard.current[Key.C].wasPressedThisFrame)
        {
            /*===
            CONCEITO: Modo Furtivo
            Fica invisivel, indetectavel e rapido.
            Se disparar sai do modo furtivo, mas o tiro da o dobro de dano.
            Dura no maximo 10s, mas se apertar novamente o botão da habilidade, sai do modo mais cedo.
            ===*/
        }
        // Habilidade 2 - Q
        if (Keyboard.current[Key.Q].wasPressedThisFrame)
        {
            /*===
            CONCEITO: sei la porra
            
            ===*/
        }
        // Habilidade 3 - E
        if (Keyboard.current[Key.E].wasPressedThisFrame)
        {
            /*===
            CONCEITO: .
            
            ===*/
        }
    }

    void HandleRun()
    {
            if (Keyboard.current[Key.LeftShift].isPressed && stamina > 0f)
            {
                speed = 7.5f;
                if (stamina > 0f)
                {
                    stamina -= 0.15f;
                }
            }
            else
            {
                speed = 3.5f;
                if (stamina < staminaMax && !Keyboard.current[Key.LeftShift].isPressed)
                {
                    if (direction != Vector3.zero)
                    {
                        stamina += 0.05f;
                    }
                    else
                    {
                        stamina += 0.25f;
                    }
                }
            }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            onGround = true;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            onGround = false;
        }
    }
    public float GetStamina()
    {
        return stamina;
    }
}
