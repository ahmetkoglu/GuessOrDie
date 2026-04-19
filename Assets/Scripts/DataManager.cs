using UnityEngine;
using System.Collections.Generic;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance;
    public GameDataContainer LoadedGameData;

    [Header("Oyuncu İlerlemesi")]
    public int totalCoins;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            LoadJsonData();
            LoadProgress(); // Kayıtlı parayı ve kilitleri yükle
        }
        else { Destroy(gameObject); }
    }

    private void LoadJsonData()
    {
        TextAsset jsonAsset = Resources.Load<TextAsset>("quiz_questions");
        if (jsonAsset != null)
        {
            LoadedGameData = JsonUtility.FromJson<GameDataContainer>(jsonAsset.text);
        }
    }

    // --- YENİ: KAYIT SİSTEMİ ---
    public void SaveProgress()
    {
        PlayerPrefs.SetInt("TotalCoins", totalCoins);
        
        // Hangi ilçelerin kilidi açık? Bunları tek tek kaydet
        foreach (var district in LoadedGameData.districts)
        {
            // district_fatih_unlocked = 1 (açık) veya 0 (kapalı)
            int unlockedState = district.is_unlocked ? 1 : 0;
            PlayerPrefs.SetInt("district_" + district.id + "_unlocked", unlockedState);
        }
        PlayerPrefs.Save();
        Debug.Log("💾 İlerleme kaydedildi!");
    }

    public void LoadProgress()
    {
        totalCoins = PlayerPrefs.GetInt("TotalCoins", 0); // Varsayılan 0

        foreach (var district in LoadedGameData.districts)
        {
            // Kayıtlı bir durum varsa onu yükle, yoksa JSON'daki varsayılanı kullan
            if (PlayerPrefs.HasKey("district_" + district.id + "_unlocked"))
            {
                int state = PlayerPrefs.GetInt("district_" + district.id + "_unlocked");
                district.is_unlocked = (state == 1);
            }
        }
        Debug.Log("📂 İlerleme yüklendi!");
    }

    public void AddCoins(int amount)
    {
        totalCoins += amount;
        SaveProgress(); // Her para kazandığında kaydet
    }
    
}