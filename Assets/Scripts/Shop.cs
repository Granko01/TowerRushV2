using UnityEngine;
using UnityEngine.UI;

public class Shop : MonoBehaviour
{

    public UIManager uIManager;
    public int CementInt = 25;
    public Text CementCost;
    private const string CementKey = "CementCost";
    void Start()
    {
        uIManager = FindAnyObjectByType<UIManager>();
        GetCost();
        CementCost.text = CementInt.ToString();
    }

    void Update()
    {
        
    }

    public void ShopMethod(string tag)
    {
        if (tag == "Cement" && uIManager.CoinsAmount > 25)
        {
            uIManager.CoinsAmount -= CementInt;
            uIManager.CementAmount++;
            uIManager.SetCement();
            uIManager.SetCoins();
            CementInt += 5;
            SetCost();
            CementCost.text = CementInt.ToString();
            uIManager.UpdateUI(uIManager.CoinText, uIManager.CoinsAmount);
            uIManager.UpdateUI(uIManager.CementText, uIManager.CementAmount);
        }
    }
    public void GetCost()
    {
        CementInt = PlayerPrefs.GetInt(CementKey, CementInt);
    }
    public void SetCost()
    {
        PlayerPrefs.SetInt(CementKey, CementInt);
    }

}
