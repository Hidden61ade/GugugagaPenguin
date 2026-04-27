using System.Collections;
using UnityEngine;

/// <summary>
/// 挂在 FlyingPenguinController 上，提供临时加速效果。
/// 由外部调用 Activate() 触发。
/// </summary>
[RequireComponent(typeof(FlyingPenguinController))]
public class SpeedBoost : MonoBehaviour
{
    [Tooltip("加速期间额外增加的最大前向速度")]
    public float bonusSpeed = 100f;

    [Tooltip("加速持续时间（秒）")]
    public float duration = 3f;

    private FlyingPenguinController controller;
    private Coroutine activeBoost;

    private void Awake()
    {
        controller = GetComponent<FlyingPenguinController>();
    }

    /// <summary>
    /// 激活加速效果。若已在加速中，重置计时器。
    /// </summary>
    public void Activate()
    {
        if (activeBoost != null)
            StopCoroutine(activeBoost);

        activeBoost = StartCoroutine(BoostRoutine());
    }

    private IEnumerator BoostRoutine()
    {
        float originalMax = controller.maxForwardSpeed;
        controller.maxForwardSpeed += bonusSpeed;

        Debug.Log($"[SpeedBoost] 加速开始！maxForwardSpeed: {controller.maxForwardSpeed}");

        yield return new WaitForSeconds(duration);

        // 恢复原速（防止其他途径改变了 maxForwardSpeed，只减去 bonus 部分）
        controller.maxForwardSpeed -= bonusSpeed;
        controller.maxForwardSpeed = Mathf.Max(controller.maxForwardSpeed, originalMax);

        Debug.Log($"[SpeedBoost] 加速结束，maxForwardSpeed 恢复: {controller.maxForwardSpeed}");
        activeBoost = null;
    }
}
