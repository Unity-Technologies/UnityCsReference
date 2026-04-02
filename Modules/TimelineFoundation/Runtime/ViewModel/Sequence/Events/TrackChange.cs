// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace Unity.Timeline.Foundation.ViewModel
{
    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal readonly struct TrackChange
    {
        [Flags]
        [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
        internal enum Type
        {
            None = 0,
            Metadata = 1 << 0,
            Content = 1 << 1,
        }

        public readonly Track track;
        public readonly Type type;

        public TrackChange(Track track, Type type)
        {
            this.track = track;
            this.type = type;
        }

        public bool IsChangeOfType(Type checkType)
        {
            return (type & checkType) == checkType;
        }

        public bool MetadataHasChanged()
        {
            return IsChangeOfType(Type.Metadata);
        }

        public bool ContentHasChanged()
        {
            return IsChangeOfType(Type.Content);
        }
    }
}
