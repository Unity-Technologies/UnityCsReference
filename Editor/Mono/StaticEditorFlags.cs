// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor
{
    // Static Editor Flags
    [Flags]
    public enum StaticEditorFlags
    {
        // Considered static for lightmapping.
        LightmapStatic        = 1,
        // Considered static for occlusion.
        OccluderStatic       = 2,
        // Considered static for occlusion.
        OccludeeStatic       = 16,
        // Consider for static batching.
        BatchingStatic        = 4,
        // Considered static for navigation.
        NavigationStatic      = 8,
        // Auto-generate OffMeshLink.
        OffMeshLinkGeneration = 32,
        ReflectionProbeStatic = 64
    }
}
