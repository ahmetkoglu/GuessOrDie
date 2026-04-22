using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening; // YENİ: DOTween kütüphanesini ekledik

public class UIManager : MonoBehaviour
{
    [Header("Animasyon Hafızası")]
    private Vector2[] originalOptionPositions;
    [Header("Joker Sistemi")]
    public int jokerCost = 100; // Her bir jokerin fiyatı
    public Button btnJoker50;
    public Button btnJokerTime;
    private bool isFiftyFiftyUsedThisQuestion = false; // Bir soruda iki kere 50/50 basılmasını engeller
    [Header("Animasyonlu Butonlar")]
    public Transform mainPlayButtonTransform;
    [Header("Harita Yöneticisi")]
    public MapManager mapManager; // Kilit açılınca haritayı yenilemek için
    [Header("Pop-up Sistemi")]
    public GameObject unlockPopupPanel;
    public TextMeshProUGUI popupMessageText;
    private DistrictData pendingDistrictToUnlock; // Seçilen ilçeyi aklımızda tutalım
    public GameObject popupBox; // YENİ: Asıl animasyon uygulayacağımız iç kutu
    
    [Header("Coin UI")]
public TextMeshProUGUI coinTextUI; // Ekranda altını gösterecek yazı
public int rewardPerQuestion = 50;  // Her doğru cevap kaç puan?
    [Header("Paneller")]
    public GameObject mainMenuPanel;
    public GameObject questionPanel;
    public GameObject mapPanel; // Bu satırın olduğundan emin ol

    [Header("Soru Ekranı UI Objeleri")]
    public TextMeshProUGUI questionTextUI;
    public TextMeshProUGUI[] optionTextsUI;
    public TextMeshProUGUI questionCountTextUI; // YENİ EKLENEN SATIR
    public Image[] optionButtonsImage;
    public Image questionImageUI;
    
    [Header("Can Sistemi")]
    public TextMeshProUGUI livesTextUI;
    public int maxLives = 3;
    private int currentLives;

    [Header("Süre Sistemi")]
    public Image timerFillImage;     
    public TextMeshProUGUI timerTextUI; // YENİ: Süreyi rakamla yazacağımız yer
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

    void Start()
    {
        // (Eğer Start içinde başka kodların varsa onlar kalsın, bunu altlarına ekle)
        UpdateCoinDisplay();
        // Buton sayısı kadar hafıza yuvası aç
        originalOptionPositions = new Vector2[optionTextsUI.Length];
        
        // Oyun başlar başlamaz tüm butonların orijinal 'Anchored Position'larını kaydet
        for (int i = 0; i < optionTextsUI.Length; i++)
        {
            RectTransform btnRect = optionTextsUI[i].transform.parent.GetComponent<RectTransform>();
            originalOptionPositions[i] = btnRect.anchoredPosition;
        }
        
    }
    public void OnPlayButtonClicked()
    {
        // 1. Önce butona tokat (Punch) animasyonu veriyoruz: %10 küçülüp 0.2 saniyede geri yaylanacak
        mainPlayButtonTransform.DOPunchScale(new Vector3(-0.5f, -0.5f, 0), 0.2f, 10, 1).OnComplete(() =>
        {
            // 2. Bu kısımlar animasyon BİTİNCE çalışacak (0.2 saniye gecikmeli, şık bir geçiş)
            string districtToLoad = "fatih"; 
            
            if (mapManager != null && !string.IsNullOrEmpty(mapManager.selectedDistrictId))
            {
                districtToLoad = mapManager.selectedDistrictId;
            }

            Debug.Log("🚀 OYUN BAŞLIYOR: " + districtToLoad);
            mainMenuPanel.SetActive(false);
            questionPanel.SetActive(true);
            
            currentLives = maxLives; 
            UpdateLivesUI();
            
            LoadDistrict(districtToLoad); 
        });
    }

    public void LoadDistrict(string districtId)
    {
        if (DataManager.Instance != null && DataManager.Instance.LoadedGameData != null)
        {
            currentDistrict = DataManager.Instance.LoadedGameData.districts.Find(d => d.id == districtId);
            currentQuestionIndex = 0;
            LoadQuestion();
        }
    }

    private void LoadQuestion()
    {
        isAnswering = false;

        if (currentDistrict != null && currentQuestionIndex < currentDistrict.questions.Count)
        {
            currentQuestion = currentDistrict.questions[currentQuestionIndex];
            
            // --- 1. Soru Metnini Atama (Henüz göstermiyoruz, kayarak gelecek) ---
            questionTextUI.text = currentQuestion.question;

            // --- 2. Soru Sayacını Güncelle ---
            if (questionCountTextUI != null)
            {
                questionCountTextUI.text = $"Soru: {currentQuestionIndex + 1} / {currentDistrict.questions.Count}";
            }

            // --- 3. Görsel Yükleme ---
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
                    Debug.LogWarning("Resim klasörde bulunamadı: " + cleanImageName);
                    questionImageUI.gameObject.SetActive(false); 
                }
            }
            else
            {
                questionImageUI.gameObject.SetActive(false); 
            }

            // --- 4. DÜZELTİLMİŞ: SORU METNİ ANİMASYONU ---
            // Önceki animasyonu anında bitir ve yazıyı gerçek yerine oturt (Bug önleyici)
            questionTextUI.transform.DOKill(true);
            
            // Saydamlığı 0 yap
            questionTextUI.color = new Color(questionTextUI.color.r, questionTextUI.color.g, questionTextUI.color.b, 0f);

            // SİHİR BURADA: From(true) ile "Kendi yerinden 800 birim sağda başla ve gerçek yerine gel" diyoruz
            questionTextUI.transform.DOLocalMoveX(800f, 0.5f).From(true).SetEase(Ease.OutQuint);
            questionTextUI.DOFade(1f, 0.5f);

            // --- 5. DÜZELTİLMİŞ VE KUSURSUZ ŞIK ANİMASYONU ---
            for (int i = 0; i < optionTextsUI.Length; i++)
            {
                if (i < currentQuestion.options.Length)
                {
                    optionTextsUI[i].text = currentQuestion.options[i];
                    optionButtonsImage[i].color = normalColor; 
                }

                Transform btnTransform = optionTextsUI[i].transform.parent;
                RectTransform btnRect = btnTransform.GetComponent<RectTransform>();
                
                // Joker için boyut ve tıklanabilirlik sıfırlaması
                btnTransform.localScale = Vector3.one; 
                btnTransform.GetComponent<Button>().interactable = true;

                // Önceki tüm titreme/hareket animasyonlarını acımasızca durdur!
                btnTransform.DOKill(true);

                // Butonu, HAFIZADAKİ kendi orijinal yerinden 800 piksel sağa ışınla
                btnRect.anchoredPosition = new Vector2(originalOptionPositions[i].x + 800f, originalOptionPositions[i].y);

                // Ve şimdi tam olarak HAFIZADAKİ o kesin koordinata geri kaydır!
                btnRect.DOAnchorPos(originalOptionPositions[i], 0.4f).SetDelay(i * 0.1f).SetEase(Ease.OutBack);
            }

            // --- 6. Süreyi Başlat ve Ekrana İlk Değeri Yaz ---
            currentTime = timePerQuestion;
            timerTextUI.text = currentTime.ToString(); 
            isTimerRunning = true;
        }
        else
        {
            Debug.Log("🎉 TEBRİKLER! BÖLÜM BİTTİ!");
            isTimerRunning = false; 
            StartCoroutine(WaitAndReturnToMenu(2f));
        }
        
        UpdateCoinDisplay();
    }

    private void Update()
    {
        if (isTimerRunning)
        {
            currentTime -= Time.deltaTime;
            
            timerFillImage.fillAmount = currentTime / timePerQuestion;

            // YENİ: Kalan süreyi yukarı yuvarlayıp ekrana yazdırıyoruz (14.2 -> 15 görünür)
            timerTextUI.text = Mathf.CeilToInt(currentTime).ToString();

            if (currentTime <= 0)
            {
                isTimerRunning = false;
                currentTime = 0;
                timerFillImage.fillAmount = 0;
                timerTextUI.text = "0"; // Süre bitince ekranda 0 yazsın
                OnTimeRanOut(); 
            }
        }
    }

    public void OnOptionSelected(int selectedIndex)
    {
        if (isAnswering) return; 
        
        isAnswering = true;
        isTimerRunning = false; 

        if (currentQuestion != null)
        {
            if (selectedIndex == currentQuestion.answer)
            {   
                // 2. DOTWEEN ANİMASYONU (Mega Kombo)
                Image btnImage = optionButtonsImage[selectedIndex];
                Transform btnTransform = btnImage.transform; // Butonun fiziksel konumu

                Sequence correctSeq = DOTween.Sequence();
                
                // Küt diye değil, 0.2 saniyede yumuşakça senin belirlediğin 'correctColor' rengine dönsün
                correctSeq.Append(btnImage.DOColor(correctColor, 0.2f));
                
                // Aynı anda hem biraz büyüsün hem de sevinçle yukarı zıplasın
                correctSeq.Join(btnTransform.DOScale(1.1f, 0.2f));
                correctSeq.Join(btnTransform.DOPunchPosition(Vector3.up * 10f, 0.4f, 5, 0.5f));
                
                // Son olarak eski orijinal boyutuna yavaşça geri dönsün
                correctSeq.Append(btnTransform.DOScale(1f, 0.2f));
                DataManager.Instance.AddCoins(rewardPerQuestion); // PARAYI EKLE!
                UpdateCoinDisplay(); // EKRANI GÜNCELLE
                StartCoroutine(WaitAndLoadNextQuestion(1f));
            }
            else
            {
                HandleWrongAnswer(selectedIndex);
            }
        }
    }

    private void OnTimeRanOut()
    {
        isAnswering = true;
        Debug.Log("⏰ SÜRE BİTTİ!");
        
        optionButtonsImage[currentQuestion.answer].color = correctColor;
        HandleWrongAnswer(-1); 
    }

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
            Debug.Log("💀 OYUN BİTTİ!");
            StartCoroutine(WaitAndReturnToMenu(2f));
        }
        else
        {
            StartCoroutine(WaitAndLoadNextQuestion(1.5f)); 
        }
    }

    private void UpdateLivesUI()
    {
        livesTextUI.text = currentLives.ToString();
    }
    // Parayı her ekranda güncellemek için bir fonksiyon
public void UpdateCoinDisplay()
    {
        // Hedef paramız DataManager'dan (veya PlayerPrefs'ten) gelen asıl para
        int targetCoins = DataManager.Instance.totalCoins;

        // Oyun ilk açıldığında (-1 durumu) animasyon yapmadan direkt güncel parayı yazsın
        if (currentlyDisplayedCoins == -1)
        {
            currentlyDisplayedCoins = targetCoins;
            // Altın yazısını gösteren TextMeshPro objenin adı neyse buraya onu yaz (örneğin coinTextUI)
            coinTextUI.text = currentlyDisplayedCoins.ToString();
            return;
        }

        // --- DÖNEN SAYILAR (ROLLING COUNTER) ANİMASYONU ---
        // 1 saniye içinde ekrandaki sayıyı, hedef sayıya doğru hızla saydırır (Ease.OutExpo ile yavaşlayarak durur)
        DOTween.To(() => currentlyDisplayedCoins, x =>
        {
            currentlyDisplayedCoins = x;
            coinTextUI.text = currentlyDisplayedCoins.ToString();
        }, targetCoins, 1f).SetEase(Ease.OutExpo);

        // --- EKSTRA JUICINESS: YAZININ KALP GİBİ ATARAK BÜYÜYÜP KÜÇÜLMESİ ---
        // Altın yazısının transformunu hafifçe şişirip geri bırakıyoruz
        coinTextUI.transform.DOPunchScale(new Vector3(0.2f, 0.2f, 0), 0.5f, 5, 1);
    }

    private IEnumerator WaitAndLoadNextQuestion(float waitTime)
{
    yield return new WaitForSeconds(waitTime);
    if (currentLives > 0)
    {
        currentQuestionIndex++;
        
        // --- YÖNTEM 2 BURADA ÇALIŞIR ---
        if (btnJoker50 != null) btnJoker50.interactable = true;
        if (btnJokerTime != null) btnJokerTime.interactable = true;
        // (Eğer boolean değişkenlerin varsa onları da burada false yapabilirsin)
        isFiftyFiftyUsedThisQuestion = false;
        
        
        LoadQuestion(); 
    }
}
    private IEnumerator WaitAndReturnToMenu(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        questionPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }
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
            
            // --- YENİ: ANİMASYONLU AÇILIŞ ---
            unlockPopupPanel.SetActive(true); // Arka planı aç
            popupBox.transform.localScale = Vector3.zero; // Kutuyu önce 0'a küçült
            popupBox.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack); // 0.4 saniyede yaylanarak 1 boyutuna getir
        }
    }

    
    public void OnMapButtonClicked()
    {
        Debug.Log("🗺️ Harita açılıyor...");
        mainMenuPanel.SetActive(false); // Ana menüyü kapat
        mapPanel.SetActive(true);       // Harita panelini aç
    }
    public void ConfirmUnlock()
    {
        if (pendingDistrictToUnlock != null)
        {
            if (DataManager.Instance.totalCoins >= pendingDistrictToUnlock.unlockCost)
            {
                DataManager.Instance.totalCoins -= pendingDistrictToUnlock.unlockCost;
                pendingDistrictToUnlock.is_unlocked = true;
                DataManager.Instance.SaveProgress();
                UpdateCoinDisplay(); 
                
                if (mapManager != null) mapManager.RefreshMap();

                // --- YENİ: ANİMASYONLU KAPANIŞ ---
                popupBox.transform.DOScale(0f, 0.2f).SetEase(Ease.InBack).OnComplete(() => 
                {
                    unlockPopupPanel.SetActive(false); // Animasyon bitince paneli tamamen kapat
                });
            }
            else
            {
                // Parası yetmiyorsa kutuyu hafifçe sars (Shake efekti)
                popupMessageText.text = "Yetersiz bakiye!";
                popupBox.transform.DOShakePosition(0.3f, new Vector3(15f, 0, 0), 10, 0, false, true);
            }
        }
    }

    public void CancelUnlock()
    {
        // --- YENİ: ANİMASYONLU KAPANIŞ ---
        popupBox.transform.DOScale(0f, 0.2f).SetEase(Ease.InBack).OnComplete(() => 
        {
            unlockPopupPanel.SetActive(false);
            pendingDistrictToUnlock = null;
        });
    }    public void OnBackToMenuClicked()
    {
        mapPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }
    public void UseFiftyFiftyJoker()
    {
        // Eğer zaten basıldıysa veya soru çözülüyorsa hiçbir şey yapma
        if (isFiftyFiftyUsedThisQuestion || isAnswering) return; 

        if (DataManager.Instance.totalCoins >= jokerCost)
        {
            // Parayı kes ve altın animasyonunu tetikle
            DataManager.Instance.totalCoins -= jokerCost;
            UpdateCoinDisplay();
            isFiftyFiftyUsedThisQuestion = true;

            // Jokere basılma animasyonu (İçine çöküp geri yaylanma)
            btnJoker50.transform.DOPunchScale(new Vector3(-0.2f, -0.2f, 0), 0.3f, 10, 1);

            // Yanlış olan 2 şıkkı bul ve listeye ekle
            System.Collections.Generic.List<int> wrongOptions = new System.Collections.Generic.List<int>();
            for (int i = 0; i < currentQuestion.options.Length; i++)
            {
                if (i != currentQuestion.answer) wrongOptions.Add(i);
            }

            // Yanlış şıkları karıştır (Rastgele 2 tanesini seçeceğiz)
            for (int i = 0; i < wrongOptions.Count; i++)
            {
                int temp = wrongOptions[i];
                int randomIndex = UnityEngine.Random.Range(i, wrongOptions.Count);
                wrongOptions[i] = wrongOptions[randomIndex];
                wrongOptions[randomIndex] = temp;
            }

            // Seçilen 2 yanlış şıkkı animasyonla yok et!
            for (int i = 0; i < 2; i++)
            {
                int indexToHide = wrongOptions[i];
                Transform btnTrans = optionTextsUI[indexToHide].transform.parent;
                
                // Tıklanmasını engelle
                btnTrans.GetComponent<Button>().interactable = false;
                
                // DOTween ile "İçine çökerek kaybolma" (InBack) animasyonu
                btnTrans.DOScale(Vector3.zero, 0.4f).SetEase(Ease.InBack);
            }
        }
        else
        {
            // Para yetmiyorsa joker butonu kafa sallasın (Shake)
            btnJoker50.transform.DOShakePosition(0.3f, new Vector3(10f, 0, 0), 20);
        }
    }

    public void UseTimeJoker()
    {
        if (isAnswering) return;

        if (DataManager.Instance.totalCoins >= jokerCost)
        {
            DataManager.Instance.totalCoins -= jokerCost;
            UpdateCoinDisplay();

            // Jokere basılma animasyonu
            btnJokerTime.transform.DOPunchScale(new Vector3(-0.2f, -0.2f, 0), 0.3f, 10, 1);

            // Süreyi ekle
            currentTime += 15f;

            // SÜRE YAZISI ANİMASYONU: Zıplasın ve 0.3 saniyeliğine Yeşil olup geri beyaz olsun
            timerTextUI.transform.DOPunchScale(new Vector3(0.4f, 0.4f, 0), 0.5f, 5, 1);
            timerTextUI.DOColor(Color.green, 0.15f).OnComplete(() => timerTextUI.DOColor(Color.white, 0.3f));
        }
        else
        {
            // Para yetmiyorsa titre
            btnJokerTime.transform.DOShakePosition(0.3f, new Vector3(10f, 0, 0), 20);
        }
    }
    public void ReturnToMainMenu()
{
    // 1. Eğer oyunu durdurduysan (Pause) zamanı tekrar başlat
    Time.timeScale = 1f;

    // 2. Soru panelini kapat, Ana Menü panelini aç
    // (Panel isimlerin farklıysa kendi değişkenlerinle değiştir)
    if (questionPanel != null) questionPanel.SetActive(false);
    if (mainMenuPanel != null) mainMenuPanel.SetActive(true);

    // 3. (Opsiyonel) Eğer her şeyi sıfırlayıp tertemiz dönmek istersen 
    // sahneyi baştan da yükletebilirsin:
    // SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

    Debug.Log("🏠 Ana menüye dönüldü.");
}
}