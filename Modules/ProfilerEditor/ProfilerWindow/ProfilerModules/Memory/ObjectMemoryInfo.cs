// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Serialization;

namespace UnityEditorInternal
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public sealed class ObjectMemoryInfo
    {
        [Obsolete("instanceId is deprecated. Use entityId instead.")]
        public int instanceId { get => entityId; set => entityId = value; }
        [FormerlySerializedAs("instanceId")]
        public EntityId entityId;
        public long memorySize;
        public int count;
        public int reason;
        public string name;
        public string className;
    }
}
