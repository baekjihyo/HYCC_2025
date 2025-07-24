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

        var quitButton = root.Q<Button>("Button_Quit");
        var quitPopup = root.Q<VisualElement>("QuitPopupContainer");
        var confirmQuitButton = root.Q<Button>("Button_QuitConfirm");
        var cancelQuitButton = root.Q<Button>("Button_QuitCancel");

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

        quitButton.clicked += () =>
        {
            quitPopup.style.display = DisplayStyle.Flex;
        };

        cancelQuitButton.clicked += () =>
        {
            quitPopup.style.display = DisplayStyle.None;
        };

        confirmQuitButton.clicked += () =>
        {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        };
    }
}