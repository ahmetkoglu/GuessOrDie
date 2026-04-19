using System.Collections.Generic;

[System.Serializable]
public class QuestionData 
{
    public int id;
    public string question;
    public string[] options;
    public int answer;
    public string questionImage; // YENİ: JSON'daki görsel adını tutacak
}

[System.Serializable]
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

[System.Serializable]
public class GameDataContainer 
{
    public string game;
    public string version;
    public int total_questions;
    public List<DistrictData> districts;
}