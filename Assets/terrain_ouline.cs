using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter))]
public class terrain_ouline : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private MeshFilter sourceMeshFilter;

    [Header("Outline")]
    [SerializeField] private Material outlineMaterial;
    [SerializeField] private float outlineWidth = 0.6f;
    [SerializeField] private float yOffset = 0.05f;
    [SerializeField] private Color outlineColor = Color.black;

    [Header("Line Settings")]
    [SerializeField] private int cornerVertices = 4;
    [SerializeField] private bool rebuildInEditor = true;
    [Tooltip("Editor mode rebuild interval in seconds.")]
    [SerializeField] private float rebuildInterval = 0.25f;

    private LineRenderer lineRenderer;
    private bool isDirty;
    private float nextRebuildTime;
    private int lastMeshId;
    private int lastVertexCount;
    private int lastTriangleCount;
    private bool hasWarnedUnreadable;

    private struct EdgeKey
    {
        public int a;
        public int b;

        public EdgeKey(int v0, int v1)
        {
            if (v0 < v1)
            {
                a = v0;
                b = v1;
            }
            else
            {
                a = v1;
                b = v0;
            }
        }

        public override int GetHashCode()
        {
            return a * 73856093 ^ b * 19349663;
        }

        public override bool Equals(object obj)
        {
            if (obj is not EdgeKey other)
            {
                return false;
            }

            return a == other.a && b == other.b;
        }
    }

    private void Reset()
    {
        sourceMeshFilter = GetComponent<MeshFilter>();
        EnsureLineRenderer();
        MarkDirty();
    }

    private void OnEnable()
    {
        if (sourceMeshFilter == null)
        {
            sourceMeshFilter = GetComponent<MeshFilter>();
        }

        EnsureLineRenderer();
        MarkDirty();
    }

    private void OnValidate()
    {
        if (!rebuildInEditor)
        {
            return;
        }

        if (sourceMeshFilter == null)
        {
            sourceMeshFilter = GetComponent<MeshFilter>();
        }

        EnsureLineRenderer();
        MarkDirty();
    }

    private void Update()
    {
        if (!rebuildInEditor)
        {
            return;
        }

        if (Application.isPlaying)
        {
            return;
        }

        if (!isDirty)
        {
            return;
        }

        if (!IsMeshReadable())
        {
            isDirty = false;
            return;
        }

        if (Time.realtimeSinceStartup < nextRebuildTime)
        {
            return;
        }

        if (!IsMeshChanged())
        {
            isDirty = false;
            return;
        }

        RebuildOutline();
        isDirty = false;
        nextRebuildTime = Time.realtimeSinceStartup + Mathf.Max(0.05f, rebuildInterval);
    }

    [ContextMenu("Rebuild Terrain Outline")]
    public void RebuildOutline()
    {
        if (sourceMeshFilter == null || sourceMeshFilter.sharedMesh == null)
        {
            return;
        }

        if (!IsMeshReadable())
        {
            return;
        }

        EnsureLineRenderer();

        Mesh mesh = sourceMeshFilter.sharedMesh;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        CacheMeshState(mesh, vertices.Length, triangles.Length);

        Dictionary<EdgeKey, int> edgeUseCount = new Dictionary<EdgeKey, int>();

        for (int i = 0; i < triangles.Length; i += 3)
        {
            AddEdge(edgeUseCount, triangles[i], triangles[i + 1]);
            AddEdge(edgeUseCount, triangles[i + 1], triangles[i + 2]);
            AddEdge(edgeUseCount, triangles[i + 2], triangles[i]);
        }

        Dictionary<int, List<int>> adjacency = new Dictionary<int, List<int>>();

        foreach (var pair in edgeUseCount)
        {
            if (pair.Value != 1)
            {
                continue;
            }

            AddNeighbor(adjacency, pair.Key.a, pair.Key.b);
            AddNeighbor(adjacency, pair.Key.b, pair.Key.a);
        }

        List<int> loop = BuildLargestBoundaryLoop(adjacency, vertices);

        if (loop == null || loop.Count < 3)
        {
            lineRenderer.positionCount = 0;
            return;
        }

        Vector3[] linePositions = new Vector3[loop.Count];

        for (int i = 0; i < loop.Count; i++)
        {
            Vector3 p = vertices[loop[i]];
            p.y += yOffset;
            linePositions[i] = p;
        }

        lineRenderer.positionCount = linePositions.Length;
        lineRenderer.SetPositions(linePositions);
        lineRenderer.loop = true;
        lineRenderer.widthMultiplier = outlineWidth;
        lineRenderer.startColor = outlineColor;
        lineRenderer.endColor = outlineColor;
        lineRenderer.numCornerVertices = cornerVertices;
        lineRenderer.numCapVertices = 0;
    }

    private void MarkDirty()
    {
        isDirty = true;
    }

    private bool IsMeshChanged()
    {
        if (sourceMeshFilter == null || sourceMeshFilter.sharedMesh == null)
        {
            return false;
        }

        if (!IsMeshReadable())
        {
            return false;
        }

        Mesh mesh = sourceMeshFilter.sharedMesh;
        int meshId = mesh.GetInstanceID();
        int vertexCount = mesh.vertexCount;
        int triangleCount = mesh.triangles.Length;

        return meshId != lastMeshId
            || vertexCount != lastVertexCount
            || triangleCount != lastTriangleCount;
    }

    private void CacheMeshState(Mesh mesh, int vertexCount, int triangleCount)
    {
        lastMeshId = mesh.GetInstanceID();
        lastVertexCount = vertexCount;
        lastTriangleCount = triangleCount;
    }

    private bool IsMeshReadable()
    {
        if (sourceMeshFilter == null || sourceMeshFilter.sharedMesh == null)
        {
            return false;
        }

        Mesh mesh = sourceMeshFilter.sharedMesh;
        if (mesh.isReadable)
        {
            return true;
        }

        if (!hasWarnedUnreadable)
        {
            Debug.LogWarning("terrain_ouline: mesh is not readable. Enable Read/Write in import settings to build outline.");
            hasWarnedUnreadable = true;
        }

        return false;
    }

    private void EnsureLineRenderer()
    {
        lineRenderer = GetComponent<LineRenderer>();

        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        lineRenderer.useWorldSpace = false;
        lineRenderer.alignment = LineAlignment.View;
        lineRenderer.textureMode = LineTextureMode.Stretch;

        lineRenderer.shadowCastingMode = ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        lineRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;

        if (outlineMaterial != null)
        {
            lineRenderer.sharedMaterial = outlineMaterial;
        }
    }

    private static void AddEdge(Dictionary<EdgeKey, int> edgeUseCount, int a, int b)
    {
        EdgeKey key = new EdgeKey(a, b);

        if (edgeUseCount.ContainsKey(key))
        {
            edgeUseCount[key]++;
        }
        else
        {
            edgeUseCount.Add(key, 1);
        }
    }

    private static void AddNeighbor(Dictionary<int, List<int>> adjacency, int a, int b)
    {
        if (!adjacency.TryGetValue(a, out List<int> list))
        {
            list = new List<int>();
            adjacency.Add(a, list);
        }

        if (!list.Contains(b))
        {
            list.Add(b);
        }
    }

    private static List<int> BuildLargestBoundaryLoop(
        Dictionary<int, List<int>> adjacency,
        Vector3[] vertices)
    {
        HashSet<int> visited = new HashSet<int>();
        List<int> bestLoop = null;
        float bestLength = -1f;

        foreach (int start in adjacency.Keys)
        {
            if (visited.Contains(start))
            {
                continue;
            }

            List<int> loop = WalkLoop(start, adjacency, visited);

            if (loop == null || loop.Count < 3)
            {
                continue;
            }

            float length = GetLoopLength(loop, vertices);

            if (length > bestLength)
            {
                bestLength = length;
                bestLoop = loop;
            }
        }

        return bestLoop;
    }

    private static List<int> WalkLoop(
        int start,
        Dictionary<int, List<int>> adjacency,
        HashSet<int> globalVisited)
    {
        List<int> loop = new List<int>();

        int previous = -1;
        int current = start;

        for (int safe = 0; safe < 100000; safe++)
        {
            loop.Add(current);
            globalVisited.Add(current);

            if (!adjacency.TryGetValue(current, out List<int> neighbors) || neighbors.Count == 0)
            {
                break;
            }

            int next = -1;

            for (int i = 0; i < neighbors.Count; i++)
            {
                if (neighbors[i] != previous)
                {
                    next = neighbors[i];
                    break;
                }
            }

            if (next == -1)
            {
                break;
            }

            if (next == start)
            {
                break;
            }

            previous = current;
            current = next;
        }

        return loop;
    }

    private static float GetLoopLength(List<int> loop, Vector3[] vertices)
    {
        float length = 0f;

        for (int i = 0; i < loop.Count; i++)
        {
            Vector3 a = vertices[loop[i]];
            Vector3 b = vertices[loop[(i + 1) % loop.Count]];
            length += Vector3.Distance(a, b);
        }

        return length;
    }
}