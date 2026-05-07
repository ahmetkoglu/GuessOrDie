using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening; 
using Solo.MOST_IN_ONE;

/// <summary> Handles main gameplay loop, quiz mechanics, and user interface transitions. </summary>
public class UIManager : MonoBehaviour
{
    [Header("Oyun Sonu Paneli")]
    public GameObject resultPanel;
    public Sprite successSprite;   
    public Sprite failureSprite;   

    [Header("Ses Ayarları")]
    public AudioSource sfxSource; 
    public AudioClip correctSound; 
    public AudioClip wrongSound;   
    public AudioClip quizCompleteSound;
    
    private Vector2[] originalOptionPositions;

    [Header("Joker Sistemi")]
    public int jokerCost = 100; 
    public Button btnJoker50;
    public Button btnJokerTime;
    private bool isFiftyFiftyUsedThisQuestion = false; 

    [Header("Animasyonlu Butonlar")]
    public Transform mainPlayButtonTransform;

    [Header("Harita Yöneticisi")]
    public MapManager mapManager; 

    [Header("Pop-up Sistemi")]
    public GameObject unlockPopupPanel;
    public TextMeshProUGUI popupMessageText;
    private DistrictData pendingDistrictToUnlock; 
    public GameObject popupBox; 
    
    [Header("Coin UI")]
    public TextMeshProUGUI coinTextUI; 
    public int rewardPerQuestion = 50;  

    [Header("Paneller")]
    public GameObject mainMenuPanel;
    public GameObject questionPanel;
    public GameObject mapPanel; 

    [Header("Soru Ekranı UI Objeleri")]
    public TextMeshProUGUI questionTextUI;
    public TextMeshProUGUI[] optionTextsUI;
    public TextMeshProUGUI questionCountTextUI; 
    public Image[] optionButtonsImage;
    public Image questionImageUI;
    
    [Header("Can Sistemi")]
    public TextMeshProUGUI livesTextUI;
    public int maxLives = 3;
    private int currentLives;

    [Header("Süre Sistemi")]
    public Image timerFillImage;     
    public TextMeshProUGUI timerTextUI; 
    public float timePerQuestion = 15f; 
    private float currentTime;
    private bool isTimerRunning = false;

    [Header("Renk Ayarları")]
    public Color normalColor = Color.white;
    public Color correctColor = Color.green;
    public Color wrongColor = Color.red;

    private QuestionData currentQuestion;
    private DistrictData currentDistrict;
    private int currentQuestionIndex;
    private bool isAnswering = false;
    private int currentlyDisplayedCoins = -1;
    private GameState currentGameState = GameState.MainMenu;

    /// <summary> Subscribes to the global coin update event. </summary>
    private void OnEnable() => DataManager.OnCoinsChanged += HandleCoinsChanged;

    /// <summary> Unsubscribes to prevent memory leaks. </summary>
    private void OnDisable() => DataManager.OnCoinsChanged -= HandleCoinsChanged;

    /// <summary> Initializes the UI state and saves original button positions. </summary>
    private void Start()
    {
        if (DataManager.Instance != null)
        {
            HandleCoinsChanged(DataManager.Instance.TotalCoins);
        }

        originalOptionPositions = new Vector2[optionTextsUI.Length];
        for (int i = 0; i < optionTextsUI.Length; i++)
        {
            RectTransform btnRect = optionTextsUI[i].transform.parent.GetComponent<RectTransform>();
            originalOptionPositions[i] = btnRect.anchoredPosition;
        }
    }

    /// <summary> Event callback for coin changes. </summary>
    private void HandleCoinsChanged(int targetCoins)
    {
        UpdateCoinDisplay(targetCoins);
    }

    /// <summary> Animates the transition into the main gameplay state. </summary>
    public void OnPlayButtonClicked()
    {
        mainPlayButtonTransform.DOPunchScale(new Vector3(-0.5f, -0.5f, 0), 0.2f, 10, 1).OnComplete(() =>
        {
            string districtToLoad = "fatih"; 
            if (mapManager != null && !string.IsNullOrEmpty(mapManager.selectedDistrictId))
            {
                districtToLoad = mapManager.selectedDistrictId;
            }

            currentGameState = GameState.Playing;
            mainMenuPanel.SetActive(false);
            questionPanel.SetActive(true);
            
            currentLives = maxLives; 
            UpdateLivesUI();
            
            LoadDistrict(districtToLoad); 
        });
    }

    /// <summary> Loads district data and prepares the first question. </summary>
    public void LoadDistrict(string districtId)
    {
        if (DataManager.Instance != null && DataManager.Instance.LoadedGameData != null)
        {
            currentDistrict = DataManager.Instance.LoadedGameData.districts.Find(d => d.id == districtId);
            currentQuestionIndex = 0;
            LoadQuestion();
        }
    }

    /// <summary> Loads the specific question data and animates UI elements in. </summary>
    private void LoadQuestion()
    {
        isAnswering = false;

        if (currentDistrict != null && currentQuestionIndex < currentDistrict.questions.Count)
        {
            currentQuestion = currentDistrict.questions[currentQuestionIndex];
            
            questionTextUI.text = currentQuestion.question;

            if (questionCountTextUI != null)
            {
                questionCountTextUI.text = $"Soru: {currentQuestionIndex + 1} / {currentDistrict.questions.Count}";
            }

            if (!string.IsNullOrEmpty(currentQuestion.questionImage))
            {
                string cleanImageName = currentQuestion.questionImage.Replace(".png", "");
                Sprite loadedSprite = Resources.Load<Sprite>("Gorseller/" + cleanImageName);

                if (loadedSprite != null)
                {
                    questionImageUI.sprite = loadedSprite;
                    questionImageUI.gameObject.SetActive(true); 
                }
                else
                {
                    questionImageUI.gameObject.SetActive(false); 
                }
            }
            else
            {
                questionImageUI.gameObject.SetActive(false); 
            }

            questionTextUI.transform.DOKill(true);
            questionTextUI.color = new Color(questionTextUI.color.r, questionTextUI.color.g, questionTextUI.color.b, 0f);
            questionTextUI.transform.DOLocalMoveX(800f, 0.5f).From(true).SetEase(Ease.OutQuint);
            questionTextUI.DOFade(1f, 0.5f);

            for (int i = 0; i < optionTextsUI.Length; i++)
            {
                if (i < currentQuestion.options.Length)
                {
                    optionTextsUI[i].text = currentQuestion.options[i];
                    optionButtonsImage[i].color = normalColor; 
                }

                Transform btnTransform = optionTextsUI[i].transform.parent;
                RectTransform btnRect = btnTransform.GetComponent<RectTransform>();
                
                btnTransform.localScale = Vector3.one; 
                btnTransform.GetComponent<Button>().interactable = true;

                btnTransform.DOKill(true);
                btnRect.anchoredPosition = new Vector2(originalOptionPositions[i].x + 800f, originalOptionPositions[i].y);
                btnRect.DOAnchorPos(originalOptionPositions[i], 0.4f).SetDelay(i * 0.1f).SetEase(Ease.OutBack);
            }

            currentTime = timePerQuestion;
            timerTextUI.text = currentTime.ToString(); 
            isTimerRunning = true;
        }
        else
        {
            ShowResult(true);
            isTimerRunning = false; 
            StartCoroutine(WaitAndReturnToMenu(15f));
        }
    }

    /// <summary> Handles countdown logic per frame. </summary>
    private void Update()
    {
        if (isTimerRunning)
        {
            currentTime -= Time.deltaTime;
            timerFillImage.fillAmount = currentTime / timePerQuestion;
            timerTextUI.text = Mathf.CeilToInt(currentTime).ToString();

            if (currentTime <= 0)
            {
                isTimerRunning = false;
                currentTime = 0;
                timerFillImage.fillAmount = 0;
                timerTextUI.text = "0"; 
                OnTimeRanOut(); 
            }
        }
    }

    /// <summary> Processes user answer selection and triggers feedback. </summary>
    public void OnOptionSelected(int selectedIndex)
    {
        if (isAnswering) return; 
        
        isAnswering = true;
        isTimerRunning = false; 

        if (currentQuestion != null)
        {
            if (selectedIndex == currentQuestion.answer)
            {   
                if (sfxSource != null && correctSound != null) sfxSource.PlayOneShot(correctSound);
                MOST_HapticFeedback.Generate(MOST_HapticFeedback.HapticTypes.Success);

                Image btnImage = optionButtonsImage[selectedIndex];
                Transform btnTransform = btnImage.transform; 

                Sequence correctSeq = DOTween.Sequence();
                correctSeq.Append(btnImage.DOColor(correctColor, 0.2f));
                correctSeq.Join(btnTransform.DOScale(1.1f, 0.2f));
                correctSeq.Join(btnTransform.DOPunchPosition(Vector3.up * 10f, 0.4f, 5, 0.5f));
                correctSeq.Append(btnTransform.DOScale(1f, 0.2f));
                
                // DELEGATE/EVENT TRIGGER: Adding coins updates UI automatically[cite: 7]
                DataManager.Instance.AddCoins(rewardPerQuestion); 
                
                StartCoroutine(WaitAndLoadNextQuestion(1f));
            }
            else
            {
                if (sfxSource != null && wrongSound != null) sfxSource.PlayOneShot(wrongSound);
                MOST_HapticFeedback.Generate(MOST_HapticFeedback.HapticTypes.Failure);
                HandleWrongAnswer(selectedIndex);
            }
        }
    }

    /// <summary> Handles logic when the question timer reaches zero. </summary>
    private void OnTimeRanOut()
    {
        isAnswering = true;
        optionButtonsImage[currentQuestion.answer].color = correctColor;
        HandleWrongAnswer(-1); 
    }

    /// <summary> Handles incorrect answer state, health reduction, and game over. </summary>
    private void HandleWrongAnswer(int clickedIndex)
    {
        if (clickedIndex != -1) 
        {
            optionButtonsImage[clickedIndex].color = wrongColor;
            Transform clickedButton = optionTextsUI[clickedIndex].transform.parent;
            clickedButton.DOShakePosition(0.4f, new Vector3(15f, 0, 0), 25, 90, false, true);
        }

        currentLives--;
        UpdateLivesUI();

        if (currentLives <= 0)
        {
            ShowResult(false);
            StartCoroutine(WaitAndReturnToMenu(15f));
        }
        else
        {
            StartCoroutine(WaitAndLoadNextQuestion(1.5f)); 
        }
    }

    /// <summary> Updates the health points visually on UI. </summary>
    private void UpdateLivesUI()
    {
        livesTextUI.text = currentLives.ToString();
    }

    /// <summary> Animates rolling numbers for the coin display based on an event. </summary>
    public void UpdateCoinDisplay(int targetCoins)
    {
        if (currentlyDisplayedCoins == -1)
        {
            currentlyDisplayedCoins = targetCoins;
            coinTextUI.text = currentlyDisplayedCoins.ToString();
            return;
        }

        DOTween.To(() => currentlyDisplayedCoins, x =>
        {
            currentlyDisplayedCoins = x;
            coinTextUI.text = currentlyDisplayedCoins.ToString();
        }, targetCoins, 1f).SetEase(Ease.OutExpo);

        coinTextUI.transform.DOPunchScale(new Vector3(0.2f, 0.2f, 0), 0.5f, 5, 1);
    }

    /// <summary> Delays execution before fetching the next question data. </summary>
    private IEnumerator WaitAndLoadNextQuestion(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        if (currentLives > 0)
        {
            currentQuestionIndex++;
            
            if (btnJoker50 != null) btnJoker50.interactable = true;
            if (btnJokerTime != null) btnJokerTime.interactable = true;
            isFiftyFiftyUsedThisQuestion = false;
            
            LoadQuestion(); 
        }
    }

    /// <summary> Delays execution before returning safely to main menu. </summary>
    private IEnumerator WaitAndReturnToMenu(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        ReturnToMainMenu();
    }

    /// <summary> Evaluates if a district can be opened, otherwise presents a popup. </summary>
    public void TryUnlockDistrict(string districtId)
    {
        DistrictData district = DataManager.Instance.LoadedGameData.districts.Find(d => d.id == districtId);
        
        if (district.is_unlocked)
        {
            if (mapManager != null)
            {
                if (mapManager.selectedDistrictId == districtId)
                {
                    mapPanel.SetActive(false);
                    LoadDistrict(districtId);
                }
                else
                {
                    mapManager.selectedDistrictId = districtId;
                    mapManager.RefreshMap();
                }
            }
        }
        else
        {
            pendingDistrictToUnlock = district;
            popupMessageText.text = $"{district.name} bölgesini {district.unlockCost} Altın karşılığında açmak ister misin?";
            
            unlockPopupPanel.SetActive(true); 
            popupBox.transform.localScale = Vector3.zero; 
            popupBox.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack); 
            MOST_HapticFeedback.Generate(MOST_HapticFeedback.HapticTypes.Selection);
        }
    }

    /// <summary> Transistions view to Map. </summary>
    public void OnMapButtonClicked()
    {
        currentGameState = GameState.Map;
        mainMenuPanel.SetActive(false); 
        mapPanel.SetActive(true);       
    }

    /// <summary> Executes logic for unlocking a district securely and deducts cost. </summary>
    public void ConfirmUnlock()
    {
        if (pendingDistrictToUnlock != null)
        {
            if (DataManager.Instance.TotalCoins >= pendingDistrictToUnlock.unlockCost)
            {
                MOST_HapticFeedback.Generate(MOST_HapticFeedback.HapticTypes.HeavyImpact);
                // Triggers Event automatically[cite: 7]
                DataManager.Instance.AddCoins(-pendingDistrictToUnlock.unlockCost); 
                
                pendingDistrictToUnlock.is_unlocked = true;
                DataManager.Instance.SaveProgress();
                
                if (mapManager != null) mapManager.RefreshMap();

                popupBox.transform.DOScale(0f, 0.2f).SetEase(Ease.InBack).OnComplete(() => 
                {
                    unlockPopupPanel.SetActive(false); 
                });
            }
            else
            {
                popupMessageText.text = "Yetersiz bakiye!";
                popupBox.transform.DOShakePosition(0.3f, new Vector3(15f, 0, 0), 10, 0, false, true);
                MOST_HapticFeedback.Generate(MOST_HapticFeedback.HapticTypes.Failure);
            }
        }
    }

    /// <summary> Cancels the unlock process with animation. </summary>
    public void CancelUnlock()
    {
        popupBox.transform.DOScale(0f, 0.2f).SetEase(Ease.InBack).OnComplete(() => 
        {
            unlockPopupPanel.SetActive(false);
            pendingDistrictToUnlock = null;
        });
    }    

    /// <summary> Transistions view back to Main Menu. </summary>
    public void OnBackToMenuClicked()
    {
        currentGameState = GameState.MainMenu;
        mapPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    /// <summary> Eliminates two incorrect options at the cost of coins. </summary>
    public void UseFiftyFiftyJoker()
    {
        if (isFiftyFiftyUsedThisQuestion || isAnswering) return; 

        if (DataManager.Instance.TotalCoins >= jokerCost)
        {
            MOST_HapticFeedback.Generate(MOST_HapticFeedback.HapticTypes.MediumImpact);
            DataManager.Instance.AddCoins(-jokerCost); // Updates UI automatically[cite: 7]
            isFiftyFiftyUsedThisQuestion = true;

            btnJoker50.transform.DOPunchScale(new Vector3(-0.2f, -0.2f, 0), 0.3f, 10, 1);

            List<int> wrongOptions = new List<int>();
            for (int i = 0; i < currentQuestion.options.Length; i++)
            {
                if (i != currentQuestion.answer) wrongOptions.Add(i);
            }

            for (int i = 0; i < wrongOptions.Count; i++)
            {
                int temp = wrongOptions[i];
                int randomIndex = UnityEngine.Random.Range(i, wrongOptions.Count);
                wrongOptions[i] = wrongOptions[randomIndex];
                wrongOptions[randomIndex] = temp;
            }

            for (int i = 0; i < 2; i++)
            {
                int indexToHide = wrongOptions[i];
                Transform btnTrans = optionTextsUI[indexToHide].transform.parent;
                
                btnTrans.GetComponent<Button>().interactable = false;
                btnTrans.DOScale(Vector3.zero, 0.4f).SetEase(Ease.InBack);
            }
        }
        else
        {
            btnJoker50.transform.DOShakePosition(0.3f, new Vector3(10f, 0, 0), 20);
        }
    }

    /// <summary> Adds extra time to the clock at the cost of coins. </summary>
    public void UseTimeJoker()
    {
        if (isAnswering) return;

        if (DataManager.Instance.TotalCoins >= jokerCost)
        {
            MOST_HapticFeedback.Generate(MOST_HapticFeedback.HapticTypes.MediumImpact);
            DataManager.Instance.AddCoins(-jokerCost);

            btnJokerTime.transform.DOPunchScale(new Vector3(-0.2f, -0.2f, 0), 0.3f, 10, 1);

            currentTime += 15f;

            timerTextUI.transform.DOPunchScale(new Vector3(0.4f, 0.4f, 0), 0.5f, 5, 1);
            timerTextUI.DOColor(Color.green, 0.15f).OnComplete(() => timerTextUI.DOColor(Color.white, 0.3f));
        }
        else
        {
            btnJokerTime.transform.DOShakePosition(0.3f, new Vector3(10f, 0, 0), 20);
        }
    }

    /// <summary> Safely resets state and transitions to Main Menu. </summary>
    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        currentGameState = GameState.MainMenu;

        if (questionPanel != null) questionPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    /// <summary> Displays the final outcome panel of a quiz round. </summary>
    public void ShowResult(bool isWin)
    {
        currentGameState = GameState.Result;
        resultPanel.SetActive(true);
        
        resultPanel.transform.localScale = Vector3.zero;
        resultPanel.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack);

        if (isWin)
        {
            resultPanel.GetComponent<Image>().sprite = successSprite;
            MOST_HapticFeedback.Generate(MOST_HapticFeedback.HapticTypes.Success);
            if (sfxSource != null && quizCompleteSound != null)
            {
                sfxSource.PlayOneShot(quizCompleteSound); 
            }
        }
        else
        {
            resultPanel.GetComponent<Image>().sprite = failureSprite;
            MOST_HapticFeedback.Generate(MOST_HapticFeedback.HapticTypes.Failure);
        }
    }

    /// <summary> Closes the result panel safely. </summary>
    public void CloseResultPanel()
    {
        MOST_HapticFeedback.Generate(MOST_HapticFeedback.HapticTypes.Selection);

        resultPanel.transform.DOScale(0f, 0.3f).SetEase(Ease.InBack).OnComplete(() => {
            resultPanel.SetActive(false);
            ReturnToMainMenu();
        });
    }
}