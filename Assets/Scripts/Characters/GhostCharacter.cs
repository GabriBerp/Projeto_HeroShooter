using UnityEngine;
using UnityEngine.InputSystem;
public class GhostCharacter : CharacterAbility
{
    private CharacterControll controll;
    public ShootPool shootPool;
    public MeleeAttack furtiveAttack;
    private SonarFx sonar;
    public Color sonarColor = Color.white;
    private CharacterMove move;
    private MouseOrbit orbit;
    // public Transform throwOrigin;
    public float throwForce = 12f;
    public float upwardForce = 5f;
    public bool furtiveMode = false;

    private float maxFurtiveModeTime = 8f;
    private float furtiveModeTime = 0f;

    public float FurtivePercent => furtiveModeTime / maxFurtiveModeTime; // pra usar numa barra de UI

    SmokeBomb smokeBombInstance; // o "pool" de 1 objeto

    private void Cache()
    {
        if (controll != null) return;
        controll = GetComponent<CharacterControll>();
        shootPool = controll.shootPool;
        furtiveAttack = controll.meleeAttack;
        furtiveAttack.ghost = this;
        move = GetComponent<CharacterMove>();
        orbit = Camera.main.GetComponent<MouseOrbit>();
        sonar = Camera.main.GetComponent<SonarFx>();
        sonar.baseColor = sonarColor;
    }

    protected override void Update()
    {
        base.Update();

        furtiveModeTime -= Time.deltaTime;

        if (furtiveModeTime <= 0 && furtiveMode)
        {
            EndFurtiveMode();
        }
    }

    protected override void HandleAbility()
    {
        if (Keyboard.current == null) return;

        if (furtiveMode)
        {
            sonar.origin = transform.position;
            sonar.baseColor = sonarColor;
            hab2Timer = 0f;
            return;
        }

        // Habilidade 1 - E
        if (Keyboard.current[Key.E].wasPressedThisFrame && hab1Timer >= hab1Cooldown)
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
            CONCEITO: Bomba de Fumaça.
            Dispara uma bombinha de fumaça que fica liberando fumaça (particula), inimigos que entrarem em contato com essas particulas tem dificuldade em detectar o jogador, e caso estejam perseguindo o jogador, perdem ele de vista.
            ===*/
        Cache();
        if (smokeBombInstance == null)
        {
            Debug.Log("Debug: Bomba instanciada!");
            GameObject obj = Instantiate(controll.smokeBombPrefab);
            smokeBombInstance = obj.GetComponent<SmokeBomb>();
            obj.SetActive(false);
        }
        Vector3 dir = transform.forward * throwForce + Vector3.up * upwardForce;
        smokeBombInstance.Launch(transform.position + transform.forward * 0.5f, dir);

        Debug.Log("Debug: Bomba disparada!");
    }

    protected override void ExecuteHab2()
    {
        /*===
            CONCEITO: Modo Furtivo.
            O Jogador desaparece por alguns instantes, não podendo ser detectado pelos inimigos, e se movendo mais rapidamente, seu ataque muda para um ataque corpo-a-corpo que quando realizado (e acertar um inimigo) encerra automaticamente o modo, que se encerra sozinho depois de um tempo.
            ===*/
        Cache();
        furtiveMode = true;
        move.furtive = true;
        shootPool.enabled = false;
        furtiveAttack.enabled = true;
        furtiveAttack.isFurtiveAttack = true;
        furtiveModeTime = maxFurtiveModeTime;
        sonar.Show();
    }

    public void EndFurtiveMode()
    {
       furtiveMode = false; 
       move.furtive = false;
       shootPool.enabled = true;
       furtiveAttack.isFurtiveAttack = false;
       furtiveAttack.enabled = false;
       sonar.Hide();
       hab2Timer = 0f;
    }
}
