using UnityEngine;
using TMPro;
using UnityEngine.UI; // Buton renklerini değiştirmek için eklendi
using System.Collections; // Bekleme (Coroutine) işlemleri için eklendi

public class UIManager : MonoBehaviour
{
    [Header("Paneller")]
    public GameObject mainMenuPanel;
    public GameObject questionPanel;

    [Header("Soru Ekranı UI Objeleri")]
    public TextMeshProUGUI questionTextUI;
    public TextMeshProUGUI[] optionTextsUI;
    public Image[] optionButtonsImage; // Butonların arkaplan rengi için

    [Header("Renk Ayarları")]
    public Color normalColor = Color.white;
    public Color correctColor = Color.green;
    public Color wrongColor = Color.red;

    private QuestionData currentQuestion;
    private string currentDistrictId;
    private int currentQuestionIndex;
    private bool isAnswering = false; // Oyuncu butona bastıktan sonra spam yapmasını engellemek için

    public void OnPlayButtonClicked()
    {
        mainMenuPanel.SetActive(false);
        questionPanel.SetActive(true);
        currentDistrictId = "fatih";
        currentQuestionIndex = 0;
        LoadQuestion(); 
    }

    private void LoadQuestion()
    {
        isAnswering = false; // Yeni soru geldi, cevaplama kilidini aç

        if (DataManager.Instance != null && DataManager.Instance.LoadedGameData != null)
        {
            DistrictData district = DataManager.Instance.LoadedGameData.districts.Find(d => d.id == currentDistrictId);
            
            if (district != null && currentQuestionIndex < district.questions.Count)
            {
                currentQuestion = district.questions[currentQuestionIndex];
                questionTextUI.text = currentQuestion.question;

                for (int i = 0; i < optionTextsUI.Length; i++)
                {
                    if (i < currentQuestion.options.Length)
                    {
                        optionTextsUI[i].text = currentQuestion.options[i];
                        // Buton renklerini sıfırla (Eski sorudan yeşil/kırmızı kalmasın)
                        optionButtonsImage[i].color = normalColor; 
                    }
                }
            }
            else
            {
                Debug.Log("🎉 TEBRİKLER! BÖLÜM BİTTİ!");
            }
        }
    }

    public void OnOptionSelected(int selectedIndex)
    {
        // Eğer oyuncu zaten bir şıkka tıkladıysa ve bekleme süresindeysek, diğer butonlara basmasını engelle
        if (isAnswering) return; 
        
        isAnswering = true;

        if (currentQuestion != null)
        {
            if (selectedIndex == currentQuestion.answer)
            {
                // Doğru bilinen butonu yeşil yap
                optionButtonsImage[selectedIndex].color = correctColor;
                Debug.Log("✅ DOĞRU!");
                
                // 1 saniye bekleyip yeni soruya geç
                StartCoroutine(WaitAndLoadNextQuestion(1f));
            }
            else
            {
                // Yanlış bilinen butonu kırmızı yap
                optionButtonsImage[selectedIndex].color = wrongColor;
                
                // Oyuncu doğru cevabı da görsün diye doğru olan butonu yeşil yap
                optionButtonsImage[currentQuestion.answer].color = correctColor;
                
                Debug.Log("❌ YANLIŞ!");
                // Şimdilik yanlış bilince de 1.5 saniye bekleyip yeni soruya geçirelim
                StartCoroutine(WaitAndLoadNextQuestion(1.5f)); 
            }
        }
    }

    // Bekleme İşlemi (Coroutine)
    private IEnumerator WaitAndLoadNextQuestion(float waitTime)
    {
        yield return new WaitForSeconds(waitTime); // Belirtilen süre kadar bekle
        currentQuestionIndex++;
        LoadQuestion(); // Yeni soruyu yükle
    }
}