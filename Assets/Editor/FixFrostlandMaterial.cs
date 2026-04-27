using UnityEditor;
using UnityEngine;

public class FixFrostlandMaterial
{
    [MenuItem("Tools/Fix Frostland Material")]
    public static void Execute()
    {
        string basePath = "Assets/Mesh/frostland-stylized-low-poly-asset-pack";
        string albedoPath = basePath + "/textures/T_frostland_A.png";
        string emissionPath = basePath + "/textures/T_frostland_E.png";
        string matSavePath = "Assets/Materials/M_Frostland.mat";

        // 加载贴图
        Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath);
        Texture2D emission = AssetDatabase.LoadAssetAtPath<Texture2D>(emissionPath);

        if (albedo == null)
        {
            Debug.LogError($"Albedo texture not found: {albedoPath}");
            return;
        }
        Debug.Log($"Loaded albedo: {albedo.width}x{albedo.height}");

        // 创建或加载材质
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matSavePath);
        if (mat == null)
        {
            Shader shader = Shader.Find("Standard");
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, matSavePath);
            Debug.Log("Created new material: M_Frostland");
        }

        // 设置 Albedo
        mat.mainTexture = albedo;
        mat.color = Color.white;

        // 设置 Emission
        if (emission != null)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetTexture("_EmissionMap", emission);
            mat.SetColor("_EmissionColor", Color.white);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            Debug.Log("Applied emission texture");
        }

        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();

        // 应用到场景中 Frostland 下的所有 Renderer
        GameObject frostland = GameObject.Find("Frostland");
        if (frostland == null)
        {
            Debug.LogError("Frostland not found in scene!");
            return;
        }

        Renderer[] renderers = frostland.GetComponentsInChildren<Renderer>(true);
        int count = 0;
        foreach (Renderer r in renderers)
        {
            // 替换所有材质槽
            Material[] mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                mats[i] = mat;
            }
            r.sharedMaterials = mats;
            EditorUtility.SetDirty(r.gameObject);
            count++;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        Debug.Log($"Frostland material fix complete! Applied to {count} renderers.");
    }
}
