// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditorInternal.Profiling;

namespace UnityEditorInternal
{
    internal interface IProfilerWindowController
    {
        void SetSelectedPropertyPath(string path);
        void ClearSelectedPropertyPath();

        void SetClearOnPlay(bool enabled);
        bool GetClearOnPlay();


        ProfilerProperty GetRootProfilerProperty(ProfilerColumn sortType);
        FrameDataView GetFrameDataView(ProfilerViewType viewType, ProfilerColumn profilerSortColumn, bool sortAscending);
        int GetActiveVisibleFrameIndex();
        bool IsRecording();
        void Repaint();
    }
}
