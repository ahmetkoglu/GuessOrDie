using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // En üste bu kütüphaneyi eklemelisin

public class MapManager : MonoBehaviour
{
    [System.Serializable]
    public class DistrictUI
    {
        public string districtId;      
        public Button button;
        public GameObject lockIcon;
        public GameObject pinIcon;
    }

    public DistrictUI[] districtButtons;
    public string selectedDistrictId = ""; // Hangi ilçenin seçili olduğunu aklında tutacak

    private void OnEnable()
    {
        RefreshMap(); 
    }

    public void RefreshMap()
    {
        foreach (var item in districtButtons)
        {
            DistrictData data = DataManager.Instance.LoadedGameData.districts.Find(d => d.id == item.districtId);

            if (data != null)
            {
                if (data.is_unlocked)
                {
                    // 1. Bölge AÇIKSA: Kilidi kesinlikle gizle
                    if(item.lockIcon) item.lockIcon.SetActive(false);
                    
                    // 2. Eğer bu ilçe SEÇİLİYSE pini göster, değilse pini de gizle
                    if (item.districtId == selectedDistrictId)
                    {
                        if(item.pinIcon) item.pinIcon.SetActive(true);
                    }
                    else
                    {
                        if(item.pinIcon) item.pinIcon.SetActive(false);
                    }
                }
                else
                {
                    // Bölge KİLİTLİYSE: Sadece kilidi göster
                    if(item.lockIcon) item.lockIcon.SetActive(true);
                    if(item.pinIcon) item.pinIcon.SetActive(false);
                }
            }
        }
    }
    public void ResetAllGameData()
{
    Debug.Log("🔴 Hard Reset Başladı: Disk ve RAM tamamen yok ediliyor..."); 

    // 1. DİSKİ SİL (Editördeki tuşun aynısı)
    PlayerPrefs.DeleteAll();
    PlayerPrefs.Save();

    // 2. RAM'İ SİL (Ölümsüz DataManager'ı acımasızca yok et!)
    if (DataManager.Instance != null)
    {
        Destroy(DataManager.Instance.gameObject);
        DataManager.Instance = null; // Bağlantıyı tamamen kopar
    }

    // 3. Zamanı düzelt ve sahneyi baştan yükle
    Time.timeScale = 1f;
    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
}
}