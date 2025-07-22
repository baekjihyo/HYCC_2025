using UnityEngine;
using System.IO;
using UnityEditor;

public static class NicknameManager
{
    [SerializeField]
    static string filePath = Path.Combine(Application.persistentDataPath, "nickname.json");

    [System.Serializable]
    private class NicknameData
    {
        public string nickname;
    }

    public static void SaveNickname(string nickname)
    {
        var data = new NicknameData { nickname = nickname };
        File.WriteAllText(filePath, JsonUtility.ToJson(data));
    }

    public static string LoadNickname()
    {
        if (!File.Exists(filePath)) return "";
        var json = File.ReadAllText(filePath);
        return JsonUtility.FromJson<NicknameData>(json).nickname;
    }
}
