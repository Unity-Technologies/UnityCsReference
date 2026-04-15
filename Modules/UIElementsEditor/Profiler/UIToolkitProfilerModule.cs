// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

using System;
using Unity.Profiling;
using Unity.Profiling.Editor;
namespace UnityEditor.UIElements
{
    [Serializable]
    [ProfilerModuleMetadata("UI Toolkit", IconPath = "Profiler.UIToolkit", Tooltip = "Shows how much time UI Toolkit spends on picking, runtime bindings, style resolution, layout, animation, preparing render data, and drawing panels.")]
    internal class UIToolkitProfilerModule : ProfilerModule
    {
        const int k_DefaultOrderIndex = 9; // before UI (Canvas)
        private protected override int defaultOrderIndex => k_DefaultOrderIndex;
        // Specify a list of Profiler category names, which should be auto-enabled when the module is active.
        static readonly string[] k_AutoEnabledCategoryNames = new string[]
        {
            ProfilerCategory.UIToolkit.Name,
            ProfilerCategory.Render.Name,
        };

        // Core UI Toolkit markers (manual: UI Toolkit profiler markers — Core markers), excluding ImmediateRepaint
        static readonly ProfilerCounterDescriptor[] k_ChartCounters = new ProfilerCounterDescriptor[]
        {
            new ProfilerCounterDescriptor("Pick All", "Identify the element under the pointer or being hovered.", ProfilerCategory.UIToolkit),
            new ProfilerCounterDescriptor("Runtime Bindings", "Update runtime bindings.", ProfilerCategory.UIToolkit),
            new ProfilerCounterDescriptor("Update Style", "Apply style sheets and compute the resolved style of the element.", ProfilerCategory.UIToolkit),
            new ProfilerCounterDescriptor("Update Layout", "Compute and update the layout position of every element.", ProfilerCategory.UIToolkit),
            new ProfilerCounterDescriptor("Update Animation", "Update the animations.", ProfilerCategory.UIToolkit),
            new ProfilerCounterDescriptor("Prepare Render", "Prepare graphics data for rendering the panel.", ProfilerCategory.UIToolkit),
            new ProfilerCounterDescriptor("Render Panels", "Render the panel.", ProfilerCategory.UIToolkit),
        };

        /// <summary>
        /// Exact profiler hierarchy marker names for <see cref="k_ChartCounters"/> (same order). Must stay in sync with native ProfilerUIToolkit.cpp kUpdaterMarkerNames.
        /// </summary>
        internal static readonly string[] k_HierarchyMarkerNames = new string[]
        {
            "UIElements.PickAll",
            "UIElements.UpdateRuntimeBindings",
            "UIElements.UpdateStyle",
            "UIElements.UpdateLayout",
            "UIElements.UpdateAnimation",
            "UIElements.UpdateRenderData",
            "UIR.DrawChain",
        };

        /// <summary>
        /// Short column titles for the details list (same order as <see cref="k_ChartCounters"/>).
        /// </summary>
        internal static readonly string[] k_ColumnDisplayTitles = new string[]
        {
            L10n.Tr("Pick All"),
            L10n.Tr("Runtime Bindings"),
            L10n.Tr("Update Style"),
            L10n.Tr("Update Layout"),
            L10n.Tr("Update Animation"),
            L10n.Tr("Prepare Render"),
            L10n.Tr("Render Panels"),
        };

        public UIToolkitProfilerModule() : base(k_ChartCounters, ProfilerModuleChartType.StackedTimeArea, autoEnabledCategoryNames: k_AutoEnabledCategoryNames) { }

        public override ProfilerModuleViewController CreateDetailsViewController()
        {
            return new UIToolkitProfilerModuleDetailsView(ProfilerWindow, k_ChartCounters, k_HierarchyMarkerNames, k_ColumnDisplayTitles);
        }
    }
}
