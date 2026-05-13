// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Hierarchy.Editor
{
    /// <summary>
    /// Prefab style utility class.
    /// </summary>
    static class HierarchyViewPrefabStyleUtility
    {
        static readonly string s_HierarchyItemNormalPrefabStyle = "hierarchy-item__prefab-normal-node";
        static readonly string s_HierarchyItemVariantPrefabStyle = "hierarchy-item__prefab-variant-node";
        static readonly string s_HierarchyItemModelPrefabStyle = "hierarchy-item__prefab-model-node";
        static readonly string s_HierarchyItemGameObjectStyle = "hierarchy-item__gameobject-node";

        static readonly string s_HierarchyItemPrefabBlueTextStyle = "hierarchy-item__prefab-node-blue-text";
        static readonly string s_HierarchyItemMissingPrefabStyle = "hierarchy-item__prefab-node-red-text";
        static readonly string s_HierarchyItemPrefabOverlayStyle = "hierarchy-item__prefab-overlay-node";
        static readonly string s_HierarchyItemPrefabOverrideStyle = "hierarchy-item__prefab-override-bar";

        static readonly ManipulatorActivationFilter k_PrefabAltActivationFilter = new() { button = MouseButton.LeftMouse, modifiers = EventModifiers.Alt };

        /// <summary>
        /// Set styles for root prefabs
        /// </summary>
        public static void SetNodePrefabRootStyle(GameObject gameObject, HierarchyViewItem item)
        {
            var sourceGO = PrefabUtility.GetOriginalSourceOrVariantRoot(gameObject);
            var prefabAssetType = PrefabUtility.GetPrefabAssetType(gameObject);
            if (sourceGO != null && sourceGO != gameObject)
            {
                prefabAssetType = PrefabUtility.GetPrefabAssetType(sourceGO);
            }

            SetPrefabIcon(prefabAssetType, item);
        }

        public static void ClearPrefabRootStyle(HierarchyViewItem item)
        {
            SetPrefabIcon(PrefabAssetType.NotAPrefab, item);
        }

        /// <summary>
        /// Set the minimum required styles for a broken or disconnected prefab instance
        /// so that we can bypass expensive PrefabUtility calls that would normally run.
        /// </summary>
        public static void SetBrokenPrefabStyle(HierarchyViewItem item)
        {
            item.EnableInClassList(s_HierarchyItemPrefabBlueTextStyle, false);
            item.EnableInClassList(s_HierarchyItemMissingPrefabStyle, true);
            item.EnableInClassList(s_HierarchyItemPrefabOverlayStyle, false);
            item.OverrideBarContainer.EnableInClassList(s_HierarchyItemPrefabOverrideStyle, false);
            SetPrefabIcon(PrefabAssetType.MissingAsset, item);
            ClearNavigationButton(item);
        }

        /// <summary>
        /// Set different prefab styles for the node.
        /// </summary>
        public static void SetNodePrefabGenericStyle(GameObject gameObject, HierarchyViewItem item)
        {
            var isPartOfPrefab = PrefabUtility.IsPartOfAnyPrefab(gameObject);
            var isMissing = isPartOfPrefab && PrefabUtility.IsPrefabAssetMissing(gameObject);

            item.EnableInClassList(s_HierarchyItemPrefabBlueTextStyle, isPartOfPrefab && !isMissing);
            item.EnableInClassList(s_HierarchyItemMissingPrefabStyle, isMissing);
            item.OverrideBarContainer.EnableInClassList(s_HierarchyItemPrefabOverrideStyle,
                isPartOfPrefab
                && PrefabUtility.IsOutermostPrefabInstanceRoot(gameObject)
                && PrefabUtility.HasPrefabInstanceNonDefaultOverridesOrUnusedOverrides_CachedForUI(gameObject));
            item.EnableInClassList(s_HierarchyItemPrefabOverlayStyle, PrefabUtility.IsAddedGameObjectOverride(gameObject));
        }

        static void SetPrefabIcon(PrefabAssetType prefabType, HierarchyViewItem element)
        {
            var isRegularOrMissing = prefabType == PrefabAssetType.Regular || prefabType == PrefabAssetType.MissingAsset;
            var isVariant = prefabType == PrefabAssetType.Variant;
            var isModel = prefabType == PrefabAssetType.Model;

            element.EnableInClassList(s_HierarchyItemNormalPrefabStyle, isRegularOrMissing);
            element.EnableInClassList(s_HierarchyItemVariantPrefabStyle, isVariant);
            element.EnableInClassList(s_HierarchyItemModelPrefabStyle, isModel);

            element.EnableInClassList(s_HierarchyItemGameObjectStyle, !(isRegularOrMissing || isVariant || isModel));
        }

        internal static void SetNavigationButton(GameObject gameObject, HierarchyViewItem item)
        {
            var btnContent = PrefabStageUtility.GetPrefabButtonContent(gameObject.GetEntityId());
            var navigateButton = item.NavigateIntoButton;
            if (navigateButton == null)
                return;

            navigateButton.style.display = DisplayStyle.Flex;
            navigateButton.tooltip = btnContent.tooltip;

            // TODO: when we support multiple actions for the navigation button this will have to change.
            // For now, it assumes that there is never more than a single distinct action
            if (navigateButton.userData == null)
            {
                navigateButton.clickable.activators.Add(k_PrefabAltActivationFilter);
                navigateButton.clickable.clickedWithEventInfo += OpenPrefab;
            }
            navigateButton.userData = gameObject;
        }

        internal static void ClearNavigationButton(HierarchyViewItem item)
        {
            var navigateButton = item.NavigateIntoButton;
            if (navigateButton == null)
                return;

            navigateButton.style.display = DisplayStyle.None;
            navigateButton.tooltip = null;
            navigateButton.userData = null;
            navigateButton.clickable.clickedWithEventInfo -= OpenPrefab;
            navigateButton.clickable.activators.Remove(k_PrefabAltActivationFilter);
        }

        internal static void OpenPrefab(GameObject gameObject)
        {
            OpenPrefab(HierarchyPreferences.DefaultPrefabModeFromHierarchy, gameObject);
        }

        static void OpenPrefab(EventBase evt)
        {
            // Note: GetPrefabStageModeFromModifierKeys uses Event.alt to validate if the alt key is held.
            if (evt.target is not Button button)
                return;
            var gameObject = button.userData as GameObject;
            if (gameObject != null)
                OpenPrefab(PrefabStageUtility.GetPrefabStageModeFromModifierKeys(), gameObject);
        }

        static void OpenPrefab(PrefabStage.Mode mode, GameObject gameObject)
        {
            string assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObject);
            if (string.IsNullOrWhiteSpace(assetPath)) //In case its a broken prefab
                assetPath = PrefabUtility.GetAssetPathOfSourcePrefab(gameObject);

            var originalSource = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (originalSource != null)
            {
                PrefabStageUtility.OpenPrefab(assetPath, gameObject, mode, StageNavigationManager.Analytics.ChangeType.EnterViaInstanceHierarchyRightArrow);
            }
        }
    }
}
