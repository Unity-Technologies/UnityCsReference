// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor
{
    [Serializable]
    internal class WindowUsage
    {
        public string id;
        public bool inPlayMode;
        public int openSamples;
        public int visibleSamples;
        public int focusedSamples;
        public int focusGains;
        public int peakInstances;

        internal int m_LastSampleStamp;
        internal int m_Instances;
        internal bool m_VisibleCounted;
    }
}
