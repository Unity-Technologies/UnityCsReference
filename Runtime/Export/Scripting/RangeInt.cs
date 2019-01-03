// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine
{
    public struct RangeInt
    {
        public int start;
        public int length;

        public int end { get { return start + length; } }

        public RangeInt(int start, int length)
        {
            this.start = start;
            this.length = length;
        }
    }
}
