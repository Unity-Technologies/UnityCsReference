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
        internal static class BackdropFilterConstants
        {
            public static readonly string BackdropFilter = StylePropertyId.BackdropFilter.UssName();
        }

        public void BindBackdropFilterStyleField(BuilderStyleRow styleRow, FilterStyleField filterStyleField)
        {
            filterStyleField.SetContainingRow(styleRow);

            // Custom filters are not supported for backdrop-filter
            filterStyleField.allowCustomFilters = false;

            filterStyleField.SetInspectorStylePropertyName(BackdropFilterConstants.BackdropFilter);
            GetOrCreateFieldListForStyleName(BackdropFilterConstants.BackdropFilter).Add(filterStyleField);

            SetUpContextualMenuOnStyleField(filterStyleField);

            filterStyleField.RegisterCallback<FilterListChangedEvent, FilterStyleField>(OnBackdropFilterListChanged, filterStyleField);
            filterStyleField.RegisterCallback<FilterFunctionReorderedEvent, FilterStyleField>(OnBackdropFilterFunctionReordered, filterStyleField);
        }

        public void RefreshBackdropFilterStyleField(FilterStyleField filterStyleField)
        {
            // Cancel any running animation so we get the current filter count when querying the computed style.
            if (currentVisualElement.HasRunningAnimation(StylePropertyId.BackdropFilter))
                currentVisualElement.CancelAnimation(StylePropertyId.BackdropFilter);

            var result = new List<FilterFunction>();
            bool hasCustomFilter = false;

            foreach (var unmanagedFilter in currentVisualElement.computedStyle.backdropFilter)
            {
                var f = (FilterFunction)unmanagedFilter;

                // Skip custom filters - they are not supported for backdrop-filter
                if (f.type == FilterFunctionType.Custom)
                {
                    hasCustomFilter = true;
                    continue;
                }
                result.Add(f);
            }

            if (hasCustomFilter)
            {
                Debug.LogWarning($"Custom filters found in backdrop-filter for element '{currentVisualElement.name}' have been removed. Custom filters are not supported for backdrop-filter.");
            }

            filterStyleField.SetValueWithoutNotify(result);

            var prop = GetLastStyleProperty(currentRule, BackdropFilterConstants.BackdropFilter);
            m_Inspector.UpdateFieldStatus(filterStyleField, prop);
        }

        void ApplyBackdropFilterListChange(List<FilterFunction> newFilterList, bool refreshField, VisualElement elementTarget)
        {
            ApplyFilterListChangeCore(BackdropFilterConstants.BackdropFilter, newFilterList, refreshField, elementTarget);
        }

        void OnBackdropFilterListChanged(FilterListChangedEvent evt, FilterStyleField filterStyleField)
        {
            // Custom filters are not supported for backdrop-filter - remove them
            var validFilters = new List<FilterFunction>();
            bool hasCustomFilter = false;

            foreach (var filter in evt.newFilterList)
            {
                if (filter.type == FilterFunctionType.Custom)
                {
                    hasCustomFilter = true;
                    continue; // Skip custom filters
                }
                validFilters.Add(filter);
            }

            if (hasCustomFilter)
            {
                Debug.LogWarning("Custom filters are not supported for backdrop-filter and have been removed.");
            }

            ApplyBackdropFilterListChange(validFilters, evt.refreshField, evt.elementTarget);
        }

        void OnBackdropFilterFunctionReordered(FilterFunctionReorderedEvent evt, FilterStyleField filterStyleField)
        {
            var filter = filterStyleField.value;
            ApplyBackdropFilterListChange(filter, false, filterStyleField);
        }
    }
}
