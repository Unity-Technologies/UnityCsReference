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

    internal static bool IsSubSceneHeader(GameObject gameObject)
    {
        return IsUsingSubScenes() && m_SubSceneHeadersMap.ContainsKey(gameObject);
    }

    internal static bool HandleGameObjectContextMenu(GenericMenu menu, GameObject gameObject)
    {
        SceneHierarchyHooks.SubSceneInfo subScene;
        if (m_SubSceneHeadersMap.TryGetValue(gameObject, out subScene))
        {
            Scene scene = subScene.scene;
            if (!scene.IsValid())
                return false; // use default GameObject context menu if no Scene has been specified in the SubScene component yet

            if (scene.isLoaded)
            {
                var content = EditorGUIUtility.TrTextContent("Set Active Scene");
                if (SceneManager.GetActiveScene() != scene)
                    menu.AddItem(content, false, SetSceneActive, scene);
                else
                    menu.AddDisabledItem(content, true);
                menu.AddSeparator("");
            }

            var saveSceneContent = EditorGUIUtility.TrTextContent("Save Scene");
            if (!EditorApplication.isPlaying && subScene.scene.isLoaded)
                menu.AddItem(saveSceneContent, false, SaveSubSceneMenuHandler, subScene);
            else
                menu.AddDisabledItem(saveSceneContent);

            menu.AddSeparator("");
            var selectAssetContent = EditorGUIUtility.TrTextContent("Select Scene Asset");
            if (!string.IsNullOrEmpty(scene.path))
                menu.AddItem(selectAssetContent, false, SelectSceneAsset, subScene);
            else
                menu.AddDisabledItem(selectAssetContent);

            return true;
        }
        return false;
    }

    static void SetSceneActive(object userData)
    {
        Scene scene = (Scene)userData;
        EditorSceneManager.SetActiveScene(scene);
    }

    static void SaveSubSceneMenuHandler(object userData)
    {
        var subScene = (SceneHierarchyHooks.SubSceneInfo)userData;
        if (subScene.scene.isLoaded && subScene.scene.isDirty)
            EditorSceneManager.SaveScene(subScene.scene);
    }

    static void SelectSceneAsset(object userData)
    {
        var subScene = (SceneHierarchyHooks.SubSceneInfo)userData;
        var sceneAssetObject = AssetDatabase.LoadMainAssetAtPath(subScene.scene.path);
        Selection.activeObject = sceneAssetObject;
        EditorGUIUtility.PingObject(sceneAssetObject);
    }

    internal static string GetSubSceneHeaderText(GameObject gameObject)
    {
        SceneHierarchyHooks.SubSceneInfo subScene;
        if (m_SubSceneHeadersMap.TryGetValue(gameObject, out subScene))
            return subScene.sceneName;

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

    internal static void DrawSubSceneHeaderBackground(Rect rect, GameObject gameObject)
    {
        float indent = CalcIndentOfVerticalLine(gameObject);
        if (indent < 0)
            indent = 0f;
        Rect headerRect = rect;
        headerRect.xMin += indent;

        Color oldColor = GUI.color;
        GUI.color = GUI.color * new Color(1, 1, 1, 0.9f); // dimmed compared to main scene headers
        GUI.Label(headerRect, GUIContent.none, GameObjectTreeViewGUI.GameObjectStyles.sceneHeaderBg);
        GUI.color = oldColor;
    }

    internal static void DrawVerticalLine(Rect rect, GameObject gameObject)
    {
        if (Event.current.type == EventType.Repaint)
        {
            float indent = CalcIndentOfVerticalLine(gameObject);
            if (indent > 0)
            {
                Rect lineRect = rect;
                lineRect.x += indent;
                lineRect.width = 1;

                EditorGUI.DrawRect(lineRect, GetColorForSubScene(gameObject.scene));
            }
        }
    }

    static float CalcIndentOfVerticalLine(GameObject gameObject)
    {
        SceneHierarchyHooks.SubSceneInfo subScene;
        if (gameObject == null || !m_SceneToSubSceneMap.TryGetValue(gameObject.scene, out subScene))
            return -1;  // Input is not a sub scene GameObject

        int hierarchyDepth = CalculateHierarchyDepthOfSubScene(subScene);
        if (hierarchyDepth > 0)
        {
            float padding = 28; // visibility icon area
            float indentWidth = 14f;
            float indent = hierarchyDepth * indentWidth + 4f + padding;
            return indent;
        }
        return -1f;
    }

    internal static int CalculateHierarchyDepthOfSubScene(SceneHierarchyHooks.SubSceneInfo subScene)
    {
        if (!subScene.isValid)
            return -1;

        int hierarchyDepth = 0; // Root scene offset
        while (true)
        {
            hierarchyDepth += FindTransformDepth(subScene.transform) + 1;  // the +1 is for the SubScene header
            if (!m_SceneToSubSceneMap.TryGetValue(subScene.transform.gameObject.scene, out subScene))
                break;
        }

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
