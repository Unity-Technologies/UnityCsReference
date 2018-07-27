// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;

namespace UnityEditor
{
    internal class PrefabOverridesWindow : PopupWindowContent
    {
        RectOffset k_TreeViewPadding = new RectOffset(0, 0, 4, 4);
        const float k_HeaderHeight = 32f;
        const float k_ButtonWidth = 120;
        const float k_HeaderLeftMargin = 6;

        TreeViewState m_TreeViewState;
        PrefabOverridesTreeView m_TreeView;
        GameObject m_SelectedGameObject;

        GUIContent m_StageContent = new GUIContent();
        GUIContent m_InstanceContent = new GUIContent();
        GUIContent m_RevertAllContent = new GUIContent();
        GUIContent m_ApplyAllContent = new GUIContent();

        bool m_Immutable;
        bool m_InvalidComponentOnInstance;
        bool m_InvalidComponentOnAsset;
        static class Styles
        {
            public static GUIContent revertAllContent = EditorGUIUtility.TrTextContent("Revert All", "Revert all overrides.");
            public static GUIContent applyAllContent = EditorGUIUtility.TrTextContent("Apply All", "Apply all overrides to Prefab source '{0}'.");
            public static GUIContent applyAllToBaseContent = EditorGUIUtility.TrTextContent("Apply All to Base", "Apply all overrides to base Prefab source '{0}'.");
            public static GUIContent instanceLabel = EditorGUIUtility.TrTextContent("Overrides to");
            public static GUIContent contextLabel = EditorGUIUtility.TrTextContent("in");

            public static GUIStyle boldRightAligned;

            static Styles()
            {
                boldRightAligned = new GUIStyle(EditorStyles.boldLabel);
                boldRightAligned.alignment = TextAnchor.MiddleRight;
            }
        }

        internal PrefabOverridesWindow(GameObject selectedGameObject)
        {
            m_SelectedGameObject = selectedGameObject;
            m_TreeViewState = new TreeViewState();
            m_TreeView = new PrefabOverridesTreeView(selectedGameObject, m_TreeViewState);

            GameObject prefabAssetRoot = PrefabUtility.GetCorrespondingObjectFromSource(m_SelectedGameObject);

            m_TreeView.SetApplyTarget(m_SelectedGameObject, prefabAssetRoot, AssetDatabase.GetAssetPath(prefabAssetRoot));

            UpdateText(prefabAssetRoot);

            m_Immutable = PrefabUtility.IsPartOfImmutablePrefab(prefabAssetRoot);
            m_InvalidComponentOnInstance = PrefabUtility.HasInvalidComponent(m_SelectedGameObject);
            m_InvalidComponentOnAsset = PrefabUtility.HasInvalidComponent(prefabAssetRoot);
        }

        bool IsDisconnected()
        {
            return PrefabUtility.IsDisconnectedFromPrefabAsset(m_SelectedGameObject);
        }

        bool IsShowingActionButton()
        {
            if (m_TreeView.hasModifications || IsDisconnected())
                return true;

            return false;
        }

        public override Vector2 GetWindowSize()
        {
            var height = k_HeaderHeight;

            if (!IsDisconnected())
                height += k_TreeViewPadding.top + m_TreeView.totalHeight + k_TreeViewPadding.bottom;

            const float applyButtonHeight = 32f;
            if (IsShowingActionButton())
                height += applyButtonHeight;
            if (m_TreeView.hasModifications || IsDisconnected())
                height += 40;

            // Width should be no smaller than minimum width, but we could potentially improve
            // width handling by making it expand if needed based on tree view content.
            return new Vector2(300, height);
        }

        Color headerBgColor { get { return EditorGUIUtility.isProSkin ? new Color(0.5f, 0.5f, 0.5f, 0.2f) : new Color(0.9f, 0.9f, 0.9f, 0.6f); } }

        public override void OnGUI(Rect rect)
        {
            Rect headerRect = GUILayoutUtility.GetRect(20, 10000, k_HeaderHeight, k_HeaderHeight);
            EditorGUI.DrawRect(headerRect, headerBgColor);

            float labelSize = EditorStyles.boldLabel.CalcSize(Styles.instanceLabel).x;

            headerRect.height = EditorGUIUtility.singleLineHeight;

            Rect labelRect = new Rect(headerRect.x + k_HeaderLeftMargin, headerRect.y, labelSize, headerRect.height);
            Rect contentRect = headerRect;
            contentRect.xMin = labelRect.xMax;

            GUI.Label(labelRect, Styles.instanceLabel, Styles.boldRightAligned);
            GUI.Label(contentRect, m_InstanceContent, EditorStyles.boldLabel);

            labelRect.y += EditorGUIUtility.singleLineHeight;
            contentRect.y += EditorGUIUtility.singleLineHeight;
            GUI.Label(labelRect, Styles.contextLabel, Styles.boldRightAligned);
            GUI.Label(contentRect, m_StageContent, EditorStyles.boldLabel);

            GUILayout.Space(k_TreeViewPadding.top);

            if (!IsDisconnected())
            {
                Rect treeViewRect = GUILayoutUtility.GetRect(100, 1000, 0, 1000);
                m_TreeView.OnGUI(treeViewRect);
            }

            if (IsShowingActionButton())
            {
                if (IsDisconnected())
                {
                    EditorGUILayout.HelpBox("Disconnected. Cannot show overrides.", MessageType.Warning);
                }
                else if (m_TreeView.hasModifications)
                {
                    if (m_InvalidComponentOnAsset)
                        EditorGUILayout.HelpBox(
                            "Click on individual items to review and revert.\nThe Prefab file contains an invalid script. Applying is not possible. Enter Prefab Mode and remove the script.",
                            MessageType.Info);
                    else if (m_InvalidComponentOnInstance)
                        EditorGUILayout.HelpBox(
                            "Click on individual items to review and revert.\nThe Prefab instance contains an invalid script. Applying is not possible. Remove the script.",
                            MessageType.Info);
                    else if (PrefabUtility.IsPartOfModelPrefab(m_SelectedGameObject))
                        EditorGUILayout.HelpBox(
                            "Click on individual items to review and revert.\nApplying to a model Prefab is not possible.",
                            MessageType.Info);
                    else if (m_Immutable)
                        EditorGUILayout.HelpBox(
                            "Click on individual items to review and revert.\nThe Prefab file is immutable. Applying is not possible.",
                            MessageType.Info);
                    else
                        EditorGUILayout.HelpBox("Click on individual items to review, revert and apply.",
                            MessageType.Info);
                }

                GUILayout.BeginHorizontal();

                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(m_InvalidComponentOnAsset))
                {
                    if (GUILayout.Button(m_RevertAllContent, GUILayout.Width(k_ButtonWidth)))
                    {
                        PrefabUtility.RevertPrefabInstance(m_SelectedGameObject, InteractionMode.UserAction);

                        if (editorWindow != null)
                        {
                            editorWindow.Close();
                            GUIUtility.ExitGUI();
                        }
                    }

                    using (new EditorGUI.DisabledScope(m_Immutable || m_InvalidComponentOnInstance))
                    {
                        if (GUILayout.Button(m_ApplyAllContent, GUILayout.Width(k_ButtonWidth)))
                        {
                            string assetPath =
                                PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(m_SelectedGameObject);
                            if (PrefabUtility.PromptAndCheckoutPrefabIfNeeded(assetPath, PrefabUtility.SaveVerb.Apply))
                            {
                                PrefabUtility.ApplyPrefabInstance(m_SelectedGameObject, InteractionMode.UserAction);

                                if (editorWindow != null)
                                {
                                    editorWindow.Close();
                                    GUIUtility.ExitGUI();
                                }
                            }
                        }
                    }
                }
            }

            // Escape closes the window
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                editorWindow.Close();
                GUIUtility.ExitGUI();
            }
        }

        void UpdateText(GameObject prefabAsset)
        {
            var stage = SceneManagement.StageNavigationManager.instance.currentItem;
            if (stage.isMainStage)
            {
                m_StageContent.image = EditorGUIUtility.IconContent("SceneAsset Icon").image;
                m_StageContent.text = "Scene";
            }
            else
            {
                m_StageContent.image = (Texture2D)AssetDatabase.GetCachedIcon(stage.prefabAssetPath);
                m_StageContent.text = stage.displayName;
            }

            m_InstanceContent.image = (Texture2D)AssetDatabase.GetCachedIcon(AssetDatabase.GetAssetPath(prefabAsset));
            m_InstanceContent.text = prefabAsset.name;

            m_RevertAllContent.text = Styles.revertAllContent.text;
            m_RevertAllContent.tooltip = Styles.revertAllContent.tooltip;

            var applyAllContent = Styles.applyAllContent;
            if (stage.isPrefabStage && PrefabUtility.IsPartOfVariantPrefab(AssetDatabase.LoadAssetAtPath<Object>(stage.prefabAssetPath)))
                applyAllContent = Styles.applyAllToBaseContent;

            m_ApplyAllContent.text = applyAllContent.text;
            m_ApplyAllContent.tooltip = string.Format(applyAllContent.tooltip, prefabAsset.name);
        }
    }
}
