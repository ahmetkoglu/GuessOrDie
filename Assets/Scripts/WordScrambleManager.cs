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
    public UnityEngine.UI.Image questionImageUI;

    [Header("Prefablar")]
    public GameObject slotPrefab; // İçinde sadece çerçeve olan boş kare obje
    public GameObject letterButtonPrefab; // Üzerinde LetterButton.cs olan obje

    [Header("Yerleşim Ayarları")]
    [SerializeField] private int horizontalSafePadding = 40;
    [SerializeField] private float minimumBoxSize = 50f;

    private string currentAnswerWord;
    private List<Transform> activeSlots = new List<Transform>();
    private List<LetterButton> placedLetters = new List<LetterButton>();
    private bool isResolvingAnswer = false;

    private void Awake() => Instance = this;

    /// <summary> UIManager tarafından JSON verisiyle çağrılır </summary>
    public void LoadWordPuzzle(QuestionData data)
    {
        CleanUp();
        ApplyHorizontalSafePadding();

        currentAnswerWord = data.answerWord.ToUpper();
        questionTextUI.text = data.question;

        if (questionImageUI != null)
        {
            if (!string.IsNullOrEmpty(data.questionImage))
            {
                string cleanImageName = data.questionImage.Replace(".png", "");
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
        }

        Canvas.ForceUpdateCanvases();
        float boxSize = CalculateFittingBoxSize(currentAnswerWord.Length);
        ApplyLetterGridBoxSize(boxSize);

        // 1. Cevap uzunluğu kadar boş slot (kutu) oluştur
        for (int i = 0; i < currentAnswerWord.Length; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotsContainer);
            ApplySlotBoxSize(slotObj, boxSize);
            activeSlots.Add(slotObj.transform);
        }
        slotsContainer.localScale = Vector3.one;

        // 2. Cevabı harflere böl ve karıştır (Fisher-Yates)
        List<char> scrambledChars = currentAnswerWord.ToList();
        ShuffleList(scrambledChars);

        // 3. Karışık harfleri havuza (Letters Container) diz
        foreach (char c in scrambledChars)
        {
            GameObject letterObj = Instantiate(letterButtonPrefab, lettersContainer);
            
            // Havuzda (Grid) düzgün görünmesi için stretch yapıyoruz
            RectTransform rt = letterObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            letterObj.GetComponent<LetterButton>().Init(c);
        }
    }

    /// <summary> Uzun kelimelerde kutu boyutunu safe area içinde kalacak şekilde küçültür. </summary>
    private float CalculateFittingBoxSize(int letterCount)
    {
        RectTransform containerRT = slotsContainer != null ? slotsContainer.GetComponent<RectTransform>() : null;
        RectTransform prefabRT = slotPrefab != null ? slotPrefab.GetComponent<RectTransform>() : null;
        HorizontalLayoutGroup slotsLayout = slotsContainer != null ? slotsContainer.GetComponent<HorizontalLayoutGroup>() : null;

        float baseSize = GetRectWidth(prefabRT, 120f);
        float spacing = slotsLayout != null ? slotsLayout.spacing : 0f;
        float containerWidth = GetRectWidth(containerRT, 0f);
        float availableWidth = Mathf.Max(0f, containerWidth - (horizontalSafePadding * 2f));

        if (letterCount <= 0 || availableWidth <= 0f)
        {
            return baseSize;
        }

        float spacingWidth = Mathf.Max(0, letterCount - 1) * spacing;
        float fittingSize = (availableWidth - spacingWidth) / letterCount;

        // Ekrana tam sığma önceliklidir; minimum değer yalnızca yeterli alan varsa korunur.
        if (fittingSize < minimumBoxSize)
        {
            return Mathf.Max(1f, fittingSize);
        }

        return Mathf.Min(baseSize, fittingSize);
    }

    private float GetRectWidth(RectTransform rectTransform, float fallback)
    {
        if (rectTransform == null) return fallback;

        if (rectTransform.rect.width > 0f) return rectTransform.rect.width;
        if (rectTransform.sizeDelta.x > 0f) return rectTransform.sizeDelta.x;

        return fallback;
    }

    private void ApplySlotBoxSize(GameObject slotObj, float boxSize)
    {
        if (slotObj == null) return;

        RectTransform rt = slotObj.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.sizeDelta = new Vector2(boxSize, boxSize);
        }

        LayoutElement layoutElement = slotObj.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = slotObj.AddComponent<LayoutElement>();
        }

        layoutElement.minWidth = boxSize;
        layoutElement.minHeight = boxSize;
        layoutElement.preferredWidth = boxSize;
        layoutElement.preferredHeight = boxSize;
        layoutElement.flexibleWidth = 0f;
        layoutElement.flexibleHeight = 0f;
    }

    private void ApplyLetterGridBoxSize(float boxSize)
    {
        GridLayoutGroup lettersLayout = lettersContainer != null ? lettersContainer.GetComponent<GridLayoutGroup>() : null;
        if (lettersLayout != null)
        {
            lettersLayout.cellSize = new Vector2(boxSize, boxSize);
        }
    }

    /// <summary> Slot ve harf havuzlarının sol/sağ kenarlara yapışmasını engeller. </summary>
    private void ApplyHorizontalSafePadding()
    {
        int safePadding = Mathf.Max(0, horizontalSafePadding);

        HorizontalLayoutGroup slotsLayout = slotsContainer != null ? slotsContainer.GetComponent<HorizontalLayoutGroup>() : null;
        if (slotsLayout != null)
        {
            slotsLayout.padding.left = safePadding;
            slotsLayout.padding.right = safePadding;
        }

        GridLayoutGroup lettersLayout = lettersContainer != null ? lettersContainer.GetComponent<GridLayoutGroup>() : null;
        if (lettersLayout != null)
        {
            lettersLayout.padding.left = safePadding;
            lettersLayout.padding.right = safePadding;
        }
    }

    /// <summary> Bir harfe tıklandığında çalışır </summary>
    public void HandleLetterSelection(LetterButton letter)
    {
        if (letter == null || isResolvingAnswer) return;

        if (letter.IsPlaced)
        {
            TryUndoLastLetter(letter);
            return;
        }

        if (placedLetters.Count >= activeSlots.Count) return; // Tüm slotlar doluysa işlem yapma

        // Harf slota yerleştiğinde tıklanabilir kalır; böylece en son harfe tekrar tıklayıp geri alabiliriz.
        letter.button.interactable = true;
        letter.IsPlaced = true;

        // Sıradaki ilk boş slotu bul
        Transform targetSlot = activeSlots[placedLetters.Count];
        placedLetters.Add(letter);

        // KİLİT NOKTA: Grid Layout'un etkisinden kurtarmak için parent'ı slot yapıyoruz
        letter.transform.SetParent(targetSlot);
        
        // Harfin slotu tam kaplaması için RectTransform ayarlarını yapıyoruz
        RectTransform rt = letter.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;

        // DOTween ile "Cuk" oturma animasyonu
        // Not: Stretched olduğu için LocalMove(zero) zaten merkezler
        letter.transform.localScale = Vector3.zero; // Başta küçük olsun ki büyüme animasyonu görünsün
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

    /// <summary> Sadece en son yerleştirilen harfe tekrar tıklanınca onu havuza geri alır. </summary>
    private void TryUndoLastLetter(LetterButton letter)
    {
        if (placedLetters.Count == 0) return;

        LetterButton lastLetter = placedLetters[placedLetters.Count - 1];
        if (lastLetter != letter) return;

        placedLetters.RemoveAt(placedLetters.Count - 1);
        letter.ReturnToPool();
    }

    private void CheckWinCondition()
    {
        isResolvingAnswer = true;

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
        isResolvingAnswer = false;
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
        isResolvingAnswer = false;
        slotsContainer.localScale = Vector3.one;
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