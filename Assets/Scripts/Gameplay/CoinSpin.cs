using UnityEngine;

/// <summary>
/// 让硬币围绕 Y 轴持续自转。
/// </summary>
public class CoinSpin : MonoBehaviour
{
    [Tooltip("每秒旋转角度（度）")]
    public float spinSpeed = 180f;

    void Update()
    {
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
    }
}
