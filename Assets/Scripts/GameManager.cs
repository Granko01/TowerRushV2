using UnityEngine;

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

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
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
        UIManager.Instance.UpdateHUD();
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
        State = GameState.Idle;
        TowerManager.Instance.ClearTower();
        UIManager.Instance.ShowMenu();
    }
}
