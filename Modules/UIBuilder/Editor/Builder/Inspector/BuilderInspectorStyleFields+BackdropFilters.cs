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
            foreach (var unmanagedFilter in currentVisualElement.computedStyle.backdropFilter)
                result.Add((FilterFunction)unmanagedFilter);

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
            ApplyBackdropFilterListChange(evt.newFilterList, evt.refreshField, evt.elementTarget);
        }

        void OnBackdropFilterFunctionReordered(FilterFunctionReorderedEvent evt, FilterStyleField filterStyleField)
        {
            var filter = filterStyleField.value;
            ApplyBackdropFilterListChange(filter, false, filterStyleField);
        }
    }
}
