using UnityEngine;
public class GhostCharacter : CharacterAbility
{
    protected override void ExecuteHab1()
    {
        /*===
            CONCEITO: Bomba de Fumaça.
            Dispara uma bombinha de fumaça que cobre uma pequena area ao seu redor, inimigos que entrarem em contato com essas particulas tem dificuldade em detectar o jogador.
            ===*/
    }

    protected override void ExecuteHab2()
    {
        /*===
            CONCEITO: Modo Furtivo.
            O Jogador desaparece por alguns instantes, não podendo ser detectado pelos inimigos, e se movendo mais rapidamente, atacar durante esse modo encerra o modo mais rapidamente.
            ===*/
    }
}
