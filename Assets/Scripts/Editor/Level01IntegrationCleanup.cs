using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// Level01 接入 main/UIManager 后的场景清理与校验工具。
/// </summary>
public static class Level01IntegrationCleanup
{
    private const string Level01ScenePath = "Assets/Scenes/Level01.unity";

    /// <summary>
    /// 应从 Level01 删除的根物体（UI 由 main 的 UIManager 提供）。
    /// 删除根物体时会一并删除其子物体（Crosshair、ReStart、Die 等）。
    /// </summary>
    private static readonly string[] LegacyUiRootNames =
    {
        "HUD",
        "LevelComplete",
        "GameOver",
        "PauseMenu",
        "MainMenu",
        "Main_Menu",
        "Die",
        "UIManager",
    };

    [MenuItem("Roofbound/Level01 集成/清理关卡内冗余 UI")]
    public static void RemoveLegacyUiFromLevel01()
    {
        if (!System.IO.File.Exists(Level01ScenePath))
        {
            EditorUtility.DisplayDialog("清理失败", "找不到 Level01 场景。", "确定");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(Level01ScenePath, OpenSceneMode.Single);
        List<GameObject> removed = RemoveLegacyUiRoots(scene);

        ClearLevelControllerLegacyPanelRefs(scene);
        EnsureEventSystem(scene);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        EditorUtility.DisplayDialog(
            "清理完成",
            $"已从 Level01 删除 {removed.Count} 个冗余 UI 根物体。\n" +
            "已清空 LevelController 的 winPanel/losePanel 引用。\n" +
            "请保存后从 main 场景 Play 验证。",
            "确定");
    }

    [MenuItem("Roofbound/Level01 集成/校验 Level01 场景结构")]
    public static void ValidateLevel01Structure()
    {
        if (!System.IO.File.Exists(Level01ScenePath))
        {
            Debug.LogError("[Level01Integration] 找不到 Level01 场景。");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(Level01ScenePath, OpenSceneMode.Single);
        GameObject[] roots = scene.GetRootGameObjects();

        var report = new System.Text.StringBuilder();
        report.AppendLine("=== Level01 集成校验 ===");

        foreach (string legacyName in LegacyUiRootNames)
        {
            if (FindRootByName(roots, legacyName) != null)
            {
                report.AppendLine($"[应删除] 冗余 UI 根物体: {legacyName}");
            }
        }

        string[] requiredRoots = { "LevelController", "Fleet_Spawner", "TrackPath", "destination", "Buildings" };
        foreach (string required in requiredRoots)
        {
            if (FindRootByName(roots, required) == null)
            {
                report.AppendLine($"[缺失] 建议保留: {required}");
            }
            else
            {
                report.AppendLine($"[OK] {required}");
            }
        }

        levelController controller = Object.FindObjectOfType<levelController>();
        if (controller == null)
        {
            report.AppendLine("[缺失] 场景中无 levelController");
        }
        else
        {
            SerializedObject so = new SerializedObject(controller);
            if (so.FindProperty("winPanel").objectReferenceValue != null)
                report.AppendLine("[应清空] LevelController.winPanel 仍绑定关卡内 UI");
            if (so.FindProperty("losePanel").objectReferenceValue != null)
                report.AppendLine("[应清空] LevelController.losePanel 仍绑定关卡内 UI");
        }

        if (Object.FindObjectOfType<EventSystem>() == null)
        {
            report.AppendLine("[建议添加] EventSystem（UIManager 按钮在关卡中需要）");
        }

        Debug.Log(report.ToString());
        EditorUtility.DisplayDialog("校验完成", "详见 Console 输出。", "确定");
    }

    private static List<GameObject> RemoveLegacyUiRoots(Scene scene)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        var removed = new List<GameObject>();

        for (int i = 0; i < roots.Length; i++)
        {
            for (int j = 0; j < LegacyUiRootNames.Length; j++)
            {
                if (roots[i].name != LegacyUiRootNames[j])
                {
                    continue;
                }

                removed.Add(roots[i]);
                Object.DestroyImmediate(roots[i]);
                break;
            }
        }

        return removed;
    }

    private static void ClearLevelControllerLegacyPanelRefs(Scene scene)
    {
        levelController controller = Object.FindObjectOfType<levelController>();
        if (controller == null)
        {
            return;
        }

        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("winPanel").objectReferenceValue = null;
        so.FindProperty("losePanel").objectReferenceValue = null;
        so.FindProperty("finishMenuPanel").objectReferenceValue = null;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsureEventSystem(Scene scene)
    {
        if (Object.FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
        SceneManager.MoveGameObjectToScene(eventSystem, scene);
    }

    private static GameObject FindRootByName(GameObject[] roots, string objectName)
    {
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name == objectName)
            {
                return roots[i];
            }
        }

        return null;
    }
}
