// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface IItemListView
    {
        VisualElement element { get; }
        IListItem GetListItem(string itemUniqueId);
        void ScrollToSelection();
        void OnVisualStateChange(IReadOnlyCollection<VisualState> visualStates);
        void OnListRebuild(IPage page);
        void OnListUpdate(ListUpdateArgs args);
    }
}
