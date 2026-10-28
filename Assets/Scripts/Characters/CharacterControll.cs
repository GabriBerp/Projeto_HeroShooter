using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterControll : MonoBehaviour
{
    public PlayerSpawnControl spawn;
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
    public MeleeAttack meleeAttack;
    [Header("Light Parameter")]
    public Light flashlight;
    [Header("Engineer")]
    public GameObject dronePrefab;
    [Header("Ghost")]
    public GameObject smokeBombPrefab;
    public GameObject noiseMakerPrefab;

    public bool isResistent = false;

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
                    meleeAttack.SetWeapon(MeleeAttack.MeleeType.Axe);
                    break;
                case 2: 
                    characterType = CharacterType.GhostCharacter;
                    characterAbility = gameObject.AddComponent<GhostCharacter>();
                    characterAbility.hab1Cooldown = 30f;
                    characterAbility.hab2Cooldown = 40f;

                    capsuleMesh.material = characterMaterial[1];
                    visorMesh.material = visorMaterial[1];
                    minimapMesh.material = visorMaterial[1];
                    characterMove.baseSpeed = 5f;
                    maxHealth = 14f;
                    shootPool.SetWeapon(ShootPool.WeaponType.Silent_Revolver);
                    meleeAttack.SetWeapon(MeleeAttack.MeleeType.Knife);
                    break;
                case 3: 
                    characterType = CharacterType.EnginerCharacter;
                    characterAbility = gameObject.AddComponent<EnginerCharacter>();
                    characterAbility.hab1Cooldown = 20f;
                    characterAbility.hab2Cooldown = 60f;
                    
                    capsuleMesh.material = characterMaterial[2];
                    visorMesh.material = visorMaterial[2];
                    minimapMesh.material = visorMaterial[2];
                    characterMove.baseSpeed = 4.5f;
                    maxHealth = 16f;
                    shootPool.SetWeapon(ShootPool.WeaponType.Nailgun);
                    meleeAttack.SetWeapon(MeleeAttack.MeleeType.Bat);
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
                    meleeAttack.SetWeapon(MeleeAttack.MeleeType.Axe);
                    break;
                case CharacterType.GhostCharacter: 
                    // characterType = CharacterType.GhostCharacter;
                    characterAbility = gameObject.AddComponent<GhostCharacter>();
                    characterAbility.hab1Cooldown = 30f;
                    characterAbility.hab2Cooldown = 40f;

                    capsuleMesh.material = characterMaterial[1];
                    visorMesh.material = visorMaterial[1];
                    minimapMesh.material = visorMaterial[1];
                    characterMove.baseSpeed = 5f;
                    maxHealth = 14f;
                    shootPool.SetWeapon(ShootPool.WeaponType.Silent_Revolver);
                    meleeAttack.SetWeapon(MeleeAttack.MeleeType.Knife);
                    break;
                case CharacterType.EnginerCharacter: 
                    // characterType = CharacterType.EnginerCharacter;
                    characterAbility = gameObject.AddComponent<EnginerCharacter>();
                    characterAbility.hab1Cooldown = 15f;
                    characterAbility.hab2Cooldown = 60f;
                    
                    capsuleMesh.material = characterMaterial[2];
                    visorMesh.material = visorMaterial[2];
                    minimapMesh.material = visorMaterial[2];
                    characterMove.baseSpeed = 4.5f;
                    maxHealth = 16f;
                    shootPool.SetWeapon(ShootPool.WeaponType.Nailgun);
                    meleeAttack.SetWeapon(MeleeAttack.MeleeType.Bat);
                    break;
            }
        }
    }

    void Start() 
    {
        currentHealth = maxHealth;
        SwitchWeapons(false);
    } 

    void Update() 
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            // 1 - Troca pra arma principal (shootPool)
            SwitchWeapons(false);
        }
        else if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            // 2 - Troca pra arma secundária (meleeAttack)
            SwitchWeapons(true);
        }
    }

    void SwitchWeapons(bool state)
    {
        shootPool.enabled = !state;
        meleeAttack.enabled = state;
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0f)
        {
            Die();
            return;
        }
    }

    void Die()
    {
        spawn.ReturnToPool(this);
    }
}
