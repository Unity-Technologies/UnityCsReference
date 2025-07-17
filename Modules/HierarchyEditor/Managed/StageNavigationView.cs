// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UIElements;

namespace Unity.Hierarchy.Editor
{
    class StageNavigationView : VisualElement, IDisposable
    {
        readonly Image m_Icon;
        readonly Label m_Label;
        readonly Label m_UnsavedChanges;
        readonly VisualElement m_ContextMenuButton;

        public StageNavigationView()
        {
            var tpl = EditorGUIUtility.Load("UXML/HierarchyWindow/hierarchy-prefab-stage.uxml") as VisualTreeAsset;
            var variables = EditorGUIUtility.Load($"StyleSheets/HierarchyWindow/hierarchy-prefab-stage_{(EditorGUIUtility.isProSkin ? "dark" : "light")}.uss") as StyleSheet;
            var uss = EditorGUIUtility.Load("StyleSheets/HierarchyWindow/hierarchy-prefab-stage.uss") as StyleSheet;
            VisualElement container = tpl.Instantiate();
            VisualElement nameContainer = container.Q(className: "hierarchy-prefab-stage__name-container");
            m_ContextMenuButton = container.Q(className: "hierarchy-prefab-stage__context-menu");
            container.Q(className: "hierarchy-prefab-stage__back").RegisterCallback<PointerUpEvent>(OnBackPressed);
            nameContainer.RegisterCallback<PointerUpEvent>(OnNameContainerPressed);
            nameContainer.AddManipulator(new ContextualMenuManipulator(OpenContextMenu));

            var manipulator = new ContextualMenuManipulator(OpenContextMenu);
            manipulator.activators.Clear();
            manipulator.activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
            m_ContextMenuButton.AddManipulator(manipulator);

            m_Icon = container.Q<Image>(className: "hierarchy-prefab-stage__name-icon");
            m_Label = container.Q<Label>(className: "hierarchy-prefab-stage__name-label");
            m_UnsavedChanges = container.Q<Label>(className: "hierarchy-prefab-stage__name-unsaved-changes");

            styleSheets.Add(variables);
            styleSheets.Add(uss);
            Add(container);

            Update();

            StageNavigationManager.instance.stageChanged += OnStageChanged;
            StageNavigationManager.instance.StagesTicked += OnStagesTicked;
        }

        void OnStagesTicked()
        {
            var currentStage = StageNavigationManager.instance.currentStage;
            if (currentStage is MainStage)
                return;

            UpdateStyles(currentStage);
        }

        private void OpenContextMenu(ContextualMenuPopulateEvent evt)
        {
            using var poolHandle = GenericMenu.Pool.Get(out var genericMenu);
            StageNavigationManager.instance.currentStage.BuildContextMenuForStageHeader(genericMenu);
            evt.menu.AppendFromGenericMenu(genericMenu);
        }

        private void OnNameContainerPressed(PointerUpEvent evt)
        {
            if (evt.button != (int)MouseButton.LeftMouse)
                return;

            var currentStage = StageNavigationManager.instance.currentStage;
            if (currentStage.isAssetMissing)
                return;

            EditorGUIUtility.PingObject(AssetDatabase.GetMainAssetEntityId(currentStage.assetPath));
        }

        private void OnBackPressed(PointerUpEvent evt)
            => StageNavigationManager.instance.NavigateBack(StageNavigationManager.Analytics.ChangeType.NavigateBackViaHierarchyHeaderLeftArrow);

        void OnStageChanged(Stage before, Stage after) => Update();

        void Update()
        {
            var currentStage = StageNavigationManager.instance.currentStage;
            if (currentStage is MainStage)
            {
                style.display = DisplayStyle.None;
            }
            else
            {
                style.display = DisplayStyle.Flex;

                var headerContent = currentStage.CreateHeaderContent();

                m_Label.text = headerContent.text;
                m_Label.tooltip = headerContent.tooltip;
                m_Icon.image = headerContent.image;

                UpdateStyles(currentStage);
            }
        }

        void UpdateStyles(Stage currentStage)
        {
            m_Icon.style.display = currentStage.isAssetMissing ? DisplayStyle.None : DisplayStyle.Flex;
            m_UnsavedChanges.style.display = currentStage.hasUnsavedChanges ? DisplayStyle.Flex : DisplayStyle.None;
            m_Label.EnableInClassList("hierarchy-prefab-stage__name-label__asset-missing", currentStage.isAssetMissing);
            m_ContextMenuButton.style.display = currentStage.SupportsSaving() ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void Dispose()
        {
            StageNavigationManager.instance.stageChanged -= OnStageChanged;
            StageNavigationManager.instance.StagesTicked -= OnStagesTicked;
        }

        internal static class TestHelper
        {
            internal static void TriggerOnStagesTicked(StageNavigationView view)
            {
                view.OnStagesTicked();
            }
        }
    }
}
