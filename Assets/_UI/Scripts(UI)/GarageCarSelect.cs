using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Collections.Generic;


public class GarageCarSelect : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private GameObject targetObject;
    
    private void OnEnable()
    {

        Vector3[] carPositions = new Vector3[]
        {
            new Vector3(20, 2, 40),
            new Vector3(20, 2, 0),
            new Vector3(20, 2, -40),
            new Vector3(-20, 2, 40),
            new Vector3(-20, 2, 0),
            new Vector3(-20, 2, -40)
        };

        var root = uiDocument.rootVisualElement;
        var SelectCar1Button = root.Q<Button>("SelectCar1Button");
        var SelectCar2Button = root.Q<Button>("SelectCar2Button");
        var SelectCar3Button = root.Q<Button>("SelectCar3Button");
        var SelectCar4Button = root.Q<Button>("SelectCar4Button");
        var SelectCar5Button = root.Q<Button>("SelectCar5Button");
        var SelectCar6Button = root.Q<Button>("SelectCar6Button");

        SelectCar1Button.clicked += () =>
        {
            targetObject.transform.position = carPositions[0];
        };
        SelectCar2Button.clicked += () =>
        {
            targetObject.transform.position = carPositions[1];
        };
        SelectCar3Button.clicked += () =>
        {
            targetObject.transform.position = carPositions[2];
        };
        SelectCar4Button.clicked += () =>
        {
            targetObject.transform.position = carPositions[3];
        };
        SelectCar5Button.clicked += () =>
        {
            targetObject.transform.position = carPositions[4];
        };
        SelectCar6Button.clicked += () =>
        {
            targetObject.transform.position = carPositions[5];
        };
    }
}