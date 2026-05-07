using UnityEngine;
using System;

/// <summary> Manages global game data, saving/loading, and economy using Singleton pattern. </summary>
public class DataManager : MonoBehaviour
{
    // ENCAPSULATION: Property instead of public field
    public static DataManager Instance { get; private set; }
    public GameDataContainer LoadedGameData { get; private set; }

    // EVENTS: Broadcasts coin changes so UI can listen without tight coupling[cite: 7]
    public static event Action<int> OnCoinsChanged;

    // ENCAPSULATION: Read-only outside, write-only inside
    [field: SerializeField] public int TotalCoins { get; private set; }

    /// <summary> Initializes the Singleton and applies target frame rates. </summary>
    private void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            LoadJsonData();
            LoadProgress(); 
        }
        else 
        { 
            Destroy(gameObject); 
        }
    }
    
    /// <summary> Loads question data from the JSON resource file. </summary>
    private void LoadJsonData()
    {
        TextAsset jsonAsset = Resources.Load<TextAsset>("quiz_questions");
        if (jsonAsset != null)
        {
            LoadedGameData = JsonUtility.FromJson<GameDataContainer>(jsonAsset.text);
        }
    }

    /// <summary> Saves the current coin balance and district unlocks to PlayerPrefs. </summary>
    public void SaveProgress()
    {
        PlayerPrefs.SetInt("TotalCoins", TotalCoins);
        
        foreach (var district in LoadedGameData.districts)
        {
            int unlockedState = district.is_unlocked ? 1 : 0;
            PlayerPrefs.SetInt("district_" + district.id + "_unlocked", unlockedState);
        }
        PlayerPrefs.Save();
        Debug.Log("💾 İlerleme kaydedildi!");
    }

    /// <summary> Loads saved progress from PlayerPrefs. </summary>
    public void LoadProgress()
    {
        TotalCoins = PlayerPrefs.GetInt("TotalCoins", 0);

        foreach (var district in LoadedGameData.districts)
        {
            if (PlayerPrefs.HasKey("district_" + district.id + "_unlocked"))
            {
                int state = PlayerPrefs.GetInt("district_" + district.id + "_unlocked");
                district.is_unlocked = (state == 1);
            }
        }
        Debug.Log("📂 İlerleme yüklendi!");
    }

    /// <summary> Adds or subtracts coins and triggers the UI update event. </summary>
    public void AddCoins(int amount)
    {
        TotalCoins += amount;
        SaveProgress(); 
        OnCoinsChanged?.Invoke(TotalCoins); // Fire the event!
    }
}