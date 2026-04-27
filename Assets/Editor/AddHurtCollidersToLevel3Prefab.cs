using UnityEditor;
using UnityEngine;

public class AddHurtCollidersToLevel3Prefab
{
    private static readonly string[] obstacleNames = {
        "Bridge", "Frost_nova", "Lighthouse", "Lighthouse (1)",
        "Sun_temple", "Frostland", "Flag_1", "Bread_Chemney",
        "Ramp_1", "Ramp_1 (1)", "Ramp_1 (2)", "Temple",
        "Sphere.001", "Frozen_body_3", "Tree_3",
        "Ice_Tooth_1", "Ice_Tooth_2", "Ice_Tooth_3",
        "Barrel_1", "Barrel_2", "Fountain"
    };

    [MenuItem("Tools/Add HurtColliders to Level3 Prefab")]
    public static void Execute()
    {
        string prefabPath = "Assets/Prefabs/Levels/Level3.prefab";
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        if (prefabRoot == null)
        {
            Debug.LogError("Level3.prefab not found!");
            return;
        }

        // 创建 HurtColliders 容器
        Transform hurtParent = prefabRoot.transform.Find("HurtColliders");
        if (hurtParent == null)
        {
            GameObject hc = new GameObject("HurtColliders");
            hc.transform.SetParent(prefabRoot.transform, false);
            hc.transform.localPosition = Vector3.zero;
            hurtParent = hc.transform;
        }

        int addedCount = 0;

        foreach (string objName in obstacleNames)
        {
            Transform objTransform = prefabRoot.transform.Find(objName);
            if (objTransform == null)
            {
                Debug.LogWarning($"Object not found in prefab: {objName}");
                continue;
            }

            MeshFilter[] meshFilters = objTransform.GetComponentsInChildren<MeshFilter>(true);
            foreach (MeshFilter mf in meshFilters)
            {
                if (mf.sharedMesh == null || mf.sharedMesh.vertexCount == 0)
                    continue;

                string colliderName = $"{mf.gameObject.name}_MeshCollider";

                // 检查是否已存在
                bool exists = false;
                for (int i = 0; i < hurtParent.childCount; i++)
                {
                    if (hurtParent.GetChild(i).name == colliderName)
                    {
                        exists = true;
                        break;
                    }
                }
                if (exists) continue;

                // 创建碰撞体对象（使用本地坐标系，相对于 prefab root）
                GameObject colliderObj = new GameObject(colliderName);
                colliderObj.transform.SetParent(hurtParent, false);

                // 复制变换（相对于 prefab root 的世界坐标）
                colliderObj.transform.position = mf.transform.position;
                colliderObj.transform.rotation = mf.transform.rotation;
                colliderObj.transform.localScale = mf.transform.lossyScale;

                MeshCollider mc = colliderObj.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                mc.convex = false;

                addedCount++;
            }
        }

        // 同时确保 ScensLoadAheadTrigger 存在
        Transform trigger = prefabRoot.transform.Find("ScensLoadAheadTrigger");
        if (trigger == null)
        {
            GameObject t = new GameObject("ScensLoadAheadTrigger");
            t.transform.SetParent(prefabRoot.transform, false);
            t.transform.localPosition = new Vector3(0f, 190f, 2800f);
            t.transform.localScale = new Vector3(600f, 400f, 100f);

            BoxCollider col = t.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = Vector3.one;

            t.AddComponent<LevelLoadTrigger>();
            Debug.Log("Added ScensLoadAheadTrigger to Level3.prefab");
        }

        // 保存 prefab
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);

        Debug.Log($"Level3.prefab updated! Added {addedCount} HurtCollider objects.");
    }
}
