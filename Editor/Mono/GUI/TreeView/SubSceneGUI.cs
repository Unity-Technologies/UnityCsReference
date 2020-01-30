// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

static class SubSceneGUI
{
    static Dictionary<GameObject, SceneHierarchyHooks.SubSceneInfo> m_SubSceneHeadersMap = new Dictionary<GameObject, SceneHierarchyHooks.SubSceneInfo>();
    static Dictionary<Scene, SceneHierarchyHooks.SubSceneInfo> m_SceneToSubSceneMap = new Dictionary<Scene, SceneHierarchyHooks.SubSceneInfo>();
    static Dictionary<SceneAsset, SceneHierarchyHooks.SubSceneInfo> m_SceneAssetToSubSceneMap = new Dictionary<SceneAsset, SceneHierarchyHooks.SubSceneInfo>();
    const int kMaxSubSceneIterations = 100;
    static float s_HalfFoldoutWidth = 6f;
    static float s_SubSceneHeaderIndentAdjustment = -2f;

    internal static void FetchSubSceneInfo()
    {
        if (SceneHierarchyHooks.provideSubScenes != null)
        {
            m_SubSceneHeadersMap = new Dictionary<GameObject, SceneHierarchyHooks.SubSceneInfo>();
            m_SceneToSubSceneMap = new Dictionary<Scene, SceneHierarchyHooks.SubSceneInfo>();
            m_SceneAssetToSubSceneMap = new Dictionary<SceneAsset, SceneHierarchyHooks.SubSceneInfo>();

            var subSceneInfos = SceneHierarchyHooks.provideSubScenes();
            foreach (var subSceneInfo in subSceneInfos)
            {
                if (subSceneInfo.transform == null)
                {
                    Debug.LogError("Invalid Transform");
                    continue;
                }

                m_SubSceneHeadersMap[subSceneInfo.transform.gameObject] = subSceneInfo;

                if (subSceneInfo.scene.IsValid())
                    m_SceneToSubSceneMap[subSceneInfo.scene] = subSceneInfo;
                if (subSceneInfo.sceneAsset)
                    m_SceneAssetToSubSceneMap[subSceneInfo.sceneAsset] = subSceneInfo;
            }
        }
    }

    internal static bool IsUsingSubScenes()
    {
        return SceneHierarchyHooks.provideSubScenes != null;
    }

    static Transform GetParentCheckSubScenes(Transform transform)
    {
        if (transform.parent == null)
        {
            SceneHierarchyHooks.SubSceneInfo subScene;
            if (m_SceneToSubSceneMap.TryGetValue(transform.gameObject.scene, out subScene))
                return subScene.transform;
            else
                return null; // Reached root of a root scene
        }

        return transform.parent;
    }

    internal static bool IsChildOrSameAsOtherTransform(Transform transform, Transform otherTransform)
    {
        if (IsUsingSubScenes())
        {
            Transform current = transform;
            int i = 0;
            while (current != null && i++ < kMaxSubSceneIterations)
            {
                if (current == otherTransform)
                    return true;

                current = GetParentCheckSubScenes(current);
            }

            if (i >= kMaxSubSceneIterations)
                Debug.LogError("Recursive SubScene setup detected. Report a bug.");
        }
        else
        {
            Transform current = transform;
            while (current != null)
            {
                if (current == otherTransform)
                    return true;

                current = current.parent;
            }
        }
        return false;
    }

    internal static bool IsSubSceneHeader(GameObject gameObject)
    {
        return IsUsingSubScenes() && m_SubSceneHeadersMap.ContainsKey(gameObject);
    }

    internal static void CreateClosedSubSceneContextClick(GenericMenu menu, SceneHierarchyHooks.SubSceneInfo subScene)
    {
        var selectAssetContent = EditorGUIUtility.TrTextContent("Select Scene Asset");
        if (subScene.sceneAsset)
            menu.AddItem(selectAssetContent, false, SelectSceneAsset, subScene.sceneAsset);
        else
            menu.AddDisabledItem(selectAssetContent);
    }

    static void SelectSceneAsset(object userData)
    {
        SceneAsset sceneAssetObject = (SceneAsset)userData;
        Selection.activeObject = sceneAssetObject;
        EditorGUIUtility.PingObject(sceneAssetObject);
    }

    internal static string GetSubSceneHeaderText(GameObject gameObject)
    {
        SceneHierarchyHooks.SubSceneInfo subScene;
        if (m_SubSceneHeadersMap.TryGetValue(gameObject, out subScene))
        {
            if (SceneHierarchyHooks.provideSubSceneName != null)
                return SceneHierarchyHooks.provideSubSceneName(subScene);

            return subScene.sceneName;
        }

        return null;
    }

    internal static bool UseBoldFontForGameObject(GameObject gameObject)
    {
        SceneHierarchyHooks.SubSceneInfo subScene;
        if (m_SubSceneHeadersMap.TryGetValue(gameObject, out subScene))
        {
            return subScene.scene == SceneManager.GetActiveScene();
        }
        return false;
    }

    internal static SceneHierarchyHooks.SubSceneInfo GetSubSceneInfo(Scene scene)
    {
        SceneHierarchyHooks.SubSceneInfo subScene;
        if (m_SceneToSubSceneMap.TryGetValue(scene, out subScene))
            return subScene;

        return default(SceneHierarchyHooks.SubSceneInfo);
    }

    internal static SceneHierarchyHooks.SubSceneInfo GetSubSceneInfo(SceneAsset sceneAsset)
    {
        SceneHierarchyHooks.SubSceneInfo subScene;
        if (m_SceneAssetToSubSceneMap.TryGetValue(sceneAsset, out subScene))
            return subScene;

        return default(SceneHierarchyHooks.SubSceneInfo);
    }

    internal static SceneHierarchyHooks.SubSceneInfo GetSubSceneInfo(GameObject gameObject)
    {
        SceneHierarchyHooks.SubSceneInfo subScene;
        if (gameObject != null && m_SubSceneHeadersMap.TryGetValue(gameObject, out subScene))
            return subScene;

        return default(SceneHierarchyHooks.SubSceneInfo);
    }

    internal static Color GetColorForSubScene(Scene scene)
    {
        SceneHierarchyHooks.SubSceneInfo subScene;
        if (m_SceneToSubSceneMap.TryGetValue(scene, out subScene))
            return subScene.color;

        return Color.grey;
    }

    internal static Scene GetSubScene(GameObject gameObject)
    {
        SceneHierarchyHooks.SubSceneInfo subScene;
        if (m_SubSceneHeadersMap.TryGetValue(gameObject, out subScene))
            return subScene.scene;

        return default(Scene);
    }

    internal static void DrawSubSceneHeaderBackground(Rect rect, float baseIndent, float indentWidth, GameObject gameObject)
    {
        float indent = CalcIndentOfSubSceneHeader(gameObject, baseIndent, indentWidth);
        if (indent < 0)
        {
            Debug.LogError("Only call DrawSubSceneHeaderBackground if IsSubSceneHeader() is true");
            return;
        }
        Rect headerRect = rect;
        headerRect.xMin += indent;

        Color oldColor = GUI.color;
        GUI.color = GUI.color * new Color(1, 1, 1, 0.9f); // dimmed compared to main scene headers
        GUI.Label(headerRect, GUIContent.none, GameObjectTreeViewGUI.GameObjectStyles.sceneHeaderBg);
        GUI.color = oldColor;
    }

    static float CalcIndentOfSubSceneHeader(GameObject gameObject, float baseIndent, float indentWidth)
    {
        SceneHierarchyHooks.SubSceneInfo subSceneInfo;
        if (gameObject == null || !m_SubSceneHeadersMap.TryGetValue(gameObject, out subSceneInfo))
            return -1;  // Input is not a sub scene GameObject

        int hierarchyDepth = CalculateHierarchyDepthOfSubScene(subSceneInfo);
        if (hierarchyDepth > 0)
        {
            return baseIndent + s_SubSceneHeaderIndentAdjustment + (hierarchyDepth * indentWidth);
        }
        return -1f;
    }

    // Temp cache for optimizing vertical line drawing
    static SceneHierarchyHooks.SubSceneInfo s_LastSubSceneInfo;
    static Rect s_LastRectCalculated;

    internal static Rect GetRectForVerticalLine(Rect rowRect, float baseIndent, float indentWidth, Scene scene)
    {
        // Fast path: reuse last rect if same scene
        if (s_LastSubSceneInfo.isValid && s_LastSubSceneInfo.scene == scene)
        {
            s_LastRectCalculated.y = rowRect.y;
            return s_LastRectCalculated;
        }

        // Reset and calculate new rect
        s_LastRectCalculated = new Rect();
        if (!m_SceneToSubSceneMap.TryGetValue(scene, out s_LastSubSceneInfo))
            return new Rect();

        if (s_LastSubSceneInfo.color.a == 0)
            return new Rect();

        float indent = CalcIndentOfVerticalLine(s_LastSubSceneInfo, baseIndent, indentWidth);
        if (indent < 0)
            return new Rect();

        s_LastRectCalculated = rowRect;
        s_LastRectCalculated.x +=  indent;
        s_LastRectCalculated.width = 1;

        return s_LastRectCalculated;
    }

    internal static void DrawVerticalLine(Rect rowRect, float baseIndent, float indentWidth, GameObject gameObject)
    {
        if (gameObject == null)
            return;

        if (Event.current.type == EventType.Repaint)
        {
            Scene scene = gameObject.scene;
            Rect lineRect = GetRectForVerticalLine(rowRect, baseIndent, indentWidth, scene);
            if (lineRect.width > 0f)
            {
                Color color = GetColorForSubScene(scene);
                GUI.DrawTexture(lineRect, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill, true, 1, color, color, color, color, Vector4.zero, Vector4.zero, false);
            }
        }
    }

    static float CalcIndentOfVerticalLine(SceneHierarchyHooks.SubSceneInfo subSceneInfo, float baseIndent, float indentWidth)
    {
        int hierarchyDepth = CalculateHierarchyDepthOfSubScene(subSceneInfo);
        if (hierarchyDepth > 0)
        {
            return baseIndent + hierarchyDepth * indentWidth + s_HalfFoldoutWidth;
        }
        return -1f;
    }

    internal static int CalculateHierarchyDepthOfSubScene(SceneHierarchyHooks.SubSceneInfo subSceneInfo)
    {
        if (!subSceneInfo.isValid)
            return -1;

        int hierarchyDepth = 0;
        int i = 0;
        while (i++ < kMaxSubSceneIterations)
        {
            hierarchyDepth += FindTransformDepth(subSceneInfo.transform) + 1;  // the +1 is for the SubScene header
            if (!m_SceneToSubSceneMap.TryGetValue(subSceneInfo.transform.gameObject.scene, out subSceneInfo))
                break;
        }

        if (i >= kMaxSubSceneIterations)
            Debug.LogError("Recursive SubScene setup detected. Report a bug.");

        return hierarchyDepth;
    }

    static int FindTransformDepth(Transform transform)
    {
        var trans = transform.parent;
        int depth = 0;
        while (trans != null)
        {
            depth++;
            trans = trans.parent;
        }
        return depth;
    }
}
