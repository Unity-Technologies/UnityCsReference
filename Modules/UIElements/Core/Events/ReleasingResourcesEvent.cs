// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    // Sent to an element's own callbacks only, while the element is still usable, right before ReleaseResources.
    [EventCategory(EventCategory.ReleaseResources)]
    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    internal class ReleasingResourcesEvent : EventBase<ReleasingResourcesEvent>, IProfilerIgnoredEvent
    {
        static ReleasingResourcesEvent()
        {
            SetCreateFunction(() => new ReleasingResourcesEvent());
        }
    }
}
