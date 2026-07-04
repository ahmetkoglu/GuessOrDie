using System.Collections.Generic;
using System;

// YENİ: Soru tiplerini birbirinden ayırmak için eklenen Enum
public enum QuestionType { StandardQuiz, WordScramble }

// YENİ: GameState içine "WordScramble" paneli durumu eklendi
public enum GameState { MainMenu, Map, Playing, Result, Puzzle, WordScramble } 
public enum PuzzlePieceState { Idle, Dragging, Placed }

public interface IResettable
{
    void ResetToOriginalState();
}

[Serializable]
public class QuestionData 
{
    // YENİ: JSON'dan okunacak soru tipi. (JSON'da yazılmazsa otomatik StandardQuiz sayar)
    public QuestionType type = QuestionType.StandardQuiz; 
    
    public int id;
    public string question;
    
    // Sadece Quiz İçin:
    public string[] options;
    public int answer;
    public string questionImage; 
    
    // YENİ - Sadece Kelime Oyunu İçin:
    public string answerWord; // Örn: "ÇAMLICA"
}

// Hangi sorudan sonra hangi prefabın yükleneceğini tutar
[Serializable]
public class PuzzleTriggerData 
{
    public int triggerAfterQuestionIndex; // Örn: 5 (5. soruyu bitirince çıkar)
    public string puzzlePrefabName;       // Örn: "fatih_puzzle_1"
}

[Serializable]
public class DistrictData 
{
    public string id;
    public string name;
    public string description;
    public int unlockCost;
    public bool is_unlocked;
    public List<QuestionData> questions;
    public List<string> penalty_animations;
    public List<PuzzleTriggerData> puzzles; 
}

[Serializable]
public class GameDataContainer 
{
    public string game;
    public string version;
    public int total_questions;
    public List<DistrictData> districts;
}