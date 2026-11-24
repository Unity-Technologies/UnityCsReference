// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Timeline.Foundation.Common;
using Unity.Timeline.Foundation.ViewModel;

namespace Unity.Timeline.Foundation.View.Internals
{
    class ItemElementBuilder : ElementBuilder<ItemElementContext, IItemContent, ItemElement>
    {
        protected override IItemContent GetKey(ItemElementContext context)
        {
            return context.item.GetGenericContent();
        }

        protected override void PostBuildElement(ItemElementContext context, ItemElement element)
        {
            element.Initialize(context.viewModel);
            SelectionData selectionData = context.viewModel.selectionData;
            if (element is ISelectableElement selectable)
            {
                bool selected = selectionData.selection.Contains(selectable.ID);
                selectable.OnSelectionStateChanged(selected);
            }
        }
    }
}
