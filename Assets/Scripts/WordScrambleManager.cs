using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using Solo.MOST_IN_ONE;

public class WordScrambleManager : MonoBehaviour
{
    public static WordScrambleManager Instance { get; private set; }

    [Header("UI Referansları")]
    public TextMeshProUGUI questionTextUI;
    public Transform slotsContainer;
    public Transform lettersContainer;

    [Header("Prefablar")]
    public GameObject slotPrefab; // İçinde sadece çerçeve olan boş kare obje
    public GameObject letterButtonPrefab; // Üzerinde LetterButton.cs olan obje

    private string currentAnswerWord;
    private List<Transform> activeSlots = new List<Transform>();
    private List<LetterButton> placedLetters = new List<LetterButton>();

    private void Awake() => Instance = this;

    /// <summary> UIManager tarafından JSON verisiyle çağrılır </summary>
    public void LoadWordPuzzle(QuestionData data)
    {
        CleanUp();
        currentAnswerWord = data.answerWord.ToUpper();
        questionTextUI.text = data.question;

        // 1. Cevap uzunluğu kadar boş slot (kutu) oluştur
        for (int i = 0; i < currentAnswerWord.Length; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotsContainer);
            activeSlots.Add(slotObj.transform);
        }

        // 2. Cevabı harflere böl ve karıştır (Fisher-Yates)
        List<char> scrambledChars = currentAnswerWord.ToList();
        ShuffleList(scrambledChars);

        // 3. Karışık harfleri havuza (Letters Container) diz
        foreach (char c in scrambledChars)
        {
            GameObject letterObj = Instantiate(letterButtonPrefab, lettersContainer);
            letterObj.GetComponent<LetterButton>().Init(c);
        }
    }

    /// <summary> Bir harfe tıklandığında çalışır </summary>
    public void HandleLetterSelection(LetterButton letter)
    {
        if (placedLetters.Count >= activeSlots.Count) return; // Tüm slotlar doluysa işlem yapma

        // Tıklanan butonu etkisizleştir ki bir daha tıklanmasın
        letter.button.interactable = false;

        // Sıradaki ilk boş slotu bul
        Transform targetSlot = activeSlots[placedLetters.Count];
        placedLetters.Add(letter);

        // KİLİT NOKTA: Grid Layout'un etkisinden kurtarmak için parent'ı slot yapıyoruz
        letter.transform.SetParent(targetSlot);
        
        // DOTween ile "Cuk" oturma animasyonu
        letter.transform.DOLocalMove(Vector3.zero, 0.25f).SetEase(Ease.OutBack);
        letter.transform.DOScale(new Vector3(1.1f, 1.1f, 1f), 0.15f).OnComplete(() =>
        {
            letter.transform.DOScale(Vector3.one, 0.1f);
            
            // Eğer son harf de yerleştiyse kelimeyi kontrol et
            if (placedLetters.Count == activeSlots.Count)
            {
                CheckWinCondition();
            }
        });
    }

    private void CheckWinCondition()
    {
        string playerWord = "";
        foreach (var letter in placedLetters)
        {
            playerWord += letter.CharValue;
        }

        if (playerWord == currentAnswerWord)
        {
            // YENİ: Başarı durumunda süreyi anında durdur!
            if (UIManager.Instance != null) UIManager.Instance.StopTimer();

            MOST_HapticFeedback.Generate(MOST_HapticFeedback.HapticTypes.Success);
            slotsContainer.DOPunchScale(new Vector3(0.1f, 0.1f, 0), 0.5f, 5, 1);
            
            StartCoroutine(WaitAndComplete());
        }
        else
        {
            MOST_HapticFeedback.Generate(MOST_HapticFeedback.HapticTypes.Failure);
            slotsContainer.DOShakePosition(0.4f, new Vector3(15f, 0, 0), 20).OnComplete(ResetPlacedLetters);
        }
    }

    private void ResetPlacedLetters()
    {
        foreach (var letter in placedLetters)
        {
            letter.ReturnToPool();
        }
        placedLetters.Clear();
    }

    private IEnumerator WaitAndComplete()
    {
        yield return new WaitForSeconds(1.5f);
        UIManager.Instance.OnWordScrambleCompleted();
    }

    private void CleanUp()
    {
        foreach (Transform child in slotsContainer) Destroy(child.gameObject);
        foreach (Transform child in lettersContainer) Destroy(child.gameObject);
        activeSlots.Clear();
        placedLetters.Clear();
    }

    private void ShuffleList(List<char> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rnd = UnityEngine.Random.Range(i, list.Count);
            char temp = list[i];
            list[i] = list[rnd];
            list[rnd] = temp;
        }
    }
}