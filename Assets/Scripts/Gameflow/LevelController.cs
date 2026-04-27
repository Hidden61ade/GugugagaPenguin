using System.Collections.Generic;
using UnityEngine;

public class LevelController : MonoBehaviour
{
    public static LevelController Instance;

    [Header("Level Settings")]
    public GameObject[] Levels;                   // 用于循环生成的关卡预制体
    public GameObject[] InitialLevelsInstances;   // 场景中预放的初始关卡
    [SerializeField] private int nextExtendedLevelIndex = 0;
    [SerializeField] private float levelLength = 3000f;

    [Header("Floating Origin Settings")]
    [SerializeField] private float floatingOriginThresholdZ = 12000f;
    [SerializeField] private float floatingOriginKeepZ = 3000f;

    // ── 运行时状态 ──
    private List<GameObject> levelList = new List<GameObject>();
    private int triggerCount = 0;

    [SerializeField] private GameObject m_Player;
    [SerializeField] private GameObject MainCamera;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        m_Player = GameObject.FindGameObjectWithTag("Player");
        MainCamera = GameObject.FindGameObjectWithTag("MainCamera");

        levelList.Clear();
        foreach (var level in InitialLevelsInstances)
        {
            if (level != null)
                levelList.Add(level);
        }

        nextExtendedLevelIndex = 0;
        triggerCount = 0;
    }

    /// <summary>
    /// 每次经过 ScensLoadAheadTrigger 时调用。
    /// 每次都在前方生成新关卡；每经过 2 个关卡才删除最旧的 1 个。
    /// </summary>
    public void MoveSceneAhead()
    {
        triggerCount++;

        // ── 1. 在最前方生成新关卡 ──
        SpawnNextLevel();

        // ── 2. 每 2 次触发才删除最旧的 1 个关卡 ──
        if (triggerCount % 2 == 0 && levelList.Count > 3)
        {
            GameObject oldest = levelList[0];
            levelList.RemoveAt(0);
            if (oldest != null)
            {
                Debug.Log($"[LevelController] 删除旧关卡: {oldest.name} (trigger #{triggerCount})");
                Destroy(oldest);
            }
        }

        // ── 3. 浮点原点修正 ──
        TryCorrectFloatingOrigin();

        Debug.Log($"[LevelController] Trigger #{triggerCount}, 当前 {levelList.Count} 个关卡在缓冲区");
    }

    private void SpawnNextLevel()
    {
        if (levelList.Count == 0)
        {
            Debug.LogWarning("No levels in buffer!");
            return;
        }

        GameObject frontLevel = levelList[levelList.Count - 1];
        if (frontLevel == null)
        {
            Debug.LogWarning("Front level is null!");
            return;
        }

        Vector3 targetPos = frontLevel.transform.position + new Vector3(0f, 0f, levelLength);

        GameObject newLevel = Instantiate(
            Levels[nextExtendedLevelIndex],
            targetPos,
            Quaternion.identity
        );

        levelList.Add(newLevel);
        nextExtendedLevelIndex = (nextExtendedLevelIndex + 1) % Levels.Length;

        Debug.Log($"[LevelController] 生成新关卡: {newLevel.name} at Z={targetPos.z:F0}");
    }

    private void TryCorrectFloatingOrigin()
    {
        if (m_Player == null)
        {
            Debug.LogWarning("Player is null. Cannot correct floating origin.");
            return;
        }

        float playerZ = m_Player.transform.position.z;

        if (playerZ < floatingOriginThresholdZ)
            return;

        float correctionZ = Mathf.Floor((playerZ - floatingOriginKeepZ) / levelLength) * levelLength;

        if (correctionZ <= 0f)
            return;

        Vector3 offset = new Vector3(0f, 0f, -correctionZ);

        // 移动所有关卡
        foreach (GameObject level in levelList)
        {
            if (level != null)
                level.transform.position += offset;
        }

        // 移动玩家
        m_Player.transform.position += offset;

        // 移动摄像机（如果不是玩家子物体）
        if (MainCamera != null && !MainCamera.transform.IsChildOf(m_Player.transform))
            MainCamera.transform.position += offset;

        Debug.Log($"Floating origin corrected. Offset: {offset}");
    }
}
