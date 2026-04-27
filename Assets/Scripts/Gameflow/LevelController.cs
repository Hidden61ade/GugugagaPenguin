using System.Collections.Generic;
using UnityEngine;

public class LevelController : MonoBehaviour
{
    public static LevelController Instance;

    [Header("Level Settings")]
    public GameObject[] Levels;
    public GameObject[] InitialLevelsInstances; // 初始关卡列表
    [SerializeField] private int nextExtendedLevelIndex = 0;
    [SerializeField] private float levelLength = 3000f;

    [Header("Floating Origin Settings")]
    [SerializeField] private float floatingOriginThresholdZ = 12000f;
    [SerializeField] private float floatingOriginKeepZ = 3000f;

    private CircularLevelBuffer levelBuffer;

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

        levelBuffer = new CircularLevelBuffer(4); // 4 slots: 最多3个有效关卡

        foreach (var level in InitialLevelsInstances)
        {
            levelBuffer.Enqueue(level);
        }

        nextExtendedLevelIndex = 0;
    }

    public void MoveSceneAhead()
    {
        // 1. 先删除最旧的关卡
        GameObject oldLevel = levelBuffer.Dequeue();
        if (oldLevel != null)
        {
            Destroy(oldLevel);
        }

        // 2. 再添加新的前方关卡
        GameObject frontLevel = levelBuffer.GetLast();

        if (frontLevel == null)
        {
            Debug.LogWarning("No front level found. Cannot spawn next level.");
            return;
        }

        Vector3 targetPos = frontLevel.transform.position + new Vector3(0f, 0f, levelLength);

        GameObject newLevel = Instantiate(
            Levels[nextExtendedLevelIndex],
            targetPos,
            Quaternion.identity
        );

        levelBuffer.Enqueue(newLevel);

        nextExtendedLevelIndex = (nextExtendedLevelIndex + 1) % Levels.Length;

        // 3. 新关卡加载后，检查是否需要浮点原点修正
        TryCorrectFloatingOrigin();
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
        {
            return;
        }

        // 让玩家 z 坐标回到一个较小位置附近，同时尽量按关卡长度对齐
        float correctionZ = Mathf.Floor((playerZ - floatingOriginKeepZ) / levelLength) * levelLength;

        if (correctionZ <= 0f)
        {
            return;
        }

        Vector3 offset = new Vector3(0f, 0f, -correctionZ);

        // 移动当前缓冲区里的所有关卡：所在关卡 + 前方关卡
        foreach (GameObject level in levelBuffer.GetAll())
        {
            if (level != null)
            {
                level.transform.position += offset;
            }
        }

        // 移动玩家
        m_Player.transform.position += offset;

        // 移动主摄像机
        // 如果摄像机是玩家子物体，就不要再单独移动一次，否则会移动两倍
        if (MainCamera != null && !MainCamera.transform.IsChildOf(m_Player.transform))
        {
            MainCamera.transform.position += offset;
        }

        Debug.Log($"Floating origin corrected. Offset: {offset}");
    }
}

public class CircularLevelBuffer
{
    private GameObject[] buffer;
    private int head = 0;
    private int tail = 0;
    private int capacity;

    public CircularLevelBuffer(int size)
    {
        capacity = size;
        buffer = new GameObject[size];

        for (int i = 0; i < size; i++)
        {
            buffer[i] = null;
        }
    }

    public bool IsEmpty()
    {
        return head == tail;
    }

    public bool IsFull()
    {
        return (tail + 1) % capacity == head;
    }

    public bool Enqueue(GameObject obj)
    {
        if (IsFull())
        {
            Debug.LogWarning("Buffer is full!");
            return false;
        }

        buffer[tail] = obj;
        tail = (tail + 1) % capacity;

        // 保证 tail 永远是 null
        buffer[tail] = null;

        return true;
    }

    public GameObject Dequeue()
    {
        if (IsEmpty())
        {
            Debug.LogWarning("Buffer is empty!");
            return null;
        }

        GameObject obj = buffer[head];
        buffer[head] = null;

        head = (head + 1) % capacity;

        return obj;
    }

    public GameObject Peek()
    {
        if (IsEmpty())
        {
            Debug.LogWarning("Buffer is empty!");
            return null;
        }

        return buffer[head];
    }

    public GameObject GetLast()
    {
        if (IsEmpty())
        {
            Debug.LogWarning("Buffer is empty!");
            return null;
        }

        int lastIndex = (tail - 1 + capacity) % capacity;
        return buffer[lastIndex];
    }

    public List<GameObject> GetAll()
    {
        List<GameObject> list = new List<GameObject>();

        int i = head;
        while (i != tail)
        {
            if (buffer[i] != null)
            {
                list.Add(buffer[i]);
            }

            i = (i + 1) % capacity;
        }

        return list;
    }
}
