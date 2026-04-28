using System.Collections;
using UnityEngine;

/// <summary>
/// 挂在 FlyingPenguinController 上，提供临时加速效果。
/// 加速期间 FOV 会从正常值平滑扩大，结束后平滑恢复。
/// </summary>
[RequireComponent(typeof(FlyingPenguinController))]
public class SpeedBoost : MonoBehaviour
{
    [Tooltip("加速期间额外增加的最大前向速度")]
    public float bonusSpeed = 100f;

    [Tooltip("加速持续时间（秒）")]
    public float duration = 3f;

    [Header("FOV 效果")]
    [Tooltip("加速期间额外增加的 FOV 角度")]
    public float fovBoost = 20f;

    [Tooltip("FOV 变化的过渡时间（秒）")]
    public float fovTransitionTime = 0.3f;

    private FlyingPenguinController controller;
    private Coroutine activeBoost;
    private Camera mainCam;
    private float baseFov;

    private void Awake()
    {
        controller = GetComponent<FlyingPenguinController>();
    }

    private void EnsureCamera()
    {
        if (mainCam == null)
        {
            mainCam = Camera.main;
            if (mainCam != null)
                baseFov = mainCam.fieldOfView;
        }
    }

    /// <summary>
    /// 激活加速效果。若已在加速中，重置计时器。
    /// </summary>
    public void Activate()
    {
        EnsureCamera();

        if (activeBoost != null)
            StopCoroutine(activeBoost);

        activeBoost = StartCoroutine(BoostRoutine());
    }

    private IEnumerator BoostRoutine()
    {
        float originalMax = controller.maxForwardSpeed;
        controller.maxForwardSpeed += bonusSpeed;

        Debug.Log($"[SpeedBoost] 加速开始！maxForwardSpeed: {controller.maxForwardSpeed}");

        // ── FOV 扩大（平滑过渡） ──
        if (mainCam != null)
        {
            float targetFov = baseFov + fovBoost;
            yield return StartCoroutine(LerpFov(mainCam.fieldOfView, targetFov, fovTransitionTime));
        }

        // ── 等待加速持续时间（减去过渡时间） ──
        float waitTime = duration - fovTransitionTime * 2f;
        if (waitTime > 0f)
            yield return new WaitForSeconds(waitTime);

        // ── FOV 恢复（平滑过渡） ──
        if (mainCam != null)
        {
            yield return StartCoroutine(LerpFov(mainCam.fieldOfView, baseFov, fovTransitionTime));
        }

        // 恢复原速
        controller.maxForwardSpeed -= bonusSpeed;
        controller.maxForwardSpeed = Mathf.Max(controller.maxForwardSpeed, originalMax);

        Debug.Log($"[SpeedBoost] 加速结束，maxForwardSpeed 恢复: {controller.maxForwardSpeed}");
        activeBoost = null;
    }

    private IEnumerator LerpFov(float from, float to, float time)
    {
        if (mainCam == null || time <= 0f)
        {
            if (mainCam != null) mainCam.fieldOfView = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < time)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / time);
            mainCam.fieldOfView = Mathf.Lerp(from, to, t);
            yield return null;
        }
        mainCam.fieldOfView = to;
    }
}
