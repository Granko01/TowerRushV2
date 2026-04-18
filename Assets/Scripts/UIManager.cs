using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Menu Panel")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private TMP_InputField betInput;
    [SerializeField] private Button playButton;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Slider levelSlider;

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
        if (playButton != null)     playButton.onClick.AddListener(OnPlayClicked);
        if (cashOutButton != null)  cashOutButton.onClick.AddListener(OnCashOutClicked);
        if (playAgainButton != null) playAgainButton.onClick.AddListener(OnPlayAgainClicked);
        if (menuButton != null)     menuButton.onClick.AddListener(OnMenuClicked);

        if (menuPanel != null) ShowMenu();
    }

    // ─── Panels ──────────────────────────────────────────────────────────────

    public void ShowMenu()
    {
        if (menuPanel != null)   menuPanel.SetActive(true);
        if (hudPanel != null)    hudPanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);

        int current = GameManager.GetCurrentLevel();
        if (levelText != null)
            levelText.text = $"Level {current}";
        if (levelSlider != null)
            levelSlider.value = (float)(current - 1) / (GameManager.MaxLevel - 1);
    }

    public void UpdateHUD()
    {
        if (menuPanel != null)   menuPanel.SetActive(false);
        if (hudPanel != null)    hudPanel.SetActive(true);
        if (resultPanel != null) resultPanel.SetActive(false);

        float m = GameManager.Instance.Multiplier;
        int   f = GameManager.Instance.Floor;
        float b = GameManager.Instance.BetAmount;

        if (multiplierText != null) multiplierText.text = $"{m:F2}x";
        if (floorText != null)      floorText.text      = $"Floor {f} / {GameManager.Instance.EscapeFloor}";
        if (betText != null)        betText.text        = $"Bet: {b:F2}";
    }

    public void ShowResult(bool won, float amount, bool escaped = false)
    {
        if (hudPanel != null)    hudPanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(true);

        if (escaped)
        {
            if (resultTitleText != null)  resultTitleText.text  = "ESCAPED!";
            if (resultAmountText != null) resultAmountText.text = $"+{amount:F2}";
        }
        else if (won)
        {
            if (resultTitleText != null)  resultTitleText.text  = "CASHED OUT!";
            if (resultAmountText != null) resultAmountText.text = $"+{amount:F2}";
        }
        else
        {
            if (resultTitleText != null)  resultTitleText.text  = "BUSTED!";
            if (resultAmountText != null) resultAmountText.text = "-" + GameManager.Instance.BetAmount.ToString("F2");
        }
    }

    // ─── Button handlers ─────────────────────────────────────────────────────

    private void OnPlayClicked()
    {
        float bet = 10f;
        if (betInput != null && float.TryParse(betInput.text, out float parsed))
            bet = Mathf.Max(1f, parsed);

        PlayerPrefs.SetFloat("PendingBet", bet);
        SceneManager.LoadScene("Gameplay" + GameManager.GetCurrentLevel());
    }

    private void OnCashOutClicked()
    {
        GameManager.Instance.CashOut();
    }

    private void OnPlayAgainClicked()
    {
        PlayerPrefs.SetFloat("PendingBet", GameManager.Instance.BetAmount);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnMenuClicked()
    {
        SceneManager.LoadScene("Menu");
    }
}
