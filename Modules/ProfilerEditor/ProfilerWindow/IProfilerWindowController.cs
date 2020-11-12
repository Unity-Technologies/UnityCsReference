// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal.Profiling;
using UnityEngine.Profiling;

namespace UnityEditorInternal
{
    internal enum ProfilerViewType
    {
        Hierarchy = 0,
        Timeline = 1,
        RawHierarchy = 2
    }

    internal interface IProfilerWindowController
    {
        event ProfilerWindow.FrameChangedCallback currentFrameChanged;
        void SetClearOnPlay(bool enabled);
        bool GetClearOnPlay();

        HierarchyFrameDataView GetFrameDataView(string groupName, string threadName, ulong threadId, HierarchyFrameDataView.ViewModes viewMode, int profilerSortColumn, bool sortAscending);
        HierarchyFrameDataView GetFrameDataView(int threadIndex, HierarchyFrameDataView.ViewModes viewMode, int profilerSortColumn, bool sortAscending);
        int GetActiveVisibleFrameIndex();
        void SetActiveVisibleFrameIndex(int frame);
        bool IsRecording();
        void Repaint();

        string ConnectedTargetName { get; }
        bool ConnectedToEditor { get; }

        ProfilerProperty CreateProperty();
        ProfilerProperty CreateProperty(int sortType);

        ProfilerModuleBase SelectedModule { get; }
        void SelectModule(ProfilerModuleBase module);
        void CloseModule(ProfilerModuleBase module);

        void SetAreasInUse(IEnumerable<ProfilerArea> areas, bool inUse);
    }
}
