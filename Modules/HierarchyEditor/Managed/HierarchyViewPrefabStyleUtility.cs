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
        /// Set different prefab styles for the node.
        /// </summary>
        public static void SetNodePrefabStyle(GameObject gameObject, HierarchyViewItem item)
        {
            if (PrefabUtility.ShowPrefabModeButton(gameObject))
                SetNavigationButton(gameObject, item);
            else
                ClearNavigationButton(item);

            if (PrefabUtility.IsAnyPrefabInstanceRoot(gameObject))
            {
                var sourceGO = PrefabUtility.GetOriginalSourceOrVariantRoot(gameObject);
                var prefabAssetType = PrefabUtility.GetPrefabAssetType(gameObject);
                if (sourceGO != null && sourceGO != gameObject)
                {
                    prefabAssetType = PrefabUtility.GetPrefabAssetType(sourceGO);
                }

                SetPrefabIcon(prefabAssetType, item);
            }
            else
                ClearPrefabIcon(item);

            var isPartOfPrefab = PrefabUtility.IsPartOfAnyPrefab(gameObject);
            var isMissing = PrefabUtility.IsPrefabAssetMissing(gameObject);
            item.EnableInClassList(s_HierarchyItemPrefabBlueTextStyle, isPartOfPrefab && !isMissing);
            item.EnableInClassList(s_HierarchyItemMissingPrefabStyle, isMissing);
            item.EnableInClassList(s_HierarchyItemPrefabOverlayStyle, PrefabUtility.IsAddedGameObjectOverride(gameObject));

            item.OverrideBarContainer.EnableInClassList(s_HierarchyItemPrefabOverrideStyle, PrefabUtility.IsOutermostPrefabInstanceRoot(gameObject)
                && PrefabUtility.HasPrefabInstanceNonDefaultOverridesOrUnusedOverrides_CachedForUI(gameObject));
        }

        /// <summary>
        /// Clean up all the styles set in SetNodePrefabStyle when releasing the visual element.
        /// </summary>
        public static void CleanUpNodePrefabStyle(HierarchyViewItem item)
        {
            item.RemoveFromClassList(s_HierarchyItemPrefabBlueTextStyle);
            item.RemoveFromClassList(s_HierarchyItemMissingPrefabStyle);
            item.RemoveFromClassList(s_HierarchyItemPrefabOverlayStyle);
            item.OverrideBarContainer.RemoveFromClassList(s_HierarchyItemPrefabOverrideStyle);

            ClearPrefabIcon(item);
            ClearNavigationButton(item);
        }

        static void SetPrefabIcon(PrefabAssetType prefabType, HierarchyViewItem element)
        {
            switch (prefabType)
            {
                case PrefabAssetType.NotAPrefab:
                    ClearPrefabIcon(element);
                    break;

                case PrefabAssetType.Regular:
                    element.AddToClassList(s_HierarchyItemNormalPrefabStyle);
                    element.RemoveFromClassList(s_HierarchyItemVariantPrefabStyle);
                    element.RemoveFromClassList(s_HierarchyItemModelPrefabStyle);
                    element.RemoveFromClassList(s_HierarchyItemGameObjectStyle);
                    break;

                case PrefabAssetType.Model:
                    element.AddToClassList(s_HierarchyItemModelPrefabStyle);
                    element.RemoveFromClassList(s_HierarchyItemNormalPrefabStyle);
                    element.RemoveFromClassList(s_HierarchyItemVariantPrefabStyle);
                    element.RemoveFromClassList(s_HierarchyItemGameObjectStyle);
                    break;

                case PrefabAssetType.Variant:
                    element.AddToClassList(s_HierarchyItemVariantPrefabStyle);
                    element.RemoveFromClassList(s_HierarchyItemNormalPrefabStyle);
                    element.RemoveFromClassList(s_HierarchyItemModelPrefabStyle);
                    element.RemoveFromClassList(s_HierarchyItemGameObjectStyle);
                    break;

                case PrefabAssetType.MissingAsset:
                    element.AddToClassList(s_HierarchyItemNormalPrefabStyle);
                    element.RemoveFromClassList(s_HierarchyItemVariantPrefabStyle);
                    element.RemoveFromClassList(s_HierarchyItemModelPrefabStyle);
                    element.RemoveFromClassList(s_HierarchyItemGameObjectStyle);
                    break;

                default:
                    break;
            }
        }

        static void ClearPrefabIcon(HierarchyViewItem item)
        {
            item.RemoveFromClassList(s_HierarchyItemNormalPrefabStyle);
            item.RemoveFromClassList(s_HierarchyItemVariantPrefabStyle);
            item.RemoveFromClassList(s_HierarchyItemModelPrefabStyle);
        }

        static void SetNavigationButton(GameObject gameObject, HierarchyViewItem item)
        {
            item.NavigateIntoButton.style.display = DisplayStyle.Flex;

            var btnContent = PrefabStageUtility.GetPrefabButtonContent(gameObject.GetEntityId());

            item.NavigateIntoButton.tooltip = btnContent.tooltip;

            // TODO: when we support multiple actions for the navigation button this will have to change.
            // For now, it assumes that there is never more than a single distinct action
            if (item.NavigateIntoButton.userData == null)
            {
                item.NavigateIntoButton.clickable.activators.Add(k_PrefabAltActivationFilter);
                item.NavigateIntoButton.clickable.clickedWithEventInfo += OpenPrefab;
            }
            item.NavigateIntoButton.userData = gameObject;
        }

        static void ClearNavigationButton(HierarchyViewItem item)
        {
            item.NavigateIntoButton.style.display = DisplayStyle.None;
            item.NavigateIntoButton.tooltip = null;
            item.NavigateIntoButton.userData = null;
            item.NavigateIntoButton.clickable.clickedWithEventInfo -= OpenPrefab;
            item.NavigateIntoButton.clickable.activators.Remove(k_PrefabAltActivationFilter);
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
