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
    public class CarData
    {
        public string Name;
        public float TopSpeed;
        public float Acceleration;
        public float Handling;
    }

    [System.Serializable]
    private class PlayerData
    {
        public string nickname;
        public int selectedCarIndex;
    }

    [Header("References")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private Transform targetObject;
    [SerializeField] private CinemachineCamera cinemachineCamera;
    [SerializeField] private List<CarData> carDataList;
    [SerializeField] private Vector3[] carPositions =
    {
        new Vector3(20, 2, 40),
        new Vector3(20, 2, 0),
        new Vector3(20, 2, -40),
        new Vector3(-20, 2, 40),
        new Vector3(-20, 2, 0),
        new Vector3(-20, 2, -40)
    };


    private VisualElement infoPanel;
    private Label nameLabel, speedLabel, accelLabel, handlingLabel;
    private Button confirmCarButton;

    private VisualElement nicknameDialog;
    private TextField nicknameInput;
    private Button confirmNicknameButton, cancelNicknameButton;

    private int currentCarIndex = -1;
    private bool isAnimating = false;


    private void OnEnable()
    {
        var root = uiDocument.rootVisualElement;

        infoPanel = root.Q<VisualElement>("InfoPanel");
        nameLabel = root.Q<Label>("CarNameLabel");
        speedLabel = root.Q<Label>("SpeedValue");
        accelLabel = root.Q<Label>("AccelValue");
        handlingLabel = root.Q<Label>("HandlingValue");

        nicknameDialog = root.Q<VisualElement>("NicknameDialog");
        nicknameInput = root.Q<TextField>("NicknameInput");
        confirmNicknameButton = root.Q<Button>("ConfirmNicknameButton");
        cancelNicknameButton = root.Q<Button>("CancelButton");

        for (int i = 0; i < carPositions.Length && i < carDataList.Count; i++)
        {
            int index = i;
            var selectCarbutton = root.Q<Button>($"SelectCar{index + 1}Button");
            selectCarbutton.clicked += () => SelectCar(index);
        }

        confirmNicknameButton.clicked += ConfirmNickname;
        cancelNicknameButton.clicked += HideNicknameDialog;

        var confirmButton = root.Q<Button>("ConfirmButton");
        confirmButton.clicked += () =>
        {
            nicknameDialog.style.display = DisplayStyle.Flex;
            nicknameDialog.pickingMode = PickingMode.Position;
            nicknameInput.Focus();
        };
    }

    private void SelectCar(int index)
    {
        if (isAnimating) return;

        targetObject.position = carPositions[index];
        cinemachineCamera.LookAt = targetObject;

        bool shouldShow = infoPanel.style.display == DisplayStyle.None || currentCarIndex != index;
        StartCoroutine(AnimateInfoPanel(shouldShow, index));

        currentCarIndex = index;
    }

    private IEnumerator AnimateInfoPanel(bool show, int index)
    {
        isAnimating = true;

        if (show)
        {
            UpdateCarInfo(index);
            infoPanel.style.display = DisplayStyle.Flex;
            infoPanel.pickingMode = PickingMode.Position;
        }

        float from = infoPanel.resolvedStyle.left;
        float to = show ? 0 : -infoPanel.resolvedStyle.width;
        float t = 0f;
        const float duration = 0.3f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            infoPanel.style.left = Mathf.Lerp(from, to, t);
            yield return null;
        }

        infoPanel.style.left = to;

        if (!show)
        {
            infoPanel.style.display = DisplayStyle.None;
            infoPanel.pickingMode = PickingMode.Ignore;
        }

        isAnimating = false;
    }

    private void UpdateCarInfo(int index)
    {
        var car = carDataList[index];
        nameLabel.text = car.Name;
        speedLabel.text = $"{car.TopSpeed:0} km/h";
        accelLabel.text = $"{car.Acceleration:0.0}s";
        handlingLabel.text = $"{car.Handling:0}/10";
    }

    private void HideNicknameDialog()
    {
        nicknameDialog.style.display = DisplayStyle.None;
        nicknameDialog.pickingMode = PickingMode.Ignore;
        nicknameInput.value = "";
    }

    private void ConfirmNickname()
    {
        string nickname = nicknameInput.value.Trim();
        if (string.IsNullOrEmpty(nickname))
        {
            Debug.LogWarning("닉네임을 입력해주세요!");
            return;
        }

        SavePlayerData(nickname);
        HideNicknameDialog();
        SceneManager.LoadScene("4_Racing");
    }

    private void SavePlayerData(string nickname)
    {
        var data = new PlayerData
        {
            nickname = nickname,
            selectedCarIndex = currentCarIndex
        };

        string path = Path.Combine(Application.persistentDataPath, "playerData.json");
        File.WriteAllText(path, JsonUtility.ToJson(data, true));
        Debug.Log($"PlayerData saved to: {path}");
    }
}
