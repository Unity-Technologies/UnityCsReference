// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Timeline.Foundation.ViewModel;

namespace Unity.Timeline.Foundation.View
{
    readonly struct ItemElementContext
    {
        public readonly ISequenceViewModel viewModel;
        public readonly Item item;

        public ItemElementContext(ISequenceViewModel vm, Item item)
        {
            viewModel = vm;
            this.item = item;
        }
    }
}
