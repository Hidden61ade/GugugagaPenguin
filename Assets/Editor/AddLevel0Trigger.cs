using UnityEditor;
using UnityEngine;

public class AddLevel0Trigger
{
    [MenuItem("Tools/Add ScensLoadAheadTrigger to Level0")]
    public static void Execute()
    {
        // ── 1. 场景中的 Level0 ──
        GameObject level0Scene = GameObject.Find("Level0");
        if (level0Scene != null)
        {
            AddTriggerToLevel(level0Scene);
            EditorUtility.SetDirty(level0Scene);
            Debug.Log("Added ScensLoadAheadTrigger to scene Level0");
        }

        // ── 2. Level0.prefab ──
        string prefabPath = "Assets/Prefabs/Levels/Level0.prefab";
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        if (prefabRoot != null)
        {
            // 检查是否已有 Trigger
            Transform existing = prefabRoot.transform.Find("ScensLoadAheadTrigger");
            if (existing == null)
            {
                AddTriggerToLevel(prefabRoot);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                Debug.Log("Added ScensLoadAheadTrigger to Level0.prefab");
            }
            else
            {
                Debug.Log("Level0.prefab already has ScensLoadAheadTrigger");
            }
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        // 保存场景
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        Debug.Log("Level0 trigger setup complete!");
    }

    private static void AddTriggerToLevel(GameObject level)
    {
        // 检查是否已有
        Transform existing = level.transform.Find("ScensLoadAheadTrigger");
        if (existing != null)
        {
            Debug.Log($"{level.name} already has ScensLoadAheadTrigger");
            return;
        }

        // 创建与 Level1 完全一致的 Trigger
        GameObject trigger = new GameObject("ScensLoadAheadTrigger");
        trigger.transform.SetParent(level.transform, false);

        // 本地坐标：与 Level1 的设置一致（x=0, y=190, z=385 是 Level1 的末尾）
        // Level0 的 Track 末尾在 Z≈3411，本地坐标类似位置
        // 但因为 Level1 的 localPosition.z=385 对应的是 Level1 内部坐标，
        // Level0 和 Level1 结构一致，都在 z≈2900 附近触发
        trigger.transform.localPosition = new Vector3(0f, 190f, 2900f);
        trigger.transform.localScale = new Vector3(600f, 400f, 100f);

        BoxCollider col = trigger.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = Vector3.one;
        col.center = Vector3.zero;

        trigger.AddComponent<LevelLoadTrigger>();
    }
}
