// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Experimental;
using UnityEditor.IMGUI.Controls;
using UnityEditor.StyleSheets;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityEditor
{
    internal static class SceneVisibilityHierarchyGUI
    {
        internal static class Styles
        {
            public class IconState
            {
                public GUIContent visibleAll;
                public GUIContent visibleMixed;
                public GUIContent hiddenAll;
                public GUIContent hiddenMixed;
                public GUIContent pickingEnabledAll;
                public GUIContent pickingEnabledMixed;
                public GUIContent pickingDisabledAll;
                public GUIContent pickingDisabledMixed;
            }

            public static readonly IconState iconNormal = new IconState
            {
                visibleAll = EditorGUIUtility.TrIconContent("scenevis_visible"),
                visibleMixed = EditorGUIUtility.TrIconContent("scenevis_visible-mixed"),
                hiddenAll = EditorGUIUtility.TrIconContent("scenevis_hidden"),
                hiddenMixed = EditorGUIUtility.TrIconContent("scenevis_hidden-mixed"),
                pickingEnabledAll = EditorGUIUtility.TrIconContent("scenepicking_pickable"),
                pickingEnabledMixed = EditorGUIUtility.TrIconContent("scenepicking_pickable-mixed"),
                pickingDisabledAll = EditorGUIUtility.TrIconContent("scenepicking_notpickable"),
                pickingDisabledMixed = EditorGUIUtility.TrIconContent("scenepicking_notpickable-mixed"),
            };

            public static readonly IconState iconHovered = new IconState
            {
                visibleAll = EditorGUIUtility.TrIconContent("scenevis_visible_hover"),
                visibleMixed = EditorGUIUtility.TrIconContent("scenevis_visible-mixed_hover"),
                hiddenAll = EditorGUIUtility.TrIconContent("scenevis_hidden_hover"),
                hiddenMixed = EditorGUIUtility.TrIconContent("scenevis_hidden-mixed_hover"),
                pickingEnabledAll = EditorGUIUtility.TrIconContent("scenepicking_pickable_hover"),
                pickingEnabledMixed = EditorGUIUtility.TrIconContent("scenepicking_pickable-mixed_hover"),
                pickingDisabledAll = EditorGUIUtility.TrIconContent("scenepicking_notpickable_hover"),
                pickingDisabledMixed = EditorGUIUtility.TrIconContent("scenepicking_notpickable-mixed_hover"),
            };

            public static readonly Color backgroundColor = EditorResources.GetStyle("game-object-tree-view-scene-visibility")
                .GetColor("background-color");

            public static readonly Color hoveredBackgroundColor = EditorResources.GetStyle("game-object-tree-view-scene-visibility")
                .GetColor("-unity-object-hovered-color");

            public static readonly Color selectedBackgroundColor = EditorResources.GetStyle("game-object-tree-view-scene-visibility")
                .GetColor("-unity-object-selected-color");

            public static readonly Color selectedNoFocusBackgroundColor = EditorResources.GetStyle("game-object-tree-view-scene-visibility")
                .GetColor("-unity-object-selected-no-focus-color");

            public static readonly GUIStyle sceneVisibilityStyle = "SceneVisibility";

            public static Color GetItemBackgroundColor(bool isHovered, bool isSelected, bool isFocused)
            {
                if (isSelected)
                {
                    if (isFocused)
                        return selectedBackgroundColor;

                    return selectedNoFocusBackgroundColor;
                }

                if (isHovered)
                    return hoveredBackgroundColor;

                return backgroundColor;
            }
        }

        private const int k_VisibilityIconPadding = 0;
        private const int k_IconWidth = 16;

        private static float k_sceneHeaderOverflow => GameObjectTreeViewGUI.GameObjectStyles.sceneHeaderBg.fixedHeight + 2*GameObjectTreeViewGUI.GameObjectStyles.sceneHeaderWidth - EditorGUIUtility.singleLineHeight;
        private static bool m_PrevItemWasScene;

        public const float utilityBarWidth = k_VisibilityIconPadding * 3 + k_IconWidth * 2;

        public static void DrawBackground(Rect rect)
        {
            rect.width = utilityBarWidth;

            using (new GUI.BackgroundColorScope(Styles.backgroundColor))
            {
                GUI.Label(rect, GUIContent.none, GameObjectTreeViewGUI.GameObjectStyles.hoveredItemBackgroundStyle);
            }
        }

        public static void DoItemGUI(Rect rect, GameObjectTreeViewItem goItem, bool isSelected, bool isHovered, bool isFocused, bool isDragging)
        {
            if (Event.current.isKey || Event.current.type == EventType.Layout)
                return;

            Rect iconRect = rect;
            iconRect.xMin += k_VisibilityIconPadding;
            iconRect.width = k_IconWidth;
            isHovered = isHovered && !isDragging;
            bool isIconHovered = !isDragging && iconRect.Contains(Event.current.mousePosition);

            Rect icon2Rect = rect;
            icon2Rect.xMin += 2 * k_VisibilityIconPadding + k_IconWidth;
            icon2Rect.width = k_IconWidth;
            bool isIcon2Hovered = !isDragging && icon2Rect.Contains(Event.current.mousePosition);

            if (isHovered)
            {
                GUIView.current.MarkHotRegion(GUIClip.UnclipToWindow(iconRect));
                GUIView.current.MarkHotRegion(GUIClip.UnclipToWindow(icon2Rect));
            }

            GameObject gameObject = goItem.objectPPTR as GameObject;
            if (gameObject)
            {
                // The scene header overlaps it's next item by some pixels. Displace the background so it doesn't draw on top of the scene header.
                // Don't displace when selected or hovered (They already show on top of the header)
                if (m_PrevItemWasScene && !isSelected && !isHovered)
                    rect.yMin += k_sceneHeaderOverflow;

                DrawItemBackground(rect, false, isSelected, isHovered, isFocused);
                DrawGameObjectItemVisibility(iconRect, gameObject, isHovered, isIconHovered);
                DrawGameObjectItemPicking(icon2Rect, gameObject, isHovered, isIcon2Hovered);

                m_PrevItemWasScene = false;
            }
            else
            {
                Scene scene = goItem.scene;
                if (scene.IsValid())
                {
                    DrawItemBackground(rect, true, isSelected, isHovered, isFocused);
                    DrawSceneItemVisibility(iconRect, scene, isHovered, isIconHovered);
                    DrawSceneItemPicking(icon2Rect, scene, isHovered, isIcon2Hovered);
                    m_PrevItemWasScene = true;
                }
            }
        }

        private static void DrawItemBackground(Rect rect, bool isScene, bool isSelected, bool isHovered, bool isFocused)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            rect.width = utilityBarWidth;

            if (isScene)
            {
                if (isSelected)
                {
                    TreeViewGUI.Styles.selectionStyle.Draw(rect, false, false, true, isFocused);
                }
            }
            else
            {
                using (new GUI.BackgroundColorScope(Styles.GetItemBackgroundColor(isHovered, isSelected, isFocused))
                )
                {
                    GUI.Label(rect, GUIContent.none,
                        GameObjectTreeViewGUI.GameObjectStyles.hoveredItemBackgroundStyle);
                }
            }
        }

        private static void DrawGameObjectItemVisibility(Rect rect, GameObject gameObject, bool isItemHovered, bool isIconHovered)
        {
            var isHidden = SceneVisibilityManager.instance.IsHidden(gameObject);
            bool shouldDisplayIcon = isItemHovered || isHidden;
            Styles.IconState iconState = isIconHovered ? Styles.iconHovered : Styles.iconNormal;

            GUIContent icon;
            if (isHidden)
            {
                icon = gameObject.transform.childCount == 0 || SceneVisibilityManager.instance.AreAllDescendantsHidden(gameObject)
                    ? iconState.hiddenAll : iconState.hiddenMixed;
            }
            else if (!SceneVisibilityManager.instance.AreAllDescendantsVisible(gameObject))
            {
                icon = iconState.visibleMixed;
                shouldDisplayIcon = true;
            }
            else
            {
                icon = iconState.visibleAll;
            }

            if (shouldDisplayIcon && GUI.Button(rect, icon, Styles.sceneVisibilityStyle))
            {
                SceneVisibilityManager.instance.ToggleVisibility(gameObject, !Event.current.alt);
            }
        }

        private static void DrawGameObjectItemPicking(Rect rect, GameObject gameObject, bool isItemHovered, bool isIconHovered)
        {
            var isPickingDisabled = SceneVisibilityManager.instance.IsPickingDisabled(gameObject);
            bool shouldDisplayIcon = isItemHovered || isPickingDisabled;
            Styles.IconState iconState = isIconHovered ? Styles.iconHovered : Styles.iconNormal;

            GUIContent icon;
            if (isPickingDisabled)
            {
                icon = gameObject.transform.childCount == 0 || SceneVisibilityManager.instance.IsPickingDisabledOnAllDescendants(gameObject)
                    ? iconState.pickingDisabledAll : iconState.pickingDisabledMixed;
            }
            else if (!SceneVisibilityManager.instance.IsPickingEnabledOnAllDescendants(gameObject))
            {
                icon = iconState.pickingEnabledMixed;
                shouldDisplayIcon = true;
            }
            else
            {
                icon = iconState.pickingEnabledAll;
            }

            if (shouldDisplayIcon && GUI.Button(rect, icon, Styles.sceneVisibilityStyle))
            {
                SceneVisibilityManager.instance.TogglePicking(gameObject, !Event.current.alt);
            }
        }

        private static void DrawSceneItemVisibility(Rect rect, Scene scene, bool isItemHovered, bool isIconHovered)
        {
            var state = SceneVisibilityManager.instance.GetSceneVisibilityState(scene);
            bool shouldDisplayIcon = true;
            Styles.IconState iconState = isIconHovered ? Styles.iconHovered : Styles.iconNormal;

            GUIContent icon;
            if (state == SceneVisibilityManager.SceneVisState.AllHidden)
            {
                icon = iconState.hiddenAll;
            }
            else if (state == SceneVisibilityManager.SceneVisState.Mixed)
            {
                icon = iconState.visibleMixed;
            }
            else
            {
                icon = iconState.visibleAll;
                shouldDisplayIcon = isItemHovered;
            }


            if (shouldDisplayIcon && GUI.Button(rect, icon, Styles.sceneVisibilityStyle))
            {
                SceneVisibilityManager.instance.ToggleScene(scene, state);
            }
        }

        private static void DrawSceneItemPicking(Rect rect, Scene scene, bool isItemHovered, bool isIconHovered)
        {
            var state = SceneVisibilityManager.instance.GetScenePickingState(scene);
            bool shouldDisplayIcon = true;
            Styles.IconState iconState = isIconHovered ? Styles.iconHovered : Styles.iconNormal;

            GUIContent icon;
            var enablePicking = false;
            if (state == SceneVisibilityManager.ScenePickingState.PickingDisabledAll)
            {
                icon = iconState.pickingDisabledAll;
                enablePicking = true;
            }
            else if (state == SceneVisibilityManager.ScenePickingState.Mixed)
            {
                icon = iconState.pickingEnabledMixed;
            }
            else
            {
                icon = iconState.pickingEnabledAll;
                shouldDisplayIcon = isItemHovered;
            }

            if (shouldDisplayIcon && GUI.Button(rect, icon, Styles.sceneVisibilityStyle))
            {
                if (enablePicking)
                {
                    SceneVisibilityManager.instance.EnablePicking(scene);
                }
                else
                {
                    SceneVisibilityManager.instance.DisablePicking(scene);
                }
            }
        }
    }
}
