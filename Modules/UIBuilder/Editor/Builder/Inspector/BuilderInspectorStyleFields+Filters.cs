// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.UIToolkit.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;
using UnityEngine.UIElements.Unmanaged;

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

            filterStyleField.RegisterCallback<FilterFunctionAddedEvent, FilterStyleField>(OnFilterFunctionAdded, filterStyleField);
            filterStyleField.RegisterCallback<FilterFunctionRemovedEvent, FilterStyleField>(OnFilterFunctionRemoved, filterStyleField);
            filterStyleField.RegisterCallback<FilterFunctionChangedEvent, FilterStyleField>(OnFilterFunctionChanged, filterStyleField);
            filterStyleField.RegisterCallback<FilterFunctionValueChangedEvent, FilterStyleField>(OnFilterFunctionValueChanged, filterStyleField);
            filterStyleField.RegisterCallback<FilterFunctionReorderedEvent, FilterStyleField>(OnFilterFunctionReordered, filterStyleField);
        }

        public void RefreshStyleField(FilterStyleField filterStyleField)
        {
            // It's important to cancel any running animation so that when we query the computed style
            // we get the new number of filters. Otherwise, the first frame of any transition will
            // have the old number of filters before the add/remove of the filter.

            if (currentVisualElement.HasRunningAnimation(StylePropertyId.Filter))
                currentVisualElement.CancelAnimation(StylePropertyId.Filter);

            var result = new List<FilterFunction>();
            foreach (var f in currentVisualElement.computedStyle.filter)
                result.Add(f);
            filterStyleField.SetValueWithoutNotify(result);

            var prop = GetLastStyleProperty(currentRule, FilterConstants.Filter);
            m_Inspector.UpdateFieldStatus(filterStyleField, prop);
        }

        void OnFilterFunctionAdded(FilterFunctionAddedEvent evt, FilterStyleField filterStyleField)
        {
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            var styleProperty = GetOrCreateStylePropertyByStyleName(FilterConstants.Filter);

            AddFilterFunctionToStyleSheet(styleProperty, evt.filterFunction);

            s_StyleChangeList.Clear();
            s_StyleChangeList.Add(FilterConstants.Filter);
            NotifyStyleChanges(s_StyleChangeList, true);
        }

        void OnFilterFunctionRemoved(FilterFunctionRemovedEvent evt, FilterStyleField filterStyleField)
        {
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            var filter = currentVisualElement.computedStyle.filter;

            // Quick sanity check over the indices
            foreach (var index in evt.indices)
            {
                if (index >= filter.Length)
                    return;
            }

            // Since filter functions have a variable number of arguments, it's simpler to
            // remove the entire list of functions and re-add them (and skip the removed functions).
            RemoveFilterFunctionsFromStyleSheet();

            if (filter.Length > evt.indices.Count)
            {
                var styleProperty = GetOrCreateStylePropertyByStyleName(FilterConstants.Filter);
                var manipulator = styleProperty.GetManipulator(styleSheet);

                for (var i = 0; i < filter.Length; ++i)
                {
                    if (evt.indices.Contains(i))
                        continue; // Skip this filter

                    manipulator.AddFilterFunction(filter[i]);
                }
            }

            s_StyleChangeList.Clear();
            s_StyleChangeList.Add(FilterConstants.Filter);
            NotifyStyleChanges(s_StyleChangeList, true);
        }

        void AddFilterFunctionToStyleSheet(StyleProperty styleProperty, FilterFunction filterFunction)
        {
            var manipulator = styleProperty.GetManipulator(styleSheet);
            manipulator.AddFilterFunction(filterFunction);
        }

        void RemoveFilterFunctionsFromStyleSheet()
        {
            var styleProperty = GetOrCreateStylePropertyByStyleName(FilterConstants.Filter);
            styleSheet.RemoveProperty(currentRule, styleProperty);
        }

        void OnFilterFunctionChanged(FilterFunctionChangedEvent evt, FilterStyleField filterStyleField)
        {
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);
            var index = evt.index;
            var filter = currentVisualElement.computedStyle.filter;

            s_StyleChangeList.Clear();

            if (index < filter.Length)
            {
                // Since filter functions have a variable number of arguments, it's simpler to
                // remove the entire list of functions and re-add them.
                RemoveFilterFunctionsFromStyleSheet();
                var styleProperty = GetOrCreateStylePropertyByStyleName(FilterConstants.Filter);
                var manipulator = styleProperty.GetManipulator(styleSheet);

                for (var i = 0; i < filter.Length; ++i)
                {
                    manipulator.AddFilterFunction(i == index ? evt.filterFunction : filter[i]);
                }

                s_StyleChangeList.Add(FilterConstants.Filter);
            }

            NotifyStyleChanges(s_StyleChangeList, true);
        }

        void OnFilterFunctionValueChanged(FilterFunctionValueChangedEvent evt, FilterStyleField filterStyleField)
        {
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            // Skip the filter function rules one-by-one until we reach the one we want to change.
            // We need to do this since the filter function have a variable number of parameters.

            var filter = currentVisualElement.computedStyle.filter;
            if (evt.index >= filter.Length)
                return;

            var styleProperty = GetOrCreateStylePropertyByStyleName(FilterConstants.Filter);
            var manipulator = styleProperty.GetManipulator(styleSheet);
            manipulator.SetFilterFunction(evt.index, evt.filterFunction);

            NotifyStyleChanges(s_StyleChangeList, false);
        }

        void OnFilterFunctionReordered(FilterFunctionReorderedEvent evt, FilterStyleField filterStyleField)
        {
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            var filter = filterStyleField.value;

            var styleProperty = GetOrCreateStylePropertyByStyleName(FilterConstants.Filter);
            styleProperty.SetFilterFunctionList(styleSheet, filter);

            NotifyStyleChanges(s_StyleChangeList, false);
        }
    }
}
