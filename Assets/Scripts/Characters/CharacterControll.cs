using UnityEngine;

public class CharacterControll : MonoBehaviour
{
    /*
    === CONTROLE DO JOGADOR ===
    - Controla Vida
    - Controla Variante de Personagem
    ===                     ===
    */

    public enum CharacterType
    {
        HunterCharacter,
        GhostCharacter,
        EnginerCharacter
    }

    [Header("Life Parameters")]
    public float maxHealth;
    public float currentHealth;
    [Header("Character Type Parameters")]
    public CharacterType characterType = CharacterType.HunterCharacter;
    public MeshRenderer capsuleMesh;
    public MeshRenderer visorMesh;
    public MeshRenderer minimapMesh;
    public Material[] characterMaterial;
    public Material[] visorMaterial;
    private CharacterAbility characterAbility;
    private CharacterMove characterMove;
    public ShootPool shootPool;
    [Header("Light Parameter")]
    public Light flashlight;
    [Header("Engineer")]
    public GameObject dronePrefab;
    [Header("Ghost")]
    public GameObject smokeBombPrefab;


    void Awake() {
        characterMove = gameObject.GetComponent<CharacterMove>();

        /* 
        Switch pra adicionar automaticamente o script das habilidades do personagem dependendo de qual personagem é, isso ajuda a ter somente 1 objeto do player (e não 1 pra cada personagem) que vai receber um componente dependendo do valor de uma simples variavel.
        e ainda de quebra é usado pra definir alguns valores tipo o material do modelo (so pra mudar corzinha), velocidade base, vida maxima e assim vai indo.
        Pra evitar trabalho a gente faz mais trabalho erriessi
        */
        if (GameMaster.Instance != null){
            switch (GameMaster.Instance.id)
            {
                case 1: 
                    characterType = CharacterType.HunterCharacter;
                    characterAbility = gameObject.AddComponent<HunterCharacter>();
                    characterAbility.hab1Cooldown = 25f;
                    characterAbility.hab2Cooldown = 50f;

                    capsuleMesh.material = characterMaterial[0];
                    visorMesh.material = visorMaterial[0];
                    minimapMesh.material = visorMaterial[0];
                    characterMove.baseSpeed = 4f;
                    maxHealth = 20f;
                    shootPool.SetWeapon(ShootPool.WeaponType.Shotgun);
                    break;
                case 2: 
                    characterType = CharacterType.GhostCharacter;
                    characterAbility = gameObject.AddComponent<GhostCharacter>();
                    characterAbility.hab1Cooldown = 25f;
                    characterAbility.hab2Cooldown = 50f;

                    capsuleMesh.material = characterMaterial[1];
                    visorMesh.material = visorMaterial[1];
                    minimapMesh.material = visorMaterial[1];
                    characterMove.baseSpeed = 5f;
                    maxHealth = 14f;
                    shootPool.SetWeapon(ShootPool.WeaponType.Silent_Revolver);
                    break;
                case 3: 
                    characterType = CharacterType.EnginerCharacter;
                    characterAbility = gameObject.AddComponent<EnginerCharacter>();
                    characterAbility.hab1Cooldown = 25f;
                    characterAbility.hab2Cooldown = 50f;
                    
                    capsuleMesh.material = characterMaterial[2];
                    visorMesh.material = visorMaterial[2];
                    minimapMesh.material = visorMaterial[2];
                    characterMove.baseSpeed = 4.5f;
                    maxHealth = 16f;
                    shootPool.SetWeapon(ShootPool.WeaponType.Nailgun);
                    break;
            }
        }else{
            switch (characterType)
            {
                case CharacterType.HunterCharacter: 
                    // characterType = CharacterType.HunterCharacter;
                    characterAbility = gameObject.AddComponent<HunterCharacter>();
                    characterAbility.hab1Cooldown = 25f;
                    characterAbility.hab2Cooldown = 50f;

                    capsuleMesh.material = characterMaterial[0];
                    visorMesh.material = visorMaterial[0];
                    minimapMesh.material = visorMaterial[0];
                    characterMove.baseSpeed = 4f;
                    maxHealth = 20f;
                    shootPool.SetWeapon(ShootPool.WeaponType.Shotgun);
                    break;
                case CharacterType.GhostCharacter: 
                    // characterType = CharacterType.GhostCharacter;
                    characterAbility = gameObject.AddComponent<GhostCharacter>();
                    characterAbility.hab1Cooldown = 25f;
                    characterAbility.hab2Cooldown = 50f;

                    capsuleMesh.material = characterMaterial[1];
                    visorMesh.material = visorMaterial[1];
                    minimapMesh.material = visorMaterial[1];
                    characterMove.baseSpeed = 5f;
                    maxHealth = 14f;
                    shootPool.SetWeapon(ShootPool.WeaponType.Silent_Revolver);
                    break;
                case CharacterType.EnginerCharacter: 
                    // characterType = CharacterType.EnginerCharacter;
                    characterAbility = gameObject.AddComponent<EnginerCharacter>();
                    characterAbility.hab1Cooldown = 25f;
                    characterAbility.hab2Cooldown = 50f;
                    
                    capsuleMesh.material = characterMaterial[2];
                    visorMesh.material = visorMaterial[2];
                    minimapMesh.material = visorMaterial[2];
                    characterMove.baseSpeed = 4.5f;
                    maxHealth = 16f;
                    shootPool.SetWeapon(ShootPool.WeaponType.Nailgun);
                    break;
            }
        }
    }

    void Start() {
        currentHealth = maxHealth;
    } 

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
    }
}
