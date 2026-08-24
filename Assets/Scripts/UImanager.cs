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
    public GameObject puzzlePanel; // YENİ EKLENDİ
    
    [Header("Ayarlar Paneli")]
    public GameObject settingsPanel;
    public UnityEngine.UI.Slider volumeSlider;
    public TMPro.TextMeshProUGUI volumePercentText;
    public UnityEngine.UI.Toggle muteToggle;
    public UnityEngine.UI.Toggle vibrationToggle;

    [Header("Can Bitti Pop-up Sistemi")]
    public GameObject outOfLivesPopupPanel;

    [Header("Süre Bitti Pop-up Sistemi")]
    public GameObject timeOutPopupPanel;

    [Header("Kelime Oyunu Paneli")]
    public GameObject wordScramblePanel;

    [Header("Kelime Oyunu Süre/Sayaç Elemanları")]
    public Image scrambleTimerFillImage;     
    public TextMeshProUGUI scrambleTimerTextUI; 
    public TextMeshProUGUI scrambleQuestionCountTextUI;

    [Header("Soru Ekranı UI Objeleri")]
    public TextMeshProUGUI questionTextUI;
    public TextMeshProUGUI[] optionTextsUI;
    public TextMeshProUGUI questionCountTextUI; 
    public Image[] optionButtonsImage;
    public Image questionImageUI;
    
    [Header("Can Sistemi")]
    public TextMeshProUGUI livesTextUI;
    public int maxLives = 5;
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
    [Header("Veri Yükleme Hilesi")]
    // Eğer UIManager içinde avatarPanel zaten tanımlıysa bu satıra gerek yok
    public GameObject avatarPanelToFlicker;

    private QuestionData currentQuestion;
    private DistrictData currentDistrict;
    private int currentQuestionIndex;
    private bool isAnswering = false;
    private bool hasTimedOutThisQuestion = false;
    private bool isWaitingForTimeOutDecision = false;
    private int currentlyDisplayedCoins = -1;
    private GameState currentGameState = GameState.MainMenu;
    private Coroutine returnToMenuCoroutine;
    private TMPro.TextMeshProUGUI puzzleLivesTextUI;
    private UnityEngine.UI.Image puzzleTimerFillImage;
    private TMPro.TextMeshProUGUI puzzleTimerTextUI;
    private TMPro.TextMeshProUGUI timeOutIconTextUI;
    private TMPro.TextMeshProUGUI timeOutPopupMessageTextUI;

    /// <summary> Subscribes to the global coin update event. </summary>
    private void OnEnable() => DataManager.OnCoinsChanged += HandleCoinsChanged;

    /// <summary> Unsubscribes to prevent memory leaks. </summary>
    private void OnDisable() => DataManager.OnCoinsChanged -= HandleCoinsChanged;
    // Bunu en başa ekle:
    public static UIManager Instance { get; private set; }
    private List<QuestionData> activeQuestions = new List<QuestionData>();

    // Start metodunun hemen üstüne bunu ekle:
    private void Awake()
    {
        if (Instance == null) Instance = this;

        EnsureResultPanelOnCanvas();
    }

    /// <summary>
    /// Keeps the final result popup directly under Canvas so it remains visible even when
    /// QuestionPanel or WordScramblePanel gets disabled at the end of a district.
    /// </summary>
    private void EnsureResultPanelOnCanvas()
    {
        if (resultPanel == null) return;

        Transform canvasTrans = null;
        Canvas resultCanvas = resultPanel.GetComponentInParent<Canvas>(true);
        if (resultCanvas != null)
        {
            canvasTrans = resultCanvas.transform;
        }
        else
        {
            Canvas managerCanvas = GetComponentInParent<Canvas>();
            if (managerCanvas != null)
            {
                canvasTrans = managerCanvas.transform;
            }
            else
            {
                GameObject canvasObj = GameObject.Find("Canvas");
                if (canvasObj != null) canvasTrans = canvasObj.transform;
            }
        }

        if (canvasTrans != null && resultPanel.transform.parent != canvasTrans)
        {
            resultPanel.transform.SetParent(canvasTrans, false);
        }
    }

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
        StartCoroutine(FlickerPanelsForDataLoad());
        
        InitializeSettings();
    }

    private void InitializeSettings()
    {
        // Find Settings Panel and elements automatically if null
        if (settingsPanel == null)
        {
            Transform canvasTrans = GetComponentInParent<Canvas>()?.transform;
            if (canvasTrans != null)
            {
                Transform spTrans = canvasTrans.Find("SettingsPanel");
                if (spTrans != null)
                {
                    settingsPanel = spTrans.gameObject;
                    volumeSlider = spTrans.Find("VolumeSection/VolumeSlider")?.GetComponent<UnityEngine.UI.Slider>();
                    volumePercentText = spTrans.Find("VolumeSection/PercentText")?.GetComponent<TMPro.TextMeshProUGUI>();
                    muteToggle = spTrans.Find("VolumeSection/MuteToggle")?.GetComponent<UnityEngine.UI.Toggle>();
                    vibrationToggle = spTrans.Find("VibrationSection/VibrationToggle")?.GetComponent<UnityEngine.UI.Toggle>();
                }
            }
        }

        // Apply saved values to systems
        float savedVolume = PlayerPrefs.GetFloat("SavedVolume", 1.0f);
        bool savedMute = PlayerPrefs.GetInt("SavedMute", 0) == 1;
        bool savedHaptics = Solo.MOST_IN_ONE.MOST_HapticFeedback.HapticsEnabled;

        AudioListener.volume = savedMute ? 0f : savedVolume;
        Solo.MOST_IN_ONE.MOST_HapticFeedback.HapticsEnabled = savedHaptics;

        // Apply saved values to UI controls
        if (volumeSlider != null)
        {
            volumeSlider.value = savedVolume;
            volumeSlider.onValueChanged.RemoveAllListeners();
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }
        if (muteToggle != null)
        {
            muteToggle.isOn = savedMute;
            muteToggle.onValueChanged.RemoveAllListeners();
            muteToggle.onValueChanged.AddListener(OnMuteToggled);
        }
        if (vibrationToggle != null)
        {
            vibrationToggle.isOn = savedHaptics;
            vibrationToggle.onValueChanged.RemoveAllListeners();
            vibrationToggle.onValueChanged.AddListener(OnVibrationToggled);
        }
        if (volumePercentText != null)
        {
            volumePercentText.text = Mathf.RoundToInt(savedVolume * 100) + "%";
        }

        // Settings Button in Top Panel
        Button settingsBtn = GameObject.Find("Canvas/TopPanel/settings")?.GetComponent<Button>();
        if (settingsBtn == null) settingsBtn = GameObject.Find("TopPanel/settings")?.GetComponent<Button>();
        if (settingsBtn != null)
        {
            settingsBtn.onClick.RemoveAllListeners();
            settingsBtn.onClick.AddListener(OpenSettingsPanel);
        }

        // Close Button in Settings Panel
        Button spCloseBtn = settingsPanel?.transform.Find("CloseButton")?.GetComponent<Button>();
        if (spCloseBtn != null)
        {
            spCloseBtn.onClick.RemoveAllListeners();
            spCloseBtn.onClick.AddListener(CloseSettingsPanel);
        }

        // Find Puzzle Panel elements automatically if assigned
        if (puzzlePanel != null)
        {
            puzzleLivesTextUI = puzzlePanel.transform.Find("lives (1)/livestext")?.GetComponent<TMPro.TextMeshProUGUI>();
            puzzleTimerFillImage = puzzlePanel.transform.Find("TimerBackground/TimerFill")?.GetComponent<UnityEngine.UI.Image>();
            puzzleTimerTextUI = puzzlePanel.transform.Find("TimerBackground/TimerFill/TimerText")?.GetComponent<TMPro.TextMeshProUGUI>();
        }

        // Find Out of Lives Popup Panel automatically if null
        if (outOfLivesPopupPanel == null)
        {
            Transform canvasTrans = GetComponentInParent<Canvas>()?.transform;
            if (canvasTrans != null)
            {
                Transform oolTrans = canvasTrans.Find("OutOfLivesPopup");
                if (oolTrans != null)
                {
                    outOfLivesPopupPanel = oolTrans.gameObject;
                    
                    Button yesBtn = oolTrans.Find("Yes")?.GetComponent<Button>();
                    if (yesBtn != null)
                    {
                        yesBtn.onClick.RemoveAllListeners();
                        yesBtn.onClick.AddListener(BuyLivesAndContinue);
                    }
                    Button noBtn = oolTrans.Find("no")?.GetComponent<Button>();
                    if (noBtn != null)
                    {
                        noBtn.onClick.RemoveAllListeners();
                        noBtn.onClick.AddListener(DeclineOutOfLives);
                    }
                }
            }
        }

        if (timeOutPopupPanel == null)
        {
            Transform canvasTrans = GetComponentInParent<Canvas>()?.transform;
            if (canvasTrans != null)
            {
                Transform timeoutTrans = canvasTrans.Find("TimeOutPopup");
                if (timeoutTrans != null)
                {
                    timeOutPopupPanel = timeoutTrans.gameObject;
                }
            }
        }

        if (timeOutPopupPanel != null)
        {
            timeOutIconTextUI = timeOutPopupPanel.transform.Find("Icon/Text (TMP)")?.GetComponent<TMPro.TextMeshProUGUI>();
            timeOutPopupMessageTextUI = timeOutPopupPanel.transform.Find("popuptext")?.GetComponent<TMPro.TextMeshProUGUI>();

            Button continueBtn = timeOutPopupPanel.transform.Find("Yes")?.GetComponent<Button>();
            if (continueBtn != null)
            {
                continueBtn.onClick.RemoveAllListeners();
                continueBtn.onClick.AddListener(ContinueAfterTimeOut);
            }

            Button closeBtn = timeOutPopupPanel.transform.Find("no")?.GetComponent<Button>();
            if (closeBtn != null)
            {
                closeBtn.onClick.RemoveAllListeners();
                closeBtn.onClick.AddListener(CloseTimeOutPopupToMenu);
            }
        }

        HideTimeOutPopup();
    }

    public void OpenSettingsPanel()
    {
        if (settingsPanel == null) return;
        
        float savedVolume = PlayerPrefs.GetFloat("SavedVolume", 1.0f);
        bool savedMute = PlayerPrefs.GetInt("SavedMute", 0) == 1;
        bool savedHaptics = Solo.MOST_IN_ONE.MOST_HapticFeedback.HapticsEnabled;

        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.RemoveAllListeners();
            volumeSlider.value = savedVolume;
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }
        if (muteToggle != null)
        {
            muteToggle.onValueChanged.RemoveAllListeners();
            muteToggle.isOn = savedMute;
            muteToggle.onValueChanged.AddListener(OnMuteToggled);
        }
        if (vibrationToggle != null)
        {
            vibrationToggle.onValueChanged.RemoveAllListeners();
            vibrationToggle.isOn = savedHaptics;
            vibrationToggle.onValueChanged.AddListener(OnVibrationToggled);
        }
        if (volumePercentText != null)
        {
            volumePercentText.text = Mathf.RoundToInt(savedVolume * 100) + "%";
        }

        settingsPanel.SetActive(true);
        settingsPanel.transform.localScale = Vector3.zero;
        settingsPanel.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack).SetUpdate(true);
        
        Solo.MOST_IN_ONE.MOST_HapticFeedback.Generate(Solo.MOST_IN_ONE.MOST_HapticFeedback.HapticTypes.Selection);
    }

    public void CloseSettingsPanel()
    {
        if (settingsPanel == null) return;
        
        Solo.MOST_IN_ONE.MOST_HapticFeedback.Generate(Solo.MOST_IN_ONE.MOST_HapticFeedback.HapticTypes.Selection);
        
        settingsPanel.transform.DOScale(0f, 0.3f).SetEase(Ease.InBack).SetUpdate(true).OnComplete(() =>
        {
            settingsPanel.SetActive(false);
        });
    }

    private void OnVolumeChanged(float value)
    {
        if (muteToggle != null && !muteToggle.isOn)
        {
            AudioListener.volume = value;
        }
        if (volumePercentText != null)
        {
            volumePercentText.text = Mathf.RoundToInt(value * 100) + "%";
        }
        PlayerPrefs.SetFloat("SavedVolume", value);
        PlayerPrefs.Save();
    }

    private void OnMuteToggled(bool isMuted)
    {
        AudioListener.volume = isMuted ? 0f : (volumeSlider != null ? volumeSlider.value : 1.0f);
        PlayerPrefs.SetInt("SavedMute", isMuted ? 1 : 0);
        PlayerPrefs.Save();
        
        Solo.MOST_IN_ONE.MOST_HapticFeedback.Generate(Solo.MOST_IN_ONE.MOST_HapticFeedback.HapticTypes.Selection);
    }

    private void OnVibrationToggled(bool isEnabled)
    {
        Solo.MOST_IN_ONE.MOST_HapticFeedback.HapticsEnabled = isEnabled;
        
        Solo.MOST_IN_ONE.MOST_HapticFeedback.Generate(Solo.MOST_IN_ONE.MOST_HapticFeedback.HapticTypes.Selection);
    }

    private void SaveSessionProgress()
    {
        if (currentDistrict == null) return;

        PlayerPrefs.SetInt("HasSavedSession", 1);
        PlayerPrefs.SetString("SessionDistrict", currentDistrict.id);
        PlayerPrefs.SetInt("SavedIndex", Mathf.Max(0, currentQuestionIndex));
        PlayerPrefs.SetInt("SavedLives", currentLives);
        PlayerPrefs.Save();
    }

    private void ClearSavedSession()
    {
        PlayerPrefs.SetInt("HasSavedSession", 0);
        PlayerPrefs.DeleteKey("SessionDistrict");
        PlayerPrefs.DeleteKey("SavedIndex");
        PlayerPrefs.DeleteKey("SavedLives");
        PlayerPrefs.Save();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && currentDistrict != null && (currentGameState == GameState.Playing || currentGameState == GameState.Puzzle || currentGameState == GameState.WordScramble))
        {
            SaveSessionProgress();
        }
    }

    private void OnApplicationQuit()
    {
        if (currentDistrict != null && (currentGameState == GameState.Playing || currentGameState == GameState.Puzzle || currentGameState == GameState.WordScramble))
        {
            SaveSessionProgress();
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
            
            
            
            LoadDistrict(districtToLoad); 
        });
    }

    /// <summary> Loads district data and prepares the first question. </summary>
   /// <summary> Loads district data and prepares the first question or loads saved session. </summary>
    /// <summary> Loads district data and prepares the first question or loads saved session. </summary>
    /// <summary> Loads district data and prepares the first question or loads saved session. </summary>
    public void LoadDistrict(string districtId)
    {
        if (DataManager.Instance != null && DataManager.Instance.LoadedGameData != null)
        {
            currentDistrict = DataManager.Instance.LoadedGameData.districts.Find(d => d.id == districtId);
            
            if (currentDistrict != null && currentDistrict.questions != null)
            {
                bool isSavedSession = (PlayerPrefs.GetInt("HasSavedSession", 0) == 1) && 
                                      (PlayerPrefs.GetString("SessionDistrict", "") == districtId);

                // YENİ: Akıllı karıştırma ve dizme metodumuzu çağırıyoruz
                PrepareAndShuffleQuestions(currentDistrict.questions, !isSavedSession);

                if (isSavedSession)
                {
                    currentQuestionIndex = PlayerPrefs.GetInt("SavedIndex", 0);
                    currentLives = PlayerPrefs.GetInt("SavedLives", maxLives);
                }
                else
                {
                    currentQuestionIndex = 0;
                    currentLives = maxLives;
                }
                
                UpdateLivesUI(); 
            }

            LoadQuestion();
        }
    }

    /// <summary> Loads the specific question data and animates UI elements in. </summary>
    private void LoadQuestion()
    {
        isAnswering = false;
        hasTimedOutThisQuestion = false;
        HideTimeOutPopup();

        if (currentDistrict != null && currentQuestionIndex < activeQuestions.Count)
        {
            currentQuestion = activeQuestions[currentQuestionIndex];
            SaveSessionProgress();
            // YENİ: Sayaç güncellemesi Switch-Case'in Dışına ve Üstüne Alındı! (İkisi için de çalışır)
            if (questionCountTextUI != null)
            {
                questionCountTextUI.text = $"Soru: {currentQuestionIndex + 1} / {activeQuestions.Count}";
            }
            if (scrambleQuestionCountTextUI != null)
            {
                scrambleQuestionCountTextUI.text = $"Soru: {currentQuestionIndex + 1} / {activeQuestions.Count}";
            }
            // YENİ: Soru tipine göre ilgili paneli açan "Switch-Case" yönlendiricisi
            switch (currentQuestion.type)
            {
                case QuestionType.StandardQuiz:
                    currentGameState = GameState.Playing; 
                    if (wordScramblePanel != null && wordScramblePanel.activeSelf)
                    {
                        LoadStandardQuizUI();
                        TransitionPanels(wordScramblePanel, questionPanel, null);
                    }
                    else
                    {
                        questionPanel.SetActive(true);
                        if (wordScramblePanel != null) wordScramblePanel.SetActive(false);
                        LoadStandardQuizUI();
                    }
                    break;
                    
                case QuestionType.WordScramble:
                    currentGameState = GameState.WordScramble; 
                    if (questionPanel.activeSelf)
                    {
                        LoadWordScrambleUI();
                        TransitionPanels(questionPanel, wordScramblePanel, null);
                    }
                    else
                    {
                        questionPanel.SetActive(false);
                        if (wordScramblePanel != null) wordScramblePanel.SetActive(true);
                        LoadWordScrambleUI();
                    }
                    break;
            }
        }
        else
        {
            ShowResult(true);
            isTimerRunning = false; 
        }
    }

    private void LoadWordScrambleUI()
    {
        // YENİ: Süreyi Kelime oyunu için de başlatıyoruz!
        currentTime = timePerQuestion;
        if (timerTextUI != null) timerTextUI.text = currentTime.ToString(); 
        if (scrambleTimerTextUI != null) scrambleTimerTextUI.text = currentTime.ToString(); 
        if (timerFillImage != null) timerFillImage.fillAmount = 1f;
        if (scrambleTimerFillImage != null) scrambleTimerFillImage.fillAmount = 1f;
        isTimerRunning = true; 

        // Fix: Ensure WordScrambleManager instance exists even if panel was inactive
        WordScrambleManager manager = WordScrambleManager.Instance;
        if (manager == null && wordScramblePanel != null)
        {
            manager = wordScramblePanel.GetComponent<WordScrambleManager>();
        }

        if (manager != null)
        {
            manager.LoadWordPuzzle(currentQuestion);
        }
    }

    private void TransitionPanels(GameObject fromPanel, GameObject toPanel, System.Action onComplete)
    {
        if (fromPanel == null || toPanel == null)
        {
            if (fromPanel != null) fromPanel.SetActive(false);
            if (toPanel != null) toPanel.SetActive(true);
            onComplete?.Invoke();
            return;
        }

        CanvasGroup fromGroup = GetOrAddCanvasGroup(fromPanel);
        CanvasGroup toGroup = GetOrAddCanvasGroup(toPanel);

        // Ensure toPanel is active but transparent initially
        toPanel.SetActive(true);
        toGroup.alpha = 0f;
        
        // Slightly scale down fromPanel and scale up toPanel for a gorgeous Zoom/Fade transition
        RectTransform fromRect = fromPanel.GetComponent<RectTransform>();
        RectTransform toRect = toPanel.GetComponent<RectTransform>();

        // Set initial states
        fromGroup.alpha = 1f;
        fromRect.localScale = Vector3.one;
        toRect.localScale = new Vector3(0.9f, 0.9f, 1f);

        // Slide/Fade/Scale animation
        fromGroup.DOKill();
        toGroup.DOKill();
        fromRect.DOKill();
        toRect.DOKill();

        Sequence seq = DOTween.Sequence();
        seq.SetUpdate(true); // Run even if timeScale is paused
        
        // Animate fromPanel out
        seq.Join(fromGroup.DOFade(0f, 0.4f).SetEase(Ease.OutQuad));
        seq.Join(fromRect.DOScale(0.95f, 0.4f).SetEase(Ease.OutQuad));
        
        // Animate toPanel in
        seq.Join(toGroup.DOFade(1f, 0.4f).SetEase(Ease.OutQuad));
        seq.Join(toRect.DOScale(1f, 0.4f).SetEase(Ease.OutBack));

        seq.OnComplete(() =>
        {
            fromPanel.SetActive(false);
            // Reset scales for future use
            fromRect.localScale = Vector3.one;
            fromGroup.alpha = 1f;
            onComplete?.Invoke();
        });
    }

    /// <summary> Klasik 4 şıklı quiz ekranını ve animasyonlarını hazırlar. </summary>
    private void LoadStandardQuizUI()
    {
        questionTextUI.text = currentQuestion.question;

        if (questionCountTextUI != null)
        {
            questionCountTextUI.text = $"Soru: {currentQuestionIndex + 1} / {activeQuestions.Count}";
        }
        if (scrambleQuestionCountTextUI != null)
        {
            scrambleQuestionCountTextUI.text = $"Soru: {currentQuestionIndex + 1} / {activeQuestions.Count}";
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
        if (timerTextUI != null) timerTextUI.text = currentTime.ToString(); 
        if (scrambleTimerTextUI != null) scrambleTimerTextUI.text = currentTime.ToString(); 
        if (timerFillImage != null) timerFillImage.fillAmount = 1f;
        if (scrambleTimerFillImage != null) scrambleTimerFillImage.fillAmount = 1f;
        isTimerRunning = true;
    }
    /// <summary> WordScrambleManager'dan çağrılır. Başarı durumunda sıradaki soruyu yükler. </summary>
    /// <summary> WordScrambleManager'dan çağrılır. Başarı durumunda sıradaki soruyu yükler. </summary>
    public void OnWordScrambleCompleted()
    {
        // YENİ: Altın ödülünü ver ve başarı sesini çal
        DataManager.Instance.AddCoins(rewardPerQuestion); 
        if (sfxSource != null && correctSound != null) sfxSource.PlayOneShot(correctSound);

        StartCoroutine(WaitAndLoadNextQuestion(0f)); 
    }

    

    /// <summary> Handles countdown logic per frame. </summary>
    /// <summary> Handles countdown logic per frame. </summary>
    private void Update()
    {
        // KİLİT NOKTA: Artık Playing (Quiz), WordScramble ve Puzzle durumunda süre akacak
        if (currentGameState != GameState.Playing && currentGameState != GameState.WordScramble && currentGameState != GameState.Puzzle) return;
        
        if (isTimerRunning)
        {
            currentTime -= Time.deltaTime;
            
            if (timerFillImage != null) timerFillImage.fillAmount = currentTime / timePerQuestion;
            if (timerTextUI != null) timerTextUI.text = Mathf.CeilToInt(currentTime).ToString();

            if (scrambleTimerFillImage != null) scrambleTimerFillImage.fillAmount = currentTime / timePerQuestion;
            if (scrambleTimerTextUI != null) scrambleTimerTextUI.text = Mathf.CeilToInt(currentTime).ToString();

            if (puzzleTimerFillImage != null) puzzleTimerFillImage.fillAmount = currentTime / timePerQuestion;
            if (puzzleTimerTextUI != null) puzzleTimerTextUI.text = Mathf.CeilToInt(currentTime).ToString();

            if (currentTime <= 0)
            {
                isTimerRunning = false;
                currentTime = 0;
                if (timerFillImage != null) timerFillImage.fillAmount = 0;
                if (timerTextUI != null) timerTextUI.text = "0"; 
                if (scrambleTimerFillImage != null) scrambleTimerFillImage.fillAmount = 0;
                if (scrambleTimerTextUI != null) scrambleTimerTextUI.text = "0"; 
                if (puzzleTimerFillImage != null) puzzleTimerFillImage.fillAmount = 0;
                if (puzzleTimerTextUI != null) puzzleTimerTextUI.text = "0"; 
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
        if (hasTimedOutThisQuestion || isWaitingForTimeOutDecision) return;

        hasTimedOutThisQuestion = true;
        isAnswering = true;
        isTimerRunning = false;
        isWaitingForTimeOutDecision = true;
        SaveSessionProgress();

        ShowTimeOutPopup();
        
        // YENİ: Sadece standart quizdeysek şıkkı yeşile boya. Kelime oyunundaysa direkt can düş.
        if (currentQuestion != null && currentQuestion.type == QuestionType.StandardQuiz)
        {
            optionButtonsImage[currentQuestion.answer].color = correctColor;
        }
    }

    private void ShowTimeOutPopup()
    {
        if (timeOutPopupPanel == null) return;

        int remainingLivesAfterPenalty = Mathf.Max(currentLives - 1, 0);

        if (timeOutIconTextUI != null)
        {
            timeOutIconTextUI.text = remainingLivesAfterPenalty.ToString();
        }

        if (timeOutPopupMessageTextUI != null)
        {
            timeOutPopupMessageTextUI.text = $"Süre bitti! <b>1 can kaybedeceksin.</b> Devam edersen kalan canın: <b>{remainingLivesAfterPenalty}</b>";
        }

        Button continueBtn = timeOutPopupPanel.transform.Find("Yes")?.GetComponent<Button>();
        if (continueBtn != null)
        {
            continueBtn.interactable = true;
        }

        timeOutPopupPanel.SetActive(true);
        timeOutPopupPanel.transform.DOKill();
        timeOutPopupPanel.transform.localScale = Vector3.zero;
        timeOutPopupPanel.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack).SetUpdate(true);
    }

    private void HideTimeOutPopup()
    {
        isWaitingForTimeOutDecision = false;

        if (timeOutPopupPanel == null) return;

        timeOutPopupPanel.transform.DOKill();
        timeOutPopupPanel.SetActive(false);
        timeOutPopupPanel.transform.localScale = Vector3.one;
    }

    public void ContinueAfterTimeOut()
    {
        if (!isWaitingForTimeOutDecision) return;

        Button continueBtn = timeOutPopupPanel != null ? timeOutPopupPanel.transform.Find("Yes")?.GetComponent<Button>() : null;
        if (continueBtn != null)
        {
            continueBtn.interactable = false;
        }

        MOST_HapticFeedback.Generate(MOST_HapticFeedback.HapticTypes.Selection);
        HideTimeOutPopup();
        HandleWrongAnswer(-1);
    }

    public void CloseTimeOutPopupToMenu()
    {
        if (!isWaitingForTimeOutDecision) return;

        MOST_HapticFeedback.Generate(MOST_HapticFeedback.HapticTypes.Selection);
        HideTimeOutPopup();
        ReturnToMainMenu(false);
    }

    private void PlayCorrectAnswerBlink(int correctIndex)
    {
        if (correctIndex < 0 || correctIndex >= optionButtonsImage.Length) return;

        Image correctButtonImage = optionButtonsImage[correctIndex];
        if (correctButtonImage == null) return;

        correctButtonImage.DOKill();

        Sequence blinkSequence = DOTween.Sequence();
        blinkSequence.Append(correctButtonImage.DOColor(correctColor, 0.15f));
        blinkSequence.Append(correctButtonImage.DOColor(normalColor, 0.15f));
        blinkSequence.Append(correctButtonImage.DOColor(correctColor, 0.15f));
        blinkSequence.Append(correctButtonImage.DOColor(normalColor, 0.15f));
        blinkSequence.Append(correctButtonImage.DOColor(correctColor, 0.15f));
    }

    /// <summary> Handles incorrect answer state, health reduction, and game over. </summary>
    private void HandleWrongAnswer(int clickedIndex)
    {
        if (clickedIndex != -1 && currentQuestion != null && currentQuestion.type == QuestionType.StandardQuiz)
        {
            PlayCorrectAnswerBlink(currentQuestion.answer);
        }

        if (clickedIndex != -1) 
        {
            optionButtonsImage[clickedIndex].color = wrongColor;
            Transform clickedButton = optionTextsUI[clickedIndex].transform.parent;
            clickedButton.DOShakePosition(0.4f, new Vector3(15f, 0, 0), 25, 90, false, true);
        }

        currentLives--;
        UpdateLivesUI();
        SaveSessionProgress();

        if (currentLives <= 0)
        {
            if (outOfLivesPopupPanel != null)
            {
                OpenOutOfLivesPopup();
            }
            else
            {
                ShowResult(false);
            }
        }
        else
        {
            if (currentGameState == GameState.Puzzle)
            {
                HidePuzzlePanel();
            }
            else
            {
                StartCoroutine(WaitAndLoadNextQuestion(1.5f)); 
            }
        }
    }

    public void OpenOutOfLivesPopup()
    {
        isTimerRunning = false; // Pause timer!
        HideTimeOutPopup();
        if (outOfLivesPopupPanel != null)
        {
            Button continueBtn = outOfLivesPopupPanel.transform.Find("Yes")?.GetComponent<Button>();
            if (continueBtn != null)
            {
                continueBtn.interactable = true;
            }

            outOfLivesPopupPanel.SetActive(true);
            outOfLivesPopupPanel.transform.localScale = Vector3.zero;
            outOfLivesPopupPanel.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack).SetUpdate(true);
            MOST_HapticFeedback.Generate(MOST_HapticFeedback.HapticTypes.Selection);
        }
    }

    public void BuyLivesAndContinue()
    {
        int cost = 150; // Buy 5 lives for 150 coins
        if (DataManager.Instance != null && DataManager.Instance.TotalCoins >= cost)
        {
            Button continueBtn = outOfLivesPopupPanel != null ? outOfLivesPopupPanel.transform.Find("Yes")?.GetComponent<Button>() : null;
            if (continueBtn != null)
            {
                continueBtn.interactable = false;
            }

            MOST_HapticFeedback.Generate(MOST_HapticFeedback.HapticTypes.HeavyImpact);
            DataManager.Instance.AddCoins(-cost);
            currentLives = maxLives;
            UpdateLivesUI();

            if (outOfLivesPopupPanel != null)
            {
                outOfLivesPopupPanel.transform.DOScale(0f, 0.2f).SetEase(Ease.InBack).OnComplete(() =>
                {
                    outOfLivesPopupPanel.SetActive(false);
                    isAnswering = false;
                    isTimerRunning = false;
                    StartCoroutine(WaitAndLoadNextQuestion(0f));
                });
            }
            else
            {
                isAnswering = false;
                isTimerRunning = false;
                StartCoroutine(WaitAndLoadNextQuestion(0f));
            }
        }
        else
        {
            MOST_HapticFeedback.Generate(MOST_HapticFeedback.HapticTypes.Failure);
            if (outOfLivesPopupPanel != null)
            {
                outOfLivesPopupPanel.transform.DOShakePosition(0.3f, new Vector3(15f, 0, 0), 10, 0, false, true);
            }
        }
    }

    public void DeclineOutOfLives()
    {
        MOST_HapticFeedback.Generate(MOST_HapticFeedback.HapticTypes.Selection);
        if (outOfLivesPopupPanel != null)
        {
            outOfLivesPopupPanel.transform.DOScale(0f, 0.2f).SetEase(Ease.InBack).OnComplete(() =>
            {
                outOfLivesPopupPanel.SetActive(false);
                ReturnToMainMenuAfterOutOfLivesDeclined();
            });
        }
        else
        {
            ReturnToMainMenuAfterOutOfLivesDeclined();
        }
    }

    /// <summary>
    /// Handles the "No" action on the out-of-lives popup as a fresh restart path:
    /// clear saved quiz session, restore lives, then return to the main menu.
    /// </summary>
    private void ReturnToMainMenuAfterOutOfLivesDeclined()
    {
        currentLives = maxLives;
        currentQuestionIndex = 0;
        isAnswering = false;
        isTimerRunning = false;

        ClearSavedSession();

        UpdateLivesUI();
        ReturnToMainMenu(false);
    }

    /// <summary> Updates the health points visually on UI. </summary>
    private void UpdateLivesUI()
    {
        livesTextUI.text = currentLives.ToString();
        if (puzzleLivesTextUI != null)
        {
            puzzleLivesTextUI.text = currentLives.ToString();
        }
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
            if (currentQuestionIndex < activeQuestions.Count)
            {
                SaveSessionProgress();
            }
            // --- YENİ: ARADA PUZZLE VAR MI KONTROLÜ ---
        if (currentDistrict.puzzles != null)
        {
            // O an biten sorunun sırasına (currentQuestionIndex) uygun bir puzzle var mı?
            PuzzleTriggerData pendingPuzzle = currentDistrict.puzzles.Find(p => p.triggerAfterQuestionIndex == currentQuestionIndex);
            
            if (pendingPuzzle != null)
            {
                // Puzzle Bulundu! Quizi durdur, puzzle'a geç.
                isTimerRunning = false;
                ShowPuzzlePanel();
                PuzzleManager.Instance.LoadDynamicPuzzle(pendingPuzzle.puzzlePrefabName);
                yield return null; // Normal soru yüklemeyi durdur!
            }
        }
        // ------------------------------------------
            
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
        returnToMenuCoroutine = null;
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
                    LoadDistrict(districtId);
                }
                else
                {
                    mapManager.selectedDistrictId = districtId;
                    PlayerPrefs.SetString("SavedDistrict", districtId);
                    PlayerPrefs.Save();
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
                
                if (mapManager != null)
                {
                    mapManager.selectedDistrictId = pendingDistrictToUnlock.id;
                    PlayerPrefs.SetString("SavedDistrict", pendingDistrictToUnlock.id);
                    PlayerPrefs.Save();
                    mapManager.RefreshMap();
                }

                popupBox.transform.DOScale(0f, 0.2f).SetEase(Ease.InBack).OnComplete(() => 
                {
                    unlockPopupPanel.SetActive(false); 
                    pendingDistrictToUnlock = null;
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

            if (timerTextUI != null)
            {
                timerTextUI.transform.DOPunchScale(new Vector3(0.4f, 0.4f, 0), 0.5f, 5, 1);
                timerTextUI.DOColor(Color.green, 0.15f).OnComplete(() => timerTextUI.DOColor(Color.white, 0.3f));
            }

            if (scrambleTimerTextUI != null)
            {
                scrambleTimerTextUI.transform.DOPunchScale(new Vector3(0.4f, 0.4f, 0), 0.5f, 5, 1);
                scrambleTimerTextUI.DOColor(Color.green, 0.15f).OnComplete(() => scrambleTimerTextUI.DOColor(Color.white, 0.3f));
            }
        }
        else
        {
            btnJokerTime.transform.DOShakePosition(0.3f, new Vector3(10f, 0, 0), 20);
        }
    }

    /// <summary> Safely resets state and transitions to Main Menu. </summary>
    /// <summary> Safely resets state, forcefully closes ALL panels, and transitions to Main Menu. </summary>
    public void ReturnToMainMenu()
    {
        ReturnToMainMenu(true);
    }

    public void ReturnToMainMenu(bool saveSession)
    {
        if (returnToMenuCoroutine != null)
        {
            StopCoroutine(returnToMenuCoroutine);
            returnToMenuCoroutine = null;
        }

        // YENİ: WordScramble durumu da kayıt işlemine eklendi
        if (saveSession && (currentGameState == GameState.Playing || currentGameState == GameState.Puzzle || currentGameState == GameState.WordScramble) && currentDistrict != null)
        {
            SaveSessionProgress();
        }

        Time.timeScale = 1f;
        currentGameState = GameState.MainMenu;

        if (questionPanel != null) questionPanel.SetActive(false);
        if (puzzlePanel != null) puzzlePanel.SetActive(false); 
        // YENİ: Ana menüye dönerken kelime oyununu da zorla kapat
        if (wordScramblePanel != null) wordScramblePanel.SetActive(false); 
        if (mapPanel != null) mapPanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);
        if (unlockPopupPanel != null) unlockPopupPanel.SetActive(false);
        HideTimeOutPopup();

        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    /// <summary> Displays the final outcome panel of a quiz round. </summary>
    public void ShowResult(bool isWin)
    {
        HideTimeOutPopup();
        ClearSavedSession();
        currentGameState = GameState.Result;
        isTimerRunning = false;
        EnsureResultPanelOnCanvas();
        
        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
            resultPanel.transform.SetAsLastSibling();
            
            resultPanel.transform.localScale = Vector3.zero;
            resultPanel.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack);

            Image panelImage = resultPanel.GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.sprite = isWin ? successSprite : failureSprite;
            }
        }

        if (isWin)
        {
            MOST_HapticFeedback.Generate(MOST_HapticFeedback.HapticTypes.Success);
            if (sfxSource != null && quizCompleteSound != null)
            {
                sfxSource.PlayOneShot(quizCompleteSound); 
            }
        }
        else
        {
            MOST_HapticFeedback.Generate(MOST_HapticFeedback.HapticTypes.Failure);
        }
    }

    /// <summary> Closes the result panel safely. </summary>
    public void CloseResultPanel()
    {
        MOST_HapticFeedback.Generate(MOST_HapticFeedback.HapticTypes.Selection);

        if (returnToMenuCoroutine != null)
        {
            StopCoroutine(returnToMenuCoroutine);
            returnToMenuCoroutine = null;
        }

        resultPanel.transform.DOScale(0f, 0.3f).SetEase(Ease.InBack).OnComplete(() => {
            resultPanel.SetActive(false);
            ReturnToMainMenu();
        });
    }
    /// <summary> Soru panelini yavaşça karartır (Fade Out), Puzzle panelini aydınlatarak (Fade In) ekrana getirir. </summary>
    public void ShowPuzzlePanel()
    {
        currentGameState = GameState.Puzzle;
        
        if (questionPanel != null && puzzlePanel != null) 
        {
            questionPanel.SetActive(true);
            puzzlePanel.SetActive(true);

            // CanvasGroup bileşenlerini al (Yoksa kodla otomatik ekle)
            CanvasGroup qGroup = GetOrAddCanvasGroup(questionPanel);
            CanvasGroup pGroup = GetOrAddCanvasGroup(puzzlePanel);

            // Başlangıç değerleri (Soru tam görünür, Puzzle tamamen şeffaf)
            qGroup.alpha = 1f;
            pGroup.alpha = 0f;

            // Panellerin merkezde olduğundan emin ol (Önceki kaydırma kodundan kalma bozuklukları düzeltmek için)
            questionPanel.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            puzzlePanel.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

            // 1. Soru Paneli Fade Out (Yok Olma)
            qGroup.DOFade(0f, 0.4f).OnComplete(() => 
            {
                questionPanel.SetActive(false); 
                qGroup.alpha = 1f; // Sonraki kullanımlar için Soru panelinin görünürlüğünü sıfırla
            });
            
            // 2. Puzzle Paneli Fade In (Belirme)
            pGroup.DOFade(1f, 0.4f);

            if (puzzleLivesTextUI != null)
            {
                puzzleLivesTextUI.text = currentLives.ToString();
            }

            // Initialize timer for puzzle
            currentTime = timePerQuestion;
            if (puzzleTimerTextUI != null) puzzleTimerTextUI.text = currentTime.ToString();
            if (puzzleTimerFillImage != null) puzzleTimerFillImage.fillAmount = 1f;
            isTimerRunning = true;
            
            MOST_HapticFeedback.Generate(MOST_HapticFeedback.HapticTypes.MediumImpact);
        }
    }

    /// <summary> Puzzle panelini yavaşça karartır, Soru panelini aydınlatarak geri getirir. </summary>
    public void HidePuzzlePanel()
    {
        currentGameState = GameState.Playing;
        isTimerRunning = false;
        
        if (questionPanel != null && puzzlePanel != null) 
        {
            questionPanel.SetActive(true);
            puzzlePanel.SetActive(true);
            
            CanvasGroup qGroup = GetOrAddCanvasGroup(questionPanel);
            CanvasGroup pGroup = GetOrAddCanvasGroup(puzzlePanel);

            // Başlangıç değerleri (Puzzle tam görünür, Soru tamamen şeffaf)
            pGroup.alpha = 1f;
            qGroup.alpha = 0f;

            // 1. Puzzle Paneli Fade Out
            pGroup.DOFade(0f, 0.4f).OnComplete(() => 
            {
                puzzlePanel.SetActive(false); 
                pGroup.alpha = 1f; // Sonraki kullanımlar için Puzzle panelinin görünürlüğünü sıfırla
            });
            
            // 2. Soru Paneli Fade In
            qGroup.DOFade(1f, 0.4f);

            LoadQuestion();
        }
    }

    /// <summary> Obje üzerinde CanvasGroup yoksa otomatik ekler, Unity Editöründe unutulma ihtimaline karşı koruma sağlar. </summary>
    private CanvasGroup GetOrAddCanvasGroup(GameObject targetObj)
    {
        CanvasGroup group = targetObj.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = targetObj.AddComponent<CanvasGroup>();
        }
        return group;
    }
    private IEnumerator FlickerPanelsForDataLoad()
    {
        // 1. Paneli aç (İçindeki AvatarManager'ın OnEnable metodu anında tetiklenir ve veriyi çeker)
        avatarPanelToFlicker.SetActive(true);

        // 2. Unity'nin arka planda işlemleri yapması için tam 1 kare (frame) bekle
        yield return null;

        // 3. Oyuncu görmeden paneli hemen geri kapat
        avatarPanelToFlicker.SetActive(false);
    }
    /// <summary> Fisher-Yates algoritması ile soru listesini rastgele karıştırır. </summary>
    /// <summary> Seed mantığı ile soruları karıştırır. Kayıtlı oyunda listeyi bozmaz. </summary>
    /// <summary> Soruları tipine göre ayırır, karıştırır ve özel bir sırayla (her 5 soruda 1 kelime) birleştirir. </summary>
    private void PrepareAndShuffleQuestions(List<QuestionData> rawQuestions, bool isNewGame)
    {
        int seed;
        if (isNewGame)
        {
            seed = (int)System.DateTime.Now.Ticks; 
            PlayerPrefs.SetInt("ShuffleSeed", seed);
        }
        else
        {
            seed = PlayerPrefs.GetInt("ShuffleSeed", 0); 
        }
        
        UnityEngine.Random.InitState(seed);

        List<QuestionData> standardQs = rawQuestions.FindAll(q => q.type == QuestionType.StandardQuiz);
        List<QuestionData> scrambleQs = rawQuestions.FindAll(q => q.type == QuestionType.WordScramble);

        ShuffleList(standardQs);
        ShuffleList(scrambleQs);

        activeQuestions = new List<QuestionData>();
        int stdIndex = 0;
        int scrIndex = 0;

        for (int i = 0; i < rawQuestions.Count; i++)
        {
            // 5., 10., 15., 20. ve 25. soruları kelime oyunu yap
            if ((i + 1) % 5 == 0 && scrIndex < scrambleQs.Count)
            {
                activeQuestions.Add(scrambleQs[scrIndex]);
                scrIndex++;
            }
            else if (stdIndex < standardQs.Count)
            {
                activeQuestions.Add(standardQs[stdIndex]);
                stdIndex++;
            }
            else if (scrIndex < scrambleQs.Count) 
            {
                activeQuestions.Add(scrambleQs[scrIndex]);
                scrIndex++;
            }
        }
    }

    /// <summary> Alt liste karıştırma yardımcı metodu </summary>
    private void ShuffleList(List<QuestionData> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = UnityEngine.Random.Range(i, list.Count);
            QuestionData temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
    /// <summary> Dışarıdan süreyi anında durdurmak için kullanılır. </summary>
    public void StopTimer()
    {
        isTimerRunning = false;
    }
}