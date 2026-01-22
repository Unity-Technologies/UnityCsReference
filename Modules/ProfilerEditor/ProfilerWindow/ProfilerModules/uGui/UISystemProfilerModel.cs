// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditorInternal;

namespace UnityEditor
{
    internal class UISystemProfilerModel
    {
        public bool ShowEvents;
        public Color EventsColor;
        public EventMarker[] Events;
        public string[] MarkerNames;
        public int DomainSize;
        public int DomainOffset;
    }
}
