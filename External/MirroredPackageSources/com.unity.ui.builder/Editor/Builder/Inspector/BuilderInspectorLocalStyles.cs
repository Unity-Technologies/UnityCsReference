using UnityEngine.UIElements;
using System.Collections.Generic;
using System;
using UnityEngine.Assertions;

namespace Unity.UI.Builder
{
    internal class BuilderInspectorLocalStyles : IBuilderInspectorSection
    {
        BuilderInspector m_Inspector;
        BuilderInspectorStyleFields m_StyleFields;

        PersistedFoldout m_LocalStylesSection;

        readonly Dictionary<PersistedFoldout, List<VisualElement>> m_StyleCategories = new Dictionary<PersistedFoldout, List<VisualElement>>();

        public VisualElement root => m_LocalStylesSection;

        public BuilderInspectorLocalStyles(BuilderInspector inspector, BuilderInspectorStyleFields styleFields)
        {
            m_Inspector = inspector;
            m_StyleFields = styleFields;

            m_StyleFields.updateFlexColumnGlobalState = UpdateFlexColumnGlobalState;
            m_StyleFields.updateStyleCategoryFoldoutOverrides = UpdateStyleCategoryFoldoutOverrides;

            m_LocalStylesSection = m_Inspector.Q<PersistedFoldout>("inspector-local-styles-foldout");

            // We need to hide new Text Asset style property fields in any Unity version older than 2021.1.
            m_LocalStylesSection.Query(className: "unity-builder-font-asset-property-container").ForEach(e => e.style.display = DisplayStyle.None);

            var styleCategories = m_LocalStylesSection.Query<PersistedFoldout>(
                className: "unity-builder-inspector__style-category-foldout").ToList();

            foreach (var styleCategory in styleCategories)
            {
                styleCategory.Q<VisualElement>(null, PersistedFoldout.headerUssClassName)
                    .AddManipulator(new ContextualMenuManipulator(StyleCategoryContextualMenu));

                var categoryStyleFields = new List<VisualElement>();
                var styleRows = styleCategory.Query<BuilderStyleRow>().ToList();
                foreach (var styleRow in styleRows)
                {
                    var bindingPath = styleRow.bindingPath;
                    var currentStyleFields = styleRow.Query<BindableElement>().ToList();

                    if (styleRow.ClassListContains("unity-builder-double-field-row"))
                        m_StyleFields.BindDoubleFieldRow(styleRow);

                    foreach (var styleField in currentStyleFields)
                    {
                        // Avoid fields within fields.
                        if (styleField.parent != styleRow)
                            continue;

                        if (styleField is FoldoutNumberField)
                        {
                            m_StyleFields.BindStyleField(styleRow, styleField as FoldoutNumberField);
                        }
                        else if (styleField is FoldoutColorField)
                        {
                            m_StyleFields.BindStyleField(styleRow, styleField as FoldoutColorField);
                        }
                        else if (!string.IsNullOrEmpty(styleField.bindingPath))
                        {
                            m_StyleFields.BindStyleField(styleRow, styleField.bindingPath, styleField);
                        }
                        else
                        {
                            BuilderStyleRow.ReAssignTooltipToChild(styleField);
                            m_StyleFields.BindStyleField(styleRow, bindingPath, styleField);
                        }

                        categoryStyleFields.Add(styleField);
                    }
                }
                m_StyleCategories.Add(styleCategory, categoryStyleFields);
            }
        }

        void StyleCategoryContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction(
                BuilderConstants.ContextMenuSetMessage,
                action => {},
                action => DropdownMenuAction.Status.Disabled,
                evt.target);

            evt.menu.AppendAction(
                BuilderConstants.ContextMenuUnsetMessage,
                UnsetStyleProperties,
                action => DropdownMenuAction.Status.Normal,
                evt.target);

            evt.menu.AppendAction(
                BuilderConstants.ContextMenuUnsetAllMessage,
                m_StyleFields.UnsetAllStyleProperties,
                m_StyleFields.UnsetAllActionStatus,
                evt.target);
        }

        void UnsetStyleProperties(DropdownMenuAction obj)
        {
            var foldout = (PersistedFoldout)(obj.userData as VisualElement)?.parent;
            Assert.IsNotNull(foldout);
            List<VisualElement> styleFields;

            if (m_StyleCategories.TryGetValue(foldout, out styleFields))
            {
                var tempFields = new List<VisualElement>();
                foreach (var styleField in styleFields)
                {
                    if (!(styleField is FoldoutField))
                        tempFields.Add(styleField);
                }
                m_StyleFields.UnsetStyleProperties(tempFields);
            }
        }

        public void Enable()
        {
            m_Inspector.Query<BuilderStyleRow>().ForEach(e =>
            {
                e.SetEnabled(true);
            });
        }

        public void Disable()
        {
            m_Inspector.Query<BuilderStyleRow>().ForEach(e =>
            {
                e.SetEnabled(false);
            });
        }

        public void Refresh()
        {
            if (m_Inspector.currentVisualElement == null)
                return;

            if (BuilderSharedStyles.IsSelectorElement(m_Inspector.currentVisualElement))
                m_LocalStylesSection.text = BuilderConstants.InspectorLocalStylesSectionTitleForSelector;
            else
                m_LocalStylesSection.text = BuilderConstants.InspectorLocalStylesSectionTitleForElement;

            var styleRows = m_LocalStylesSection.Query<BuilderStyleRow>().ToList();

            foreach (var styleRow in styleRows)
            {
                var bindingPath = styleRow.bindingPath;
                var styleFields = styleRow.Query<BindableElement>().ToList();

                foreach (var styleField in styleFields)
                {
                    // Avoid fields within fields.
                    if (styleField.parent != styleRow)
                        continue;

                    if (styleField is FoldoutField)
                    {
                        m_StyleFields.RefreshStyleField(styleField as FoldoutField);
                    }
                    else if (!string.IsNullOrEmpty(styleField.bindingPath))
                    {
                        m_StyleFields.RefreshStyleField(styleField.bindingPath, styleField);
                    }
                    else
                    {
                        m_StyleFields.RefreshStyleField(bindingPath, styleField);
                    }
                }
            }

            UpdateStyleCategoryFoldoutOverrides();
        }

        public void UpdateStyleCategoryFoldoutOverrides()
        {
            foreach (var pair in m_StyleCategories)
            {
                var styleCategory = pair.Key;
                var hasOverridenField = styleCategory.Q(className: BuilderConstants.InspectorLocalStyleOverrideClassName) != null;
                styleCategory.EnableInClassList(BuilderConstants.InspectorCategoryFoldoutOverrideClassName, hasOverridenField);
            }
        }

        void UpdateFlexColumnGlobalState(Enum newValue)
        {
            var newDirection = (FlexDirection)newValue;
            m_LocalStylesSection.EnableInClassList(BuilderConstants.InspectorFlexColumnModeClassName, newDirection == FlexDirection.Column);
            m_LocalStylesSection.EnableInClassList(BuilderConstants.InspectorFlexColumnReverseModeClassName, newDirection == FlexDirection.ColumnReverse);
            m_LocalStylesSection.EnableInClassList(BuilderConstants.InspectorFlexRowModeClassName, newDirection == FlexDirection.Row);
            m_LocalStylesSection.EnableInClassList(BuilderConstants.InspectorFlexRowReverseModeClassName, newDirection == FlexDirection.RowReverse);
        }
    }
}
