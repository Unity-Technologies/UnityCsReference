// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.View
{
    /// <summary>
    /// All elements representing tracks must inherit from ITrackElement
    /// </summary>
    interface ITrackElement : ISelectableElement { }

    interface ITrackHeaderElement : ITrackElement, ITrackElementNotification { }

    interface ITrackContentElement : ITrackElement, ITrackElementNotification
    {
        VisualElement GetItemsContainer();
        VisualElement GetMarkersContainer();
    }

    interface ITrackElementNotification
    {
        void OnTrackMetadataChanged() { }
        void OnTrackContentsChanged() { }
        void OnItemContentsChanged() { }
    }
}
