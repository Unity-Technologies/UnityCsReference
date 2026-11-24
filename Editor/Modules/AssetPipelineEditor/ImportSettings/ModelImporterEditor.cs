// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Linq;

using UnityEngine;
using UnityEditor.Experimental.AssetImporters.ImportBlocks;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEditor
{
    [CustomEditor(typeof(ModelImporter))]
    [CanEditMultipleObjects]
    internal class ModelImporterEditor : AssetImporterTabbedEditor
    {
        static readonly string s_LocalizedTitle = L10n.Tr("Model Import Settings", null);

        // The modelimporterclipeditor is drawing its own preview for clips to be editable.
        protected override bool useAssetDrawPreview => !(activeTab is ModelImporterClipEditor);

        public override void OnEnable()
        {
            if (tabs == null)
            {
                if (ImportBlocksToggle.IsEnabled)
                {
                    tabs = new BaseAssetImporterTabUI[] { new ModelImporterModelEditor(this), new ModelImporterRigEditor(this), new ModelImporterClipEditor(this), new ModelImporterMaterialEditor(this), new BlocksTabUI(this) };
                    m_TabNames = new string[] {"Model", "Rig", "Animation", "Materials", "Blocks"};
                }
                else
                {
                    tabs = new BaseAssetImporterTabUI[] { new ModelImporterModelEditor(this), new ModelImporterRigEditor(this), new ModelImporterClipEditor(this), new ModelImporterMaterialEditor(this) };
                    m_TabNames = new string[] {"Model", "Rig", "Animation", "Materials"};
                }
            }
            base.OnEnable();
        }

        public override VisualElement CreateInspectorGUI()
        {
            // With blocks disabled there is no Blocks tab: fall back to the tabbed base's IMGUI inspector rather
            // than routing every model import through the UI Toolkit per-tab path.
            if (!ImportBlocksToggle.IsEnabled)
                return null;

            var root = new VisualElement();
            var tabContents = new VisualElement[tabs.Length];

            void UpdateTabVisibility()
            {
                for (int i = 0; i < tabContents.Length; i++)
                    tabContents[i].style.display = i == activeTabIndex ? DisplayStyle.Flex : DisplayStyle.None;
            }

            // IMGUI so the tab bar stays usable on a read-only asset: UI Toolkit cannot re-enable a disabled subtree.
            root.Add(new IMGUIContainer(() =>
            {
                GUI.enabled = true;
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    using (var check = new EditorGUI.ChangeCheckScope())
                    {
                        int index = GUILayout.Toolbar(activeTabIndex, m_TabNames, "LargeButton", GUI.ToolbarButtonSize.FitToContents);
                        if (check.changed)
                        {
                            SetActiveTabIndex(index);
                            UpdateTabVisibility();
                        }
                    }
                    GUILayout.FlexibleSpace();
                }
            }));

            for (int i = 0; i < tabs.Length; i++)
            {
                var tab = tabs[i];
                tabContents[i] = tab.CreateInspectorGUI() ?? CreateImguiTabContainer(tab);
                root.Add(tabContents[i]);
            }

            root.Add(CreateImguiContainer(ApplyRevertGUI));

            UpdateTabVisibility();
            return root;
        }

        IMGUIContainer CreateImguiContainer(Action onGUI)
        {
            IMGUIContainer container = null;
            container = new IMGUIContainer(() =>
            {
                var prevViewWidth = EditorGUIUtility.currentViewWidth;
                var prevHierarchyMode = EditorGUIUtility.hierarchyMode;
                var prevWideMode = EditorGUIUtility.wideMode;
                EditorGUIUtility.ResetGUIState();
                EditorGUIUtility.hierarchyMode = true;
                InspectorElement.SetWideModeForWidth(container);
                try
                {
                    // ResetGUIState cleared the disabled state the container inherited, so re-apply it.
                    using (new EditorGUI.DisabledScope(!IsEnabled() || !container.enabledInHierarchy))
                        onGUI();
                }
                finally
                {
                    EditorGUIUtility.wideMode = prevWideMode;
                    EditorGUIUtility.hierarchyMode = prevHierarchyMode;
                    EditorGUIUtility.currentViewWidth = prevViewWidth;
                }
            });
            container.style.overflow = Overflow.Visible;
            return container;
        }

        IMGUIContainer CreateImguiTabContainer(BaseAssetImporterTabUI tab)
        {
            return CreateImguiContainer(() =>
            {
                serializedObject.Update();
                extraDataSerializedObject?.Update();
                EditorGUILayout.BeginVertical(EditorStyles.inspectorDefaultMargins);
                tab.OnInspectorGUI();
                EditorGUILayout.EndVertical();
                extraDataSerializedObject?.ApplyModifiedProperties();
                serializedObject.ApplyModifiedProperties();
            });
        }

        private protected override bool showAssetPostprocessorsFoldout => !(activeTab is BlocksTabUI);

        internal override void PostSerializedObjectCreation()
        {
            if(tabs != null)
            {
                foreach (var tab in tabs)
                    tab?.PostSerializedObjectCreation();
            }
        }

        public override void OnDisable()
        {
            foreach (var tab in tabs)
            {
                tab.OnDisable();
            }
            base.OnDisable();
        }

        //None of the ModelImporter sub editors support multi preview
        public override bool HasPreviewGUI()
        {
            return base.HasPreviewGUI() && targets.Length < 2;
        }

        public override GUIContent GetPreviewTitle()
        {
            var tab = activeTab as ModelImporterClipEditor;
            if (tab != null)
                return new GUIContent(tab.selectedClipName);

            return base.GetPreviewTitle();
        }

        protected override void Apply()
        {
            base.Apply();

            // This is necessary to enforce redrawing the static preview icons in the project browser,
            // because some settings may have changed the preview completely.
            foreach (ProjectBrowser pb in ProjectBrowser.GetAllProjectBrowsers())
                pb.Repaint();
        }

        // Only show the imported GameObject when the Model tab is active; not when the Animation tab is active
        public override bool showImportedObject { get { return activeTab is ModelImporterModelEditor; } }

        internal override string targetTitle
        {
            get
            {
                if (assetTargets == null || assetTargets.Length == 1 || !m_AllowMultiObjectAccess)
                    return base.targetTitle;
                else
                    return assetTargets.Length + " " + s_LocalizedTitle;
            }
        }
    }
}
