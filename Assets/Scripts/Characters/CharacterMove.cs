using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterMove : MonoBehaviour
{
    public float baseSpeed;
    private float speed;
    public Rigidbody rb;
    private Vector3 direction;
    private int onGround = 0;
    private float staminaMax = 100f;
    private float stamina;

    public bool furtive = false;
    public bool inRage = false;
    public float jumpForce = 6f;
    private bool jumpRequested = false;

    void Start()
    {
        stamina = staminaMax;    
        speed = baseSpeed;
    }

    void Update()
    {
        if (!GameManager.GameStarted) return;
        
        transform.rotation = Quaternion.Euler(0, Camera.main.transform.eulerAngles.y, 0);
        direction = Vector3.zero;

        HandleMovement();
        HandleRun();
        HandleJump();

    }

    void FixedUpdate()
    {
        direction = Vector3.ClampMagnitude(direction, 1f);

        float yVel = rb.linearVelocity.y;
        if (jumpRequested)
        {
            yVel = jumpForce;
            jumpRequested = false;
        }

        rb.linearVelocity = new Vector3(direction.x * speed, yVel, direction.z * speed);
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

    void HandleJump()
    {
        if (Keyboard.current[Key.Space].wasPressedThisFrame && onGround > 0)
        {
            jumpRequested = true;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            onGround++;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            onGround--;
        }
    }
    public float GetStamina()
    {
        return stamina;
    }
}
