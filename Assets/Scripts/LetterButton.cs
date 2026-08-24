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
    [HideInInspector] public bool IsPlaced;

    public void Init(char c)
    {
        CharValue = c;
        letterText.text = c.ToString();
        originalParent = transform.parent;
        IsPlaced = false;
        
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
        IsPlaced = false;
        button.interactable = true;
        transform.DOKill();
        transform.SetParent(originalParent);
        
        // Havuza döndüğünde Grid Layout'a uyum sağlaması için stretch ayarlarını koruyoruz
        RectTransform rt = GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack);
    }
}