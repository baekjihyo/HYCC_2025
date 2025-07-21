using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;
using System.Collections.Generic;

public class GarageCarSelect : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private GameObject targetObject;
    [SerializeField] private CinemachineCamera cinemachineCamera;

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

        for (int i = 0; i < 6; i++)
        {
            int index = i;
            var button = root.Q<Button>($"SelectCar{index + 1}Button");

            button.clicked += () =>
            {
                targetObject.transform.position = carPositions[index];

                if (cinemachineCamera.TryGetComponent(out CinemachineRotationComposer composer))
                {
                    cinemachineCamera.LookAt = targetObject.transform;
                }
            };
        }
    }
}
