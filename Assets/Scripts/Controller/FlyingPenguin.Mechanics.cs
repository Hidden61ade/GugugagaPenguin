using System.Collections;
using UnityEngine;

public partial class FlyingPenguinController : MonoBehaviour
{
    // ── 关卡入口快照（用于重置） ──────────────────────────────────────────────
    private Vector3 levelEntryPosition;
    private Quaternion levelEntryRotation;
    private Vector3 levelEntryVelocity;
    private bool levelEntryRecorded;

    // ── 受击无敌帧 ──────────────────────────────────────────────────────────
    private float lastHurtTime = -999f;
    private const float HurtCooldown = 1.0f;

    /// <summary>
    /// 由 LevelLoadTrigger 或关卡系统调用，记录进入当前关卡时的状态。
    /// </summary>
    public void RecordLevelEntry()
    {
        EnsureReferences();
        levelEntryPosition = transform.position;
        levelEntryRotation = transform.rotation;
        levelEntryVelocity = rb.velocity;
        levelEntryRecorded = true;
        Debug.Log($"[LevelEntry] pos={levelEntryPosition}, vel={levelEntryVelocity.magnitude:F1}m/s");
    }

    void OnCollisionEnter(Collision collision)
    {
        // 只处理 HurtColliders 下的对象（排除 WALL）
        if (!IsHurtCollider(collision.gameObject))
            return;

        // 无敌帧检查
        if (Time.time - lastHurtTime < HurtCooldown)
            return;

        lastHurtTime = Time.time;

        // 扣血
        int remaining = -1;
        if (HudManager.Instance != null)
        {
            remaining = HudManager.Instance.LoseLife();
            HudManager.Instance.Shake();
        }

        Debug.Log($"[Hurt] 撞到 {collision.gameObject.name}，剩余生命: {remaining}");

        // 销毁被撞的 collider 对象
        Destroy(collision.gameObject);

        // 3 命全失 → 直接进入结算
        if (remaining <= 0)
        {
            StartCoroutine(EnterVictoryOnDeath());
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Coin"))
        {
            Debug.Log("碰到了金币");
            this.maxForwardSpeed += 5f;
            maxForwardSpeed = Mathf.Min(maxForwardSpeed, 900);
            HudManager.Instance?.AddScore(10);
            Destroy(other.gameObject);
        }
        else if (other.CompareTag("Firework"))
        {
            Debug.Log("碰到了烟花！触发加速");
            SpeedBoost boost = GetComponent<SpeedBoost>();
            if (boost == null)
                boost = gameObject.AddComponent<SpeedBoost>();
            boost.Activate();
            Destroy(other.gameObject);
        }
    }

    /// <summary>
    /// 判断碰撞对象是否属于 HurtColliders（排除名称含 WALL 的）。
    /// </summary>
    private bool IsHurtCollider(GameObject obj)
    {
        Transform current = obj.transform;
        bool underHurtColliders = false;

        while (current != null)
        {
            if (current.name == "HurtColliders")
            {
                underHurtColliders = true;
                break;
            }
            current = current.parent;
        }

        if (!underHurtColliders)
            return false;

        if (obj.name.Contains("WALL"))
            return false;

        return true;
    }

    /// <summary>
    /// 血扣完后直接进入结算面板。
    /// </summary>
    private IEnumerator EnterVictoryOnDeath()
    {
        SetInputEnabled(false);

        EnsureReferences();
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        yield return new WaitForSeconds(1f);

        // 进入 Victory 状态
        FlowControlClean.Instance.NotifyFinishReached();

        // 显示结算面板
        if (VictoryManager.Instance != null)
        {
            VictoryManager.Instance.ShowVictory();
        }

        Debug.Log("[Death] 生命耗尽，进入结算");
    }
}
