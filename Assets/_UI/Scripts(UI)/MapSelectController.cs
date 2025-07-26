using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[System.Serializable]
public struct MapInfo
{
    public string mapName;
    public string mapDifficulty;
    public string mapInformation;
}

public class MapSelectController : MonoBehaviour
{
    [Header("UI Document")]
    public UIDocument uiDocument;

    [Header("Map Information Lists")]
    public MapInfo[] mapInfos;

    private VisualElement   root;
    private Button[]        mapSelectButtons;
    private VisualElement[] mapPictures;
    private Button          confirmButton;

    private Label nameLabel;
    private Label diffLabel;
    private Label infoLabel;

    private int currentIndex = 0;
    private Coroutine runningExpand, runningShrink;

    private string dataPath;
    private PlayerData playerData;


    void Awake()
    {

        string saveDir = Path.Combine(Application.dataPath, "_Save");
        if (!Directory.Exists(saveDir))
            Directory.CreateDirectory(saveDir);
        dataPath = Path.Combine(saveDir, "playerData.json");

        try
        {
            var json = File.ReadAllText(dataPath);
            var list = JsonUtility.FromJson<PlayerDataList>(json);
            playerData = (list != null && list.Records.Count > 0)
                        ? list.Records[list.Records.Count - 1]
                        : new PlayerData();
        }
        catch
        {
            playerData = new PlayerData();
        }

        if (uiDocument == null)
        {
            Debug.LogError("[MapSelectController] uiDocument 필드가 할당되지 않음.");
            enabled = false;
            return;
        }
        root = uiDocument.rootVisualElement;
        if (root == null)
        {
            Debug.LogError("[MapSelectController] rootVisualElement 획득 실패");
            enabled = false;
            return;
        }

        var infoPanel = root.Q<VisualElement>("MapInformations");
        nameLabel  = infoPanel?.Q<Label>("MapName");
        diffLabel  = infoPanel?.Q<Label>("MapDifficulty");
        infoLabel  = infoPanel?.Q<Label>("MapInformation");
        confirmButton = infoPanel?.Q<Button>("MapConfirm");

        confirmButton.clicked += OnConfirmClicked;

        int count = mapInfos.Length;
        mapSelectButtons = new Button[count];
        mapPictures      = new VisualElement[count];

        for (int i = 0; i < count; i++)
        {
            int idx = i;
            mapSelectButtons[idx] = root.Query<Button>($"MapSelectButton{idx}").First();
            mapPictures[idx]      = root.Query<VisualElement>($"MapPicture{idx}").First();

            mapSelectButtons[idx].clicked += () => OnMapSelected(idx);
        }

        if (count > 0 && mapPictures[0] != null)
        {
            ApplySelection(0);
        }
    }

    private void OnMapSelected(int idx)
    {
        if (idx == currentIndex) return;
        ApplySelection(idx);
    }

    private void ApplySelection(int idx)
    {
        if (mapPictures[currentIndex] != null)
        {
            if (runningExpand != null) StopCoroutine(runningExpand);
            if (runningShrink != null) StopCoroutine(runningShrink);

            runningShrink = StartCoroutine(AnimateScale(
                mapPictures[currentIndex],
                from: new Vector3(1f, 1.15f, 1f),
                to:   Vector3.one,
                duration: 0.15f
            ));
        }

        if (mapPictures[idx] != null)
        {
            runningExpand = StartCoroutine(AnimateScale(
                mapPictures[idx],
                from: Vector3.one,
                to:   new Vector3(1f, 1.15f, 1f),
                duration: 0.15f
            ));
        }

        var info = mapInfos[idx];
        if (nameLabel != null) nameLabel.text = info.mapName;
        if (diffLabel != null) diffLabel.text = info.mapDifficulty;
        if (infoLabel != null) infoLabel.text = info.mapInformation;

        currentIndex = idx;
    }

    void OnConfirmClicked()
    {
        PlayerDataList dataList;
        try
        {
            string j = File.ReadAllText(dataPath);
            dataList = JsonUtility.FromJson<PlayerDataList>(j);
            if (dataList == null) throw new System.Exception();
        }
        catch
        {
            dataList = new PlayerDataList();
        }

        var record = new PlayerData {
            tutorialScore     = playerData.tutorialScore,
            selectedMapIndex  = currentIndex,
            selectedCarIndex  = playerData.selectedCarIndex,
            playerNickname    = playerData.playerNickname,
            RunSpeed          = 0f,
            Fallcount         = 0,
            selectedAt        = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };
        dataList.Records.Add(record);

        string outJson = JsonUtility.ToJson(dataList, true);
        File.WriteAllText(dataPath, outJson);

        SceneManager.LoadScene("3_Garage");
    }

    private IEnumerator AnimateScale(VisualElement ve, Vector3 from, Vector3 to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float x = Mathf.Clamp01(t / duration);
            float e = x * x * (3f - 2f * x);
            ve.transform.scale = Vector3.Lerp(from, to, e);
            yield return null;
        }
        ve.transform.scale = to;
    }
}
