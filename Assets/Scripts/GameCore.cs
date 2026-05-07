using System.Collections.Generic;
using System;

// ENUMS: State management without relying on booleans
public enum GameState { MainMenu, Map, Playing, Result }
public enum PuzzlePieceState { Idle, Dragging, Placed }

// INTERFACES: Contract for resettable objects
public interface IResettable
{
    /// <summary> Resets the object to its initial state. </summary>
    void ResetToOriginalState();
}

// DATA MODELS
[Serializable]
public class QuestionData 
{
    public int id;
    public string question;
    public string[] options;
    public int answer;
    public string questionImage; 
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
}

[Serializable]
public class GameDataContainer 
{
    public string game;
    public string version;
    public int total_questions;
    public List<DistrictData> districts;
}