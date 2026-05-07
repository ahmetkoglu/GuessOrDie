using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; 
using System;

/// <summary> Manages Map UI pins, lock icons, and hard resetting game data. </summary>
public class MapManager : MonoBehaviour
{
    [Serializable]
    public class DistrictUI
    {
        public string districtId;      
        public Button button;
        public GameObject lockIcon;
        public GameObject pinIcon;
    }

    public DistrictUI[] districtButtons;
    public string selectedDistrictId = ""; 

    private void OnEnable()
    {
        RefreshMap(); 
    }

    /// <summary> Updates visually which district is selected and unlocked based on Singleton Data. </summary>
    public void RefreshMap()
    {
        foreach (var item in districtButtons)
        {
            DistrictData data = DataManager.Instance.LoadedGameData.districts.Find(d => d.id == item.districtId);

            if (data != null)
            {
                if (data.is_unlocked)
                {
                    if(item.lockIcon) item.lockIcon.SetActive(false);
                    
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
                    if(item.lockIcon) item.lockIcon.SetActive(true);
                    if(item.pinIcon) item.pinIcon.SetActive(false);
                }
            }
        }
    }

    /// <summary> Hard destroys all player progress and reloads scene. </summary>
    public void ResetAllGameData()
    {
        Debug.Log("🔴 Hard Reset Başladı: Disk ve RAM tamamen yok ediliyor..."); 

        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        if (DataManager.Instance != null)
        {
            Destroy(DataManager.Instance.gameObject);
            // We cannot set Instance = null here because of private set, but Destroying the GO works.
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}