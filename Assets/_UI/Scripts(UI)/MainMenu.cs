using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    void OnEnable()
    {
        var root = uiDocument.rootVisualElement;

        var mainMenuContainer = root.Q<VisualElement>("MainMenuContainer");
        var tutorialPromptContainer = root.Q<VisualElement>("TutorialPromptContainer");

        var startButton = root.Q<Button>("Button_Start");
        var tutorialYesButton = root.Q<Button>("Button_TutorialYes");
        var tutorialNoButton = root.Q<Button>("Button_TutorialNo");

        startButton.clicked += () =>
        {
            mainMenuContainer.style.display = DisplayStyle.None;
            tutorialPromptContainer.style.display = DisplayStyle.Flex;
        };

        tutorialYesButton.clicked += () =>
        {
            SceneManager.LoadScene("1_Tutorial");
        };

        tutorialNoButton.clicked += () =>
        {
            SceneManager.LoadScene("3_Garage");
        };
    }
}
