using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI ammoText;
    public Image hab1Image;
    public Image hab2Image;
    public CharacterControll player;
    public CharacterAbility playerAbility;
    public ShootPool ammo;

    void Start()
    {
        playerAbility = player.gameObject.GetComponent<CharacterAbility>();
    }

    void Update()
    {
        UpdateGUI();
    }

    void UpdateGUI()
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
}
