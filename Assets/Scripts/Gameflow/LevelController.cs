using System.Collections.Generic;
using UnityEngine;

public class LevelController : MonoBehaviour
{
    public static LevelController Instance;
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public GameObject[] Levels;
    [SerializeField] private int nextExtendedLevelIndex = 0;
    public GameObject[] InitialLevelsInstances; // 初始关卡列表
    private CircularLevelBuffer levelBuffer;
    [SerializeField] private GameObject m_Player;
    [SerializeField] private GameObject MainCamera;

    void Start()
    {
        m_Player = GameObject.FindGameObjectWithTag("Player");
        MainCamera = GameObject.FindGameObjectWithTag("MainCamera");

        levelBuffer = new CircularLevelBuffer(4); // 4 slots: 最多3个

        foreach (var level in InitialLevelsInstances)
        {
            levelBuffer.Enqueue(level);
        }
        nextExtendedLevelIndex = 0; // 从第一个非初始关卡开始循环
    }
    public void MoveSceneAhead()
    {
        // 先删除最旧的
        GameObject oldLevel = levelBuffer.Dequeue();
        if (oldLevel != null) { Destroy(oldLevel); }

        // 再添加新的
        Vector3 targetPos = levelBuffer.Peek().transform.position + new Vector3(0, 0, 3000);
        var newLevel = Instantiate(Levels[nextExtendedLevelIndex], targetPos, Quaternion.identity);
        levelBuffer.Enqueue(newLevel);
        nextExtendedLevelIndex = (nextExtendedLevelIndex + 1) % Levels.Length;
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

        // 初始化为 null（其实默认就是）
        for (int i = 0; i < size; i++)
            buffer[i] = null;
    }

    // 判断是否为空
    public bool IsEmpty()
    {
        return head == tail;
    }

    // 判断是否满（最多只能用 capacity-1 个）
    public bool IsFull()
    {
        return (tail + 1) % capacity == head;
    }

    // 入队
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

    // 出队
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
    // 获取当前所有有效元素（调试用）
    public List<GameObject> GetAll()
    {
        List<GameObject> list = new List<GameObject>();

        int i = head;
        while (i != tail)
        {
            list.Add(buffer[i]);
            i = (i + 1) % capacity;
        }

        return list;
    }
}
