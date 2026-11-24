// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

using System;
using Unity.Profiling;
using Unity.Profiling.Editor;
using Unity.Scripting.LifecycleManagement;
namespace UnityEditor.UIElements
{
    [Serializable]
    [ProfilerModuleMetadata("UI Toolkit", IconPath = "Profiler.UIToolkit", Tooltip = "Shows how much time UI Toolkit spends on picking, bindings, style resolution, layout, animation, preparing render data, drawing panels, and updating Panel Renderer components.")]
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

        // Core UI Toolkit markers (manual: UI Toolkit profiler markers — Core markers), excluding ImmediateRepaint, followed by the Panel Renderer markers
        [NoAutoStaticsCleanup]
        internal static readonly ProfilerCounterDescriptor[] k_ChartCounters = new ProfilerCounterDescriptor[]
        {
            new ProfilerCounterDescriptor("Pick All", "Time spent identifying the element under the pointer or being hovered.", ProfilerCategory.UIToolkit),
            new ProfilerCounterDescriptor("Runtime Bindings", "Time spent updating bindings, including Editor bindings.", ProfilerCategory.UIToolkit, L10n.Tr("Bindings", null)),
            new ProfilerCounterDescriptor("Update Style", "Time spent applying style sheets and computing the resolved style of elements.", ProfilerCategory.UIToolkit),
            new ProfilerCounterDescriptor("Update Layout", "Time spent computing and updating the layout of elements.", ProfilerCategory.UIToolkit),
            new ProfilerCounterDescriptor("Update Animation", "Time spent updating animations.", ProfilerCategory.UIToolkit),
            new ProfilerCounterDescriptor("Prepare Render", "Time spent preparing graphics data for rendering the panels.", ProfilerCategory.UIToolkit),
            new ProfilerCounterDescriptor("Render Panels", "Time spent rendering the panels.", ProfilerCategory.UIToolkit),
            new ProfilerCounterDescriptor("Prepare Panel Renderers", "Time spent loading the source assets of Panel Renderer components, invoking their UI reload callbacks, and applying changes to their enabled state, sorting order, and hierarchy.", ProfilerCategory.UIToolkit),
            new ProfilerCounterDescriptor("Update Panel Renderers", "Time spent updating Panel Renderer components after the panels update, including the transform, bounds, and collider of world space panels.", ProfilerCategory.UIToolkit),
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
            "UIElements.PreUpdatePanelRenderers",
            "UIElements.UpdatePanelRenderers",
        };

        /// <summary>
        /// Short column titles for the details list (same order as <see cref="k_ChartCounters"/>).
        /// </summary>
        internal static readonly string[] k_ColumnDisplayTitles = new string[]
        {
            L10n.Tr("Pick All", null),
            L10n.Tr("Bindings", null),
            L10n.Tr("Update Style", null),
            L10n.Tr("Update Layout", null),
            L10n.Tr("Update Animation", null),
            L10n.Tr("Prepare Render", null),
            L10n.Tr("Render Panels", null),
            L10n.Tr("Prepare Panel Renderers", null),
            L10n.Tr("Update Panel Renderers", null),
        };

        public UIToolkitProfilerModule() : base(k_ChartCounters, ProfilerModuleChartType.StackedTimeArea, autoEnabledCategoryNames: k_AutoEnabledCategoryNames) { }

        public override ProfilerModuleViewController CreateDetailsViewController()
        {
            return new UIToolkitProfilerModuleDetailsView(ProfilerWindow, k_ChartCounters, k_HierarchyMarkerNames, k_ColumnDisplayTitles);
        }
    }
}
