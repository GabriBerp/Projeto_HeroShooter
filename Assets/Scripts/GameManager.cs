using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.SceneManagement;

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
    [SerializeField] private float matchDuration = 300f;
    private float timeRemaining;
    private bool isRunning = false;

    [Header("Match End")]
    public GameObject endPanel; // painel de fim de partida (começa desativado)
    public TextMeshProUGUI resultText; // texto do resultado dentro do painel
    [SerializeField] private float overtimeDuration = 60f;
    private bool overtimeUsed = false;

    public static bool MatchEnded { get; private set; }

    private enum MatchResult { PlayerWin, EnemyWin, Draw }

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
        MatchEnded = false;
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
        timeRemaining = matchDuration;
        isRunning = true;
    }

    void Update()
    {
        drone = engineer != null ? engineer.ActiveDrone : null;

        GUIMode desired = GetDesiredMode();
        if (desired != currentMode)
            SetGUIMode(desired);

        if (GameStarted && isRunning)
        {
            timeRemaining -= Time.deltaTime;
            UpdateTimerText();

            // Vitória antecipada: um time com 100% nas 3 zonas ao mesmo tempo
            if (CheckTotalDomination(out MatchResult dominationResult))
            {
                EndMatch(dominationResult);
            }
            else if (timeRemaining <= 0f)
            {
                OnMatchEnd();
            }
        }

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

    private void UpdateTimerText()
    {
        int totalSeconds = Mathf.CeilToInt(timeRemaining);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        string prefix = overtimeUsed ? "PRORROGAÇÃO  " : "";
        gameStartText.text = $"{prefix}{minutes:00}:{seconds:00}";
    }

    private void OnMatchEnd()
    {
        timeRemaining = 0f;

        MatchResult result = EvaluateZones();

        // Empate no tempo normal: prorrogação (só uma vez)
        if (result == MatchResult.Draw && !overtimeUsed)
        {
            overtimeUsed = true;
            timeRemaining = overtimeDuration;   // isRunning continua true
            Debug.Log("Empate! Prorrogação de +1 minuto.");
            return;
        }

        EndMatch(result);
    }

    // Compara a % de cada time em cada zona. Quem tem mais zonas vence.
    private MatchResult EvaluateZones()
    {
        int playerZones = 0;
        int enemyZones = 0;

        CountZone(zAPlayerPorc, zAEnemyPorc, ref playerZones, ref enemyZones);
        CountZone(zBPlayerPorc, zBEnemyPorc, ref playerZones, ref enemyZones);
        CountZone(zCPlayerPorc, zCEnemyPorc, ref playerZones, ref enemyZones);

        Debug.Log($"Zonas -> Jogador: {playerZones} | Inimigos: {enemyZones}");

        if (playerZones > enemyZones) return MatchResult.PlayerWin;
        if (enemyZones > playerZones) return MatchResult.EnemyWin;
        return MatchResult.Draw;
    }

    private void CountZone(float player, float enemy, ref int playerZones, ref int enemyZones)
    {
        const float epsilon = 0.01f;
        if (player > enemy + epsilon) playerZones++;
        else if (enemy > player + epsilon) enemyZones++;
        // iguais (ex.: 0 x 0 ou 50 x 50): zona neutra, ninguém pontua
    }

    private bool CheckTotalDomination(out MatchResult result)
    {
        const float full = 99.99f;

        bool player = zAPlayerPorc >= full && zBPlayerPorc >= full && zCPlayerPorc >= full;
        bool enemy  = zAEnemyPorc  >= full && zBEnemyPorc  >= full && zCEnemyPorc  >= full;

        result = player ? MatchResult.PlayerWin : MatchResult.EnemyWin;
        return player || enemy;
    }

    private void EndMatch(MatchResult result)
    {
        isRunning = false;
        MatchEnded = true;
        Time.timeScale = 0f;   // congela o jogo
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        string message = result switch
        {
            MatchResult.PlayerWin => "VITÓRIA!",
            MatchResult.EnemyWin  => "DERROTA!",
            _                     => "EMPATE!"
        };

        gameStartText.text = "00:00";
        if (resultText != null) resultText.text = message;
        if (endPanel != null) endPanel.SetActive(true);
        if (tabPanel != null) tabPanel.SetActive(true);   // mostra o placar final

        Debug.Log("Partida encerrada: " + message);
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

    // Sessão de Funções para o Painel de Fim de Partida
    public void PlayAgain()
    {
        SceneManager.LoadScene(1); // carrega a mesma cena novamente
    }

    public void GoBackToMenu()
    {
        SceneManager.LoadScene(0); // carrega a cena do menu
    }
}
