using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AvatarManager : MonoBehaviour
{
    [Header("Paneller")]
    public GameObject avatarPanel;       // Avatar seçme ekranı
    public GameObject mainMenuPanel;     // YENİ: Ana Menü ekranı

    [Header("UI Referansları")]
    public Image mainAvatarDisplay;      // Avatar panelindeki BÜYÜK resim
    public Image mainMenuAvatarIcon;     // YENİ: Ana Menüdeki KÜÇÜK resim

    [Header("Veriler ve Butonlar")]
    public Sprite[] availableAvatars;
    public Button[] avatarButtons;
    public GameObject[] checkmarks;

    private int currentSelectedIndex = 0;

    private void Start()
    {
        // Oyuna ilk girişte Ana Menüdeki avatarı otomatik olarak güncelle
        currentSelectedIndex = PlayerPrefs.GetInt("SelectedAvatar", 0);
        UpdateMainMenuAvatar();

        for (int i = 0; i < avatarButtons.Length; i++)
        {
            int index = i; 
            avatarButtons[i].onClick.AddListener(() => OnAvatarClicked(index));
            
            if (i < availableAvatars.Length)
            {
                avatarButtons[i].image.sprite = availableAvatars[i];
            }
        }
    }

    private void OnEnable()
    {
        // Panel açıldığında kaydedilmiş olanı seçili göster
        currentSelectedIndex = PlayerPrefs.GetInt("SelectedAvatar", 0);
        UpdateUI();
    }

    public void OnAvatarClicked(int index)
    {
        if (index < availableAvatars.Length)
        {
            currentSelectedIndex = index;
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        if (availableAvatars.Length > 0)
        {
            mainAvatarDisplay.sprite = availableAvatars[currentSelectedIndex];
        }

        for (int i = 0; i < checkmarks.Length; i++)
        {
            if (checkmarks[i] != null)
            {
                checkmarks[i].SetActive(i == currentSelectedIndex);
            }
        }
    }

    // YENİ: Sadece Ana Menüdeki resmi güncelleyen fonksiyon
    private void UpdateMainMenuAvatar()
    {
        if (availableAvatars.Length > 0 && mainMenuAvatarIcon != null)
        {
            mainMenuAvatarIcon.sprite = availableAvatars[currentSelectedIndex];
        }
    }

    // YENİ: Ana menüden Avatar ekranına geçiş fonksiyonu
    public void OpenAvatarPanel()
    {
        mainMenuPanel.SetActive(false); // Ana menüyü kapat
        avatarPanel.SetActive(true);    // Avatar ekranını aç
    }

    // GÜNCELLENDİ: Kaydet ve Dön
    public void SaveAndClose()
    {
        PlayerPrefs.SetInt("SelectedAvatar", currentSelectedIndex);
        PlayerPrefs.Save();
        
        UpdateMainMenuAvatar(); // Kaydedince Ana Menüdeki resmi de değiştir
        
        avatarPanel.SetActive(false);
        mainMenuPanel.SetActive(true);  // Ana menüyü geri aç
    }

    // GÜNCELLENDİ: Kaydetmeden Çık ve Dön
    public void CloseWithoutSaving()
    {
        currentSelectedIndex = PlayerPrefs.GetInt("SelectedAvatar", 0);
        avatarPanel.SetActive(false);
        mainMenuPanel.SetActive(true);  // Ana menüyü geri aç
    }
}