// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Timeline.Foundation.Common;
using Unity.Timeline.Foundation.Model;
using UnityEngine.Bindings;

namespace Unity.Timeline.Foundation.ViewModel
{
    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal interface IManipulationHandler : IContentHandler
    {
        void Begin();
        void Manipulate(Track track);
        void Commit();
        void Cancel();

        bool SupportsMoveToTrack(IItemContent content);
        bool CanMoveToTrack(IItemContent content, Track destination);
    }
}
