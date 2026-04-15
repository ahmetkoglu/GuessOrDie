using UnityEngine;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance;

    [Header("Oyun Verileri")]
    public GameDataContainer LoadedGameData;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadJsonData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadJsonData()
    {
        // Resources klasöründeki quiz_questions.json dosyasını bul ve oku
        TextAsset jsonAsset = Resources.Load<TextAsset>("quiz_questions");

        if (jsonAsset != null)
        {
            LoadedGameData = JsonUtility.FromJson<GameDataContainer>(jsonAsset.text);
            Debug.Log("✅ JSON Başarıyla Yüklendi! Toplam Bölge (District) Sayısı: " + LoadedGameData.districts.Count);
        }
        else
        {
            Debug.LogError("❌ quiz_questions.json dosyası Resources klasöründe bulunamadı!");
        }
    }
}