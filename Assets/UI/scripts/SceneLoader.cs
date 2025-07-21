using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] UIDocument uiDocument;
    private VisualElement root;
    void Start()
    {
        root = uiDocument.rootVisualElement;

        var Button_Start = root.Q<Button>("Button_Start");

        if (Button_Start != null)
        {
            Button_Start.clicked += () =>
            {
                SceneManager.LoadScene("1_Tutorial");
            };
        }
        else
        {
            Debug.LogWarning("Button_Start not found in UI Document.");
        }
    }

    void Update()
    {

    }
}
