// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface IPackageListView
    {
        PackageItem GetPackageItem(string packageUniqueId);
        void ScrollToSelection();
        void OnVisualStateChange(IEnumerable<VisualState> visualStates);
        void OnListRebuild(IPage page);
        void OnListUpdate(ListUpdateArgs args);
        void OnFilterTabChanged(PackageFilterTab filterTab);
        void OnSeeAllPackageVersionsChanged(bool value);

        void OnKeyDownShortcut(KeyDownEvent evt);
    }
}
