// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace Unity.Timeline.Foundation.ViewModel
{
    //view has access to this, for reading, and 'writing' via Dispatch()
    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal interface ISequenceViewModel : IViewModel
    {
        PlayerData playerData { get; }
        SequenceData sequenceData { get; }
        ViewData viewData { get; }
        SelectionData selectionData { get; }
        TimeData timeData { get; }
    }
}
