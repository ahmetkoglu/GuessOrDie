using UnityEngine;
using UnityEngine.UI;

/// <summary> Handles changing player avatars and syncing across scenes. </summary>
public class AvatarManager : MonoBehaviour
{
    [Header("Paneller")]
    public GameObject avatarPanel;       
    public GameObject mainMenuPanel;     

    [Header("UI Referansları")]
    public Image mainAvatarDisplay;      
    public Image mainMenuAvatarIcon;     

    [Header("Veriler ve Butonlar")]
    public Sprite[] availableAvatars;
    public Button[] avatarButtons;
    public GameObject[] checkmarks;

    private int currentSelectedIndex = 0;

    /// <summary> Prepares listeners for the avatars on start. </summary>
    private void Start()
    {
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

    /// <summary> Refreshes visual state when the panel enables. </summary>
    private void OnEnable()
    {
        currentSelectedIndex = PlayerPrefs.GetInt("SelectedAvatar", 0);
        UpdateUI();
    }

    /// <summary> Handles user click selection on an avatar. </summary>
    public void OnAvatarClicked(int index)
    {
        if (index < availableAvatars.Length)
        {
            currentSelectedIndex = index;
            UpdateUI();
        }
    }

    /// <summary> Refreshes the large view and checkmark positions. </summary>
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

    /// <summary> Syncs the main menu icon visual silently. </summary>
    private void UpdateMainMenuAvatar()
    {
        if (availableAvatars.Length > 0 && mainMenuAvatarIcon != null)
        {
            mainMenuAvatarIcon.sprite = availableAvatars[currentSelectedIndex];
        }
    }

    /// <summary> Transitions to avatar screen. </summary>
    public void OpenAvatarPanel()
    {
        mainMenuPanel.SetActive(false); 
        avatarPanel.SetActive(true);    
    }

    /// <summary> Saves preference via PlayerPrefs and applies across app. </summary>
    public void SaveAndClose()
    {
        PlayerPrefs.SetInt("SelectedAvatar", currentSelectedIndex);
        PlayerPrefs.Save();
        
        UpdateMainMenuAvatar(); 
        
        avatarPanel.SetActive(false);
        mainMenuPanel.SetActive(true);  
    }

    /// <summary> Reverts to last saved profile and cancels changes. </summary>
    public void CloseWithoutSaving()
    {
        currentSelectedIndex = PlayerPrefs.GetInt("SelectedAvatar", 0);
        avatarPanel.SetActive(false);
        mainMenuPanel.SetActive(true);  
    }
}