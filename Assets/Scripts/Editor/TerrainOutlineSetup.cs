using UnityEngine;
using UnityEditor;

public class TerrainOutlineSetup
{
    [MenuItem("Tools/Roofbound/Setup Terrain Outline Material")]
    public static void SetupTerrainOutlineMaterial()
    {
        // 查找或创建 Material 文件夹
        string materialFolderPath = "Assets/Materials";
        if (!AssetDatabase.IsValidFolder(materialFolderPath))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
            Debug.Log($"Created folder: {materialFolderPath}");
        }

        // 查找或创建 TerrainOutline Material
        string materialPath = "Assets/Materials/TerrainOutline.mat";
        Material outlineMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

        if (outlineMaterial == null)
        {
            // 查找 shader
            Shader shader = Shader.Find("Custom/URP_TerrainOutlineLine");
            if (shader == null)
            {
                Debug.LogError("Cannot find shader 'Custom/URP_TerrainOutlineLine'. Please check the shader path.");
                return;
            }

            // 创建新 Material
            outlineMaterial = new Material(shader);
            outlineMaterial.SetColor("_BaseColor", new Color(0.02f, 0.015f, 0.01f, 1f));

            // 保存到 Assets
            AssetDatabase.CreateAsset(outlineMaterial, materialPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"Created terrain outline material at {materialPath}");
        }
        else
        {
            Debug.Log($"Material already exists at {materialPath}");
        }

        // 查找 SandTile prefab
        string prefabPath = "Assets/Prefabs/SandTile.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        
        if (prefab == null)
        {
            Debug.LogError($"Cannot find prefab at {prefabPath}");
            return;
        }

        // 查找 terrain_ouline 组件
        terrain_ouline outlineComponent = prefab.GetComponent<terrain_ouline>();
        if (outlineComponent == null)
        {
            Debug.LogError("Cannot find 'terrain_ouline' component on SandTile prefab.");
            return;
        }

        // 通过反射设置 outlineMaterial（因为它是 private SerializeField）
        var field = outlineComponent.GetType().GetField("outlineMaterial", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(outlineComponent, outlineMaterial);
            EditorUtility.SetDirty(prefab);
            AssetDatabase.SaveAssets();
            Debug.Log($"✓ Successfully configured terrain outline on SandTile.prefab");
            Debug.Log($"  - Material: {materialPath}");
            Debug.Log($"  - Shader: Custom/URP_TerrainOutlineLine");
            Debug.Log($"  - The outline should now render on terrain tiles in-game.");
        }
        else
        {
            Debug.LogError("Cannot find 'outlineMaterial' field on terrain_ouline component.");
        }
    }
}
