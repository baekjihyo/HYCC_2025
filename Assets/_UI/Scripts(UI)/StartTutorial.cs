using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class StartTutorial : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    private Button startButton;
    private VisualElement mainMenuContainer;
    private VisualElement tutorialPromptContainer;
    private Button tutorialYesButton;
    private Button tutorialNoButton;

    void OnEnable()
    {
        Debug.Log("OnEnable 실행됨");
        RegisterButton();
    }

    void OnDisable()
    {
        UnregisterEvents();
    }

    void Start()
    {
        Debug.Log("Start() 실행됨");
        RegisterButton(); // 중복 호출 방지 필요
    }

    private void RegisterButton()
    {
        if (uiDocument == null)
        {
            Debug.LogError("UIDocument 연결되지 않음");
            return;
        }

        var root = uiDocument.rootVisualElement;

        // 1. 메인 메뉴 버튼 초기화
        startButton = root.Q<Button>("Button_Start");
        if (startButton != null) // 버튼이 있을 때만 실행
        {
            Debug.Log("Button_Start 찾음, 이벤트 등록");
            startButton.clicked -= ShowTutorialPrompt; // 기존 이벤트 제거
            startButton.clicked += ShowTutorialPrompt; // 새 이벤트 등록
        }
        else
        {
            Debug.LogError("Button_Start 찾을 수 없음");
        }

        // 2. 컨테이너 초기화
        mainMenuContainer = root.Q<VisualElement>("MainMenuContainer");
        tutorialPromptContainer = root.Q<VisualElement>("TutorialPromptContainer");

        // 3. 튜토리얼 버튼 초기화 (조건문 밖으로 이동)
        tutorialYesButton = root.Q<Button>("Button_TutorialYes");
        tutorialNoButton = root.Q<Button>("Button_TutorialNo");

        if (tutorialYesButton != null && tutorialNoButton != null)
        {
            tutorialYesButton.clicked -= OnTutorialYes; // 기존 이벤트 제거
            tutorialYesButton.clicked += OnTutorialYes; // 새 이벤트 등록
            
            tutorialNoButton.clicked -= OnTutorialNo; // 기존 이벤트 제거
            tutorialNoButton.clicked += OnTutorialNo; // 새 이벤트 등록
        }
        else
        {
            Debug.LogError("튜토리얼 버튼 찾을 수 없음");
        }
    }

    private void UnregisterEvents()
    {
        if (startButton != null)
        {
            startButton.clicked -= ShowTutorialPrompt;
        }
        if (tutorialYesButton != null)
        {
            tutorialYesButton.clicked -= OnTutorialYes;
        }
        if (tutorialNoButton != null)
        {
            tutorialNoButton.clicked -= OnTutorialNo;
        }
    }

    private void OnTutorialYes()
    {
        SceneManager.LoadScene("1_Tutorial");
    }

    private void OnTutorialNo()
    {
        SceneManager.LoadScene("3_Garage");
    }

    private void ShowTutorialPrompt()
    {
        Debug.Log("튜토리얼 선택지 표시");
        if (mainMenuContainer != null && tutorialPromptContainer != null)
        {
            mainMenuContainer.style.display = DisplayStyle.None;
            tutorialPromptContainer.style.display = DisplayStyle.Flex;
        }
    }
}