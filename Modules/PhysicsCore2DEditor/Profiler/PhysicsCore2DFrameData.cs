// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.U2D.PhysicsCore2D.Profiler
{
    // The per-world blob read back from the profiler frame metadata. Nothing serializes this, the profiler
    // reads a tag's blob by reinterpreting its raw bytes, so the layout is what matters and the field order
    // must stay in step with the native PhysicsCore2DFrameData that emits it.
    // The world's name is a slice of the separate name blob the same frame carries, captured as the name
    // stood at the time rather than looked up now, since the world may since have been renamed or destroyed.
    // The layout is not versioned: the profiler divides the blob's byte count by this struct's size, so a
    // capture written by a build with a different row layout is misread rather than detected.
    struct PhysicsCore2DFrameData
    {
        public Unity.U2D.Physics.PhysicsWorld.WorldCounters worldCounters;
        public Unity.U2D.Physics.PhysicsWorld.WorldProfile worldProfile;
        public uint worldNameOffset;
        public uint worldNameLength;
    }
}
