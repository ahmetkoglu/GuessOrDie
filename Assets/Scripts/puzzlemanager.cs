using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Events;
using Solo.MOST_IN_ONE;
using System;

/// <summary> Manages the overall puzzle loop and audio feedback by observing pieces. </summary>
public class PuzzleManager : MonoBehaviour
{
    [Header("Puzzle Settings")]
    public Transform puzzleBoard;
    public Transform puzzlePool;
    public int piecesToHide = 2; 

    [Header("Piece Lists")]
    public List<PuzzlePiece> allPieces;  
    public List<PuzzlePiece> fakePieces; 

    [Header("Events")]
    public UnityEvent onPuzzleCompleted; 
    public float delayBeforeQuiz = 1.5f; 

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip pickUpSound;
    public AudioClip snapSound;
    public AudioClip errorSound;
    public AudioClip puzzleCompleteSound;

    private bool isInitialized = false;
    private int placedPieces = 0;

    /// <summary> Subscribes to piece events on initialization. </summary>
    private void Awake()
    {
        foreach (var piece in allPieces)
        {
            piece.OnPiecePickedUp += HandlePiecePickedUp;
            piece.OnPieceFailed += HandlePieceFailed;
            piece.OnPiecePlacedCorrectly += HandlePiecePlaced;
        }
    }

    /// <summary> Unsubscribes to prevent memory leaks when destroyed. </summary>
    private void OnDestroy()
    {
        foreach (var piece in allPieces)
        {
            piece.OnPiecePickedUp -= HandlePiecePickedUp;
            piece.OnPieceFailed -= HandlePieceFailed;
            piece.OnPiecePlacedCorrectly -= HandlePiecePlaced;
        }
    }

    /// <summary> Event Handler: Plays pickup sound/haptic. </summary>
    private void HandlePiecePickedUp(PuzzlePiece piece) => PlaySound(pickUpSound, MOST_HapticFeedback.HapticTypes.Selection);

    /// <summary> Event Handler: Plays failure sound/haptic. </summary>
    private void HandlePieceFailed(PuzzlePiece piece) => PlaySound(errorSound, MOST_HapticFeedback.HapticTypes.Failure);

    /// <summary> Event Handler: Tracks win condition and plays success feedback. </summary>
    private void HandlePiecePlaced(PuzzlePiece piece)
    {
        PlaySound(snapSound, MOST_HapticFeedback.HapticTypes.Success);
        placedPieces++;
        
        if (placedPieces >= piecesToHide) 
        {
            Debug.Log("Puzzle Completed! Transitioning...");
            PlaySound(puzzleCompleteSound, MOST_HapticFeedback.HapticTypes.HeavyImpact); 
            StartCoroutine(CompletePuzzleRoutine());
        }
    }

    /// <summary> Restarts the puzzle forcefully. </summary>
    public void RestartPuzzle()
    {
        StartCoroutine(PreparePuzzleRoutine());
    }

    /// <summary> Coroutine to handle screen layout resets before shuffling pieces. </summary>
    private IEnumerator PreparePuzzleRoutine()
    {
        yield return null;

        if (!isInitialized)
        {
            allPieces.ForEach(p => p.SaveStartingPosition());
            isInitialized = true;
        }
        else
        {
            allPieces.ForEach(p => p.ResetToOriginalState());
            fakePieces.ForEach(f => f.ResetFake());
        }

        placedPieces = 0;
        SetupPuzzle();
    }
    
    /// <summary> Selects random real and fake pieces to distribute into the pool. </summary>
    private void SetupPuzzle()
    {
        allPieces.ForEach(p => { p.GetComponent<Image>().raycastTarget = false; p.enabled = false; p.IsFake = false; });

        List<PuzzlePiece> availablePieces = new List<PuzzlePiece>(allPieces);
        
        for (int i = 0; i < piecesToHide; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, availablePieces.Count);
            PuzzlePiece selected = availablePieces[randomIndex];
            availablePieces.RemoveAt(randomIndex); 
            PreparePieceForPool(selected);
        }

        foreach (var fake in fakePieces)
        {
            int randomIndex = UnityEngine.Random.Range(0, availablePieces.Count);
            PuzzlePiece copySource = availablePieces[randomIndex];
            availablePieces.RemoveAt(randomIndex); 

            Image fakeImage = fake.GetComponent<Image>();
            fakeImage.sprite = copySource.GetComponent<Image>().sprite;
            fakeImage.SetNativeSize(); 
            fake.IsFake = true; 
            PreparePieceForPool(fake);
        }

        ShufflePool();
    }

    /// <summary> Configures a specific piece's properties before moving it to the UI pool. </summary>
    private void PreparePieceForPool(PuzzlePiece piece)
    {
        Image img = piece.GetComponent<Image>();
        img.raycastTarget = true;
        img.alphaHitTestMinimumThreshold = 0.1f;
        piece.enabled = true;
        piece.transform.SetParent(puzzlePool, false);
        piece.transform.localScale = Vector3.one;
    }

    /// <summary> Randomizes the hierarchy order of pieces in the pool layout. </summary>
    private void ShufflePool()
    {
        int childCount = puzzlePool.childCount;
        for (int i = 0; i < childCount; i++)
        {
            puzzlePool.GetChild(i).SetSiblingIndex(UnityEngine.Random.Range(0, childCount));
        }
    }

    /// <summary> Helper function to play sound and trigger haptics. </summary>
    public void PlaySound(AudioClip clip, MOST_HapticFeedback.HapticTypes hapticType)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
        MOST_HapticFeedback.Generate(hapticType);
    }

    /// <summary> Delays the transition to the quiz portion after a win. </summary>
    private IEnumerator CompletePuzzleRoutine()
    {
        yield return new WaitForSeconds(delayBeforeQuiz);
        onPuzzleCompleted?.Invoke();
    }
}