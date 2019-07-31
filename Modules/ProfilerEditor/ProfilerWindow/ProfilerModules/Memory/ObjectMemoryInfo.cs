// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;

namespace UnityEditorInternal
{
    [System.Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public sealed class ObjectMemoryInfo
    {
        public int instanceId;
        public long memorySize;
        public int count;
        public int reason;
        public string name;
        public string className;
    }
}
