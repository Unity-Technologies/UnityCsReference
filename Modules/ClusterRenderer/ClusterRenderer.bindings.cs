// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine
{
    [NativeHeader("Modules/ClusterRenderer/ClusterNetwork.h")]
    [Obsolete("This type is deprecated and will be removed in Unity 7.", false)]
    public class ClusterNetwork
    {
        public static extern bool isMasterOfCluster { get; }
        public static extern bool isDisconnected { get; }
        public static extern int nodeIndex { get; set; }
    }
}
