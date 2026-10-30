using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuButtonScript : MonoBehaviour
{
    public GameObject now;
    public void ButtonPressed(GameObject next)
    {
        now.SetActive(false);
        next.SetActive(true);
        now = next;
    }

    public void SelectCharacter(int i)
    {
        GameMaster.Instance.id = i;
        SceneManager.LoadScene(1);
    }

    public void doExitGame() 
    {
        Application.Quit();
    }
}
