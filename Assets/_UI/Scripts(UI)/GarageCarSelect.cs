using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class GarageCarSelect : MonoBehaviour
{
    [System.Serializable]
    public class PlayerData
    {
        public string Nickname;
        public int SelectedCarIndex;
        public string SelectedMapLevel;
        public int Score;
        public string Timestamp;
    }
    
    [System.Serializable]
    private class PlayerDataList
    {
        public List<PlayerData> Records = new List<PlayerData>();
    }

    [System.Serializable]
    public class CarData
    {
        public string Name;
        public float TopSpeed, Acceleration, Handling;

    }

    [Header("References")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private Transform targetObject;
    [SerializeField] private CinemachineCamera cinemachineCamera;
    [SerializeField] private List<CarData> carDataList;
    [SerializeField] private Vector3[] carPositions;

    private VisualElement buttonHolder, infoPanel, nicknameDialog;
    private Label nameLabel, speedLabel, accelLabel, handlingLabel;
    private TextField nicknameInput;

    private int currentCarIndex = -1;
    private bool isAnimating = false;

    private PlayerData playerData;
    private string dataPath;

    void Awake()
    {
        string saveDir = Path.Combine(Application.dataPath, "_Save");
        if (!Directory.Exists(saveDir))
        {
            Directory.CreateDirectory(saveDir);
        };
        dataPath = Path.Combine(saveDir, "playerData.json");

        if (!File.Exists(dataPath))
        { 
        File.WriteAllText(dataPath, JsonUtility.ToJson(new PlayerDataList(), true));
        };
            
        var json = File.ReadAllText(dataPath);
        var list = JsonUtility.FromJson<PlayerDataList>(json);
        if (list.Records != null && list.Records.Count > 0)
            playerData = list.Records[list.Records.Count - 1];
        else
            playerData = new PlayerData();

    }

    void OnEnable()
    {
        var root = uiDocument.rootVisualElement;

        buttonHolder    = root.Q<VisualElement>("ButtonHolder");

        infoPanel       = root.Q<VisualElement>("InfoPanel");
        nameLabel       = root.Q<Label>("CarNameLabel");
        speedLabel      = root.Q<Label>("SpeedValue");
        accelLabel      = root.Q<Label>("AccelValue");
        handlingLabel   = root.Q<Label>("HandlingValue");

        nicknameDialog  = root.Q<VisualElement>("NicknameDialog");
        nicknameInput   = root.Q<TextField>("NicknameInput");

        for (int i = 0; i < carPositions.Length && i < carDataList.Count; i++)
        {
            int idx = i;
            var btn = root.Q<Button>($"SelectCar{idx+1}Button");
            btn.clicked += () => SelectCar(idx);
        }

        var confirmButton = root.Q<Button>("ConfirmButton");
        confirmButton.clicked += () =>
        {
            buttonHolder.style.display = DisplayStyle.None;
            infoPanel.style.display = DisplayStyle.None;
            nicknameDialog.style.display = DisplayStyle.Flex;
            nicknameInput.Focus();
        };

        root.Q<Button>("ConfirmNicknameButton").clicked += () =>
        {
            string nick = nicknameInput.value.Trim();
            if (string.IsNullOrEmpty(nick)) { Debug.LogWarning("닉네임을 입력해주세요!"); return; }

            var dataList = JsonUtility.FromJson<PlayerDataList>(File.ReadAllText(dataPath));
            var newRecord = new PlayerData
            {
                Nickname = nick,
                SelectedCarIndex = currentCarIndex,
                SelectedMapLevel = "Normal",
                Score = 0,
                Timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
            dataList.Records.Add(newRecord);
            File.WriteAllText(dataPath, JsonUtility.ToJson(dataList, true));
            Debug.Log($"Appended record: {newRecord.Nickname} at {newRecord.Timestamp}");

            nicknameInput.value = "";
            SceneManager.LoadScene("4_Racing");
        };

        var cancelBtn = root.Q<Button>("CancelNicknameButton");
        cancelBtn.clicked += () =>
        {
            buttonHolder.style.display = DisplayStyle.Flex;
            infoPanel.style.display = DisplayStyle.Flex;
            nicknameDialog.style.display = DisplayStyle.None;
        };

        if (!string.IsNullOrEmpty(playerData.Nickname))
        {
            nicknameInput.value = playerData.Nickname;
        }
    }

    void SelectCar(int index)
    {
        if (isAnimating) return;

        bool isClosed = infoPanel.style.display == DisplayStyle.None;
        bool differentCar = currentCarIndex != index;
        bool show = isClosed || differentCar;

        targetObject.position = carPositions[index];
        cinemachineCamera.LookAt = targetObject;

        currentCarIndex = index;
        
        StartCoroutine(AnimateInfoPanel(show, index));
    }

    private IEnumerator AnimateInfoPanel(bool show, int idx)
    {
        isAnimating = true;
        if (show)
        {
            var car = carDataList[idx];
            nameLabel.text = car.Name;
            speedLabel.text = $"{car.TopSpeed:0} km/h";
            accelLabel.text = $"{car.Acceleration:0.0}s";
            handlingLabel.text = $"{car.Handling:0}/10";
            infoPanel.style.display = DisplayStyle.Flex;
        }

        float start = infoPanel.resolvedStyle.left;
        float end = show ? 0 : -infoPanel.resolvedStyle.width;
        float t = 0, dur = 0.3f;
        
        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            infoPanel.style.left = Mathf.Lerp(start, end, t);
            yield return null;
        }

        infoPanel.style.left = end;

        if (!show)
        {
            infoPanel.style.display = DisplayStyle.None;
        }

        isAnimating = false;
    }
}
