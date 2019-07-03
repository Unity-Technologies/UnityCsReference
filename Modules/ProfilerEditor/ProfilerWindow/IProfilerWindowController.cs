// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Profiling;
using UnityEditor;
using UnityEditor.Profiling;

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
        event ProfilerWindow.SelectionChangedCallback selectionChanged;

        void SetSelectedPropertyPath(string path);
        void ClearSelectedPropertyPath();

        void SetClearOnPlay(bool enabled);
        bool GetClearOnPlay();

        HierarchyFrameDataView GetFrameDataView(string threadName, HierarchyFrameDataView.ViewModes viewMode, int profilerSortColumn, bool sortAscending);
        int GetActiveVisibleFrameIndex();
        bool IsRecording();
        void Repaint();

        string ConnectedTargetName { get; }
        bool ConnectedToEditor { get; }

        ProfilerProperty CreateProperty();
        ProfilerProperty CreateProperty(int sortType);
    }
}
