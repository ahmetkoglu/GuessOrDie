using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Solo.MOST_IN_ONE;

public class LetterButton : MonoBehaviour
{
    public TextMeshProUGUI letterText;
    public Button button;
    
    [HideInInspector] public char CharValue;
    [HideInInspector] public Transform originalParent; // Yanlış yapınca geri dönmesi için

    public void Init(char c)
    {
        CharValue = c;
        letterText.text = c.ToString();
        originalParent = transform.parent;
        
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnLetterClicked);
    }

    private void OnLetterClicked()
    {
        MOST_HapticFeedback.Generate(MOST_HapticFeedback.HapticTypes.Selection);
        // Tıklandığında Manager'a "Ben seçildim, beni boşluğa yolla" der
        WordScrambleManager.Instance.HandleLetterSelection(this);
    }

    // Harf yanlışsa orijinal havuzuna geri dönme animasyonu
    public void ReturnToPool()
    {
        button.interactable = true;
        transform.SetParent(originalParent);
        transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack);
    }
}