// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Timeline.Foundation.ViewModel;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.View.Internals
{
    interface ISequenceTreeView
    {
        void SetItems(IList<TreeViewItemData<Track>> items);
        void SetSelection(IEnumerable<int> ids);
        void SetExpanded(int id, bool expanded);

        void Rebuild();
        void Refresh();
    }
}
