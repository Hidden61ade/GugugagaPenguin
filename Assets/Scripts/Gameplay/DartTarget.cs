using System.Collections;
using UnityEngine;

/// <summary>
/// 靶子分圈检测：企鹅撞上后"钉"在靶子上，根据命中的环给分。
/// 5 秒后进入结算。挂在每个环的 Collider 对象上。
/// </summary>
public class DartTarget : MonoBehaviour
{
    [Tooltip("命中该环获得的分数")]
    public int scoreValue = 10;

    private static bool hasTriggered = false;

    private void OnEnable()
    {
        hasTriggered = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.collider.CompareTag("Player"))
            return;

        if (hasTriggered)
            return;

        hasTriggered = true;

        // ── 记录标靶得分 ──
        VictoryManager.SetDartBonus(scoreValue);
        Debug.Log($"[DartTarget] 命中 {gameObject.name}，Dart Bonus = {scoreValue}");

        // ── 让企鹅"钉"在靶子上 ──
        Rigidbody rb = collision.rigidbody;
        if (rb != null)
        {
            Vector3 hitPoint = collision.GetContact(0).point;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;

            Transform penguin = rb.transform;
            penguin.position = hitPoint + transform.forward * 0.5f;

            // 禁用企鹅输入
            FlyingPenguinController ctrl = rb.GetComponent<FlyingPenguinController>();
            if (ctrl != null)
                ctrl.SetInputEnabled(false);
        }

        // ── 5 秒后进入结算 ──
        StartCoroutine(VictoryDelayRoutine());
    }

    private IEnumerator VictoryDelayRoutine()
    {
        yield return new WaitForSeconds(5f);

        // 进入 Victory 状态
        FlowControlClean.Instance.NotifyFinishReached();

        // 显示结算面板
        if (VictoryManager.Instance != null)
        {
            VictoryManager.Instance.ShowVictory();
        }
    }
}
