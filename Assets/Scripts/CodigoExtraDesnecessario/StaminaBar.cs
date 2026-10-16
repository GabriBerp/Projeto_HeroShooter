using UnityEngine;
using UnityEngine.UI;

public class StaminaBar : MonoBehaviour
{
    public CharacterMove player;
    private float val;
    public Image staminaBar;

    void Update()
    {
        val = player.GetStamina() / 100f;
        staminaBar.fillAmount = val;
    }
}

