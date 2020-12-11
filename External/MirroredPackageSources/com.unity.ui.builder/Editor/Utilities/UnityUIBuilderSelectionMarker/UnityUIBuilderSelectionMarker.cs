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

        protected override void ExecuteDefaultAction(EventBase evt)
        {
            base.ExecuteDefaultAction(evt);

            if (evt.eventTypeId != AttachToPanelEvent.TypeId())
                return;

            RemoveFromHierarchy();
        }
    }
}
