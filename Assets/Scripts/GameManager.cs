using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections;

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
    public GameObject rageGUI; // pai de toda a interface do jogador no modo Furia
    public GameObject droneGUI;  // pai de toda a interface do drone
    private enum GUIMode { Player, Drone, Furtive, Rage }
    private GUIMode currentMode;

    [Header("Drone GUI")]
    public Image droneLifeBar;
    private EnginerCharacter engineer;
    private DroneController drone;

    [Header("Game Start")]
    public TextMeshProUGUI gameStartText;
    public float startTime = 5f;
    public bool StopCountDown;
    public static bool GameStarted { get; private set; }

    [Header("FurtiveMode GUI")]
    public Image furtiveTimeBar;
    private GhostCharacter ghost;

    [Header("RageMode GUI")]
    public Image rageTimeBar;
    private HunterCharacter hunter;
    public TextMeshProUGUI rageHealthText;
    public TextMeshProUGUI rageAmmoText;
    public Image rageHab1Image;

    [Header("FragPanel GUI")]
    public GameObject tabPanel;
    public int FragScore = 0;
    public int DeathScore = 0;
    public int PointScore = 0;
    public TextMeshProUGUI fragText;
    public TextMeshProUGUI deathText;
    public TextMeshProUGUI pointText;
    public TextMeshProUGUI playerNameText;

    [Header("Control Zones GUI")]
    // Variaveis de Porcentagem de Zona, indica quantos % da zona um time conquistou.
    // Se um time estiver conquistando uma zona, e a % restante for igual ou menor que a % do outro time, a % do outro time começa a cair caso ninguem do outro time esteja na zona.
    public Image zoneAplayer; 
    public float zAPlayerPorc = 0f;
    public Image zoneBplayer;
    public float zBPlayerPorc = 0f;
    public Image zoneCplayer;
    public float zCPlayerPorc = 0f;
    public Image zoneAenemy;
    public float zAEnemyPorc = 0f;
    public Image zoneBenemy;
    public float zBEnemyPorc = 0f;
    public Image zoneCenemy;
    public float zCEnemyPorc = 0f;
    private bool countdownFinishing;

    void Awake()
    {
        GameStarted = false;
        Time.timeScale = 0f;
    }

    void Start()
    {
        playerAbility = player.gameObject.GetComponent<CharacterAbility>();
        engineer = playerAbility as EnginerCharacter;
        ghost = playerAbility as GhostCharacter;
        hunter = playerAbility as HunterCharacter;

        StopCountDown = false;
        SetGUIMode(GUIMode.Player);

        StartCoroutine(CountdownRoutine());
    }

    IEnumerator CountdownRoutine()
    {
        float t = startTime;

        while (t > 0f)
        {
            gameStartText.text = "A Partida vai começar em: " + Mathf.Ceil(t).ToString("0");
            t -= Time.unscaledDeltaTime;   // tempo real, ignora o timeScale
            yield return null;
        }

        gameStartText.text = "Partida Começou!";
        GameStarted = true;
        StopCountDown = true;
        Time.timeScale = 1f;               // libera o jogo

        yield return new WaitForSecondsRealtime(1f);
        gameStartText.text = "";
    }

    void Update()
    {
        drone = engineer != null ? engineer.ActiveDrone : null;

        GUIMode desired = GetDesiredMode();
        if (desired != currentMode)
            SetGUIMode(desired);

        UpdateCurrentGUI();
        ControlZoneGUI();
        ScorePanel();
    }

    GUIMode GetDesiredMode()
    {
        if (drone != null) return GUIMode.Drone;
        if (ghost != null && ghost.furtiveMode) return GUIMode.Furtive;
        if (hunter != null && hunter.rageMode) return GUIMode.Rage;
        return GUIMode.Player;
    }

    void UpdateCurrentGUI()
    {
        switch (currentMode)
        {
            case GUIMode.Drone: UpdateDroneGUI(); break;
            case GUIMode.Furtive: UpdateFurtiveGUI(); break;
            case GUIMode.Rage: UpdateInRageGUI(); break;
            default: UpdatePlayerGUI(); break;
        }
    }

    void SetGUIMode(GUIMode mode)
    {
        currentMode = mode;

        playerGUI.SetActive(mode == GUIMode.Player);
        droneGUI.SetActive(mode == GUIMode.Drone);
        furtiveGUI.SetActive(mode == GUIMode.Furtive);
        rageGUI.SetActive(mode == GUIMode.Rage);
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

    void UpdateInRageGUI()
    {
        rageHealthText.text = player.currentHealth.ToString("#");
        if (!ammo.IsReloading())
        {
            rageAmmoText.text = ammo.currentAmmo + " / " + ammo.GetMagazineSize();
        }
        else
        {
            rageAmmoText.text = "Recarregando. . .";
        }
        rageHab1Image.fillAmount = playerAbility.Hab1Progress;
        rageTimeBar.fillAmount = Mathf.Clamp01(hunter.RagePercent);
    }

    void ControlZoneGUI()
    {
        zoneAplayer.fillAmount = Mathf.Clamp01(zAPlayerPorc / 100f);
        zoneBplayer.fillAmount = Mathf.Clamp01(zBPlayerPorc / 100f);
        zoneCplayer.fillAmount = Mathf.Clamp01(zCPlayerPorc / 100f);

        zoneAenemy.fillAmount = Mathf.Clamp01(zAEnemyPorc / 100f);
        zoneBenemy.fillAmount = Mathf.Clamp01(zBEnemyPorc / 100f);
        zoneCenemy.fillAmount = Mathf.Clamp01(zCEnemyPorc / 100f);
    }

    // Sessão de Funções pro Painel de Status

    public void ScorePanel()
    {
        if (Keyboard.current != null && Keyboard.current[Key.Tab].wasPressedThisFrame)
        {
            tabPanel.SetActive(!tabPanel.activeSelf);
        }
    }

    public void PlayerList(string name)
    {
        playerNameText.text = name;
    }

    public void RefreshScoreUI(CharacterStats stats)
    {
        fragText.text = stats.kills.ToString();
        deathText.text = stats.deaths.ToString();
        pointText.text = stats.points.ToString();
    }
}
