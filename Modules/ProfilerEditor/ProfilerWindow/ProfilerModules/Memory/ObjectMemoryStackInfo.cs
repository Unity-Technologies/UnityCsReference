// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Scripting;
using System.Runtime.InteropServices;

namespace UnityEditorInternal
{
    [System.Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    public sealed class ObjectMemoryStackInfo
    {
        public bool expanded;
        public bool sorted;
        public int allocated;
        public int ownedAllocated;
        public ObjectMemoryStackInfo[] callerSites;
        public string name;
    }
}
