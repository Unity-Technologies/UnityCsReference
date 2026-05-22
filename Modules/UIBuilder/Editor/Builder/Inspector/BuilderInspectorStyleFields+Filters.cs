// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UI.Builder
{
    partial class BuilderInspectorStyleFields
    {
        static class FilterConstants
        {
            public static readonly string Filter = StylePropertyId.Filter.UssName();
        }

        public void BindStyleField(BuilderStyleRow styleRow, FilterStyleField filterStyleField)
        {
            filterStyleField.SetContainingRow(styleRow);

            filterStyleField.SetInspectorStylePropertyName(FilterConstants.Filter);
            GetOrCreateFieldListForStyleName(FilterConstants.Filter).Add(filterStyleField);

            SetUpContextualMenuOnStyleField(filterStyleField);

            filterStyleField.RegisterCallback<FilterListChangedEvent, FilterStyleField>(OnFilterListChanged, filterStyleField);
            filterStyleField.RegisterCallback<FilterFunctionReorderedEvent, FilterStyleField>(OnFilterFunctionReordered, filterStyleField);
        }

        public void RefreshStyleField(FilterStyleField filterStyleField)
        {
            // It's important to cancel any running animation so that when we query the computed style
            // we get the new number of filters. Otherwise, the first frame of any transition will
            // have the old number of filters before the add/remove of the filter.

            if (currentVisualElement.HasRunningAnimation(StylePropertyId.Filter))
                currentVisualElement.CancelAnimation(StylePropertyId.Filter);

            filterStyleField.SetValueWithoutNotify(currentVisualElement.computedStyle.filter);

            var prop = GetLastStyleProperty(currentRule, FilterConstants.Filter);
            m_Inspector.UpdateFieldStatus(filterStyleField, prop);
        }

        void ApplyFilterListChange(List<FilterFunction> newFilterList, bool refreshField, VisualElement elementTarget)
        {
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            var styleProperty = GetOrCreateStylePropertyByStyleName(FilterConstants.Filter);

            if (newFilterList == null || newFilterList.Count == 0)
            {
                // Set to 'none' rather than removing the property, so the inline value
                // can override any filter coming from a selector.
                styleProperty.SetKeyword(styleSheet, StyleKeyword.None);
            }
            else
            {
                styleProperty.SetFilterFunctionList(styleSheet, newFilterList);
            }

            s_StyleChangeList.Clear();
            s_StyleChangeList.Add(FilterConstants.Filter);
            NotifyStyleChanges(s_StyleChangeList, refreshField);

            if (!refreshField)
            {
                m_Inspector.UpdateFieldStatus(elementTarget, styleProperty);
            }
        }

        void OnFilterListChanged(FilterListChangedEvent evt, FilterStyleField filterStyleField)
        {
            ApplyFilterListChange(evt.newFilterList, evt.refreshField, evt.elementTarget);
        }

        void OnFilterFunctionReordered(FilterFunctionReorderedEvent evt, FilterStyleField filterStyleField)
        {
            var filter = filterStyleField.value;
            ApplyFilterListChange(filter, false, filterStyleField);
        }
    }
}
