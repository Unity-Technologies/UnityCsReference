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
        const float k_NoOverridesLabelHeight = 26f;
        const float k_ApplyButtonHeight = 32f;
        const float k_HelpBoxHeight = 40f;

        GameObject[] m_SelectedGameObjects = null;

        // TreeView not used when there are multiple Prefabs.
        TreeViewState m_TreeViewState;
        PrefabOverridesTreeView m_TreeView;

        GUIContent m_StageContent = new GUIContent();
        GUIContent m_InstanceContent = new GUIContent();
        GUIContent m_RevertAllContent = new GUIContent();
        GUIContent m_ApplyAllContent = new GUIContent();

        bool m_AnyOverrides;
        bool m_Disconnected;
        bool m_InvalidComponentOnInstance;
        bool m_ModelPrefab;
        bool m_Immutable;
        bool m_InvalidComponentOnAsset;

        static class Styles
        {
            public static GUIContent revertAllContent = EditorGUIUtility.TrTextContent("Revert All", "Revert all overrides.");
            public static GUIContent applyAllContent = EditorGUIUtility.TrTextContent("Apply All", "Apply all overrides to Prefab source '{0}'.");
            public static GUIContent applyAllToBaseContent = EditorGUIUtility.TrTextContent("Apply All to Base", "Apply all overrides to base Prefab source '{0}'.");
            public static GUIContent instanceLabel = EditorGUIUtility.TrTextContent("Overrides to");
            public static GUIContent contextLabel = EditorGUIUtility.TrTextContent("in");

            public static GUIContent infoMultiple = EditorGUIUtility.TrTextContent("Multiple Prefabs selected. Cannot show overrides.");
            public static GUIContent infoMultipleNoApply = EditorGUIUtility.TrTextContent("Multiple Prefabs selected. Cannot show overrides.\nApplying is not possible for one or more Prefabs. Select individual Prefabs for details.");

            // Messages related to the overrides list.
            public static GUIContent infoModel = EditorGUIUtility.TrTextContent("Click on individual items to review and revert.\nApplying to a Model Prefab is not possible.");
            public static GUIContent infoDefault = EditorGUIUtility.TrTextContent("Click on individual items to review, revert and apply.");
            public static GUIContent infoNoApply = EditorGUIUtility.TrTextContent("Click on individual items to review and revert.");
            public static GUIContent warningDisconnected = EditorGUIUtility.TrTextContent("Disconnected. Cannot show overrides.");

            // Messages related to reasons for inability to apply.
            public static GUIContent warningInvalidAsset = EditorGUIUtility.TrTextContent("The Prefab file contains an invalid script. Applying is not possible. Enter Prefab Mode and remove or recover the script.");
            public static GUIContent warningInvalidInstance = EditorGUIUtility.TrTextContent("The Prefab instance contains an invalid script. Applying is not possible. Remove or recover the script.");
            public static GUIContent warningImmutable = EditorGUIUtility.TrTextContent("The Prefab file is immutable. Applying is not possible.");

            public static GUIStyle boldRightAligned;

            static Styles()
            {
                boldRightAligned = new GUIStyle(EditorStyles.boldLabel);
                boldRightAligned.alignment = TextAnchor.MiddleRight;
            }
        }

        internal PrefabOverridesWindow(GameObject selectedGameObject)
        {
            m_SelectedGameObjects = new GameObject[] { selectedGameObject };
            m_TreeViewState = new TreeViewState();
            m_TreeView = new PrefabOverridesTreeView(selectedGameObject, m_TreeViewState);

            GameObject prefabAssetRoot = PrefabUtility.GetCorrespondingObjectFromSource(selectedGameObject);

            m_TreeView.SetApplyTarget(selectedGameObject, prefabAssetRoot, AssetDatabase.GetAssetPath(prefabAssetRoot));

            UpdateTextSingle(prefabAssetRoot);
            UpdateStatusChecks(selectedGameObject);
        }

        internal PrefabOverridesWindow(GameObject[] selectedGameObjects)
        {
            m_SelectedGameObjects = selectedGameObjects;
            UpdateTextMultiple();
            for (int i = 0; i < m_SelectedGameObjects.Length; i++)
                UpdateStatusChecks(m_SelectedGameObjects[i]);
        }

        void UpdateStatusChecks(GameObject prefabInstanceRoot)
        {
            if (PrefabUtility.HasPrefabInstanceAnyOverrides(prefabInstanceRoot, false))
                m_AnyOverrides = true;
            if (PrefabUtility.IsDisconnectedFromPrefabAsset(prefabInstanceRoot))
                m_Disconnected = true;
            if (PrefabUtility.HasInvalidComponent(prefabInstanceRoot))
                m_InvalidComponentOnInstance = true;

            GameObject prefabAssetRoot = PrefabUtility.GetCorrespondingObjectFromSource(prefabInstanceRoot);

            if (PrefabUtility.IsPartOfModelPrefab(prefabAssetRoot))
                m_ModelPrefab = true;
            if (PrefabUtility.IsPartOfImmutablePrefab(prefabAssetRoot))
                m_Immutable = true;
            if (PrefabUtility.HasInvalidComponent(prefabAssetRoot))
                m_InvalidComponentOnAsset = true;
        }

        bool IsShowingActionButton()
        {
            return m_AnyOverrides || m_Disconnected;
        }

        bool HasMultiSelection()
        {
            return m_SelectedGameObjects.Length > 1;
        }

        bool DisplayingTreeView()
        {
            return m_AnyOverrides && !HasMultiSelection();
        }

        bool IsShowingApplyWarning()
        {
            return
                !HasMultiSelection() &&
                (m_AnyOverrides || m_Disconnected) &&
                !m_ModelPrefab &&
                (m_InvalidComponentOnInstance || m_InvalidComponentOnAsset || m_Immutable);
        }

        public override Vector2 GetWindowSize()
        {
            var height = k_HeaderHeight;

            if (!IsShowingActionButton())
            {
                height += k_NoOverridesLabelHeight;
            }
            else
            {
                if (DisplayingTreeView())
                    height += k_TreeViewPadding.top + m_TreeView.totalHeight + k_TreeViewPadding.bottom;

                height += k_ApplyButtonHeight + k_HelpBoxHeight;

                if (IsShowingApplyWarning())
                    height += k_HelpBoxHeight; // A second help box in this case.
            }

            // Width should be no smaller than minimum width, but we could potentially improve
            // width handling by making it expand if needed based on tree view content.
            return new Vector2(300, height);
        }

        Color headerBgColor { get { return EditorGUIUtility.isProSkin ? new Color(0.5f, 0.5f, 0.5f, 0.2f) : new Color(0.9f, 0.9f, 0.9f, 0.6f); } }

        public override void OnGUI(Rect rect)
        {
            // Escape closes the window
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                editorWindow.Close();
                GUIUtility.ExitGUI();
            }

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

            // If we know there are no overrides and thus no meaningful actions we just show that and nothing more.
            if (!IsShowingActionButton())
            {
                EditorGUILayout.LabelField("No Overrides");
                return;
            }

            // Display tree view and/or instructions related to it.
            if (HasMultiSelection())
            {
                if (m_InvalidComponentOnAsset || m_InvalidComponentOnInstance || m_ModelPrefab || m_Immutable)
                    EditorGUILayout.HelpBox(Styles.infoMultipleNoApply.text, MessageType.Info);
                else
                    EditorGUILayout.HelpBox(Styles.infoMultiple.text, MessageType.Info);
            }
            else
            {
                if (m_Disconnected)
                {
                    EditorGUILayout.HelpBox(Styles.warningDisconnected.text, MessageType.Warning);
                }
                else if (m_AnyOverrides)
                {
                    Rect treeViewRect = GUILayoutUtility.GetRect(100, 1000, 0, 1000);
                    m_TreeView.OnGUI(treeViewRect);

                    // Display info message telling user they can click on individual items for more detailed actions.
                    if (m_ModelPrefab)
                        EditorGUILayout.HelpBox(Styles.infoModel.text, MessageType.Info);
                    else if (m_Immutable || m_InvalidComponentOnInstance)
                        EditorGUILayout.HelpBox(Styles.infoNoApply.text, MessageType.Info);
                    else
                        EditorGUILayout.HelpBox(Styles.infoDefault.text, MessageType.Info);
                }

                if (IsShowingApplyWarning())
                {
                    // Display warnings about edge cases that make it impossible to apply.
                    // Model Prefabs are not an edge case and not needed to warn about so it's
                    // not included here but rather combined into the info message above.
                    if (m_InvalidComponentOnAsset)
                        EditorGUILayout.HelpBox(Styles.warningInvalidAsset.text, MessageType.Warning);
                    else if (m_InvalidComponentOnInstance)
                        EditorGUILayout.HelpBox(Styles.warningInvalidInstance.text, MessageType.Warning);
                    else
                        EditorGUILayout.HelpBox(Styles.warningImmutable.text, MessageType.Warning);
                }
            }

            // Display action buttons (Revert All and Apply All)
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            using (new EditorGUI.DisabledScope(m_InvalidComponentOnAsset))
            {
                if (GUILayout.Button(m_RevertAllContent, GUILayout.Width(k_ButtonWidth)))
                {
                    if (RevertAll() && editorWindow != null)
                    {
                        editorWindow.Close();
                        GUIUtility.ExitGUI();
                    }
                }

                using (new EditorGUI.DisabledScope(m_Immutable || m_InvalidComponentOnInstance))
                {
                    if (GUILayout.Button(m_ApplyAllContent, GUILayout.Width(k_ButtonWidth)))
                    {
                        if (ApplyAll() && editorWindow != null)
                        {
                            editorWindow.Close();
                            GUIUtility.ExitGUI();
                        }
                    }
                }
            }
            GUILayout.EndHorizontal();
        }

        bool ApplyAll()
        {
            // Collect Prefab Asset paths and also check if there's more than one of the same.
            HashSet<string> prefabAssetPaths = new HashSet<string>();
            bool multipleOfSame = false;
            for (int i = 0; i < m_SelectedGameObjects.Length; i++)
            {
                string prefabAssetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(m_SelectedGameObjects[i]);
                if (prefabAssetPaths.Contains(prefabAssetPath))
                    multipleOfSame = true;
                else
                    prefabAssetPaths.Add(prefabAssetPath);
            }

            // If more than one instance of the same Prefab Asset, show dialog to user.
            if (multipleOfSame && !EditorUtility.DisplayDialog(
                L10n.Tr("Multiple instances of same Prefab Asset"),
                L10n.Tr("Multiple instances of the same Prefab Asset were detected. Potentially conflicting overrides will be applied sequentially and will overwrite each other."),
                L10n.Tr("OK"),
                L10n.Tr("Cancel")))
                return false;

            // Make sure assets are checked out in version control.
            if (!PrefabUtility.PromptAndCheckoutPrefabIfNeeded(prefabAssetPaths.ToArray(), PrefabUtility.SaveVerb.Apply))
                return false;

            // Apply sequentially.
            for (int i = 0; i < m_SelectedGameObjects.Length; i++)
                PrefabUtility.ApplyPrefabInstance(m_SelectedGameObjects[i], InteractionMode.UserAction);

            return true;
        }

        bool RevertAll()
        {
            for (int i = 0; i < m_SelectedGameObjects.Length; i++)
                PrefabUtility.RevertPrefabInstance(m_SelectedGameObjects[i], InteractionMode.UserAction);

            return true;
        }

        void UpdateTextSingle(GameObject prefabAsset)
        {
            Texture2D icon = (Texture2D)AssetDatabase.GetCachedIcon(AssetDatabase.GetAssetPath(prefabAsset));
            string name = prefabAsset.name;
            UpdateText(icon, name);
        }

        void UpdateTextMultiple()
        {
            Texture icon = EditorGUIUtility.IconContent("Prefab Icon").image;
            string name = "(Multiple Prefabs)";
            UpdateText(icon, name);
        }

        void UpdateText(Texture assetIcon, string assetName)
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

            m_InstanceContent.image = assetIcon;
            m_InstanceContent.text = assetName;

            m_RevertAllContent.text = Styles.revertAllContent.text;
            m_RevertAllContent.tooltip = Styles.revertAllContent.tooltip;

            var applyAllContent = Styles.applyAllContent;
            if (stage.isPrefabStage && PrefabUtility.IsPartOfVariantPrefab(AssetDatabase.LoadAssetAtPath<Object>(stage.prefabAssetPath)))
                applyAllContent = Styles.applyAllToBaseContent;

            m_ApplyAllContent.text = applyAllContent.text;
            m_ApplyAllContent.tooltip = string.Format(applyAllContent.tooltip, assetName);
        }
    }
}
