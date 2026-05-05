using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

public class DailyBonus : MonoBehaviour
{
    [Header("Buttons & Timer")]
    [SerializeField] private Button unlockButton;
    [SerializeField] private Button claimButton;
    [SerializeField] private Text   timerText;
    [SerializeField] private Text   unlockCostText;

    [Header("Animation")]
    [SerializeField] private RectTransform chestTransform;  // particles spawn as children of this
    [SerializeField] private Sprite        coinSprite;
    [SerializeField] private Sprite        cementSprite;
    [SerializeField] private Text          coinRewardText;
    [SerializeField] private Text          cementRewardText;

    [Header("Rewards & Cost")]
    [SerializeField] private int coinReward   = 30;
    [SerializeField] private int cementReward = 2;
    [SerializeField] private int unlockCost   = 50;

    private const string UnlockedKey   = "DailyBonusUnlocked";
    private const string LastClaimKey  = "DailyBonusLastClaim";
    private const int    CooldownHours = 24;

    private UIManager ui;

    // ─── Init ─────────────────────────────────────────────────────────────────

    void Start()
    {
        ui = UIManager.Instance;
        if (unlockCostText != null) unlockCostText.text = unlockCost.ToString();
        SetActive(coinRewardText,   false);
        SetActive(cementRewardText, false);
        RefreshUI();
    }

    void Update()
    {
        if (IsUnlocked() && !CanClaim())
            UpdateCountdown();
    }

    // ─── Button callbacks ─────────────────────────────────────────────────────

    public void OnUnlockClicked()
    {
        if (IsUnlocked() || ui.CoinsAmount < unlockCost) return;
        AudioManager.Instance?.PlayButtonClick();

        ui.CoinsAmount -= unlockCost;
        ui.SetCoins();
        ui.UpdateUI(ui.CoinText, ui.CoinsAmount);

        PlayerPrefs.SetInt(UnlockedKey, 1);
        PlayerPrefs.Save();
        RefreshUI();
    }

    public void OnClaimClicked()
    {
        if (!IsUnlocked() || !CanClaim()) return;
        AudioManager.Instance?.PlayButtonClick();

        ui.CoinsAmount  += coinReward;
        ui.CementAmount += cementReward;
        ui.SetCoins();
        ui.SetCement();
        ui.UpdateUI(ui.CoinText,   ui.CoinsAmount);
        ui.UpdateUI(ui.CementText, ui.CementAmount);

        PlayerPrefs.SetString(LastClaimKey, DateTime.UtcNow.ToString("o"));
        PlayerPrefs.Save();

        if (claimButton != null) claimButton.interactable = false;
        StartCoroutine(PlayRewardAnimation());
    }

    // ─── UI state ─────────────────────────────────────────────────────────────
void RefreshUI()
{
    bool unlocked = IsUnlocked();
    bool ready    = CanClaim();

    // Unlock button only before unlocking
    SetActive(unlockButton, !unlocked);

    // Claim button ALWAYS visible after unlock
    SetActive(claimButton, unlocked);

    // Control interactability instead of visibility
    if (claimButton != null)
        claimButton.interactable = unlocked && ready;

    // Timer visible only when waiting
    SetActive(timerText, unlocked && !ready);
}

    void UpdateCountdown()
    {
        TimeSpan rem = NextClaimTime() - DateTime.UtcNow;
        if (rem.TotalSeconds <= 0) { RefreshUI(); return; }
        if (timerText != null)
            timerText.text = $"{(int)rem.TotalHours:00}:{rem.Minutes:00}:{rem.Seconds:00}";
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    bool IsUnlocked() => PlayerPrefs.GetInt(UnlockedKey, 0) == 1;

    bool CanClaim()
    {
        string raw = PlayerPrefs.GetString(LastClaimKey, "");
        if (string.IsNullOrEmpty(raw)) return true;
        if (!DateTime.TryParse(raw, null, DateTimeStyles.RoundtripKind, out DateTime last)) return true;
        return (DateTime.UtcNow - last).TotalHours >= CooldownHours;
    }

    DateTime NextClaimTime()
    {
        string raw = PlayerPrefs.GetString(LastClaimKey, "");
        if (!DateTime.TryParse(raw, null, DateTimeStyles.RoundtripKind, out DateTime last)) return DateTime.UtcNow;
        return last.AddHours(CooldownHours);
    }

    void SetActive(Behaviour b, bool v)  { if (b != null) b.gameObject.SetActive(v); }
    void SetActive(GameObject g, bool v) { if (g != null) g.SetActive(v); }

    // ─── Animation ────────────────────────────────────────────────────────────

    IEnumerator PlayRewardAnimation()
    {
        if (chestTransform == null) yield break;

        // Coins — 10 particles, staggered 40 ms apart
        for (int i = 0; i < 10; i++)
            StartCoroutine(SpawnParticle(coinSprite, new Color(1f, 0.85f, 0.1f), i * 0.04f));

        // Cement — 6 particles, staggered 60 ms apart
        for (int i = 0; i < 6; i++)
            StartCoroutine(SpawnParticle(cementSprite, new Color(0.78f, 0.78f, 0.78f), 0.1f + i * 0.06f));

        float textDuration = 2.5f;
        if (coinRewardText != null)
        {
            coinRewardText.text = $"+{coinReward} Gems";
            StartCoroutine(FadeText(coinRewardText, textDuration));
        }
        if (cementRewardText != null)
        {
            cementRewardText.text = $"+{cementReward} Shield";
            StartCoroutine(FadeText(cementRewardText, textDuration));
        }

        yield return new WaitForSeconds(textDuration + 0.2f);
        RefreshUI();
    }

    IEnumerator SpawnParticle(Sprite sprite, Color color, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        // Parent to chest so it renders on top of it and stays in the right canvas layer
        GameObject go = new GameObject("BonusParticle");
        go.transform.SetParent(chestTransform, false);
        go.transform.SetAsLastSibling();

        Image img         = go.AddComponent<Image>();
        img.sprite        = sprite;
        img.color         = color;
        img.raycastTarget = false;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta        = new Vector2(64f, 64f);
        rt.anchoredPosition = Vector2.zero; // start at chest center

        // Random upward burst in local space
        float   angle    = UnityEngine.Random.Range(-65f, 65f) * Mathf.Deg2Rad;
        float   speed    = UnityEngine.Random.Range(140f, 300f);
        Vector2 vel      = new Vector2(Mathf.Sin(angle) * speed, Mathf.Abs(Mathf.Cos(angle)) * speed);
        float   gravity  = 280f;
        float   lifetime = UnityEngine.Random.Range(1.8f, 2.8f);
        float   t        = 0f;

        while (t < lifetime)
        {
            t     += Time.deltaTime;
            vel.y -= gravity * Time.deltaTime;
            rt.anchoredPosition += vel * Time.deltaTime;

            float p = t / lifetime;
            if (p > 0.65f)
                img.color = new Color(color.r, color.g, color.b, 1f - (p - 0.65f) / 0.35f);

            yield return null;
        }

        Destroy(go);
    }

    IEnumerator FadeText(Text txt, float totalDuration)
    {
        txt.gameObject.SetActive(true);

        CanvasGroup cg = txt.GetComponent<CanvasGroup>();
        if (cg == null) cg = txt.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        for (float t = 0f; t < 1f; t += Time.deltaTime * 4f)
        {
            cg.alpha = Mathf.Clamp01(t);
            yield return null;
        }
        cg.alpha = 1f;

        yield return new WaitForSeconds(totalDuration - 0.5f);

        for (float t = 0f; t < 1f; t += Time.deltaTime * 3f)
        {
            cg.alpha = Mathf.Lerp(1f, 0f, Mathf.Clamp01(t));
            yield return null;
        }

        txt.gameObject.SetActive(false);
        cg.alpha = 1f;
    }
}
