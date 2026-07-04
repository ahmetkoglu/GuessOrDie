using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Events;
using Solo.MOST_IN_ONE;

public class PuzzleManager : MonoBehaviour
{
    public static PuzzleManager Instance { get; private set; }

    [Header("Alan Referansları")]
    public RectTransform puzzleBoard; 
    public RectTransform puzzlePool;  
    public RectTransform dragArea;    

    [Header("Zorluk Ayarları")]
    public int piecesToHide = 2;      
    public int fakePieceCount = 2;    

    [Header("Olaylar & Sesler")]
    public UnityEvent onPuzzleCompleted; 
    public AudioSource audioSource;
    public AudioClip pickUpSound, snapSound, errorSound, puzzleCompleteSound;

    private GameObject currentPuzzleInstance;
    private List<PuzzlePiece> activePieces = new List<PuzzlePiece>();
    private int placedCount = 0;

    private void Awake() => Instance = this;

    // QuizManager tarafından çağrılır
    public void LoadDynamicPuzzle(string prefabName)
    {
        CleanUpOldPuzzle();

        // 1. Prefab'ı Resources klasöründen bul ve sahneye (Board içine) doğur
        GameObject loadedPrefab = Resources.Load<GameObject>("Puzzles/" + prefabName);
        if (loadedPrefab == null)
        {
            Debug.LogError($"Prefab bulunamadı! Yol: Resources/Puzzles/{prefabName}");
            return;
        }

        currentPuzzleInstance = Instantiate(loadedPrefab, puzzleBoard);
        
        // 2. PREFAB KONUMUNU DÜZELT
        RectTransform instanceRect = currentPuzzleInstance.GetComponent<RectTransform>();
        
        // Sadece objeyi tam ortaya hizala. Boyutuna (sizeDelta) DOKUNMA!
        instanceRect.anchoredPosition = Vector2.zero; 
        instanceRect.localScale = Vector3.one; 

        // 3. Parçaları hazırla ve oyunu başlat
        PreparePuzzlePieces();
    }

    private void PreparePuzzlePieces()
    {
        activePieces = new List<PuzzlePiece>(currentPuzzleInstance.GetComponentsInChildren<PuzzlePiece>());
        placedCount = 0;

        foreach (var piece in activePieces)
        {
            piece.InitPiece();
            piece.dragArea = this.dragArea;
            piece.poolArea = this.puzzlePool;

            piece.OnPiecePlacedCorrectly += HandlePiecePlaced;
            piece.OnPieceFailed += HandlePieceFailed;
            piece.OnPiecePickedUp += HandlePiecePickedUp;
        }

        // 1. HAVUZA GİDECEK GERÇEK PARÇALARI SEÇ (Tahtadan Sök)
        List<PuzzlePiece> piecesLeftOnBoard = new List<PuzzlePiece>(activePieces);
        for (int i = 0; i < piecesToHide; i++)
        {
            int rnd = UnityEngine.Random.Range(0, piecesLeftOnBoard.Count);
            PuzzlePiece selected = piecesLeftOnBoard[rnd];
            piecesLeftOnBoard.RemoveAt(rnd); // Seçileni listeden çıkar ki tahtada kalmasın
            
            selected.transform.SetParent(puzzlePool, false);
        }

        // 2. TAHTADA KALAN PARÇALARI KİLİTLE (Sürüklenemesinler)
        foreach (var remainingPiece in piecesLeftOnBoard)
        {
            remainingPiece.LockPiece();
        }

        // 3. SAHTE PARÇALARI SADECE "TAHTADA KALANLARDAN" ÜRET
        // Böylece gerçek cevabın kopyası havuzda asla yer almaz.
        List<PuzzlePiece> fakeSourceCandidates = new List<PuzzlePiece>(piecesLeftOnBoard);
        for (int i = 0; i < fakePieceCount; i++)
        {
            if (fakeSourceCandidates.Count == 0) break; // Güvenlik önlemi

            int rnd = UnityEngine.Random.Range(0, fakeSourceCandidates.Count);
            PuzzlePiece sourceForFake = fakeSourceCandidates[rnd];
            
            // Aynı sahte parçadan 2 tane olmaması için seçileni aday listeden çıkar
            fakeSourceCandidates.RemoveAt(rnd); 

            // Tahtadaki kilitli parçanın klonunu havuza yarat
            GameObject fakeObj = Instantiate(sourceForFake.gameObject, puzzlePool);
            PuzzlePiece fakePiece = fakeObj.GetComponent<PuzzlePiece>();
            
            fakePiece.InitPiece(); 
            fakePiece.IsFake = true;
            fakePiece.dragArea = this.dragArea;
            fakePiece.poolArea = this.puzzlePool;
            
            // Kopya obje kilitli doğduğu için havuzda sürüklenebilmesi adına kilidini aç
            fakePiece.UnlockFakePiece(); 
            
            fakePiece.OnPieceFailed += HandlePieceFailed;
            fakePiece.OnPiecePickedUp += HandlePiecePickedUp;
        }
        
        ShufflePool();
    }
    private void HandlePiecePickedUp(PuzzlePiece piece) => PlaySound(pickUpSound, MOST_HapticFeedback.HapticTypes.Selection);
    private void HandlePieceFailed(PuzzlePiece piece) => PlaySound(errorSound, MOST_HapticFeedback.HapticTypes.Failure);
    private void HandlePiecePlaced(PuzzlePiece piece)
    {
        PlaySound(snapSound, MOST_HapticFeedback.HapticTypes.Success);
        placedCount++;
        
        if (placedCount >= piecesToHide)
        {
            PlaySound(puzzleCompleteSound, MOST_HapticFeedback.HapticTypes.HeavyImpact); 
            StartCoroutine(CompletePuzzleRoutine());
        }
    }

    private IEnumerator CompletePuzzleRoutine()
    {
        yield return new WaitForSeconds(1.5f);
        onPuzzleCompleted?.Invoke();
        // KİLİT NOKTA: UIManager'a puzzle panelini kapatıp sayacı ve oyunu devam ettirmesini söylüyoruz!
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HidePuzzlePanel();
        }
    }

    private void CleanUpOldPuzzle()
    {
        if (currentPuzzleInstance != null) Destroy(currentPuzzleInstance);
        foreach (Transform child in puzzlePool) Destroy(child.gameObject);
        activePieces.Clear();
    }

    private void ShufflePool()
    {
        int childCount = puzzlePool.childCount;
        for (int i = 0; i < childCount; i++)
            puzzlePool.GetChild(i).SetSiblingIndex(UnityEngine.Random.Range(0, childCount));
    }

    private void PlaySound(AudioClip clip, MOST_HapticFeedback.HapticTypes hapticType)
    {
        if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
        MOST_HapticFeedback.Generate(hapticType);
    }
}