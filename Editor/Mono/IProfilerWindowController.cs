// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;

namespace UnityEditorInternal
{
    internal interface IProfilerWindowController
    {
        void SetSelectedPropertyPath(string path);
        void ClearSelectedPropertyPath();
        ProfilerProperty GetRootProfilerProperty(ProfilerColumn sortType);
        int GetActiveVisibleFrameIndex();
        void SetSearch(string searchString);
        string GetSearch();
        bool IsSearching();
        void Repaint();
    }
}
