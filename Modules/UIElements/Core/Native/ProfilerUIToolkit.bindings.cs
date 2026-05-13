// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Profiling;
using UnityEngine.Scripting;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// UI Toolkit profiler frame metadata. <see cref="ProfilerUIToolkit.RecordProfilerPanelMetadataForCapture"/> runs from <c>ProfilerUIToolkit::CaptureFrame</c> and forwards to <see cref="ProfilerUIToolkitPanelMetadataCapture.RecordForCapture"/>, which calls <see cref="ProfilerUIToolkit.EmitProfilerPanelMetadata"/> once per panel.
    /// Native snapshots: editor panels from <see cref="UIElementsUtility.GetPanelsIterator"/>, then player panels in <see cref="UIElementsRuntimeUtility.GetSortedPlayerPanels"/> order.
    /// Each call passes one buffer where <c>entityIds[0]</c> is the panel and the next <c>count - 1</c> entries are IPanelComponent entity ids.
    /// </summary>
    [NativeHeader("Modules/UIElements/Core/Native/ProfilerUIToolkit.h")]
    internal static class ProfilerUIToolkit
    {
        internal static readonly Guid kProfilerMetadataGuid = new Guid("a8f3c2d1-5e4b-4a7c-9d2e-1f0a3b6c8d5e");

        // Must match UNITY_PROFILER_UI_TOOLKIT_METADATA_TAG_PANEL_ENTRIES in ProfilerUIToolkit.h.
        internal const int kProfilerUIToolkitMetadataTagPanelEntries = 0;

        [FreeFunction(Name = "ProfilerUIToolkit::EmitProfilerPanelMetadata")]
        internal static extern void EmitProfilerPanelMetadata([NotNull] EntityId[] entityIds, int count);

        [RequiredByNativeCode(Optional = true)]
        internal static void RecordProfilerPanelMetadataForCapture()
        {
            ProfilerUIToolkitPanelMetadataCapture.RecordForCapture();
        }
    }

    /// <summary>
    /// Implementation for <see cref="ProfilerUIToolkit.RecordProfilerPanelMetadataForCapture"/>. Kept separate so the native entry point does not reference types that pull in blacklisted UI controls via static analysis.
    /// </summary>
    internal static class ProfilerUIToolkitPanelMetadataCapture
    {
        static EntityId[] s_PanelEntityIdsScratch;

        static void EnsurePanelEntityIdsScratchCapacity(int minLength)
        {
            if (s_PanelEntityIdsScratch != null && s_PanelEntityIdsScratch.Length >= minLength)
                return;
            var newSize = minLength;
            if (s_PanelEntityIdsScratch != null && s_PanelEntityIdsScratch.Length > 0)
                newSize = Math.Max(minLength, s_PanelEntityIdsScratch.Length * 2);
            s_PanelEntityIdsScratch = new EntityId[newSize];
        }

        internal static void RecordForCapture()
        {
            if (!Profiler.enabled)
                return;

            EnsurePanelEntityIdsScratchCapacity(1);
            var iterator = UIElementsUtility.GetPanelsIterator();
            while (iterator.MoveNext())
            {
                var panel = iterator.Current.Value;
                if (panel.contextType != ContextType.Editor)
                    continue;
                var pid = panel.ownerObject != null ? panel.ownerObject.GetEntityId() : EntityId.None;
                s_PanelEntityIdsScratch[0] = pid;
                ProfilerUIToolkit.EmitProfilerPanelMetadata(s_PanelEntityIdsScratch, 1);
            }

            // GetSortedPlayerPanels returns UIElementsRuntimeUtility's cached list (not a new collection per call).
            // Indexing avoids any doubt about IEnumerable enumerator boxing; List<T>.Enumerator is a struct anyway.
            var playerPanels = UIElementsRuntimeUtility.GetSortedPlayerPanels();
            for (var i = 0; i < playerPanels.Count; i++)
                RecordProfilerMetadataForPlayerPanel(playerPanels[i]);
        }

        static void RecordProfilerMetadataForPlayerPanel(BaseRuntimePanel panel)
        {
            var pid = panel.ownerObject != null ? panel.ownerObject.GetEntityId() : EntityId.None;
            if (panel is RuntimePanel runtimePanel && runtimePanel.panelComponents.Count > 0)
            {
                var n = runtimePanel.panelComponents.Count;
                var count = 1 + n;
                EnsurePanelEntityIdsScratchCapacity(count);
                s_PanelEntityIdsScratch[0] = pid;
                for (var i = 0; i < n; i++)
                {
                    var comp = runtimePanel.panelComponents[i];
                    s_PanelEntityIdsScratch[1 + i] = (comp as Object) != null ? ((Object)comp).GetEntityId() : EntityId.None;
                }

                ProfilerUIToolkit.EmitProfilerPanelMetadata(s_PanelEntityIdsScratch, count);
            }
            else
            {
                EnsurePanelEntityIdsScratchCapacity(1);
                s_PanelEntityIdsScratch[0] = pid;
                ProfilerUIToolkit.EmitProfilerPanelMetadata(s_PanelEntityIdsScratch, 1);
            }
        }
    }
}
