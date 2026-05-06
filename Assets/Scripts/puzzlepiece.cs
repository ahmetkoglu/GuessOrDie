using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using Solo.MOST_IN_ONE;

public class PuzzlePiece : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Referanslar")]
    public Transform puzzleBoard;
    public Transform puzzlePool;
    public Transform dragArea;

    private RectTransform rect;
    private Image image;
    
    private Vector3 correctLocalPos; 
    public float snapDistance = 60f; 
    public bool isFake = false;

    // YENİ: Manager'ı hafızada tutmak için
    private PuzzleManager manager;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        image.alphaHitTestMinimumThreshold = 0.1f; 
        
        // Manager'ı bul ve kaydet
        manager = FindObjectOfType<PuzzleManager>();
    }

    public void SaveStartingPosition()
    {
        correctLocalPos = transform.localPosition;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        transform.SetParent(dragArea, true); 
        transform.SetAsLastSibling(); 
        
        transform.localScale = Vector3.one; 
        image.SetNativeSize(); 
        
        Color c = image.color;
        c.a = 0.8f;
        image.color = c;
        
        image.raycastTarget = false; 

        // --- SES VE HAPTİK: Parçayı eline aldığında (Hafif bir tık) ---
        if (manager != null) manager.PlayPuzzleSound(manager.pickUpSound);
        MOST_HapticFeedback.Generate(MOST_HapticFeedback.HapticTypes.Selection);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector3 globalMousePos;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
            dragArea.GetComponent<RectTransform>(), 
            eventData.position, 
            eventData.pressEventCamera, 
            out globalMousePos))
        {
            rect.position = globalMousePos;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Color c = image.color;
        c.a = 1f;
        image.color = c;
        image.raycastTarget = true;

        transform.SetParent(puzzleBoard, true);

        float distance = Vector3.Distance(transform.localPosition, correctLocalPos);

        if (!isFake && distance <= snapDistance)
        {
            enabled = false; 
            image.raycastTarget = false; 
            image.SetNativeSize(); 

            transform.DOLocalMove(correctLocalPos, 0.3f).SetEase(Ease.OutBack);
            transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack).OnComplete(() => 
            {
                // --- SES VE HAPTİK: Başarı (Animasyon "cuk" diye bittiği an) ---
                if (manager != null) manager.PlayPuzzleSound(manager.snapSound);
                MOST_HapticFeedback.Generate(MOST_HapticFeedback.HapticTypes.Success); 
                
                manager.PiecePlaced();
            });
        }
        else
        {
            // --- SES VE HAPTİK: Hata (Parçayı yanlış yere bıraktığı saniyede) ---
            if (manager != null) manager.PlayPuzzleSound(manager.errorSound);
            MOST_HapticFeedback.Generate(MOST_HapticFeedback.HapticTypes.Failure);

            enabled = false; 
            image.raycastTarget = false; 

            // Havuza doğru süzülme animasyonu
            transform.DOMove(puzzlePool.position, 0.3f).SetEase(Ease.OutQuad);
            transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutQuad).OnComplete(() => 
            {
                transform.SetParent(puzzlePool, false);
                enabled = true; 
                image.raycastTarget = true; 
            });
        }
    }
    // YENİ: Doğru parçaları tamamen ilk haline döndürür
    public void ResetToOriginalState()
    {
        transform.DOKill(); // Varsa devam eden uçma/sekme animasyonunu anında kes
        
        transform.SetParent(puzzleBoard, false); // Tahtaya geri dön
        transform.localPosition = correctLocalPos; // Doğru koordinata ışınlan
        transform.localScale = Vector3.one; 
        
        image.SetNativeSize(); 
        
        Color c = image.color;
        c.a = 1f;
        image.color = c;
        
        image.raycastTarget = false; 
        enabled = false; 
        isFake = false;
    }

    // YENİ: Çeldiricileri sıfırlar
    public void ResetFake()
    {
        transform.DOKill();
        enabled = false;
        image.raycastTarget = false;
    }
}