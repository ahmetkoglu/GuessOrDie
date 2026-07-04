using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using DG.Tweening;

[RequireComponent(typeof(CanvasGroup))]
public class PuzzlePiece : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Action<PuzzlePiece> OnPiecePickedUp;
    public Action<PuzzlePiece> OnPiecePlacedCorrectly;
    public Action<PuzzlePiece> OnPieceFailed;

    public bool IsFake { get; set; } = false;
    private bool isPlaced = false;
    
    private Vector3 originalLocalPosition; // YENİ: Çapalardan bağımsız, kesin lokal pozisyon
    private Vector2 originalSize; 
    private Transform originalParent; 
    private Vector3 dragOffset; // YENİ: Farenin objeyi tam tuttuğu noktayı hafızada tutar
    
    private float snapDistance = 150f; 

    [HideInInspector] public RectTransform dragArea; 
    [HideInInspector] public Transform poolArea;     

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    public void InitPiece()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

        originalParent = transform.parent;
        
        // KİLİT 1: Unity'nin kafa karıştırıcı Anchor pozisyonunu değil, GERÇEK 3D lokal pozisyonu alıyoruz.
        originalLocalPosition = transform.localPosition; 
        originalSize = rectTransform.sizeDelta; 
        
        transform.localScale = Vector3.one;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isPlaced) return; 

        OnPiecePickedUp?.Invoke(this);
        
        transform.SetParent(dragArea, true); 
        
        // KİLİT 2: Sürüklerken boyutu zorla eski büyük haline getir ve büyümesini engelle
        transform.localScale = Vector3.one; 
        rectTransform.sizeDelta = originalSize; 
        
        canvasGroup.blocksRaycasts = false;  
        // KİLİT NOKTA: Obje orijinal büyük boyutuna döndükten HEMEN SONRA farenin 
        // objenin neresinde kaldığını hesaplayıp hafızaya alıyoruz.
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
            dragArea, 
            eventData.position, 
            eventData.pressEventCamera, 
            out Vector3 globalMousePos))
        {
            dragOffset = rectTransform.position - globalMousePos;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isPlaced) return;

        // KİLİT 3: Senin eski kodundaki kurşungeçirmez Dünya Koordinatı sistemi! 
        // Fare neredeyse parçanın merkezini milimetrik olarak oraya taşır, UI kaymalarını sıfırlar.
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
            dragArea, 
            eventData.position, 
            eventData.pressEventCamera, 
            out Vector3 globalMousePos))
        {
            rectTransform.position = globalMousePos + dragOffset;
        }
    }
public void OnEndDrag(PointerEventData eventData)
    {
        if (isPlaced) return;
        canvasGroup.blocksRaycasts = true; 

        // Önce kendi asıl Prefab'ının içine geri al
        if (originalParent != null)
        {
            transform.SetParent(originalParent, true); 
        }

        // Bırakınca boyutların bozulmasını tekrar kilitle
        transform.localScale = Vector3.one; 
        rectTransform.sizeDelta = originalSize; 

        // KİLİT 4: Mesafeyi Anchor'lar ile değil, GERÇEK lokal pozisyonlar ile ölç!
        float distanceToTarget = Vector3.Distance(transform.localPosition, originalLocalPosition);

        if (!IsFake && distanceToTarget <= snapDistance)
        {
            // DOĞRU YER: Parçayı kilitliyoruz ki animasyon sırasında oyuncu tekrar tutamasın
            isPlaced = true;
            
            // --- YENİ EKLENEN DOTWEEN ANİMASYONU ---
            rectTransform.DOKill(); 
            Sequence snapSeq = DOTween.Sequence();

            // 1. Orijinal lokal pozisyonuna "OutBack" (hafif yaylanarak) gitsin
            snapSeq.Append(transform.DOLocalMove(originalLocalPosition, 0.25f).SetEase(Ease.OutBack, 1.5f));

            // 2. Yuvaya otururken aynı anda %15 şişsin (Büyüsün)
            snapSeq.Join(transform.DOScale(new Vector3(1.15f, 1.15f, 1f), 0.15f).SetEase(Ease.OutQuad));

            // 3. Vurma hissi için kendi boyutuna (Vector3.one) geri dönsün
            snapSeq.Append(transform.DOScale(Vector3.one, 0.1f).SetEase(Ease.InQuad));
            // ----------------------------------------

            OnPiecePlacedCorrectly?.Invoke(this);
        }
        else
        {
            // YANLIŞ YER: Havuza geri gönder
            transform.SetParent(poolArea, false);
            transform.localScale = Vector3.one; 
            OnPieceFailed?.Invoke(this);
        }
    }
    // Tahtada baştan beri var olan parçaları sürüklenmeye karşı kilitler
    public void LockPiece()
    {
        isPlaced = true;
        if (canvasGroup != null) canvasGroup.blocksRaycasts = false;
    }

    // Sahte parçalar kilitli objelerden kopyalandığı için onların kilidini açar
    public void UnlockFakePiece()
    {
        isPlaced = false;
        if (canvasGroup != null) canvasGroup.blocksRaycasts = true;
    }
}