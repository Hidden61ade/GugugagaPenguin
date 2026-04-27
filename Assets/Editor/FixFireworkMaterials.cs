using UnityEditor;
using UnityEngine;

public class FixFireworkMaterials
{
    [MenuItem("Tools/Fix Firework Materials")]
    public static void Execute()
    {
        GameObject firework = GameObject.Find("firework_");
        if (firework == null)
        {
            Debug.LogError("firework_ not found!");
            return;
        }

        // 收集所有子对象的材质（去重）
        var renderers = firework.GetComponentsInChildren<Renderer>(true);
        var processedMats = new System.Collections.Generic.HashSet<int>();

        foreach (var r in renderers)
        {
            foreach (var mat in r.sharedMaterials)
            {
                if (mat == null) continue;
                if (processedMats.Contains(mat.GetInstanceID())) continue;
                processedMats.Add(mat.GetInstanceID());

                // 将 Rendering Mode 强制设为 Opaque
                SetMaterialOpaque(mat);
                EditorUtility.SetDirty(mat);
                Debug.Log($"Fixed material: {mat.name} on {r.gameObject.name}");
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Firework materials fixed!");
    }

    private static void SetMaterialOpaque(Material mat)
    {
        // Standard Shader Opaque 设置
        mat.SetFloat("_Mode", 0f);                          // Opaque = 0
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
        mat.SetInt("_ZWrite", 1);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.DisableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = -1; // 恢复默认 renderQueue

        // 确保颜色 alpha = 1
        Color c = mat.color;
        c.a = 1f;
        mat.color = c;
    }
}
