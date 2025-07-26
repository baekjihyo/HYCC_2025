using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using System.IO;

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

    private VisualElement root;
    private Button[] mapSelectButtons;
    private VisualElement[] mapPictures;
    private Button confirmButton;

    private Label nameLabel;
    private Label diffLabel;
    private Label infoLabel;

    private int currentIndex = 0;

    private Coroutine runningExpand;
    private Coroutine runningShrink;

    void Awake()
    {
        root = uiDocument.rootVisualElement;

        var infoPanel = root.Q<VisualElement>("MapInformations");
        nameLabel = infoPanel.Q<Label>("MapName");
        diffLabel = infoPanel.Q<Label>("MapDifficulty");
        infoLabel = infoPanel.Q<Label>("MapInformation");

        int count = mapInfos.Length;
        mapSelectButtons = new Button[count];
        mapPictures = new VisualElement[count];

        confirmButton = root.Q<Button>("MapConfirm");
        confirmButton.clicked += () =>
        {
            string saveDir = Path.Combine(Application.dataPath, "_Save");
            string savePath = Path.Combine(saveDir, "playerData.json");

            if (!Directory.Exists(saveDir))
                Directory.CreateDirectory(saveDir);

            PlayerData pd = new PlayerData();
            if (File.Exists(savePath))
            {
                string existingJson = File.ReadAllText(savePath);
                pd = JsonUtility.FromJson<PlayerData>(existingJson);
            }

            pd.selectedMapIndex = currentIndex;
            pd.selectedAt = System.DateTime.Now.ToString("o");

            string newJson = JsonUtility.ToJson(pd);
            File.AppendAllText(savePath, newJson + "\n");

            SceneManager.LoadScene("3_Garage");
        };


        for (int i = 0; i < count; i++)
        {
            int idx = i;
            mapSelectButtons[idx] = root.Q<Button>($"MapSelectButton{idx}");
            mapPictures[idx] = root.Q<VisualElement>($"MapPicture{idx}");

            mapSelectButtons[idx].clickable.clicked += () =>
            {
                if (currentIndex == idx) return;

                if (currentIndex >= 0)
                {
                    if (runningExpand != null) StopCoroutine(runningExpand);
                    if (runningShrink != null) StopCoroutine(runningShrink);

                    runningShrink = StartCoroutine(AnimateScale(
                        mapPictures[currentIndex],
                        from: new Vector3(1f, 1.15f, 1f),
                        to: Vector3.one,
                        duration: 0.15f
                    ));
                }

                runningExpand = StartCoroutine(AnimateScale(
                    mapPictures[idx],
                    from: Vector3.one,
                    to: new Vector3(1f, 1.15f, 1f),
                    duration: 0.15f
                ));

                var info = mapInfos[idx];
                nameLabel.text = info.mapName;
                diffLabel.text = info.mapDifficulty;
                infoLabel.text = info.mapInformation;

                currentIndex = idx;
            };
        }

        currentIndex = 0;

        var firstInfo = mapInfos[0];
        nameLabel.text = firstInfo.mapName;
        diffLabel.text = firstInfo.mapDifficulty;
        infoLabel.text = firstInfo.mapInformation;

        mapPictures[0].transform.scale = new Vector3(1f, 1.15f, 1f);
    }

    IEnumerator AnimateScale(VisualElement ve, Vector3 from, Vector3 to, float duration)
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
