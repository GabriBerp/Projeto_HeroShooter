using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Player GUI")]
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI ammoText;
    public Image hab1Image;
    public Image hab2Image;
    public CharacterControll player;
    public CharacterAbility playerAbility;
    public ShootPool ammo;

    [Header("GUI Roots")]
    public GameObject playerGUI; // pai de toda a interface do jogador
    public GameObject furtiveGUI; // pai de toda a interface do jogador no modo Furtivo
    public GameObject droneGUI;  // pai de toda a interface do drone
    [Header("Drone GUI")]
    public Image droneLifeBar;
    private EnginerCharacter engineer;
    private DroneController drone;
    private bool droneMode;

    [Header("FurtiveMode GUI")]
    private GhostCharacter ghost;
    public Image furtiveTimeBar;
    private bool furtiveMode;

    void Start()
    {
        playerAbility = player.gameObject.GetComponent<CharacterAbility>();
        engineer = playerAbility as EnginerCharacter; // null se não for o Engenheiro
        ghost = playerAbility as GhostCharacter; // null se não for o Fantasma

        SetGUIMode("player");
    }

    void Update()
    {
        drone = engineer != null ? engineer.ActiveDrone : null;

        bool shouldBeDrone = drone != null;
        bool shouldBeFurtive = ghost != null ? ghost.furtiveMode : false;
        if (shouldBeDrone && !droneMode)
        {
            SetGUIMode("drone");
        }else if (!shouldBeDrone && droneMode)
        {
            SetGUIMode("player");
        }

        if (shouldBeFurtive)
        {
            SetGUIMode("furtive");
        }
        else
        {
            SetGUIMode("player");
        }

        if (droneMode)
        {
            UpdateDroneGUI();
        }
        else if (furtiveMode)
        {
            UpdateFurtiveGUI();
        }
        else
        {
            UpdatePlayerGUI();
        }
    }

    void SetGUIMode(string mode)
    {
        switch (mode)
        {
            case "drone":
                droneMode = true;
                playerGUI.SetActive(false);
                droneGUI.SetActive(true);
                break;
            case "player":
                droneMode = false;
                furtiveMode = false;
                droneGUI.SetActive(false);
                furtiveGUI.SetActive(false);
                playerGUI.SetActive(true);
                break;
            case "furtive":
                furtiveMode = true;
                furtiveGUI.SetActive(true);
                playerGUI.SetActive(false);
                break;

        }
    }

    void UpdatePlayerGUI()
    {
        healthText.text = player.currentHealth.ToString("#");
        if (!ammo.IsReloading())
        {
            ammoText.text = ammo.currentAmmo + " / " + ammo.GetMagazineSize();
        }
        else
        {
            ammoText.text = "Recarregando. . .";
        }
        hab1Image.fillAmount = playerAbility.Hab1Progress;
        hab2Image.fillAmount = playerAbility.Hab2Progress;
    }

    void UpdateDroneGUI()
    {
        droneLifeBar.fillAmount = Mathf.Clamp01(drone.LifePercent);
    }

    void UpdateFurtiveGUI()
    {
        furtiveTimeBar.fillAmount = Mathf.Clamp01(ghost.FurtivePercent);
    }
}
