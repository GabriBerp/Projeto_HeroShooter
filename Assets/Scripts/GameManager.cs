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
    public GameObject droneGUI;  // pai de toda a interface do drone
    [Header("Drone GUI")]
    public Image droneLifeBar;
    private EnginerCharacter engineer;
    private DroneController drone;
    private bool droneMode;

    void Start()
    {
        playerAbility = player.gameObject.GetComponent<CharacterAbility>();
        engineer = playerAbility as EnginerCharacter; // null se não for o Engenheiro

        SetGUIMode(false);
    }

    void Update()
    {
        drone = engineer != null ? engineer.ActiveDrone : null;

        bool shouldBeDrone = drone != null;
        if (shouldBeDrone != droneMode)
        {
            SetGUIMode(shouldBeDrone);
        }

        if (droneMode)
        {
            UpdateDroneGUI();
        }
        else
        {
            UpdatePlayerGUI();
        }
    }

    void SetGUIMode(bool isDrone)
    {
        droneMode = isDrone;
        playerGUI.SetActive(!isDrone);
        droneGUI.SetActive(isDrone);
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
}
