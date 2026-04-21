using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.VisualScripting;

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
    [SerializeField] private GameObject YoulosePanel;
    public GameObject NoCement;
    public GameObject LeaveButton;
    [SerializeField] private TMP_Text resultTitleText;
    [SerializeField] private TMP_Text resultAmountText;
    [SerializeField] private Button playAgainButton;
    [SerializeField] private Button[] menuButton;

    public int CementAmount = 5;
    public int CoinsAmount = 200;
    public Text[] CementText;
    public Text[] CoinText;
    private const string CementKey = "Cement";
    private const string CoinsKey = "Coins";
    public GameObject ShopPanel;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        GetData();
        if (playButton != null)     playButton.onClick.AddListener(OnPlayClicked);
        if (cashOutButton != null)  cashOutButton.onClick.AddListener(OnCashOutClicked);
        if (playAgainButton != null) playAgainButton.onClick.AddListener(OnPlayAgainClicked);
        if (SceneManager.GetActiveScene().name != "Menu" )
        {
             if (menuButton != null)     menuButton[0].onClick.AddListener(OnMenuClicked);
            if (menuButton != null)     menuButton[1].onClick.AddListener(OnMenuClicked);
        }
       

        if (menuPanel != null) ShowMenu();
    }
    public void GetData()
    {
        GetCement();
        GetCoins();
        UpdateUI(CementText, CementAmount);
        UpdateUI(CoinText, CoinsAmount);
    }

    // ─── Panels ──────────────────────────────────────────────────────────────

    public void ShowMenu()
    {
        if (menuPanel != null)   menuPanel.SetActive(true);
        if (hudPanel != null)    hudPanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);
        if (YoulosePanel != null) YoulosePanel.SetActive(false);

        int current = GameManager.GetCurrentLevel();
        if (levelText != null)
            levelText.text = $"Level {current}";
        if (levelSlider != null)
            levelSlider.value = (float)(current - 1) / (GameManager.MaxLevel - 1);
    }
    public void TestScene()
    {
        SceneManager.LoadScene("Gameplay1");
    }
    public void UpdateHUD()
    {
        if (menuPanel != null)   menuPanel.SetActive(false);
        if (hudPanel != null)    hudPanel.SetActive(true);
        if (resultPanel != null) resultPanel.SetActive(false);
        if (YoulosePanel != null) YoulosePanel.SetActive(false);

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
       

        if (escaped)
        {
            if (resultTitleText != null)  resultTitleText.text  = "ESCAPED!";
            if (resultAmountText != null) resultAmountText.text = $"+{amount:F2}";
            if (resultPanel != null) resultPanel.SetActive(true);
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
            if (YoulosePanel != null) YoulosePanel.SetActive(true);

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
     public void GetCoins()
    {
        CoinsAmount = PlayerPrefs.GetInt(CoinsKey, CoinsAmount);
    }
    public void SetCoins()
    {
        PlayerPrefs.SetInt(CoinsKey, CoinsAmount);
    }
    public void GetCement()
    {
        CementAmount = PlayerPrefs.GetInt(CementKey, CementAmount);
    }
    public void SetCement()
    {
        PlayerPrefs.SetInt(CementKey, CementAmount);
    }
    public void UpdateUI(Text[] t, int amount)
    {
        foreach (var text in t)
        {
            text.text = amount.ToString();
        }
    }
    public void OpenShop(bool show)
    {
        if (show)
        {
            ShopPanel.gameObject.SetActive(true);
        }
        else
        {
            ShopPanel.gameObject.SetActive(false);
        }
    }

    public void ShowLeaveButton()
    {
        if (LeaveButton != null)
            LeaveButton.SetActive(true);
    }

    public void OnLeaveClicked()
    {
        SceneManager.LoadScene("Menu");
    }
}
