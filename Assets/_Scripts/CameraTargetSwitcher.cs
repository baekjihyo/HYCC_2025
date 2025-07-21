using UnityEngine;
using Unity.Cinemachine;

public class CameraTargetSwitcher : MonoBehaviour
{
    public CinemachineCamera cinemachineCam;
    public Transform targetA;
    public Transform targetB;

    private bool isTrackingA = true;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T)) // 상황 트리거 (예: 키 입력)
        {
            if (isTrackingA)
            {
                SetTrackingTarget(targetB);
            }
            else
            {
                SetTrackingTarget(targetA);
            }

            isTrackingA = !isTrackingA;
        }
    }

    void SetTrackingTarget(Transform newTarget)
    {

        // Optionally set Follow / LookAt if needed:
        cinemachineCam.Follow = newTarget;
        cinemachineCam.LookAt = newTarget;
    }
}
