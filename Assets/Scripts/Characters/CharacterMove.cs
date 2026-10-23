using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterMove : MonoBehaviour
{
    public float baseSpeed;
    private float speed;
    public Rigidbody rb;
    private Vector3 direction;
    private bool onGround = true;
    private float staminaMax = 100f;
    private float stamina;

    public bool furtive = false;
    public bool inRage = false;

    void Start()
    {
        stamina = staminaMax;    
        speed = baseSpeed;
    }

    void Update()
    {
        transform.rotation = Quaternion.Euler(0, Camera.main.transform.eulerAngles.y, 0);
        direction = Vector3.zero;

        HandleMovement();
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
    void HandleRun()
    {
        if (furtive)
        {
            speed = baseSpeed * 1.5f;
        }else if (inRage)
        {
            speed = baseSpeed * 2f;
        }
        else
        {
            if (Keyboard.current[Key.LeftShift].isPressed && stamina > 0f)
            {
                speed = baseSpeed * 1.5f;
                if (stamina > 0f)
                {
                    stamina -= 0.25f;
                }
            }
            else
            {
                speed = baseSpeed;
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
