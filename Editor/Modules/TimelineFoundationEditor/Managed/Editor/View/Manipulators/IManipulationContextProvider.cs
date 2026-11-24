// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Timeline.Foundation.ViewModel;

namespace Unity.Timeline.Foundation.View
{
    interface IManipulationContextProvider
    {
        ISequenceViewModel GetViewModel();
        ManipulationContext GetManipulationContext();
        ViewContext GetViewContext();
        IManipulationHandler GetManipulationHandler();
        ItemElement GetElementFor(Item item);
        TrackElement GetElementFor(Track track);
    }
}
