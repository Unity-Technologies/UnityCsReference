// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.Bindings;

namespace Unity.Profiling.Editor
{
    /// <summary>
    /// Implemented by a details view controller that adds its own settings to the CPU module's options menu.
    /// </summary>
    /// <remarks>
    /// A view that exists for only one view type has nowhere to put its settings without placing a second options
    /// menu beside the module's. The menu is rebuilt on each click and the contributor is only consulted while its
    /// view type is selected, so the items are present exactly while that view is.
    /// </remarks>
    [VisibleToOtherModules("UnityEditor.JobsProfilerModule")]
    internal interface IProfilerViewOptionsMenuContributor
    {
        void AddViewOptionsMenuItems(GenericMenu menu);
    }
}
