using UnityEngine;
using UnityEngine.SceneManagement;

public class GameMaster : MonoBehaviour
{
    public static GameMaster Instance { get; private set; }

    public int id;
    public GameObject now;

    void Awake() {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    public void ButtonPressed(GameObject next)
    {
        now.SetActive(false);
        next.SetActive(true);
        now = next;
    }

    public void SelectCharacter(int i)
    {
        id = i;
        SceneManager.LoadScene(1);
    }
}
