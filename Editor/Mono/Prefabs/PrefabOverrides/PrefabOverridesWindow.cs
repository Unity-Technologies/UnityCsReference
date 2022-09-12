// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using System.Text;
using static UnityEditor.GameObjectTreeViewGUI;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    internal class PrefabOverridesWindow : PopupWindowContent
    {
        RectOffset k_TreeViewPadding = new RectOffset(0, 0, 4, 4);
        const float k_HeaderHeight = 60f;
        const float k_ButtonWidth = 120;
        const float k_ButtonWidthPadding = 10; //Padding in case the text is long to give the window a bit of margins around the buttons
        const float k_HeaderLeftMargin = 6;
        const float k_NoOverridesLabelHeight = 26f;
        const float k_UnusedOverridesButtonHeight = 26f;
        const float k_RowPadding = 6;
        float m_ApplyButtonHeight = 0;
        GameObject[] m_SelectedGameObjects = null;

        // TreeView not used when there are multiple Prefabs.
        TreeViewState m_TreeViewState;
        PrefabOverridesTreeView m_TreeView;

        GUIContent m_StageContent = new GUIContent();
        GUIContent m_InstanceContent = new GUIContent();
        GUIContent m_RevertAllContent = new GUIContent();
        GUIContent m_ApplyAllContent = new GUIContent();
        GUIContent m_RevertSelectedContent = new GUIContent();
        GUIContent m_ApplySelectedContent = new GUIContent();

        float m_ButtonWidth;
        float m_ApplyButtonWidth;
        bool m_AnyOverrides;
        bool m_HasApplicableOverrides;
        bool m_InvalidComponentOnInstance;
        bool m_ModelPrefab;
        bool m_Immutable;
        bool m_InvalidComponentOnAsset;
        bool m_HasManagedReferencesWithMissingTypesOnAsset;
        bool m_UnusedOverridesExist;
        static class Styles
        {
            public static GUIContent revertAllContent = EditorGUIUtility.TrTextContent("Revert All", "Revert all overrides.");
            public static GUIContent revertSelectedContent = EditorGUIUtility.TrTextContent("Revert Selected", "Revert selected overrides.");
            public static GUIContent applyAllContent = EditorGUIUtility.TrTextContent("Apply All", "Apply all overrides to Prefab source '{0}'.");
            public static GUIContent applySelectedContent = EditorGUIUtility.TrTextContent("Apply Selected", "Apply selected overrides to Prefab source '{0}'.");
            public static GUIContent applyAllToBaseContent = EditorGUIUtility.TrTextContent("Apply All to Prefab Variant parent", "Apply all overrides to Prefab Variant parent '{0}'.");
            public static GUIContent applySelectedToBaseContent = EditorGUIUtility.TrTextContent("Apply Selected to Prefab Variant parent", "Apply selected overrides to Prefab Variant parent '{0}'.");
            public static GUIContent titleLabelDefault = EditorGUIUtility.TrTextContent("Review, Revert or Apply Overrides");
            public static GUIContent titleLabelNoApply = EditorGUIUtility.TrTextContent("Review or Revert Overrides");
            public static GUIContent noOverridesText = EditorGUIUtility.TrTextContent("No overrides");
            public static GUIContent instanceLabel = EditorGUIUtility.TrTextContent("on");
            public static GUIContent contextLabel = EditorGUIUtility.TrTextContent("in");
            public static GUIContent removeUnusedOverridesButtonContent = EditorGUIUtility.TrTextContentWithIcon("Unused overrides", EditorGUIUtility.LoadIcon("Clear"));

            public static string nonApplicableTooltipApply = L10n.Tr("There are no overrides that can be applied to Prefab source '{0}'.");
            public static string nonApplicableTooltipRevert = L10n.Tr("There are no overrides that can be reverted.");

            public static GUIContent infoMultiple = EditorGUIUtility.TrTextContent("Multiple Prefabs selected. Cannot show overrides.");
            public static GUIContent infoMultipleNoApply = EditorGUIUtility.TrTextContent("Multiple Prefabs selected. Cannot show overrides.\nApplying is not possible for one or more Prefabs. Select individual Prefabs for details.");

            // Messages related to the overrides list.
            public static GUIContent infoModel = EditorGUIUtility.TrTextContent("Applying to a Model Prefab is not possible.");

            // Messages related to reasons for inability to apply.
            public static GUIContent warningInvalidAsset = EditorGUIUtility.TrTextContent("The Prefab file contains an invalid script. Applying is not possible. Enter Prefab Mode and either remove the script component or recover the script.");
            public static GUIContent warningHasManagedReferencesWithMissingTypes = EditorGUIUtility.TrTextContent("The Prefab file contains missing SerializeReference types. Applying is not possible. Enter Prefab Mode to see more details.");
            public static GUIContent warningInvalidInstance = EditorGUIUtility.TrTextContent("The Prefab instance contains an invalid script. Applying is not possible. Remove or recover the script.");
            public static GUIContent warningImmutable = EditorGUIUtility.TrTextContent("The Prefab file is immutable. Applying is not possible.");

            public static GUIStyle boldRightAligned;
            public static GUIStyle rightAligned;
            public static GUIStyle removeOverridesButtonLineStyle = "TV Line";
            public static GUIStyle removeOverridesButtonSelectionStyle = "TV Selection";

            static Styles()
            {
                boldRightAligned = new GUIStyle(EditorStyles.boldLabel);
                boldRightAligned.alignment = TextAnchor.MiddleRight;

                rightAligned = new GUIStyle(EditorStyles.label);
                rightAligned.alignment = TextAnchor.MiddleRight;

                removeOverridesButtonLineStyle.alignment = TextAnchor.MiddleLeft;
                removeOverridesButtonLineStyle.padding.left = 7;
            }
        }

        internal PrefabOverridesWindow(GameObject selectedGameObject)
        {
            m_SelectedGameObjects = new GameObject[] { selectedGameObject };
            m_TreeViewState = new TreeViewState();
            m_TreeView = new PrefabOverridesTreeView(selectedGameObject, m_TreeViewState, this);

            GameObject prefabAssetRoot = PrefabUtility.GetCorrespondingObjectFromSource(selectedGameObject);

            m_TreeView.SetApplyTarget(selectedGameObject, prefabAssetRoot, AssetDatabase.GetAssetPath(prefabAssetRoot));

            // m_TreeView.SetApplyTarget already reloads the TreeView so don't do it again in RefreshStatus.
            RefreshStatus(false);
        }

        internal PrefabOverridesWindow(GameObject[] selectedGameObjects)
        {
            m_SelectedGameObjects = selectedGameObjects;
            RefreshStatus();
        }

        public override void OnOpen()
        {
            Undo.undoRedoEvent += OnUndoRedoPerformed;
            base.OnOpen();
        }

        public override void OnClose()
        {
            Undo.undoRedoEvent -= OnUndoRedoPerformed;
            base.OnClose();
        }

        void OnUndoRedoPerformed(in UndoRedoInfo info)
        {
            RefreshStatus();
        }

        internal void RefreshStatus(bool reloadTreeView = true)
        {
            if (m_TreeView != null && reloadTreeView)
            {
                if (!m_TreeView.IsValidTargetPrefabInstance())
                {
                    editorWindow.Close();
                    return;
                }

                m_TreeView.Reload();
                m_TreeView.CullNonExistingItemsFromSelection();
            }

            m_AnyOverrides = false;
            m_InvalidComponentOnInstance = false;
            m_ModelPrefab = false;
            m_Immutable = false;
            m_InvalidComponentOnAsset = false;
            m_HasManagedReferencesWithMissingTypesOnAsset = false;

            for (int i = 0; i < m_SelectedGameObjects.Length; i++)
                UpdateStatusChecks(m_SelectedGameObjects[i]);

            if (m_SelectedGameObjects.Length == 1)
                UpdateTextSingle(PrefabUtility.GetCorrespondingObjectFromSource(m_SelectedGameObjects[0]));
            else
                UpdateTextMultiple();

            m_UnusedOverridesExist = PrefabUtility.HavePrefabInstancesUnusedOverrides(m_SelectedGameObjects);
        }

        void UpdateStatusChecks(GameObject prefabInstanceRoot)
        {
            // Can't reset values inside this method, since it's called consecutively for each target.

            if (PrefabUtility.HasPrefabInstanceAnyOverrides(prefabInstanceRoot, false))
                m_AnyOverrides = true;
            if (PrefabUtility.HasInvalidComponent(prefabInstanceRoot))
                m_InvalidComponentOnInstance = true;

            GameObject prefabAssetRoot = PrefabUtility.GetCorrespondingObjectFromSource(prefabInstanceRoot);

            if (PrefabUtility.IsPartOfModelPrefab(prefabAssetRoot))
                m_ModelPrefab = true;
            if (PrefabUtility.IsPartOfImmutablePrefab(prefabAssetRoot))
                m_Immutable = true;
            if (PrefabUtility.HasInvalidComponent(prefabAssetRoot))
                m_InvalidComponentOnAsset = true;

            if (PrefabUtility.HasManagedReferencesWithMissingTypes(prefabAssetRoot))
                m_HasManagedReferencesWithMissingTypesOnAsset = true;

            m_HasApplicableOverrides = m_TreeView == null || m_TreeView.hasApplicableModifications;
        }

        bool IsShowingActionButton()
        {
            return m_AnyOverrides;
        }

        bool IsShowingUnusedOverridesButton()
        {
            return m_UnusedOverridesExist;
        }

        bool HasMultiSelection()
        {
            return m_SelectedGameObjects.Length > 1;
        }

        bool IsShowingApplyWarning()
        {
            return
                !HasMultiSelection() &&
                m_AnyOverrides &&
                !m_ModelPrefab &&
                (m_InvalidComponentOnInstance || m_InvalidComponentOnAsset || m_HasManagedReferencesWithMissingTypesOnAsset || m_Immutable);
        }

        public override Vector2 GetWindowSize()
        {
            return CalculateWindowSize();
        }

        Vector2 CalculateWindowSize()
        {
            const float k_MaxAllowedTreeViewWidth = 1800f;
            const float k_MaxAllowedTreeViewHeight = 1000f;
            var width = 300f;
            var height = k_HeaderHeight;

            // Match the call order as in OnGUI() to ensure the correct height is calculated

            if (!IsShowingActionButton())
            {
                if (IsShowingUnusedOverridesButton())
                    height += k_UnusedOverridesButtonHeight;
                else
                    height += k_NoOverridesLabelHeight;

                return new Vector2(width, height);
            }

            if (HasMultiSelection())
            {
                if (m_InvalidComponentOnAsset || m_HasManagedReferencesWithMissingTypesOnAsset || m_InvalidComponentOnInstance || m_ModelPrefab || m_Immutable)
                    height += CalcHeightForHelpBox(Styles.infoMultipleNoApply, MessageType.Info, width);
                else
                    height += CalcHeightForHelpBox(Styles.infoMultiple, MessageType.Info, width);

                if (m_UnusedOverridesExist)
                    height += k_UnusedOverridesButtonHeight + k_RowPadding;
                else
                    height += 2;
            }
            else
            {
                height += k_TreeViewPadding.top;
                if (m_TreeView.hasModifications)
                {
                    height += Mathf.Min(k_MaxAllowedTreeViewHeight, m_TreeView.totalHeight);
                    width = Mathf.Max(Mathf.Min(m_TreeView.maxItemWidth, k_MaxAllowedTreeViewWidth), width);
                }

                if (m_AnyOverrides)
                {
                    if (m_UnusedOverridesExist)
                        height += k_UnusedOverridesButtonHeight + k_RowPadding;

                    if (m_ModelPrefab)
                        height += CalcHeightForHelpBox(Styles.infoModel, MessageType.Info, width);
                }

                if (IsShowingApplyWarning())
                {
                    if (m_InvalidComponentOnAsset)
                        height += CalcHeightForHelpBox(Styles.warningInvalidAsset, MessageType.Warning, width);
                    else if (m_InvalidComponentOnInstance)
                        height += CalcHeightForHelpBox(Styles.warningInvalidInstance, MessageType.Warning, width);
                    else if (m_HasManagedReferencesWithMissingTypesOnAsset)
                        height += CalcHeightForHelpBox(Styles.warningHasManagedReferencesWithMissingTypes, MessageType.Warning, width);
                    else if (m_Immutable)
                        height += CalcHeightForHelpBox(Styles.warningImmutable, MessageType.Warning, width);
                }
            }

            if (m_ApplyButtonHeight == 0)
                m_ApplyButtonHeight = GUI.skin.button.CalcHeight(Styles.applySelectedContent, width) + GUI.skin.button.margin.top + GUI.skin.button.margin.bottom;

            height += m_ApplyButtonHeight + k_RowPadding;

            //In case the text is long
            width = Mathf.Max(width, m_ApplyButtonWidth + m_ButtonWidth + k_ButtonWidthPadding);

            return new Vector2(width, height);
        }

        float CalcHeightForHelpBox(GUIContent content, MessageType messageType, float width)
        {
            var tempContent = EditorGUIUtility.TempContent(content.text, EditorGUIUtility.GetHelpIcon(messageType));
            return EditorStyles.helpBox.CalcHeight(tempContent, width) + EditorStyles.helpBox.margin.top + EditorStyles.helpBox.margin.bottom;
        }

        Color headerBgColor { get { return EditorGUIUtility.isProSkin ? new Color(0.5f, 0.5f, 0.5f, 0.2f) : new Color(0.9f, 0.9f, 0.9f, 0.6f); } }
        Color horizontalLineColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
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

            float titleLabelSize = 0;
            if (m_ModelPrefab || m_Immutable || m_InvalidComponentOnInstance)
            {
                titleLabelSize = EditorStyles.boldLabel.CalcSize(Styles.titleLabelNoApply).x;
                Rect titleLabelRect = new Rect(headerRect.x + k_HeaderLeftMargin, headerRect.y, titleLabelSize, headerRect.height);
                titleLabelRect.height = EditorGUIUtility.singleLineHeight;
                GUI.Label(titleLabelRect, Styles.titleLabelNoApply, Styles.boldRightAligned);
            }
            else
            {
                titleLabelSize = EditorStyles.boldLabel.CalcSize(Styles.titleLabelDefault).x;
                Rect titleLabelRect = new Rect(headerRect.x + k_HeaderLeftMargin, headerRect.y, titleLabelSize, headerRect.height);
                titleLabelRect.height = EditorGUIUtility.singleLineHeight;
                GUI.Label(titleLabelRect, Styles.titleLabelDefault, Styles.boldRightAligned);
            }

            float labelSize = EditorStyles.label.CalcSize(Styles.instanceLabel).x;

            headerRect.height = EditorGUIUtility.singleLineHeight;

            Rect labelRect = new Rect(headerRect.x + k_HeaderLeftMargin, headerRect.y + 20, labelSize, headerRect.height);
            Rect contentRect = headerRect;
            contentRect.xMin = labelRect.xMax;
            contentRect.y = labelRect.y;

            GUI.Label(labelRect, Styles.instanceLabel, Styles.rightAligned);
            GUI.Label(contentRect, m_InstanceContent, EditorStyles.label);

            labelRect.y += EditorGUIUtility.singleLineHeight;
            contentRect.y += EditorGUIUtility.singleLineHeight;
            GUI.Label(labelRect, Styles.contextLabel, Styles.rightAligned);
            GUI.Label(contentRect, m_StageContent, EditorStyles.label);

            // If we know there are no overrides and thus no meaningful actions we just show that and nothing more.
            if (!IsShowingActionButton())
            {
                if (m_UnusedOverridesExist)
                {
                    DrawUnusedOverridesButton();
                    GUILayout.Space(k_RowPadding);
                }
                else
                {
                    EditorGUILayout.LabelField(Styles.noOverridesText);
                }
                return;
            }

            // Display tree view and/or instructions related to it.
            if (HasMultiSelection())
            {
                if (m_InvalidComponentOnAsset || m_HasManagedReferencesWithMissingTypesOnAsset || m_InvalidComponentOnInstance || m_ModelPrefab || m_Immutable)
                    EditorGUILayout.HelpBox(Styles.infoMultipleNoApply.text, MessageType.Info);
                else
                    EditorGUILayout.HelpBox(Styles.infoMultiple.text, MessageType.Info);

                if (m_UnusedOverridesExist)
                {
                    DrawUnusedOverridesButton();
                    GUILayout.Space(k_RowPadding);
                }
                else
                {
                    GUILayout.Space(2);
                }
            }
            else
            {
                GUILayout.Space(k_TreeViewPadding.top);

                if (m_TreeView.hasModifications)
                {
                    Rect treeViewRect = GUILayoutUtility.GetRect(100, 10000, 0, 10000);
                    m_TreeView.OnGUI(treeViewRect);
                }

                if (m_AnyOverrides)
                {
                    if (m_UnusedOverridesExist)
                    {
                        DrawUnusedOverridesButton();
                        GUILayout.Space(k_RowPadding);
                    }

                    if (m_ModelPrefab)
                        EditorGUILayout.HelpBox(Styles.infoModel.text, MessageType.Info);
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
                    else if (m_HasManagedReferencesWithMissingTypesOnAsset)
                        EditorGUILayout.HelpBox(Styles.warningHasManagedReferencesWithMissingTypes.text, MessageType.Warning);
                    else if (m_Immutable)
                        EditorGUILayout.HelpBox(Styles.warningImmutable.text, MessageType.Warning);
                }
            }

            // Display action buttons (Revert All and Apply All)
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(m_InvalidComponentOnAsset || m_HasManagedReferencesWithMissingTypesOnAsset))
            {
                if (m_TreeView != null && m_TreeView.GetSelection().Count > 1)
                {
                    if (GUILayout.Button(m_RevertSelectedContent, GUILayout.Width(m_ButtonWidth)))
                    {
                        if (OperateSelectedOverrides(PrefabUtility.OverrideOperation.Revert))
                        {
                            RefreshStatus();
                            // We don't close the window even if there are no more overrides left.
                            // We want to diplay explicit confirmation, since it's not a given outcome
                            // when using Revert Selected. Only Revert All button closes the window.
                        }
                    }

                    using (new EditorGUI.DisabledScope(m_Immutable || m_InvalidComponentOnInstance))
                    {
                        if (GUILayout.Button(m_ApplySelectedContent, GUILayout.Width(m_ButtonWidth)))
                        {
                            if (OperateSelectedOverrides(PrefabUtility.OverrideOperation.Apply))
                            {
                                RefreshStatus();
                                // We don't close the window even if there are no more overrides left.
                                // We want to diplay explicit confirmation, since it's not a given outcome
                                // when using Apply Selected. Only Apply All button closes the window.
                            }
                        }
                    }
                }
                else
                {
                    if (GUILayout.Button(m_RevertAllContent, GUILayout.Width(m_ButtonWidth)))
                    {
                        if (RevertAll() && editorWindow != null)
                        {
                            editorWindow.Close();
                            GUIUtility.ExitGUI();
                        }
                    }

                    using (new EditorGUI.DisabledScope(m_Immutable || m_InvalidComponentOnInstance || !m_HasApplicableOverrides))
                    {
                        if (GUILayout.Button(m_ApplyAllContent, GUILayout.Width(m_ApplyButtonWidth)))
                        {
                            if (ApplyAll() && editorWindow != null)
                            {
                                editorWindow.Close();
                                GUIUtility.ExitGUI();
                            }
                        }
                    }
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(k_RowPadding);
        }

        void DrawUnusedOverridesButton()
        {
            Rect buttonRect = GUILayoutUtility.GetRect(100, 10000, k_NoOverridesLabelHeight, k_NoOverridesLabelHeight);

            var isHovered = buttonRect.Contains(UnityEngine.Event.current.mousePosition);
            if (isHovered)
            {
                GUIView.current.MarkHotRegion(GUIClip.UnclipToWindow(buttonRect));

                using (new GUI.BackgroundColorScope(GameObjectStyles.hoveredBackgroundColor))
                {
                    GUI.Label(buttonRect, GUIContent.none, GameObjectStyles.hoveredItemBackgroundStyle);
                }
            }

            if (Event.current.type == EventType.Repaint)
            {
                if (UnusedOverridesViewPopup.s_IsOpen)
                    Styles.removeOverridesButtonSelectionStyle.Draw(buttonRect, false, false, true, true);

                Rect buttonBorder = new Rect(buttonRect.x, buttonRect.y, buttonRect.width, 1);
                EditorGUI.DrawRect(buttonBorder, horizontalLineColor);// Upper border.

                buttonBorder.y = buttonRect.y + buttonRect.height;
                EditorGUI.DrawRect(buttonBorder, horizontalLineColor);// Lower border.

                Styles.removeOverridesButtonLineStyle.Draw(buttonRect, Styles.removeUnusedOverridesButtonContent, false, false, UnusedOverridesViewPopup.s_IsOpen, true);
            }

            if (GUI.Button(buttonRect, GUIContent.none, GUIStyle.none))
            {
                PopupWindowWithoutFocus.Show(buttonRect,
                    new UnusedOverridesViewPopup(m_SelectedGameObjects, this),
                    new[] { PopupLocation.Left, PopupLocation.Right });
            }
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

            PrefabUtility.ApplyPrefabInstances(m_SelectedGameObjects, InteractionMode.UserAction);

            EditorUtility.ForceRebuildInspectors();
            return true;
        }

        bool RevertAll()
        {
            for (int i = 0; i < m_SelectedGameObjects.Length; i++)
                PrefabUtility.RevertPrefabInstance(m_SelectedGameObjects[i], InteractionMode.UserAction);

            EditorUtility.ForceRebuildInspectors();
            return true;
        }

        bool OperateSelectedOverrides(PrefabUtility.OverrideOperation operation)
        {
            List<PrefabOverride> overrides = new List<PrefabOverride>();

            // Get all overrides from selection. Immediately accept any overrides with no dependencies.
            var selection = m_TreeView.GetSelection();
            for (int i = 0; i < selection.Count; i++)
            {
                PrefabOverride singleOverride = m_TreeView.FindOverride(selection[i]);
                if (singleOverride != null)
                    overrides.Add(singleOverride);
            }

            bool success = PrefabUtility.ProcessMultipleOverrides(m_SelectedGameObjects[0], overrides, operation, InteractionMode.UserAction);
            if (success)
                EditorUtility.ForceRebuildInspectors();
            return success;
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
            var stage = SceneManagement.StageNavigationManager.instance.currentStage;
            if (stage is MainStage)
            {
                m_StageContent.image = EditorGUIUtility.IconContent("SceneAsset Icon").image;
                m_StageContent.text = "Scene";
            }
            else if (stage is PrefabStage)
            {
                m_StageContent.image = (Texture2D)AssetDatabase.GetCachedIcon(stage.assetPath);
                m_StageContent.text = System.IO.Path.GetFileNameWithoutExtension(stage.assetPath);
            }

            m_InstanceContent.image = assetIcon;
            m_InstanceContent.text = assetName;

            m_RevertAllContent = Styles.revertAllContent;
            m_RevertSelectedContent = Styles.revertSelectedContent;

            m_ButtonWidth = k_ButtonWidth;
            m_ApplyButtonWidth = k_ButtonWidth;
            var applyAllContent = new GUIContent(Styles.applyAllContent);
            var revertAllContent = new GUIContent(Styles.revertAllContent);
            var applySelectedContent = Styles.applySelectedContent;
            if (stage is PrefabStage && PrefabUtility.IsPartOfVariantPrefab(AssetDatabase.LoadAssetAtPath<Object>(stage.assetPath)))
            {
                applyAllContent = Styles.applyAllToBaseContent;
                m_ApplyButtonWidth = GUI.skin.button.CalcSize(applyAllContent).x;
                applySelectedContent = Styles.applySelectedToBaseContent;

            }

            if (!m_HasApplicableOverrides)
            {
                applyAllContent.tooltip = Styles.nonApplicableTooltipApply;
                revertAllContent.tooltip = Styles.nonApplicableTooltipRevert;
            }

            m_ApplyAllContent = new GUIContent(applyAllContent.text, string.Format(applyAllContent.tooltip, assetName));
            m_RevertAllContent = new GUIContent(revertAllContent.text, revertAllContent.tooltip);
            m_ApplySelectedContent.text = applySelectedContent.text;
            m_ApplySelectedContent.tooltip = string.Format(applySelectedContent.tooltip, assetName);
        }
    }

    internal class UnusedOverridesViewPopup : PopupWindowContent
    {
        const float k_HeaderHeight = 25f;
        const float k_BodyLineHeight = 16f;
        const float k_ButtonWidth = 80f;
        const float k_ViewWidthPadding = 20f;
        const float k_BodyTextPadding = 10f;
        const float k_BodyTextPaddingSmall = 6f;
        const int k_LeftPaddingWidth = 10;
        const int k_RemoveButtonRightMargin = 6;
        const int k_MaxEntries = 3;

        Vector2 m_ViewSize = new Vector2(250f, k_HeaderHeight);

        static class Styles
        {
            public static GUIStyle borderStyle = new GUIStyle("grey_border");
            public static GUIStyle headerLabel = new GUIStyle(EditorStyles.boldLabel);
            public static GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
            public static GUIStyle bodyStyle = new GUIStyle(EditorStyles.label);
            public static GUIStyle logHintStyle = new GUIStyle(EditorStyles.label);
            public static GUIStyle headerGroupStyle = new GUIStyle();
            public static GUIContent headerContentBaseSingular = EditorGUIUtility.TrTextContent("unused override");
            public static GUIContent headerContentBasePlural = EditorGUIUtility.TrTextContent("unused overrides");
            public static GUIContent editorLogHint = EditorGUIUtility.TrTextContent("Details will be written to the Editor log.");
            public static GUIContent buttonContent = EditorGUIUtility.TrTextContent("Remove");
            public static GUIContent headerContent = EditorGUIUtility.TrTextContent("{0} unused overrides");
            public static GUIContent unusedRemovedComponentsContentSingular = EditorGUIUtility.TrTextContent("{0} has 1 unused removed component");
            public static GUIContent unusedRemovedComponentsContentPlural = EditorGUIUtility.TrTextContent("{0} has {1} unused removed components");
            public static GUIContent unusedRemovedGameObjectsContentSingular = EditorGUIUtility.TrTextContent("{0} has 1 unused removed GameObject");
            public static GUIContent unusedRemovedGameObjectsContentPlural = EditorGUIUtility.TrTextContent("{0} has {1} unused removed GameObjects");
            public static GUIContent headerContentSingular = EditorGUIUtility.TrTextContent("1 unused override");
            public static GUIContent extraOverridesContent = EditorGUIUtility.TrTextContent("and {0} others");
            public static GUIContent extraInstancesContent = EditorGUIUtility.TrTextContent("on {0} instances");
            public static GUIContent pathOnInstanceContent = EditorGUIUtility.TrTextContent("on");

            static Styles()
            {
                headerLabel.padding = new RectOffset(3, 3, 3, 3);

                headerGroupStyle.padding = new RectOffset(0, 0, 3, 3);

                headerStyle.alignment = TextAnchor.MiddleLeft;
                headerStyle.padding.left = k_LeftPaddingWidth;
                headerStyle.padding.top = 1;

                bodyStyle.alignment = TextAnchor.UpperLeft;
                bodyStyle.padding.left = k_LeftPaddingWidth;
                bodyStyle.padding.top = 1;

                logHintStyle.alignment = TextAnchor.MiddleLeft;
                logHintStyle.padding.left = k_LeftPaddingWidth;
                logHintStyle.padding.top = 1;
            }
        }

        public static bool s_IsOpen;
        PrefabOverridesWindow m_Owner;
        GameObject[] m_SelectedGameObjects;
        PrefabUtility.InstanceOverridesInfo[] m_InstanceOverridesInfos;
        PrefabUtility.InstanceOverridesInfo m_SingleInstanceWithUnusedMods;
        List<GUIContent> m_OverridesContent;
        GUIContent m_RemainingOverridesInfo = null;
        string m_HeaderText = string.Empty;
        int m_SelectedInstanceCount = 0;
        int m_AffectedInstanceCount = 0;
        int m_UnusedOverridesCount = 0;
        int m_UsedOverridesCount = 0;
        int m_UnusedRemovedGameObjectsCount = 0;
        int m_UnusedRemovedComponentsCount = 0;

        public UnusedOverridesViewPopup(GameObject[] selectedGameObjects, PrefabOverridesWindow owner)
        {
            m_SelectedGameObjects = selectedGameObjects;
            m_Owner = owner;

            m_InstanceOverridesInfos = PrefabUtility.GetPrefabInstancesOverridesInfos(m_SelectedGameObjects);

            CalculateStatistics();

            float logHintWidth = GetTextWidth(Styles.editorLogHint.text, Styles.bodyStyle);
            float headerWidth = BuildHeaderText();
            float maxSummaryLineWidth = BuildMultilineSummary();

            float maxWidth = (headerWidth > logHintWidth) ? headerWidth : logHintWidth;
            if (maxSummaryLineWidth > maxWidth)
                maxWidth = maxSummaryLineWidth;

            m_ViewSize.x = maxWidth + k_ViewWidthPadding;

            var lineHeight = GetTextHeight("a", Styles.bodyStyle);
            float height = k_HeaderHeight + k_BodyTextPadding + (lineHeight * m_OverridesContent.Count) + (k_BodyTextPadding * 2) + k_BodyTextPaddingSmall;
            height += (m_RemainingOverridesInfo != null) ? k_BodyTextPaddingSmall + EditorStyles.label.lineHeight : 0;
            m_ViewSize.y = height;
        }

        public override void OnOpen()
        {
            s_IsOpen = true;
        }

        public override void OnClose()
        {
            s_IsOpen = false;
            base.OnClose();
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(m_ViewSize.x, m_ViewSize.y + k_HeaderHeight);
        }

        public override void OnGUI(Rect rect)
        {
            Rect headerRect = new Rect(rect.x, rect.y, rect.width, k_HeaderHeight);
            DrawHeader(headerRect);

            Rect bodyRect = new Rect(rect.x, rect.y + k_HeaderHeight + k_BodyTextPadding, rect.width, rect.height - k_HeaderHeight - k_BodyLineHeight);
            GUILayout.BeginArea(bodyRect);

            foreach (GUIContent lineContent in m_OverridesContent)
            {
                GUILayout.Label(lineContent.text, Styles.bodyStyle);
            }

            if (m_RemainingOverridesInfo != null)
            {
                GUILayout.Space(k_BodyTextPaddingSmall);
                GUILayout.Label(m_RemainingOverridesInfo, Styles.bodyStyle);
            }

            GUILayout.Space(k_BodyTextPadding * 2);
            GUILayout.Label(Styles.editorLogHint, Styles.bodyStyle);
            GUILayout.Space(k_BodyTextPaddingSmall);
            GUILayout.EndArea();
        }

        void DrawHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, m_HeaderText, Styles.headerStyle);
            GUI.Label(new Rect(rect.x, rect.y, rect.width, rect.height), GUIContent.none, Styles.borderStyle);

            DrawRemoveButton(rect);
        }

        void DrawRemoveButton(Rect rect)
        {
            GUILayout.BeginArea(rect);
            GUILayout.BeginHorizontal(Styles.headerGroupStyle);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(Styles.buttonContent, EditorStyles.miniButton, GUILayout.Width(k_ButtonWidth)))
            {
                PrefabUtility.RemovePrefabInstanceUnusedOverrides(m_InstanceOverridesInfos);
                editorWindow.Close();
                m_Owner.RefreshStatus();
                GUIUtility.ExitGUI();
            }
            GUILayout.Space(k_BodyTextPaddingSmall);
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        void CalculateStatistics()
        {
            m_SingleInstanceWithUnusedMods = m_InstanceOverridesInfos[0];
            m_SelectedInstanceCount = m_InstanceOverridesInfos.Count();

            if (m_SelectedInstanceCount > 1)
            {
                foreach (PrefabUtility.InstanceOverridesInfo instanceMods in m_InstanceOverridesInfos)
                {
                    if (instanceMods.unusedOverrideCount == 0)
                        continue;

                    m_AffectedInstanceCount++;
                    m_UnusedOverridesCount += instanceMods.unusedMods.Length;
                    m_UsedOverridesCount += instanceMods.usedMods.Length;
                    m_UnusedRemovedGameObjectsCount += instanceMods.unusedRemovedGameObjectCount;
                    m_UnusedRemovedComponentsCount += instanceMods.unusedRemovedComponentCount;
                    m_SingleInstanceWithUnusedMods = instanceMods;
                }
            }
            else
            {
                m_AffectedInstanceCount = 1;
                m_UnusedOverridesCount = m_SingleInstanceWithUnusedMods.unusedMods.Length;
                m_UsedOverridesCount = m_SingleInstanceWithUnusedMods.usedMods.Length;
                m_UnusedRemovedGameObjectsCount = m_SingleInstanceWithUnusedMods.unusedRemovedGameObjectCount;
                m_UnusedRemovedComponentsCount = m_SingleInstanceWithUnusedMods.unusedRemovedComponentCount;
            }

            m_UnusedOverridesCount += m_UnusedRemovedComponentsCount;
            m_UnusedOverridesCount += m_UnusedRemovedGameObjectsCount;
        }

        float BuildHeaderText()
        {
            if (m_UnusedOverridesCount > 1)
                m_HeaderText = string.Format(Styles.headerContent.text, m_UnusedOverridesCount);
            else
                m_HeaderText = Styles.headerContentSingular.text;

            return GetTextWidth(m_HeaderText, Styles.headerStyle) + k_ViewWidthPadding + k_ButtonWidth;
        }

        float BuildMultilineSummary()
        {
            m_OverridesContent = new List<GUIContent>();
            int remainingAffectedInstanceCount = m_AffectedInstanceCount;
            float maxLineWidth = 0;

            foreach (PrefabUtility.InstanceOverridesInfo instanceMods in m_InstanceOverridesInfos)
            {
                int initialOverridesCount = m_OverridesContent.Count;

                foreach (PropertyModification mod in instanceMods.unusedMods)
                {
                    if (m_OverridesContent.Count >= k_MaxEntries)
                        break;

                    string itemText = mod.propertyPath + " " + Styles.pathOnInstanceContent.text + " " + instanceMods.instance.name;
                    m_OverridesContent.Add(new GUIContent(itemText));
                }

                if (instanceMods.unusedRemovedGameObjectCount > 0 && m_OverridesContent.Count < k_MaxEntries)
                {
                    string itemText = string.Empty;
                    if (instanceMods.unusedRemovedGameObjectCount > 1)
                        itemText = string.Format(Styles.unusedRemovedGameObjectsContentPlural.text, instanceMods.instance.name, instanceMods.unusedRemovedGameObjectCount);
                    else
                        itemText = string.Format(Styles.unusedRemovedGameObjectsContentSingular.text, instanceMods.instance.name);

                    m_OverridesContent.Add(new GUIContent(itemText));
                }

                if (instanceMods.unusedRemovedComponentCount > 0 && m_OverridesContent.Count < k_MaxEntries)
                {
                    string itemText = string.Empty;
                    if (instanceMods.unusedRemovedComponentCount > 1)
                        itemText = string.Format(Styles.unusedRemovedComponentsContentPlural.text, instanceMods.instance.name, instanceMods.unusedRemovedComponentCount);
                    else
                        itemText = string.Format(Styles.unusedRemovedComponentsContentSingular.text, instanceMods.instance.name);

                    m_OverridesContent.Add(new GUIContent(itemText));
                }

                int entriesFromThisInstance = m_OverridesContent.Count - initialOverridesCount;

                bool addedLines = entriesFromThisInstance > 0;
                if (addedLines && instanceMods.unusedMods.Length <= entriesFromThisInstance)
                    remainingAffectedInstanceCount--;
            }

            foreach (var content in m_OverridesContent)
            {
                float w = GetTextWidth(content.text, Styles.bodyStyle);
                maxLineWidth = Math.Max(w, maxLineWidth);
            }

            int remainingUnusedOverridesCount = m_UnusedOverridesCount - k_MaxEntries;
            if (remainingUnusedOverridesCount > 0)
            {
                string remainingOverridesInfo = string.Format(Styles.extraOverridesContent.text, remainingUnusedOverridesCount);

                if (remainingAffectedInstanceCount > 1)
                    remainingOverridesInfo += " " + string.Format(Styles.extraInstancesContent.text, remainingAffectedInstanceCount);

                remainingOverridesInfo += ".";
                m_RemainingOverridesInfo = new GUIContent(remainingOverridesInfo);

                float w = GetTextWidth(m_RemainingOverridesInfo.text, Styles.bodyStyle);
                maxLineWidth = Math.Max(w, maxLineWidth);
            }

            return maxLineWidth;
        }

        float GetTextWidth(string text, GUIStyle style)
        {
            var content = GUIContent.Temp(text);
            return style.CalcSize(content).x;
        }

        float GetTextHeight(string text, GUIStyle style)
        {
            var content = GUIContent.Temp(text);
            return style.CalcSize(content).y;
        }
    }
}
