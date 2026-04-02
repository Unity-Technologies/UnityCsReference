// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Timeline.Foundation.Common;
using Unity.Timeline.Foundation.ViewModel;
using Unity.Timeline.Foundation.Widgets;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.View
{
    /// <summary>
    /// Use this class to create a custom extension displayed under a Track or under a TrackHeader.
    /// </summary>
    abstract class TrackExpansion : SequenceElement<ISequenceViewModel>
    {
        public virtual void OnCanvasTransformChanged(CanvasTransform canvasTransform) { }
        public virtual void OnTrackContentsChanged() { }
        public virtual void OnTrackMetadataChanged(ITrackMetadata metadata) { }
        public virtual void OnItemContentsChanged() { }
        public virtual void SetDisplay(bool display)
        {
            style.display = display ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
