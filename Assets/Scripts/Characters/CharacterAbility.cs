using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterAbility : MonoBehaviour
{
    [Header("Hability Area")]
    public float hab1Cooldown = 8f;
    public float hab2Cooldown = 14f;
    protected float hab1Timer = 0f;
    protected float hab2Timer = 0f;

    void Update() {
        if (hab1Timer < hab1Cooldown)
        {
            hab1Timer+= Time.deltaTime;
        }
        if (hab2Timer < hab2Cooldown)
        {
            hab2Timer+= Time.deltaTime;
        }
    }

    protected void HandleAbility()
    {
        // Habilidade 1 - C
        if (Keyboard.current[Key.C].wasPressedThisFrame)
        {
            if (hab1Timer >= hab1Cooldown)
            {
               ExecuteHab1();
               hab1Timer = 0f;
            }
        }
        // Habilidade 2 - Q
        if (Keyboard.current[Key.Q].wasPressedThisFrame)
        {
            if (hab2Timer >= hab2Cooldown)
            {
               ExecuteHab2();
               hab2Timer = 0f;
            }
        }
    }
    
    protected virtual void ExecuteHab1(){   }
    protected virtual void ExecuteHab2(){   }
}
