using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Menu Panel")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private TMP_InputField betInput;
    [SerializeField] private Button playButton;

    [Header("HUD Panel")]
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private TMP_Text multiplierText;
    [SerializeField] private TMP_Text floorText;
    [SerializeField] private TMP_Text betText;
    [SerializeField] private Button cashOutButton;

    [Header("Result Panel")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultTitleText;
    [SerializeField] private TMP_Text resultAmountText;
    [SerializeField] private Button playAgainButton;
    [SerializeField] private Button menuButton;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        playButton.onClick.AddListener(OnPlayClicked);
        cashOutButton.onClick.AddListener(OnCashOutClicked);
        playAgainButton.onClick.AddListener(OnPlayAgainClicked);
        menuButton.onClick.AddListener(OnMenuClicked);

        ShowMenu();
    }

    // ─── Panels ──────────────────────────────────────────────────────────────

    public void ShowMenu()
    {
        menuPanel.SetActive(true);
        hudPanel.SetActive(false);
        resultPanel.SetActive(false);
    }

    public void UpdateHUD()
    {
        menuPanel.SetActive(false);
        hudPanel.SetActive(true);
        resultPanel.SetActive(false);

        float m = GameManager.Instance.Multiplier;
        int   f = GameManager.Instance.Floor;
        float b = GameManager.Instance.BetAmount;

        multiplierText.text = $"{m:F2}x";
        floorText.text      = $"Floor {f}";
        betText.text        = $"Bet: {b:F2}";
    }

    public void ShowResult(bool won, float amount)
    {
        hudPanel.SetActive(false);
        resultPanel.SetActive(true);

        if (won)
        {
            resultTitleText.text  = "CASHED OUT!";
            resultAmountText.text = $"+{amount:F2}";
        }
        else
        {
            resultTitleText.text  = "BUSTED!";
            resultAmountText.text = "-" + GameManager.Instance.BetAmount.ToString("F2");
        }
    }

    // ─── Button handlers ─────────────────────────────────────────────────────

    private void OnPlayClicked()
    {
        float bet = 10f;
        if (betInput != null && float.TryParse(betInput.text, out float parsed))
            bet = Mathf.Max(1f, parsed);

        GameManager.Instance.StartGame(bet);
        Camera.main.GetComponent<CameraFollow>()?.ResetCamera();
    }

    private void OnCashOutClicked()
    {
        GameManager.Instance.CashOut();
    }

    private void OnPlayAgainClicked()
    {
        float bet = GameManager.Instance.BetAmount;
        GameManager.Instance.StartGame(bet);
        Camera.main.GetComponent<CameraFollow>()?.ResetCamera();
    }

    private void OnMenuClicked()
    {
        GameManager.Instance.ReturnToMenu();
        Camera.main.GetComponent<CameraFollow>()?.ResetCamera();
    }
}
