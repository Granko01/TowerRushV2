using UnityEngine;
using UnityEngine.UI;

public class Shop : MonoBehaviour
{

    public UIManager uIManager;
    public int CementInt = 25;
    public Text CementCost;
    public Button[] Speedprogres;
    public int SpeedInt = 1;

    public Button[] SpawnProgress;
    public int SpawnInt = 1;

     public Button[] CoinSpawnProgress;
    public int CointSpawnInt = 1;


    private const string CementKey = "CementCost";
    private const string Speedkey = "Speedkey";
    private const string Spawnkey = "Spawnkey";
    private const string CoinSpawnKey = "CoinSpawnKey";
    void Start()
    {
        uIManager = FindAnyObjectByType<UIManager>();
        GetSpeed();
        GetCost();
        GetCoinSpawn();
        GetSpawn();
        CementCost.text = CementInt.ToString();
        SetSpeedProgress();
        SetCoinSpawnProgress();
        SetSpawnProgress();
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
        if (tag == "Speed" & uIManager.CoinsAmount > 25)
        {
            uIManager.CoinsAmount -= 25;
            uIManager.SetCoins();
            uIManager.UpdateUI(uIManager.CoinText, uIManager.CoinsAmount);
            SpeedInt++;
            SetSpeed();
            SetSpeedProgress();
        }
        if (tag == "Spawn" & uIManager.CoinsAmount > 25)
        {
            uIManager.CoinsAmount -= 25;
            uIManager.SetCoins();
            uIManager.UpdateUI(uIManager.CoinText, uIManager.CoinsAmount);
            SpawnInt++;
            SetSpawn();
            SetSpawnProgress();
        }
         if (tag == "CoinRate" & uIManager.CoinsAmount > 25)
        {
            uIManager.CoinsAmount -= 25;
            uIManager.SetCoins();
            uIManager.UpdateUI(uIManager.CoinText, uIManager.CoinsAmount);
            CointSpawnInt++;
            SetCoinSpawn();
            SetCoinSpawnProgress();
        }
    }
    public void SetSpeedProgress()
    {
        for (int i = 0; i < SpeedInt; i++)
        {
            Speedprogres[i].interactable = true;
        }
    }
    public void SetSpawnProgress()
    {
        for (int i = 0; i < SpawnInt; i++)
        {
            SpawnProgress[i].interactable = true;
        }
    }
    public void SetCoinSpawnProgress()
    {
        for (int i = 0; i < CointSpawnInt; i++)
        {
            CoinSpawnProgress[i].interactable = true;
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
    public void GetSpeed()
    {
        SpeedInt = PlayerPrefs.GetInt(Speedkey, SpeedInt);
    }
    public void SetSpeed()
    {
        PlayerPrefs.SetInt(Speedkey, SpeedInt);
    }

     public void GetSpawn()
    {
        SpawnInt = PlayerPrefs.GetInt(Spawnkey, SpawnInt);
    }
    public void SetSpawn()
    {
        PlayerPrefs.SetInt(Spawnkey, SpawnInt);
    }

     public void GetCoinSpawn()
    {
        CointSpawnInt = PlayerPrefs.GetInt(CoinSpawnKey, CointSpawnInt);
    }
    public void SetCoinSpawn()
    {
        PlayerPrefs.SetInt(CoinSpawnKey, CointSpawnInt);
    }

}
