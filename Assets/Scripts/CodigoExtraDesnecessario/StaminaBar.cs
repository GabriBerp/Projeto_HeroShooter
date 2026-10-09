using UnityEngine;

public class StaminaBar : MonoBehaviour
{
    public CharacterMove player;
    private float val;
    public RectTransform rectTrans;

    void Update()
    {
        val = player.GetStamina() * 5f;
        rectTrans.sizeDelta = new Vector2(val, rectTrans.sizeDelta.y);
    }
}

