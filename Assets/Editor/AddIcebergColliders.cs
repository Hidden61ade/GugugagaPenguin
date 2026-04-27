using UnityEditor;
using UnityEngine;

public class AddIcebergColliders
{
    [MenuItem("Tools/Add Iceberg Colliders")]
    public static void Execute()
    {
        PhysicMaterial slideMat = AssetDatabase.LoadAssetAtPath<PhysicMaterial>(
            "Assets/PhysicsMaterials/SlipperyGround.physicMaterial");

        // 找到场景中所有名称包含 "iceberg" 的根对象（不区分大小写）
        GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
        int addedCount = 0;

        foreach (GameObject go in allObjects)
        {
            // 只处理名称匹配 "iceberg" 的根级对象（父对象是 Level0/Level1 等）
            if (!go.name.ToLower().Contains("iceberg")) continue;

            // 为该对象及其所有子对象中有 MeshFilter 的添加 MeshCollider
            MeshFilter[] meshFilters = go.GetComponentsInChildren<MeshFilter>(true);
            foreach (MeshFilter mf in meshFilters)
            {
                if (mf.sharedMesh == null || mf.sharedMesh.vertexCount == 0) continue;
                if (mf.GetComponent<Collider>() != null) continue;

                MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                mc.convex = false;
                if (slideMat != null)
                    mc.sharedMaterial = slideMat;

                EditorUtility.SetDirty(mf.gameObject);
                addedCount++;
                Debug.Log($"Added MeshCollider: {go.name}/{mf.gameObject.name} ({mf.sharedMesh.vertexCount} verts)");
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        Debug.Log($"Iceberg colliders done! Added {addedCount} MeshColliders.");
    }
}
