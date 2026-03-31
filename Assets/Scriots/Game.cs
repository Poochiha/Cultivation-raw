using System;
using TMPro;
using UnityEngine;

public class Game : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI qiText;
    public GameObject breakthroughButton;          
    public TextMeshProUGUI balanceWindowText;      

    [Header("Qi")]
    public double qi;

    [Header("Base income at Breakthrough 0")]
    public double baseQiPerSecond0 = 1.0;
    public double baseQiPerClick0 = 1.0;

    [Header("Progression")]
    public int breakthroughCount = 0;

    [Tooltip("Growth per breakthrough for Qi/s and Qi/click")]
    public double baseGrowthPerBreakthrough = 1.35;

    [Tooltip("Costs growth per breakthrough")]
    public double costGrowthPerBreakthrough = 1.45;

    [Tooltip("Per-level effect grows per breakthrough")]
    public double meridianEffectGrowthPerBreakthrough = 1.10;

    [Header("Meridians")]
    public int meridianCount = 8;

    [Tooltip("Level cap at breakthrough")]
    public int levelCapBase = 10;

    [Tooltip("Cap increase per breakthrough")]
    public int levelCapPerBreakthrough = 2;

    [Tooltip("Cost to open meridian #1 at breakthrough 0")]
    public double openCostBase = 50;

    [Tooltip("Open cost grows per meridian index")]
    public double openCostGrowthPerIndex = 2.2;

    [Tooltip("Cost of first upgrade level for meridian #1 at breakthrough 0")]
    public double upgradeCostBase = 25;

    [Tooltip("Upgrade cost grows per level")]
    public double upgradeCostGrowthPerLevel = 1.18;

    [Tooltip("Base multiplier per level")]
    public double levelMultiplier = 0.06;

    [NonSerialized] public bool[] opened;
    [NonSerialized] public int[] levels;

    [Header("Paths")]
    public Path path = Path.None;
    public bool pathChosen = false;

    
    public long clickCount = 0;

    [Tooltip("If clickCount > this by breakthrough 5 => Blood")]
    public long bloodClicksThreshold = 100;

    [Header("Balance path window schedule")]
    public float balanceWindowDuration = 5f;
    public float balanceCooldown = 8f;

    [Header("Balance rhythm")]
    public float balanceBpm = 60f;
    public float balanceTimingWindowSec = 0.12f;   

    [Header("Balance multipliers")]
    public double balanceMultAt5 = 1.5;            
    public double balanceMultAt10 = 3.0;           

    [Header("Balance penalty on miss")]
    public float balancePenaltyDuration = 3f;
    public double balancePenaltyMultiplier = 0.05;

    [Header("Reset")]
    public bool confirmResetInConsole = false;

    
    private float balanceWindowTimer = 0f;
    private float balanceCooldownTimer = 0f;
    private float balancePenaltyTimer = 0f;

   
    private float balanceBeatT0 = 0f;

    private const string SaveKey = "SAVE_V5_BALANCE_RHYTHM";

    [Serializable]
    private class SaveData
    {
        public double qi;
        public int breakthroughCount;
        public bool[] opened;
        public int[] levels;

        public Path path;
        public bool pathChosen;
        public long clickCount;

        public float balanceWindowTimer;
        public float balanceCooldownTimer;
        public float balancePenaltyTimer;
        public float balanceBeatT0;
    }

    private void Awake()
    {
        opened = new bool[meridianCount];
        levels = new int[meridianCount];

        Load();

       
        if (balanceBeatT0 <= 0f)
            balanceBeatT0 = Time.unscaledTime;

        RefreshUI();
        UpdateBreakthroughButton();
        UpdateBalanceText();
    }

    private void Update()
    {
        qi += GetQiPerSecond() * Time.deltaTime;

     
        if (pathChosen && path == Path.Balance)
        {
            if (balancePenaltyTimer > 0f)
            {
                balancePenaltyTimer -= Time.deltaTime;
                if (balancePenaltyTimer < 0f) balancePenaltyTimer = 0f;
            }

            if (balanceWindowTimer > 0f)
            {
                balanceWindowTimer -= Time.deltaTime;
                if (balanceWindowTimer < 0f) balanceWindowTimer = 0f;
            }
            else
            {
                balanceCooldownTimer -= Time.deltaTime;
                if (balanceCooldownTimer <= 0f)
                {
                    balanceWindowTimer = balanceWindowDuration;
                    balanceCooldownTimer = balanceCooldown;

                   
                    balanceBeatT0 = Time.unscaledTime;
                }
            }
        }

        RefreshUI();
        UpdateBreakthroughButton();
        UpdateBalanceText();
    }

 
    public void MeditateClick()
    {
      
        if (!pathChosen)
            clickCount++;

     
        if (pathChosen && path == Path.Balance && balanceWindowTimer > 0f)
        {
            bool hit = IsBalanceHit();
            if (hit)
            {
              
                balanceWindowTimer += balanceCooldown;

               
                balanceWindowTimer = Mathf.Min(balanceWindowTimer, 30f);
            }
            else
            {
              
                balancePenaltyTimer = Mathf.Max(balancePenaltyTimer, balancePenaltyDuration);
                balanceWindowTimer = 0f;
                balanceCooldownTimer = balanceCooldown;
            }
        }

        qi += GetQiPerClick();

        Save();
        RefreshUI();
        UpdateBreakthroughButton();
        UpdateBalanceText();
    }

   
    private bool IsBalanceHit()
    {
        float bpm = Mathf.Max(1f, balanceBpm);
        float beatInterval = 60f / bpm;

        float t = Time.unscaledTime - balanceBeatT0;
        float phase = t % beatInterval;                     
        float distToBeat = Mathf.Min(phase, beatInterval - phase);

        float window = Mathf.Max(0.01f, balanceTimingWindowSec);
        return distToBeat <= window;
    }

   
    public double GetQiPerSecond()
    {
        double v = GetBaseQiPerSecond() * GetTotalMeridianMultiplier();

        if (pathChosen)
        {
            if (path == Path.Unity)
                v *= GetUnityPassiveMult();
            else if (path == Path.Balance)
                v *= GetBalanceTotalMult(); 
        }

        return v;
    }

    public double GetQiPerClick()
    {
    
        if (pathChosen && path == Path.Unity)
            return 0;

        double v = GetBaseQiPerClick() * GetTotalMeridianMultiplier();

        if (pathChosen)
        {
            if (path == Path.Blood)
                v *= GetBloodClickMult();
            else if (path == Path.Balance)
                v *= GetBalanceTotalMult();
        }

        return v;
    }

    private double GetBaseQiPerSecond()
    {
        return baseQiPerSecond0 * Math.Pow(baseGrowthPerBreakthrough, breakthroughCount);
    }

    private double GetBaseQiPerClick()
    {
        return baseQiPerClick0 * Math.Pow(baseGrowthPerBreakthrough, breakthroughCount);
    }

    private double GetUnityPassiveMult()
    {
        if (breakthroughCount >= 10) return 5.0;
        return 2.0;
    }

    private double GetBloodClickMult()
    {
        if (breakthroughCount >= 10) return 4.0;
        return 2.0;
    }

    private double GetBalanceTotalMult()
    {
      
        if (balancePenaltyTimer > 0f)
            return balancePenaltyMultiplier;

      
        if (balanceWindowTimer <= 0f)
            return 1.0;

       
        if (breakthroughCount >= 10) return balanceMultAt10;
        return balanceMultAt5;
    }

   
    public int GetCurrentLevelCap()
    {
        return levelCapBase + levelCapPerBreakthrough * breakthroughCount;
    }

    public double GetTotalMeridianMultiplier()
    {
        double perLevel = 1.0 + (levelMultiplier * Math.Pow(meridianEffectGrowthPerBreakthrough, breakthroughCount));
        double mult = 1.0;

        for (int i = 0; i < meridianCount; i++)
        {
            if (!opened[i]) continue;
            int L = levels[i];
            if (L > 0)
                mult *= Math.Pow(perLevel, L);
        }

        return mult;
    }

    public double GetMeridianMultiplier(int index)
    {
        int L = levels[index];
        double perLevel = 1.0 + (levelMultiplier * Math.Pow(meridianEffectGrowthPerBreakthrough, breakthroughCount));
        return Math.Pow(perLevel, L);
    }

    public bool TryOpenMeridian(int index)
    {
        if (index < 0 || index >= meridianCount) return false;
        if (opened[index]) return false;

        double cost = GetOpenCost(index);
        if (qi < cost) return false;

        qi -= cost;
        opened[index] = true;

        Save();
        return true;
    }

    public bool TryUpgradeMeridian(int index)
    {
        if (index < 0 || index >= meridianCount) return false;
        if (!opened[index]) return false;

        int cap = GetCurrentLevelCap();
        if (levels[index] >= cap) return false;

        double cost = GetUpgradeCost(index);
        if (qi < cost) return false;

        qi -= cost;
        levels[index]++;

        Save();
        return true;
    }

    public double GetOpenCost(int index)
    {
        return openCostBase
               * Math.Pow(openCostGrowthPerIndex, index)
               * Math.Pow(costGrowthPerBreakthrough, breakthroughCount);
    }

    public double GetUpgradeCost(int index)
    {
        int L = levels[index];
        return upgradeCostBase
               * Math.Pow(openCostGrowthPerIndex, index)
               * Math.Pow(upgradeCostGrowthPerLevel, L)
               * Math.Pow(costGrowthPerBreakthrough, breakthroughCount);
    }

    
    public bool CanBreakthrough()
    {
        int cap = GetCurrentLevelCap();
        for (int i = 0; i < meridianCount; i++)
        {
            if (!opened[i]) return false;
            if (levels[i] < cap) return false;
        }
        return true;
    }

    public void Breakthrough()
    {
        if (!CanBreakthrough()) return;

        breakthroughCount++;

        
        if (!pathChosen && breakthroughCount >= 5)
        {
            ChoosePathAtBreakthrough5();
        }

       
        for (int i = 0; i < meridianCount; i++)
        {
            opened[i] = false;
            levels[i] = 0;
        }

        qi = 0;

        Save();
        RefreshUI();
        UpdateBreakthroughButton();
        UpdateBalanceText();
    }

    private void ChoosePathAtBreakthrough5()
    {
        if (clickCount == 0)
            path = Path.Unity;
        else if (clickCount > bloodClicksThreshold)
            path = Path.Blood;
        else
            path = Path.Balance;

        pathChosen = true;

       
        if (path == Path.Balance)
        {
            balanceWindowTimer = 0f;
            balanceCooldownTimer = 1f;
            balancePenaltyTimer = 0f;
            balanceBeatT0 = Time.unscaledTime;
        }
    }

    private void UpdateBreakthroughButton()
    {
        if (breakthroughButton != null)
            breakthroughButton.SetActive(CanBreakthrough());
    }

    private void UpdateBalanceText()
    {
        if (balanceWindowText == null) return;

        if (pathChosen && path == Path.Balance)
        {
            if (balancePenaltyTimer > 0f)
            {
                balanceWindowText.gameObject.SetActive(true);
                balanceWindowText.text = "MISSED";
                return;
            }

            if (balanceWindowTimer > 0f)
            {
                balanceWindowText.gameObject.SetActive(true);
                balanceWindowText.text = "BALANCE WINDOW";
                return;
            }
        }

        balanceWindowText.text = "";
        balanceWindowText.gameObject.SetActive(false);
    }

   
    public void ResetAll()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();

        qi = 0;
        breakthroughCount = 0;

        for (int i = 0; i < meridianCount; i++)
        {
            opened[i] = false;
            levels[i] = 0;
        }

        path = Path.None;
        pathChosen = false;
        clickCount = 0;

        balanceWindowTimer = 0f;
        balanceCooldownTimer = 0f;
        balancePenaltyTimer = 0f;
        balanceBeatT0 = Time.unscaledTime;

        if (confirmResetInConsole)
            Debug.Log("ResetAll() done.");

        RefreshUI();
        UpdateBreakthroughButton();
        UpdateBalanceText();
    }

  
    private void RefreshUI()
    {
        if (qiText != null)
            qiText.text = $"Qi: {Format(qi)}";
    }

    public string Format(double v)
    {
        if (v < 1000) return v.ToString("0.##");
        if (v < 1_000_000) return (v / 1000d).ToString("0.##") + "K";
        if (v < 1_000_000_000) return (v / 1_000_000d).ToString("0.##") + "M";
        if (v < 1_000_000_000_000) return (v / 1_000_000_000d).ToString("0.##") + "B";
        return (v / 1_000_000_000_000d).ToString("0.##") + "T";
    }

  
    public void Save()
    {
        var s = new SaveData
        {
            qi = qi,
            breakthroughCount = breakthroughCount,
            opened = opened,
            levels = levels,

            path = path,
            pathChosen = pathChosen,
            clickCount = clickCount,

            balanceWindowTimer = balanceWindowTimer,
            balanceCooldownTimer = balanceCooldownTimer,
            balancePenaltyTimer = balancePenaltyTimer,
            balanceBeatT0 = balanceBeatT0
        };

        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(s));
        PlayerPrefs.Save();
    }

    public void Load()
    {
        if (!PlayerPrefs.HasKey(SaveKey)) return;

        try
        {
            var json = PlayerPrefs.GetString(SaveKey);
            var s = JsonUtility.FromJson<SaveData>(json);
            if (s == null) return;

            qi = s.qi;
            breakthroughCount = s.breakthroughCount;

            if (s.opened != null && s.opened.Length == meridianCount)
                opened = s.opened;

            if (s.levels != null && s.levels.Length == meridianCount)
                levels = s.levels;

            path = s.path;
            pathChosen = s.pathChosen;
            clickCount = s.clickCount;

            balanceWindowTimer = s.balanceWindowTimer;
            balanceCooldownTimer = s.balanceCooldownTimer;
            balancePenaltyTimer = s.balancePenaltyTimer;
            balanceBeatT0 = s.balanceBeatT0;
        }
        catch
        {
           
        }
    }
}
