using System.Collections.Generic;

[System.Serializable]
public class PlayerDataList
{
    public List<PlayerData> Records = new List<PlayerData>();
}

[System.Serializable]
public class PlayerData
{
    public int tutorialScore;
    public int selectedMapIndex;
    public int selectedCarIndex;
    public string playerNickname;
    public float RunSpeed;
    public int Fallcount;
    public string selectedAt;
}