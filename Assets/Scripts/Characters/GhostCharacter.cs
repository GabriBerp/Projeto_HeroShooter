using UnityEngine;
public class GhostCharacter : CharacterAbility
{
    public GameObject smokeBombPrefab;
    // public Transform throwOrigin;
    public float throwForce = 12f;
    public float upwardForce = 5f;

    SmokeBomb smokeBombInstance; // o "pool" de 1 objeto
    protected override void ExecuteHab1()
    {
        /*===   
            CONCEITO: Bomba de Fumaça.
            Dispara uma bombinha de fumaça que fica liberando fumaça (particula), inimigos que entrarem em contato com essas particulas tem dificuldade em detectar o jogador, e caso estejam perseguindo o jogador, perdem ele de vista.
            ===*/
        if (smokeBombInstance == null)
        {
            Debug.Log("Debug: Bomba instanciada!");
            GameObject obj = Instantiate(smokeBombPrefab);
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
            O Jogador desaparece por alguns instantes, não podendo ser detectado pelos inimigos, e se movendo mais rapidamente, atacar durante esse modo encerra o modo mais rapidamente.
            ===*/
    }
}
