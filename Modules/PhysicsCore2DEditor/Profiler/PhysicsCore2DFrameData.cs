// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.U2D.PhysicsCore2D.Profiler
{
    // The per-world blob read back from the profiler frame metadata.
    // The field order must stay in step with the native PhysicsCore2DFrameData that emits it.
    [Serializable]
    struct PhysicsCore2DFrameData
    {
        public Unity.U2D.Physics.PhysicsWorld.WorldCounters worldCounters;
        public Unity.U2D.Physics.PhysicsWorld.WorldProfile worldProfile;
        public int worldIndex;
    }
}
