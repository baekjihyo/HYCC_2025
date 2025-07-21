using UnityEngine;
using UnityEngine.UIElements;

public class GarageCarInformation : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    private VisualElement infoPanel;
    private Label carNameLabel;
    private Label carDescriptionLabel;

    private struct CarInfo
    {
        public string name;
        public string description;
    }

    private CarInfo[] cars = new CarInfo[]
    {
        new CarInfo { name = "Falcon GT", description = "속도와 안정성의 완벽한 조화" },
        new CarInfo { name = "Viper R9", description = "드리프트에 최적화된 경량 머신" },
        new CarInfo { name = "Thunder X", description = "강력한 토크와 오프로드 대응력" },
        new CarInfo { name = "Shadow 7", description = "스텔스 디자인의 하이브리드" },
        new CarInfo { name = "Cyclone Z", description = "제로백 2.8초의 괴물 머신" },
        new CarInfo { name = "Nova Prime", description = "밸런스형 성능과 연비의 최강자" }
    };

    private void OnEnable()
    {
        var root = uiDocument.rootVisualElement;

        infoPanel = root.Q<VisualElement>("InfoPanel");
        carNameLabel = root.Q<Label>("CarNameLabel");
        carDescriptionLabel = root.Q<Label>("CarDescriptionLabel");

        for (int i = 0; i < 6; i++)
        {
            int index = i;
            var button = root.Q<Button>($"SelectCar{index + 1}Button");

            button.clicked += () =>
            {
                ShowCarInfo(index);
            };
        }

        // 시작 시 infoPanel은 숨김
        infoPanel.RemoveFromClassList("show");
    }

    private void ShowCarInfo(int index)
    {
        carNameLabel.text = cars[index].name;
        carDescriptionLabel.text = cars[index].description;

        infoPanel.AddToClassList("show");
    }
}
