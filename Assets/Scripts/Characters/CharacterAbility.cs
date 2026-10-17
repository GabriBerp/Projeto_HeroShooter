using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterAbility : MonoBehaviour
{
    [Header("Hability Area")]
    public float hab1Cooldown;
    public float hab2Cooldown;
    public float hab1Timer = 0f;
    public float hab2Timer = 0f;

    // Opcional: útil para barras de cooldown na UI (0 a 1)
    public float Hab1Progress => hab1Timer / hab1Cooldown;
    public float Hab2Progress => hab2Timer / hab2Cooldown;

    protected virtual void Start()
    {
        // Começa com as habilidades prontas para uso.
        // Remova estas linhas se quiser que comecem em cooldown.
        hab1Timer = hab1Cooldown;
        hab2Timer = hab2Cooldown;
    }

    protected virtual void Update()
    {
        UpdateCooldowns();
        HandleAbility();
    }

    void UpdateCooldowns()
    {
        hab1Timer = Mathf.Min(hab1Timer + Time.deltaTime, hab1Cooldown);
        hab2Timer = Mathf.Min(hab2Timer + Time.deltaTime, hab2Cooldown);
    }

    protected virtual void HandleAbility()
    {
        if (Keyboard.current == null) return;

        // Habilidade 1 - C
        if (Keyboard.current[Key.C].wasPressedThisFrame && hab1Timer >= hab1Cooldown)
        {
            Debug.Log("Habilidade 1 Usada!");
            ExecuteHab1();
            hab1Timer = 0f;
        }

        // Habilidade 2 - Q
        if (Keyboard.current[Key.Q].wasPressedThisFrame && hab2Timer >= hab2Cooldown)
        {
            Debug.Log("Habilidade 2 Usada!");
            ExecuteHab2();
            hab2Timer = 0f;
        }
    }

    protected virtual void ExecuteHab1() { }
    protected virtual void ExecuteHab2() { }
}