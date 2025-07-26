using UnityEngine;

public class MiniMap2 : MonoBehaviour
{
    public Transform Vehicle;
    public Camera Cam;
    public float height = 130f;

    void Update()
    {
        //Cam.cullingMask = Cam.cullingMask;
        Cam.transform.position = new Vector3(Vehicle.position.x, height, Vehicle.position.z);
        Cam.transform.rotation = Quaternion.Euler(90f, Vehicle.eulerAngles.y, 0f);
    }
}
