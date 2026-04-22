using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class TerrainGridGenerator : MonoBehaviour
{
#if UNITY_EDITOR
    [SerializeField] private GameObject terrain;
    [SerializeField] private List<GameObject> builtTerrain = new List<GameObject>();
    [SerializeField] private string terrainName = "Terrain";
    [SerializeField] private bool autoDetectSpacing = true;
    [SerializeField] private float spacingX = 1f;
    [SerializeField] private float spacingZ = 1f;
    [SerializeField] private Vector2Int size;

    [ContextMenu("Regenerate")]
    public void Regenerate()
    {
        Clear();
        Generate();
    }

    [ContextMenu("Generate")]
    public void Generate()
    { 
        if (terrain == null)
        {
            Debug.LogError("[TerrainGridGenerator] terrain 预制体未设置，无法生成。");
            return;
        }

        Undo.RegisterCompleteObjectUndo(this, "Generate Terrain");
        Clear();

        Vector2 resolvedSpacing = ResolveSpacing();
        float halfX = (size.x - 1) * 0.5f;
        float halfY = (size.y - 1) * 0.5f;
        var transform1 = transform;

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                GameObject go = PrefabUtility.InstantiatePrefab(terrain, this.transform) as GameObject;
                Undo.RegisterCreatedObjectUndo(go, "Generate Terrain");
                go.name = terrainName + " " + x + ", " + y;
                float offsetX = (x - halfX) * resolvedSpacing.x;
                float offsetY = (y - halfY) * resolvedSpacing.y;
                go.transform.position = transform1.position
                    + (offsetX * transform1.right)
                    + (offsetY * transform1.forward);
                builtTerrain.Add(go);
            }
        }
    }

    private Vector2 ResolveSpacing()
    {
        if (!autoDetectSpacing)
        {
            return new Vector2(Mathf.Max(0.0001f, spacingX), Mathf.Max(0.0001f, spacingZ));
        }

        if (TryResolveSpacingFromMesh(out Vector2 meshSpacing))
        {
            return meshSpacing;
        }

        if (terrain.TryGetComponent<Terrain>(out Terrain terrainComponent)
            && terrainComponent.terrainData != null)
        {
            Vector3 size3 = terrainComponent.terrainData.size;
            return new Vector2(Mathf.Max(0.0001f, size3.x), Mathf.Max(0.0001f, size3.z));
        }

        if (terrain.TryGetComponent<Renderer>(out Renderer renderer))
        {
            Vector3 size3 = renderer.bounds.size;
            return new Vector2(Mathf.Max(0.0001f, size3.x), Mathf.Max(0.0001f, size3.z));
        }

        if (terrain.TryGetComponent<Collider>(out Collider collider))
        {
            Vector3 size3 = collider.bounds.size;
            return new Vector2(Mathf.Max(0.0001f, size3.x), Mathf.Max(0.0001f, size3.z));
        }

        Debug.LogWarning("[TerrainGridGenerator] 无法自动识别地块尺寸，回退到手动 spacingX/spacingZ。\n"
            + "请手动调整间距以避免重叠。");
        return new Vector2(Mathf.Max(0.0001f, spacingX), Mathf.Max(0.0001f, spacingZ));
    }

    private bool TryResolveSpacingFromMesh(out Vector2 spacing)
    {
        spacing = Vector2.zero;

        if (!terrain.TryGetComponent<MeshFilter>(out MeshFilter meshFilter)
            || meshFilter.sharedMesh == null)
        {
            return false;
        }

        Vector3 meshSize = meshFilter.sharedMesh.bounds.size;
        Vector3 scale = terrain.transform.lossyScale;

        float resolvedX = Mathf.Max(0.0001f, Mathf.Abs(meshSize.x * scale.x));
        float resolvedZ = Mathf.Max(0.0001f, Mathf.Abs(meshSize.z * scale.z));
        spacing = new Vector2(resolvedX, resolvedZ);
        return true;
    }

    [ContextMenu("Clear")]
    public void Clear()
    {
        Undo.RegisterCompleteObjectUndo(this, "Clear Terrain");
        for (int i = 0; i < builtTerrain.Count; i++)
        {
            GameObject go = builtTerrain[i];
            if (go != null)
            {
                Undo.DestroyObjectImmediate(go);
            }
        }

        builtTerrain.Clear();
        this.transform.DestroyImmediateChildren();
    }

#endif
}