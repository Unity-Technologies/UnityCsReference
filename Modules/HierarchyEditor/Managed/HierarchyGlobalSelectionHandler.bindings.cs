// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Hierarchy;
using UnityEngine.Bindings;

namespace Unity.Hierarchy.Editor
{
    [NativeHeader("Modules/HierarchyEditor/HierarchyGlobalSelectionBindings.h")]
    internal sealed partial class HierarchyGlobalSelectionHandler
    {
        [FreeFunction("HierarchyGlobalSelectionBindings::SyncGlobalSelectionFromViewModel", IsThreadSafe = true)]
        static extern bool SyncGlobalSelectionFromViewModelNative(HierarchyViewModel viewModel);

        [FreeFunction("HierarchyGlobalSelectionBindings::SyncViewModelFromGlobalSelection", IsThreadSafe = true)]
        static extern bool SyncViewModelFromGlobalSelectionNative(HierarchyViewModel viewModel);
    }
}
