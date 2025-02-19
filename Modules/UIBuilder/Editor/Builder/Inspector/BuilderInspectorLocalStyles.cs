// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine.Assertions;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;
using UnityEditor;
using UnityEngine;

namespace Unity.UI.Builder
{
    internal sealed class BuilderInspectorLocalStyles : IBuilderInspectorSection, IDisposable
    {
        BuilderInspector m_Inspector;
        BuilderInspectorStyleFields m_StyleFields;

        PersistedFoldout m_LocalStylesSection;

        readonly Dictionary<PersistedFoldout, List<VisualElement>> m_StyleCategories = new Dictionary<PersistedFoldout, List<VisualElement>>();

        public VisualElement root => m_LocalStylesSection;

        // ReSharper disable MemberCanBePrivate.Global
        internal const string refreshMarkerName = "BuilderInspectorLocalStyles.Refresh";
        // ReSharper restore MemberCanBePrivate.Global

        static readonly ProfilerMarker k_RefreshMarker = new (refreshMarkerName);

        public BuilderInspectorLocalStyles(BuilderInspector inspector, BuilderInspectorStyleFields styleFields)
        {
            m_Inspector = inspector;
            m_StyleFields = styleFields;

            m_StyleFields.updatePositionAnchorsFoldoutState = UpdatePositionAnchorsFoldoutState;
            m_StyleFields.updateStyleCategoryFoldoutOverrides = UpdateStyleCategoryFoldoutOverrides;

            m_LocalStylesSection = m_Inspector.Q<PersistedFoldout>("inspector-local-styles-foldout");

            var styleCategories = m_LocalStylesSection.Query<PersistedFoldout>(
                className: "unity-builder-inspector__style-category-foldout").Build();

            foreach (var styleCategory in styleCategories)
            {
                styleCategory.Q<VisualElement>(null, PersistedFoldout.headerUssClassName)
                    .AddManipulator(new ContextualMenuManipulator(StyleCategoryContextualMenu));

                var categoryStyleFields = new List<VisualElement>();
                var styleRows = styleCategory.Query<BuilderStyleRow>().Build();
                foreach (var styleRow in styleRows)
                {
                    var bindingPath = styleRow.bindingPath;
                    var currentStyleFields = styleRow.Query<BindableElement>().Build();

                    if (styleRow.ClassListContains(BuilderConstants.InspectorMultiFieldsRowClassName))
                        m_StyleFields.BindDoubleFieldRow(styleRow);

                    foreach (var styleField in currentStyleFields)
                    {
                        // Avoid fields within fields.
                        if (!IsIndirectChildOf(styleField, styleRow))
                            continue;

                        if (styleField is FoldoutNumberField)
                        {
                            m_StyleFields.BindStyleField(styleRow, styleField as FoldoutNumberField);
                        }
                        else if (styleField is FoldoutColorField)
                        {
                            m_StyleFields.BindStyleField(styleRow, styleField as FoldoutColorField);
                        }
                        else if (styleField is TransitionsListView transitionsListView)
                        {
                            GenerateTransitionPropertiesContent();
                            m_StyleFields.BindStyleField(styleRow, transitionsListView);
                        }
                        else if (styleField is BoxModel boxModel)
                        {
                            foreach (var bindingPathName in boxModel.bindingPathArray)
                            {
                                m_StyleFields.BindStyleField(styleRow, bindingPathName, styleField);
                            }
                        }
                        else if (!string.IsNullOrEmpty(styleField.bindingPath))
                        {
                            m_StyleFields.BindStyleField(styleRow, styleField.bindingPath, styleField);
                        }
                        else
                        {
                            m_StyleFields.BindStyleField(styleRow, bindingPath, styleField);
                        }

                        categoryStyleFields.Add(styleField);
                    }
                }
                m_StyleCategories.Add(styleCategory, categoryStyleFields);
            }
        }

        public void Dispose()
        {
            TransitionPropertyDropdownContent.Content = default;
        }

        void StyleCategoryContextualMenu(ContextualMenuPopulateEvent evt)
        {
            bool isSelector = BuilderSharedStyles.IsSelectorElement(m_Inspector.currentVisualElement);

            evt.menu.AppendAction(
                isSelector ? BuilderConstants.ContextMenuSetAsValueMessage : BuilderConstants.ContextMenuSetAsInlineValueMessage,
                action => {},
                action => DropdownMenuAction.Status.Disabled,
                evt.elementTarget);

            evt.menu.AppendAction(
                BuilderConstants.ContextMenuUnsetMessage,
                UnsetStyleProperties,
                action => DropdownMenuAction.Status.Normal,
                evt.elementTarget);

            evt.menu.AppendAction(
                BuilderConstants.ContextMenuUnsetAllMessage,
                (action) => m_StyleFields.UnsetAllStyleProperties(),
                m_StyleFields.UnsetAllActionStatus,
                evt.elementTarget);
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
                m_StyleFields.UnsetStyleProperties(tempFields, true);
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
            using var marker = k_RefreshMarker.Auto();

            if (m_Inspector.currentVisualElement == null)
                return;

            if (BuilderSharedStyles.IsSelectorElement(m_Inspector.currentVisualElement))
                m_LocalStylesSection.text = BuilderConstants.InspectorLocalStylesSectionTitleForSelector;
            else
                m_LocalStylesSection.text = BuilderConstants.InspectorLocalStylesSectionTitleForElement;

            var styleRows = m_LocalStylesSection.Query<BuilderStyleRow>().Build();

            foreach (var styleRow in styleRows)
            {
                var bindingPath = styleRow.bindingPath;
                var styleFields = styleRow.Query<BindableElement>().Build();

                foreach (var styleField in styleFields)
                {
                    // Avoid fields within fields.
                    if (!IsIndirectChildOf(styleField, styleRow))
                        continue;

                    m_Inspector.ToggleInlineEditingClasses(styleField, false);

                    if (styleField is FoldoutField)
                    {
                        m_StyleFields.RefreshStyleField(styleField as FoldoutField);
                    }
                    else if (styleField is TransitionsListView transitionsListView)
                    {
                        m_StyleFields.RefreshStyleField(transitionsListView);
                    }
                    else if (styleField is BoxModel boxModel)
                    {
                        foreach (var bindingPathName in boxModel.bindingPathArray)
                        {
                            m_StyleFields.RefreshStyleField(bindingPathName, styleField);
                        }
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
            var hasAnyBoundField = false;
            var hasAnyOverrides = false;

            foreach (var pair in m_StyleCategories)
            {
                var styleCategory = pair.Key;
                var hasOverriddenField = BuilderInspectorUtilities.HasOverriddenField(styleCategory);
                var hasBoundField = styleCategory.Q(className: BuilderConstants.InspectorLocalStyleBindingClassName) != null
                                    || styleCategory.Q(className: BuilderConstants.InspectorLocalStyleUnresolvedBindingClassName) != null;

                styleCategory.EnableInClassList(BuilderConstants.InspectorStyleCategoryFoldoutOverrideClassName, hasOverriddenField);
                styleCategory.EnableInClassList(BuilderConstants.InspectorCategoryFoldoutBindingClassName, hasBoundField);

                hasAnyBoundField |= hasBoundField;
                hasAnyOverrides |= hasOverriddenField;
            }

            m_LocalStylesSection.EnableInClassList(BuilderConstants.InspectorCategoryFoldoutOverrideClassName, hasAnyOverrides);
            m_LocalStylesSection.EnableInClassList(BuilderConstants.InspectorCategoryFoldoutBindingClassName, hasAnyBoundField);
        }

        void UpdatePositionAnchorsFoldoutState(Enum newValue)
        {
            var newPosition = (Position)newValue;
            var foldout = m_Inspector.Q<Foldout>("anchors-foldout");

            foldout.text = (newPosition == Position.Absolute) ? "Anchors" : "Offsets";
            foldout.EnableInClassList(Position.Relative.ToString().ToLowerInvariant(), newPosition == Position.Relative);
        }

        /// <summary>
        /// This will iterate over the current UI Builder hierarchy and extract the supported animatable properties.
        /// This will allow to display the options to users in the same order that they are in the builder.
        /// </summary>
        void GenerateTransitionPropertiesContent()
        {
            var content = new CategoryDropdownContent();
            content.AppendValue(new CategoryDropdownContent.ValueItem
            {
                value = "all",
                displayName = "all"
            });

            foreach (var kvp in m_StyleCategories)
            {
                var groupName = kvp.Key.text;
                if (string.IsNullOrWhiteSpace(groupName) || groupName == "Transition Animations")
                    continue;

                content.AppendCategory(new CategoryDropdownContent.Category { name = groupName });

                var stringNameHashSet = HashSetPool<string>.Get();
                var hashSet = HashSetPool<StylePropertyId>.Get();

                try
                {
                    foreach (var element in kvp.Value)
                    {
                        var styleName = element.GetProperty(BuilderConstants.InspectorStylePropertyNameVEPropertyName) as string;

                        if (!string.IsNullOrWhiteSpace(styleName))
                        {
                            var styleId = StyleDebug.GetStylePropertyIdFromName(styleName);
                            if (!StylePropertyUtil.IsAnimatable(styleId) || !stringNameHashSet.Add(styleName))
                                continue;

                            if (!string.IsNullOrWhiteSpace(styleId.ToString()))
                            {
                                content.AppendValue(
                                    new CategoryDropdownContent.ValueItem
                                    {
                                        categoryName = groupName,
                                        value = styleName,
                                        displayName = ObjectNames.NicifyVariableName(styleId.ToString())
                                    });
                            }
                        }

                        if (!(element is FoldoutField foldoutField)) continue;
                        foreach (var bindingPath in foldoutField.bindingPathArray)
                        {
                            var shortHandId = StyleDebug.GetStylePropertyIdFromName(bindingPath).GetShorthandProperty();
                            if (shortHandId == StylePropertyId.Unknown || !hashSet.Add(shortHandId))
                                continue;

                            if (!StylePropertyUtil.IsAnimatable(shortHandId))
                                continue;

                            if (!string.IsNullOrWhiteSpace(shortHandId.ToString()))
                            {
                                content.AppendValue(
                                    new CategoryDropdownContent.ValueItem
                                    {
                                        categoryName = groupName,
                                        value = BuilderNameUtilities.ConvertStyleCSharpNameToUssName(
                                            shortHandId.ToString().ToCamelCase()),
                                        displayName = ObjectNames.NicifyVariableName(shortHandId.ToString())
                                    });
                            }
                        }
                    }
                }
                finally
                {
                    HashSetPool<string>.Release(stringNameHashSet);
                    HashSetPool<StylePropertyId>.Release(hashSet);
                }
            }

            content.AppendSeparator();
            content.AppendValue(new CategoryDropdownContent.ValueItem
            {
                value = "none",
                displayName = "none"
            });

            content.AppendValue(new CategoryDropdownContent.ValueItem
            {
                value = "initial",
                displayName = "initial"
            });

            content.AppendValue(new CategoryDropdownContent.ValueItem
            {
                value = "ignored",
                displayName = "ignored"
            });

            TransitionPropertyDropdownContent.Content = content;
        }

        /// <summary>
        /// This will return if the styleField is a child of styleRow while still allowing intermediate styling elements between them.
        /// This will allow styling of composite style rows.
        /// </summary>
        static bool IsIndirectChildOf(VisualElement styleField, BuilderStyleRow styleRow)
        {
            var currentParent = styleField.parent;
            while (currentParent != null && currentParent.ClassListContains(BuilderConstants.InspectorCompositeStyleRowElementClassName))
            {
                currentParent = currentParent.parent;
            }
            return currentParent == styleRow;
        }
    }
}
