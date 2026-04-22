using UnityEngine;

public class SyncSkyRoll : MonoBehaviour
{
    public Camera skyCamera;
    public float visualTiltX = -15f;   // 整体视觉倾斜

    void LateUpdate()
    {
        if (skyCamera == null) return;

        // 跟随主相机位置和朝向
        skyCamera.transform.position = transform.position;
        skyCamera.transform.rotation = transform.rotation;

        // 额外加一个 roll
        skyCamera.transform.Rotate(visualTiltX, 0f, 0f, Space.Self);
    }
}
