using UnityEngine;
using UnityEngine.InputSystem;

public class HunterCharacter : CharacterAbility
{
    private CharacterControll controll;
    private CharacterMove move;
    public ShootPool shootPool;
    public MeleeAttack meleeAttack;
    private MouseOrbit orbit;
    private float baseShootDamage; // guarda o valor do dano da arma normal
    private float baseMeleeDamage; // guarda o valor do dano da arma corpo-a-corpo normal
    private float maxRageTime = 12f;
    private float rageTime = 0f;
    public bool rageMode = false;
    public float RagePercent => rageTime / maxRageTime; // pra usar numa barra de UI

    private void Cache()
    {
        if (controll != null) return;
        controll = GetComponent<CharacterControll>();
        shootPool = controll.shootPool;
        meleeAttack = controll.meleeAttack;
        move = GetComponent<CharacterMove>();
        orbit = Camera.main.GetComponent<MouseOrbit>();
    }

    protected override void Update()
    {
        base.Update();

        rageTime -= Time.deltaTime;

        if (rageTime <= 0 && rageMode)
        {
            EndRageMode();
        }
    }

    protected override void HandleAbility()
    {
        if (Keyboard.current == null) return;

        if (rageMode)
        {
            hab2Timer = 0f;
            return;
        }

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
        }
    }
    protected override void ExecuteHab1()
    {
        /*===
            CONCEITO: "Get over here!".
            O Jogador da um dash na direção do inimigo.
            ===*/
    }

    protected override void ExecuteHab2()
    {
        /*===
            CONCEITO: Ultimo Recurso.
            O Jogador entra em um modo de furia, causando mais dano, ganhando resistencia a dano inimigo, e ficando levemente mais rapido.
            ===*/
        Cache();
        rageMode = true;
        move.inRage = true;
        rageTime = maxRageTime;
        controll.isResistent = true;
    }

    void EndRageMode()
    {
        rageMode = false;
        move.inRage = false;
        controll.isResistent = false;
        hab2Timer = 0f;
    }
}
