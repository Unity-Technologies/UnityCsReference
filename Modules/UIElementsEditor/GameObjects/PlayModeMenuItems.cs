// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    internal static class PlayModeMenuItems
    {
        internal static readonly string k_UITKEssentialResourcesFolderPath = Path.Combine(k_AssetsFolder, k_UITKEssentialResourcesFolderName);
        internal const string k_UITKEssentialResourcesFolderName = "UI Toolkit";
        private const string k_UILayerName = "UI";
        private const string k_AssetSearchByTypePanelSettings = "t:panelsettings";
        private const string k_AssetsFolder = "Assets";
        private static readonly string k_PanelSettingsAssetPath = k_UITKEssentialResourcesFolderPath + "/PanelSettings.asset";
        private static string[] k_AssetsFolderFilter = new[] { k_AssetsFolder };

        /// <summary>
        /// Returns a PanelSettings from the project if one exists, otherwise creates one with the
        /// appropriate theme and saves it.
        /// </summary>
        /// <remarks>
        /// Theme resolution priority:
        /// 1. The project-level runtime theme from UIToolkitProjectSettings, if configured.
        /// 2. An existing ThemeStyleSheet asset in the project.
        /// 3. A newly created default theme file (project-local .tss that is included in builds).
        /// </remarks>
        internal static PanelSettings GetPanelSettingsFromProjectOrCreate()
        {
            var guids = AssetDatabase.FindAssets(k_AssetSearchByTypePanelSettings, k_AssetsFolderFilter);
            if (guids != null && guids.Length > 0)
            {
                return AssetDatabase.LoadAssetAtPath<PanelSettings>(
                    AssetDatabase.GUIDToAssetPath(guids[0]));
            }

            var projectRuntimeTheme = UIToolkitProjectSettings.defaultRuntimeTheme;
            var themeToUse = projectRuntimeTheme != null
                ? projectRuntimeTheme
                : PanelSettingsCreator.GetFirstThemeOrCreateDefaultTheme();
            var panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            panelSettings.themeStyleSheet = themeToUse;
            panelSettings.AssignICUData();

            if (!AssetDatabase.IsValidFolder(k_UITKEssentialResourcesFolderPath))
                AssetDatabase.CreateFolder(k_AssetsFolder, k_UITKEssentialResourcesFolderName);

            AssetDatabase.CreateAsset(panelSettings, k_PanelSettingsAssetPath);
            return AssetDatabase.LoadAssetAtPath<PanelSettings>(k_PanelSettingsAssetPath);
        }

        [MenuItem("GameObject/UI Toolkit/Panel Input Configuration", false, 9)]
        public static void AddPanelInputConfiguration(MenuCommand menuCommand)
        {
            AddPanelInputConfiguration(menuCommand.context as GameObject);
        }

        public static void AddPanelInputConfiguration(GameObject parent = null)
        {
            var root = ObjectFactory.CreateGameObject(nameof(PanelInputConfiguration));
            root.SetActive(false);
            root.AddComponent<PanelInputConfiguration>();
            GameObjectUtility.EnsureUniqueNameForSibling(root);
            root.SetActive(true);

            // Works for all stages.
            StageUtility.PlaceGameObjectInCurrentStage(root);
            PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                Undo.SetTransformParent(root.transform, prefabStage.prefabContentsRoot.transform, "");
            }

            Undo.SetCurrentGroupName("Create " + root.name);

            if (parent != null)
            {
                SetParentAndAlign(root, parent);
            }

            Selection.activeGameObject = root;
        }

        [MenuItem("GameObject/UI Toolkit/Legacy/UI Document", false, 10)]
        public static void AddUIDocument(MenuCommand menuCommand)
        {
            AddPanelComponentHelper<UIDocument>(menuCommand);
        }

        [MenuItem("GameObject/UI Toolkit/Panel Renderer", false, 8)]
        public static void AddPanelRenderer(MenuCommand menuCommand)
        {
            AddPanelComponentHelper<PanelRenderer>(menuCommand);
        }

        internal static T AddPanelComponentHelper<T>(MenuCommand menuCommand) where T: IPanelComponent
        {
            GameObject parent = menuCommand.context as GameObject;
            Type type = typeof(T);
            var root = ObjectFactory.CreateGameObject(type.Name, type);
            GameObjectUtility.EnsureUniqueNameForSibling(root);
            var panelComponent = root.GetComponent<T>();

            // Works for all stages.
            StageUtility.PlaceGameObjectInCurrentStage(root);
            PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                Undo.SetTransformParent(root.transform, prefabStage.prefabContentsRoot.transform, "");
            }

            Undo.SetCurrentGroupName("Create " + root.name);

            if (parent != null)
            {
                SetParentAndAlign(root, parent);
                if (panelComponent is UIDocument panelComponentAsUIDocument)
                    panelComponentAsUIDocument.ReactToHierarchyChanged();
            }
            else
            {
                root.layer = LayerMask.NameToLayer(k_UILayerName);
            }

            Selection.activeGameObject = root;

            // Set a PanelSettings instance so that the UI appears immediately on selecting the UXML.
            // If the Panel Component was created as a child of another Panel Component, this step is not necessary.
            if (panelComponent.parentUI == null)
                panelComponent.panelSettings = GetPanelSettingsFromProjectOrCreate();

            return panelComponent;
        }

        private static void SetParentAndAlign(GameObject child, GameObject parent)
        {
            if (parent == null)
                return;

            Undo.SetTransformParent(child.transform, parent.transform, "");
            child.transform.localPosition = Vector3.zero;
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = Vector3.one;

            child.layer = parent.layer;
        }
    }
}
