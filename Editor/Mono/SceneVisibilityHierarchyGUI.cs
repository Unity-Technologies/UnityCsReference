// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Experimental;
using UnityEditor.IMGUI.Controls;
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
            }

            public static readonly IconState iconNormal = new IconState
            {
                visibleAll = EditorGUIUtility.TrIconContent("scenevis_visible"),
                visibleMixed = EditorGUIUtility.TrIconContent("scenevis_visible-mixed"),
                hiddenAll = EditorGUIUtility.TrIconContent("scenevis_hidden"),
                hiddenMixed = EditorGUIUtility.TrIconContent("scenevis_hidden-mixed"),
            };

            public static readonly IconState iconHovered = new IconState
            {
                visibleAll = EditorGUIUtility.TrIconContent("scenevis_visible_hover"),
                visibleMixed = EditorGUIUtility.TrIconContent("scenevis_visible-mixed_hover"),
                hiddenAll = EditorGUIUtility.TrIconContent("scenevis_hidden_hover"),
                hiddenMixed = EditorGUIUtility.TrIconContent("scenevis_hidden-mixed_hover"),
            };

            public static readonly Color backgroundColor = EditorResources.GetStyle("game-object-tree-view-scene-visibility")
                .GetColor("background-color");

            public static readonly Color hoveredBackgroundColor = EditorResources.GetStyle("game-object-tree-view-scene-visibility")
                .GetColor("-unity-object-hovered-color");

            public static readonly Color selectedBackgroundColor = EditorResources.GetStyle("game-object-tree-view-scene-visibility")
                .GetColor("-unity-object-selected-color");

            public static readonly Color selectedNoFocusBackgroundColor = EditorResources.GetStyle("game-object-tree-view-scene-visibility")
                .GetColor("-unity-object-selected-no-focus-color");

            public static readonly GUIContent iconSceneHovered = EditorGUIUtility.TrIconContent("scenevis_scene_hover");

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

        private const int k_VisibilityIconPadding = 4;
        private const int k_IconWidth = 16;
        private static readonly float k_sceneHeaderOverflow = GameObjectTreeViewGUI.GameObjectStyles.sceneHeaderBg.fixedHeight - EditorGUIUtility.singleLineHeight;
        private static bool m_PrevItemWasScene;

        public const float utilityBarWidth = k_VisibilityIconPadding * 2 + k_IconWidth;

        public static void DrawBackground(Rect rect)
        {
            rect.width = utilityBarWidth;

            using (new GUI.BackgroundColorScope(Styles.backgroundColor))
            {
                GUI.Label(rect, GUIContent.none, GameObjectTreeViewGUI.GameObjectStyles.hoveredItemBackgroundStyle);
            }
        }

        public static void DoItemGUI(Rect rect, GameObjectTreeViewItem goItem, bool isSelected, bool isHovered, bool isFocused)
        {
            Rect iconRect = rect;
            iconRect.xMin += k_VisibilityIconPadding;
            iconRect.width = k_IconWidth;

            bool isIconHovered = iconRect.Contains(Event.current.mousePosition);

            if (isHovered)
            {
                GUIView.current.MarkHotRegion(GUIClip.UnclipToWindow(iconRect));
            }

            GameObject gameObject = goItem.objectPPTR as GameObject;
            if (gameObject)
            {
                // The scene header overlaps it's next item by some pixels. Displace the background so it doesn't draw on top of the scene header.
                // Don't displace when selected or hovered (They already show on top of the header)
                if (m_PrevItemWasScene && !isSelected && !isHovered)
                    rect.yMin += k_sceneHeaderOverflow;

                DrawItemBackground(rect, isSelected, isHovered, isFocused);
                DrawGameObjectItem(iconRect, gameObject, isHovered, isIconHovered);
                m_PrevItemWasScene = false;
            }
            else
            {
                Scene scene = goItem.scene;
                if (scene.IsValid())
                {
                    DrawSceneItem(iconRect, scene, isHovered, isIconHovered);
                    m_PrevItemWasScene = true;
                }
            }
        }

        private static void DrawItemBackground(Rect rect, bool isSelected, bool isHovered, bool isFocused)
        {
            if (Event.current.type == EventType.Repaint)
            {
                rect.width = utilityBarWidth;

                using (new GUI.BackgroundColorScope(Styles.GetItemBackgroundColor(isHovered, isSelected, isFocused)))
                {
                    GUI.Label(rect, GUIContent.none, GameObjectTreeViewGUI.GameObjectStyles.hoveredItemBackgroundStyle);
                }
            }
        }

        private static void DrawGameObjectItem(Rect rect, GameObject gameObject, bool isItemHovered, bool isIconHovered)
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
                SceneVisibilityManager.instance.ToggleVisibility(gameObject, Event.current.alt);
            }
        }

        private static void DrawSceneItem(Rect rect, Scene scene, bool isItemHovered, bool isIconHovered)
        {
            var isHidden = SceneVisibilityManager.instance.AreAllDescendantsHidden(scene);
            bool shouldDisplayIcon = true;

            GUIContent icon;
            if (!isIconHovered)
            {
                if (isHidden)
                {
                    icon = Styles.iconNormal.hiddenAll;
                }
                else if (SceneVisibilityManager.instance.AreAnyDescendantsHidden(scene))
                {
                    icon = Styles.iconNormal.hiddenMixed;
                }
                else
                {
                    icon = Styles.iconNormal.visibleAll;
                    shouldDisplayIcon = isItemHovered;
                }
            }
            else
            {
                icon = Styles.iconSceneHovered;
            }

            if (shouldDisplayIcon && GUI.Button(rect, icon, Styles.sceneVisibilityStyle))
            {
                if (isHidden)
                {
                    SceneVisibilityManager.instance.Show(scene);
                }
                else
                {
                    SceneVisibilityManager.instance.Hide(scene);
                }
            }
        }
    }
}
