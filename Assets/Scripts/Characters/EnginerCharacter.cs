using UnityEngine;
using UnityEngine.InputSystem;
public class EnginerCharacter : CharacterAbility
{
    private DroneController activeDrone;
    private CharacterControll controll;
    private CharacterMove move;
    private MouseOrbit orbit;
    public DroneController ActiveDrone => activeDrone;

    private void Cache()
    {
        if (controll != null) return;
        controll = GetComponent<CharacterControll>();
        move = GetComponent<CharacterMove>();
        orbit = Camera.main.GetComponent<MouseOrbit>();
    }
    
    protected override void HandleAbility()
    {
        if (Keyboard.current == null) return;

        if (activeDrone != null)
        {
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
            CONCEITO: Noise Maker.
            Arremessa um dispositivo que atrai a atenção dos inimigos proximos por um tempo.
            ===*/ 

            // ignorar por enquanto
    }

    protected override void ExecuteHab2()
    {
        /*===
            CONCEITO: Drone Atirador.
            Posiciona um drone atirador que dura uma certa quantidade de tempo, mas permite o jogador se mover livremente pelo ambiente voando e atirando de longe nos inimigos.
            (Sim, é o drone de COD)
            ===*/
        Cache();
        if (controll.dronePrefab == null) return;
        Vector3 pos = transform.position + Vector3.up * 1.6f + transform.forward * 1f;
        if (activeDrone == null)
        {
            Debug.Log("Debug: Drone instanciado!");
            GameObject go = Instantiate(controll.dronePrefab, pos, transform.rotation);
            activeDrone = go.GetComponent<DroneController>();
            go.SetActive(false);
        }
        activeDrone.Init(controll, OnDroneDestroyed, pos);

        // Trava o player
        move.enabled = false;
        move.rb.linearVelocity = new Vector3(0f, move.rb.linearVelocity.y, 0f);
        controll.shootPool.enabled = false;

        // Câmera vai pro drone
        orbit.droneTarget = activeDrone.gameObject.transform;
    }

    private void OnDroneDestroyed()
    {
        activeDrone = null;
        orbit.droneTarget = null;

        if (controll.currentHealth > 0f)
        {
            move.enabled = true;
            controll.shootPool.enabled = true;
        }

        hab2Timer = 0f;
    }
}
