using UnityEngine;

public class CharacterStats : MonoBehaviour
{
    public enum TeamType
    {
        Player,
        Ally,
        Enemy
    }

    public TeamType team = TeamType.Enemy;
    public int kills = 0;
    public int deaths = 0;
    public int points = 0;
    public string characterName;
    public GameManager gameManager;

    void Start()
    {
        AddPlayerName();
    }

    public void AddPlayerName()
    {
        if (team == TeamType.Player)
        {
            gameManager.PlayerList(characterName);
        }
    }

    public void AddKill()
    {
        kills++;
        AddPoint("kill");
        if (team == TeamType.Player) gameManager.RefreshScoreUI(this);
    }

    public void AddDeath()
    {
        deaths++;
        LosePoint();
        if (team == TeamType.Player) gameManager.RefreshScoreUI(this);
    }

    public void AddPoint(string cause)
    {
        switch (cause)
        {
            case "kill": points+= 5;break;
            case "control": points+= 2;break;
        }
        if (team == TeamType.Player) gameManager.RefreshScoreUI(this);
    }

    public void LosePoint()
    {
        points-= 5;
        if (team == TeamType.Player) gameManager.RefreshScoreUI(this);
    }
}
