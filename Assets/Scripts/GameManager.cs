using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Idle, Playing, GameOver }
    public GameState State { get; private set; } = GameState.Idle;

    public float BetAmount  { get; private set; } = 10f;
    public float Multiplier { get; private set; } = 1f;
    public int   Floor      { get; private set; } = 0;

    [Header("Multiplier")]
    [SerializeField] private float multiplierPerFloor = 0.30f;

    [Header("Escape")]
    [SerializeField] private int escapeFloor = 10;
    public int EscapeFloor => escapeFloor;

    [Header("Rewards")]
    [SerializeField] private int coinsPerLevel = 25;

    public const int MaxLevel = 7;
    public static int GetCurrentLevel() => PlayerPrefs.GetInt("CurrentLevel", 1);

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (SceneManager.GetActiveScene().name.StartsWith("Gameplay"))
        {
            float bet = PlayerPrefs.GetFloat("PendingBet", 10f);
            StartGame(bet);
        }
    }

    public void StartGame(float bet)
    {
        BetAmount  = bet;
        Multiplier = 1f;
        Floor      = 0;
        State      = GameState.Playing;
        TowerManager.Instance.StartTower();
        UIManager.Instance.UpdateHUD();
    }

    public void RegisterFloor()
    {
        Floor++;
        Multiplier = 1f + Floor * multiplierPerFloor;

        if (Floor >= escapeFloor)
        {
            TriggerEscape();
            return;
        }

        UIManager.Instance.UpdateHUD();
    }

    public void TriggerEscape()
    {
        if (State != GameState.Playing) return;
        State = GameState.Idle;

        int next = Mathf.Min(GetCurrentLevel() + 1, MaxLevel);
        PlayerPrefs.SetInt("CurrentLevel", next);

        UIManager.Instance.CoinsAmount += coinsPerLevel;
        UIManager.Instance.SetCoins();
        PlayerPrefs.Save();

        float winnings = BetAmount * Multiplier;
        UIManager.Instance.ShowResult(true, winnings, escaped: true);
    }

    public void CashOut()
    {
        if (State != GameState.Playing) return;
        State = GameState.Idle;
        float winnings = BetAmount * Multiplier;
        UIManager.Instance.ShowResult(true, winnings);
    }

    public void TriggerGameOver()
    {
        if (State != GameState.Playing) return;
        State = GameState.GameOver;
        UIManager.Instance.ShowResult(false, 0f);
    }

    public void ReturnToMenu()
    {
        SceneManager.LoadScene("Menu");
    }
}
