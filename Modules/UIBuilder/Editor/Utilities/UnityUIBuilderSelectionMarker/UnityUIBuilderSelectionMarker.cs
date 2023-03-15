// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    // HACK: We need a valid UIElement now that selection state is stored inside the live uxml
    // asset that is potentially used elsewhere. This mock element will immediately remove
    // itself from the hierarchy as soon as it's added.
    // This should be removed once selection is moved to a separate object. See:
    // https://unity3d.atlassian.net/browse/UIT-456
    internal class UnityUIBuilderSelectionMarker : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<UnityUIBuilderSelectionMarker, UxmlTraits> {}

        public UnityUIBuilderSelectionMarker() {}

        [EventInterest(typeof(AttachToPanelEvent))]
        protected override void HandleEventBubbleUp(EventBase evt)
        {
            base.HandleEventBubbleUp(evt);

            if (evt.eventTypeId != AttachToPanelEvent.TypeId())
                return;

            RemoveFromHierarchy();
        }
    }
}
