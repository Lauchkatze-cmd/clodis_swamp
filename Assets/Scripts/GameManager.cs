using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Controls the game state: start screen, playing, paused, won, lost.
/// Put it on an empty GameObject called "GameManager" (only once per scene).
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Goal")]
    [SerializeField] private int eggsToCollect = 5;

    [Header("UI (optional)")]
    [SerializeField] private Text eggCounterText;
    [SerializeField] private Text healthText;
    [SerializeField] private GameObject startScreen;
    [SerializeField] private GameObject pauseScreen;
    [SerializeField] private GameObject winScreen;
    [SerializeField] private GameObject loseScreen;

    [Header("Counter Symbols")]
    [Tooltip("Shown for every collected egg / remaining health point.")]
    [SerializeField] private string filledSymbol = "●";
    [Tooltip("Shown for every missing egg / lost health point.")]
    [SerializeField] private string emptySymbol = "○";

    [Header("Restart")]
    [SerializeField] private float reloadDelay = 2f;

    public bool IsPlaying { get; private set; } = true;
    public bool IsGameOver { get; private set; }
    public bool IsOnStartScreen { get; private set; }
    public int CollectedEggs { get; private set; }
    public int EggsToCollect => eggsToCollect;
    public bool HasAllEggs => CollectedEggs >= eggsToCollect;

    // Stays true while the game is running, so a restart after winning or losing skips the start screen
    private static bool hasStartedOnce;
    private float reloadTimer = -1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        hasStartedOnce = false;
    }

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
    }

    private void Start()
    {
        if (pauseScreen != null) pauseScreen.SetActive(false);
        if (winScreen != null) winScreen.SetActive(false);
        if (loseScreen != null) loseScreen.SetActive(false);
        UpdateEggText();

        if (startScreen != null && !hasStartedOnce) ShowStartScreen();
        else if (startScreen != null) startScreen.SetActive(false);
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        // On the start screen: W (or Enter) starts the game
        if (IsOnStartScreen)
        {
            if (keyboard != null && (keyboard.wKey.wasPressedThisFrame
                || keyboard.enterKey.wasPressedThisFrame
                || keyboard.numpadEnterKey.wasPressedThisFrame))
            {
                StartGame();
            }
            return;
        }

        // Esc toggles the pause menu
        if (!IsGameOver && keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            if (IsPlaying) PauseGame();
            else ResumeGame();
        }

        // Reload the level after a delay (unscaled, so it also works while time is stopped)
        if (reloadTimer >= 0f)
        {
            reloadTimer -= Time.unscaledDeltaTime;
            if (reloadTimer <= 0f)
            {
                reloadTimer = -1f;
                ReloadGame();
            }
        }
    }

    private void ShowStartScreen()
    {
        IsOnStartScreen = true;
        IsPlaying = false;
        Time.timeScale = 0f;
        startScreen.SetActive(true);
    }

    /// <summary>Called by the Play button and by pressing W on the start screen.</summary>
    public void StartGame()
    {
        hasStartedOnce = true;
        IsOnStartScreen = false;
        IsPlaying = true;
        Time.timeScale = 1f;
        if (startScreen != null) startScreen.SetActive(false);
    }

    public void PauseGame()
    {
        IsPlaying = false;
        Time.timeScale = 0f;
        if (pauseScreen != null && !IsGameOver) pauseScreen.SetActive(true);
    }

    public void ResumeGame()
    {
        if (IsGameOver) return;
        IsPlaying = true;
        Time.timeScale = 1f;
        if (pauseScreen != null) pauseScreen.SetActive(false);
    }

    public void StopGame()
    {
        IsPlaying = false;
        IsGameOver = true;
        Time.timeScale = 0f;
    }

    public void ReloadGame()
    {
        Time.timeScale = 1f;
        // The scene must be in File > Build Profiles > Scene List, otherwise this fails
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>Goes back to the start screen (e.g. from a "Main Menu" button).</summary>
    public void BackToStartScreen()
    {
        hasStartedOnce = false;
        ReloadGame();
    }

    /// <summary>Called by the Quit button. Only works in the built game, not in the editor.</summary>
    public void QuitGame()
    {
        Application.Quit();
    }

    public void AddEgg()
    {
        CollectedEggs++;
        UpdateEggText();
    }

    public void UpdateHealth(int currentHealth, int maxHealth)
    {
        if (healthText != null) healthText.text = BuildSymbols(currentHealth, maxHealth);
    }

    public void WinGame()
    {
        if (IsGameOver) return;
        StopGame();
        if (winScreen != null) winScreen.SetActive(true);
        reloadTimer = reloadDelay + 1f;
    }

    public void LoseGame()
    {
        if (IsGameOver) return;
        StopGame();
        if (loseScreen != null) loseScreen.SetActive(true);
        reloadTimer = reloadDelay;
    }

    private void UpdateEggText()
    {
        if (eggCounterText != null) eggCounterText.text = BuildSymbols(CollectedEggs, eggsToCollect);
    }

    /// <summary>Example: BuildSymbols(2, 5) gives "● ● ○ ○ ○".</summary>
    private string BuildSymbols(int filled, int total)
    {
        StringBuilder symbols = new StringBuilder();
        for (int i = 0; i < total; i++)
        {
            if (i > 0) symbols.Append(' ');
            symbols.Append(i < filled ? filledSymbol : emptySymbol);
        }
        return symbols.ToString();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
