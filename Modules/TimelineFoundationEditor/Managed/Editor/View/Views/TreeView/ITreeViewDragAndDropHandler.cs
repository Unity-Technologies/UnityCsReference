// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Timeline.Foundation.ViewModel;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.View
{
    interface ITreeViewDragAndDropHandler
    {
        bool CanStartDrag(CanStartDragArgs args, List<Track> candidateTracks);
        StartDragArgs SetupDragAndDrop(SetupDragAndDropArgs args, List<Track> draggedTracks);
        DragVisualMode DragAndDropUpdate(HandleDragAndDropArgs args, Stack newParent);
        DragVisualMode HandleDrop(HandleDragAndDropArgs args, Stack newParent);
    }
}
