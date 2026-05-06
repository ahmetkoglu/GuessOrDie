using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Events;
using Solo.MOST_IN_ONE;

public class PuzzleManager : MonoBehaviour
{
    [Header("Puzzle Ayarları")]
    public Transform puzzleBoard;
    public Transform puzzlePool;
    public int piecesToHide = 2; 

    [Header("Parça Listeleri")]
    public List<PuzzlePiece> allPieces;  
    public List<PuzzlePiece> fakePieces; 

    [Header("Geçiş Ayarları")]
    public UnityEvent onPuzzleCompleted; 
    public float delayBeforeQuiz = 1.5f; 

    // --- EKSİK OLAN SES DEĞİŞKENLERİ EKLENDİ ---
    [Header("Ses Efektleri")]
    public AudioSource audioSource;
    public AudioClip pickUpSound;         // Tıklama sesi
    public AudioClip snapSound;           // Yerine oturma sesi
    public AudioClip errorSound;          // Hata sesi
    public AudioClip puzzleCompleteSound; // Bitiş sesi
    // -------------------------------------------
    private bool isInitialized = false;

    private int placedPieces = 0;

    public void RestartPuzzle()
    {
        // Butona basıldığında zorla başlat
        StartCoroutine(PreparePuzzle());
    }

    private IEnumerator PreparePuzzle()
    {
        // Unity'nin ekranı boyutlandırması için 1 kare bekle
        yield return null;

        if (!isInitialized)
        {
            // OYUN İLK KEZ AÇILIYOR: Kusursuz yerleri ezberle
            foreach (var piece in allPieces)
            {
                piece.SaveStartingPosition();
            }
            isInitialized = true;
        }
        else
        {
            // OYUN TEKRAR OYNANIYOR: Her şeyi sıfırla ve tahtaya geri diz!
            foreach (var piece in allPieces)
            {
                piece.ResetToOriginalState();
            }
            foreach (var fake in fakePieces)
            {
                fake.ResetFake();
            }
        }

        // Yerleşen parça sayısını sıfırla ve oyunu baştan kur
        placedPieces = 0;
        SetupPuzzle();
    }
    
    void SetupPuzzle()
    {
        foreach (var piece in allPieces)
        {
            piece.GetComponent<Image>().raycastTarget = false;
            piece.enabled = false;
            piece.isFake = false; 
        }

        List<PuzzlePiece> availablePieces = new List<PuzzlePiece>(allPieces);
        
        for (int i = 0; i < piecesToHide; i++)
        {
            int randomIndex = Random.Range(0, availablePieces.Count);
            PuzzlePiece selected = availablePieces[randomIndex];
            
            availablePieces.RemoveAt(randomIndex); 

            selected.GetComponent<Image>().raycastTarget = true;
            selected.enabled = true;
            selected.transform.SetParent(puzzlePool, false);
            selected.transform.localScale = Vector3.one;
        }

        for (int i = 0; i < fakePieces.Count; i++)
        {
            int randomIndex = Random.Range(0, availablePieces.Count);
            PuzzlePiece copySource = availablePieces[randomIndex];
            
            availablePieces.RemoveAt(randomIndex); 

            Image fakeImage = fakePieces[i].GetComponent<Image>();
            fakeImage.sprite = copySource.GetComponent<Image>().sprite;
            fakeImage.SetNativeSize(); 
            
            fakeImage.raycastTarget = true;
            fakeImage.alphaHitTestMinimumThreshold = 0.1f; 
            
            fakePieces[i].isFake = true; 
            fakePieces[i].enabled = true;
            fakePieces[i].transform.SetParent(puzzlePool, false);
            fakePieces[i].transform.localScale = Vector3.one;
        }

        ShufflePool();
    }

    void ShufflePool()
    {
        int childCount = puzzlePool.childCount;
        for (int i = 0; i < childCount; i++)
        {
            puzzlePool.GetChild(i).SetSiblingIndex(Random.Range(0, childCount));
        }
    }

    // --- EKSİK OLAN SES ÇALMA FONKSİYONU EKLENDİ ---
    public void PlayPuzzleSound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    // -----------------------------------------------

    public void PiecePlaced()
    {
        placedPieces++;
        
        if (placedPieces >= piecesToHide) 
        {
            Debug.Log("Puzzle Tamamlandı! Sorulara geçiliyor...");
            
            // FİNAL SESİ VE HAPTİĞİ
            PlayPuzzleSound(puzzleCompleteSound);
            MOST_HapticFeedback.Generate(MOST_HapticFeedback.HapticTypes.HeavyImpact); 
            
            StartCoroutine(CompletePuzzleRoutine());
        }
    }

    private IEnumerator CompletePuzzleRoutine()
    {
        yield return new WaitForSeconds(delayBeforeQuiz);
        onPuzzleCompleted.Invoke();
    }
}