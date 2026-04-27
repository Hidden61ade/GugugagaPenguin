using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class SetupLevel3HurtColliders
{
    // Level3 中需要成为障碍物的装饰物名称
    private static readonly string[] obstacleNames = {
        "Bridge", "Frost_nova", "Lighthouse", "Lighthouse (1)",
        "Sun_temple", "Frostland", "Flag_1", "Bread_Chemney",
        "Ramp_1", "Ramp_1 (1)", "Ramp_1 (2)", "Temple",
        "Sphere.001", "Frozen_body_3", "Tree_3",
        "Ice_Tooth_1", "Ice_Tooth_2", "Ice_Tooth_3",
        "Barrel_1", "Barrel_2", "Fountain"
    };

    [MenuItem("Tools/Setup Level3 HurtColliders")]
    public static void Execute()
    {
        GameObject level3 = GameObject.Find("Level3");
        if (level3 == null)
        {
            Debug.LogError("Level3 not found!");
            return;
        }

        // 创建 HurtColliders 容器
        Transform hurtParent = level3.transform.Find("HurtColliders");
        if (hurtParent == null)
        {
            GameObject hc = new GameObject("HurtColliders");
            hc.transform.SetParent(level3.transform, false);
            hc.transform.localPosition = Vector3.zero;
            hurtParent = hc.transform;
            Debug.Log("Created HurtColliders container in Level3");
        }

        int addedCount = 0;

        foreach (string objName in obstacleNames)
        {
            Transform objTransform = level3.transform.Find(objName);
            if (objTransform == null)
            {
                Debug.LogWarning($"Object not found: Level3/{objName}");
                continue;
            }

            // 获取该对象及其所有子对象的 MeshFilter
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

                // 创建不可见的碰撞体对象
                GameObject colliderObj = new GameObject(colliderName);
                colliderObj.transform.SetParent(hurtParent, false);

                // 完全复制世界变换
                colliderObj.transform.position = mf.transform.position;
                colliderObj.transform.rotation = mf.transform.rotation;
                colliderObj.transform.localScale = mf.transform.lossyScale;

                MeshCollider mc = colliderObj.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                mc.convex = false;

                EditorUtility.SetDirty(colliderObj);
                addedCount++;
            }
        }

        // 保存
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        Debug.Log($"Level3 HurtColliders setup complete! Added {addedCount} colliders.");
    }
}
