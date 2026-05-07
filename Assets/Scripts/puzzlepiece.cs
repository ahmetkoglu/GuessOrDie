using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using Solo.MOST_IN_ONE;
using System;

/// <summary> Handles individual puzzle piece logic, drag events, and broadcasts its state. </summary>
public class PuzzlePiece : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IResettable
{
    [Header("References")]
    public Transform puzzleBoard;
    public Transform puzzlePool;
    public Transform dragArea;

    // EVENTS: The piece shouts its state to anyone listening (PuzzleManager)
    public event Action<PuzzlePiece> OnPiecePickedUp;
    public event Action<PuzzlePiece> OnPiecePlacedCorrectly;
    public event Action<PuzzlePiece> OnPieceFailed;

    [field: SerializeField] public bool IsFake { get; set; } = false;
    public float snapDistance = 60f; 

    private RectTransform rect;
    private Image image;
    private Vector3 correctLocalPos; 
    private PuzzlePieceState currentState = PuzzlePieceState.Idle;

    /// <summary> Initializes core component references. </summary>
    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        image.alphaHitTestMinimumThreshold = 0.1f; 
    }

    /// <summary> Saves the initial anchored position for snapping validation. </summary>
    public void SaveStartingPosition()
    {
        correctLocalPos = transform.localPosition;
    }

    /// <summary> Triggered when the user begins dragging the piece. </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        currentState = PuzzlePieceState.Dragging;
        transform.SetParent(dragArea, true); 
        transform.SetAsLastSibling(); 
        
        transform.localScale = Vector3.one; 
        image.SetNativeSize(); 
        SetImageAlpha(0.8f);
        image.raycastTarget = false; 

        OnPiecePickedUp?.Invoke(this); // Broadcast pickup[cite: 1]
    }

    /// <summary> Updates the piece position to follow the pointer/finger. </summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
            dragArea.GetComponent<RectTransform>(), 
            eventData.position, 
            eventData.pressEventCamera, 
            out Vector3 globalMousePos))
        {
            rect.position = globalMousePos;
        }
    }

    /// <summary> Validates the placement position when the drag ends. </summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        SetImageAlpha(1f);
        image.raycastTarget = true;
        transform.SetParent(puzzleBoard, true);

        float distance = Vector3.Distance(transform.localPosition, correctLocalPos);

        if (!IsFake && distance <= snapDistance)
        {
            HandleCorrectPlacement();
        }
        else
        {
            HandleWrongPlacement();
        }
    }

    /// <summary> Animates the piece snapping into the correct slot. </summary>
    private void HandleCorrectPlacement()
    {
        currentState = PuzzlePieceState.Placed;
        DisableInteraction();

        transform.DOLocalMove(correctLocalPos, 0.3f).SetEase(Ease.OutBack);
        transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack).OnComplete(() => 
        {
            OnPiecePlacedCorrectly?.Invoke(this); // Broadcast success[cite: 1]
        });
    }

    /// <summary> Animates the piece returning to the pool upon failure. </summary>
    private void HandleWrongPlacement()
    {
        OnPieceFailed?.Invoke(this); // Broadcast failure[cite: 1]
        DisableInteraction();

        transform.DOMove(puzzlePool.position, 0.3f).SetEase(Ease.OutQuad);
        transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutQuad).OnComplete(() => 
        {
            transform.SetParent(puzzlePool, false);
            EnableInteraction(); 
        });
    }

    /// <summary> Adjusts the transparency of the piece image. </summary>
    private void SetImageAlpha(float alpha)
    {
        Color c = image.color;
        c.a = alpha;
        image.color = c;
    }

    /// <summary> Disables raycasts and script execution to freeze the piece. </summary>
    private void DisableInteraction()
    {
        enabled = false; 
        image.raycastTarget = false; 
        image.SetNativeSize();
    }

    /// <summary> Re-enables interaction capabilities. </summary>
    private void EnableInteraction()
    {
        enabled = true; 
        image.raycastTarget = true;
    }

    /// <summary> Returns the piece to its original board state and clears animations. </summary>
    public void ResetToOriginalState()
    {
        transform.DOKill(); 
        transform.SetParent(puzzleBoard, false); 
        transform.localPosition = correctLocalPos; 
        transform.localScale = Vector3.one; 
        image.SetNativeSize(); 
        
        SetImageAlpha(1f);
        currentState = PuzzlePieceState.Idle;
        DisableInteraction(); 
        IsFake = false;
    }

    /// <summary> Specifically resets a fake piece back to the pool silently. </summary>
    public void ResetFake()
    {
        transform.DOKill();
        DisableInteraction();
    }
}