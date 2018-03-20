// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace UnityEngine.AI
{
    // Keep this struct in sync with the one defined in "NavMeshBuildSettings.h"
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/AI/Public/NavMeshBuildSettings.h")]
    public struct NavMeshBuildSettings
    {
        public int agentTypeID { get { return m_AgentTypeID; } set { m_AgentTypeID = value; } }
        public float agentRadius { get { return m_AgentRadius; } set { m_AgentRadius = value; } }
        public float agentHeight { get { return m_AgentHeight; } set { m_AgentHeight = value; } }
        public float agentSlope { get { return m_AgentSlope; } set { m_AgentSlope = value; } }
        public float agentClimb { get { return m_AgentClimb; } set { m_AgentClimb = value; } }
        public float minRegionArea { get { return m_MinRegionArea; } set { m_MinRegionArea = value; } }
        public bool overrideVoxelSize { get { return m_OverrideVoxelSize != 0; } set { m_OverrideVoxelSize = value ? 1 : 0; } }
        public float voxelSize { get { return m_VoxelSize; } set { m_VoxelSize = value; } }
        public bool overrideTileSize { get { return m_OverrideTileSize != 0; } set { m_OverrideTileSize = value ? 1 : 0; } }
        public int tileSize { get { return m_TileSize; } set { m_TileSize = value; } }
        public NavMeshBuildDebugSettings debug { get { return m_Debug; } set { m_Debug = value; } }

        private int m_AgentTypeID;
        private float m_AgentRadius;
        private float m_AgentHeight;
        private float m_AgentSlope;
        private float m_AgentClimb;
        private float m_LedgeDropHeight;        // Not exposed
        private float m_MaxJumpAcrossDistance;  // Not exposed
        private float m_MinRegionArea;
        private int m_OverrideVoxelSize;
        private float m_VoxelSize;
        private int m_OverrideTileSize;
        private int m_TileSize;
        private int m_AccuratePlacement;        // Not exposed
        private NavMeshBuildDebugSettings m_Debug;

        public String[] ValidationReport(Bounds buildBounds)
        {
            return InternalValidationReport(this, buildBounds);
        }

        [FreeFunction]
        [NativeHeader("Modules/AI/Public/NavMeshBuildSettings.h")]
        private static extern String[] InternalValidationReport(NavMeshBuildSettings buildSettings, Bounds buildBounds);

        // Consider exposing a "Validate" method to modify the BuildSettings in-place
    }

    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/AI/Public/NavMeshBuildDebugSettings.h")]
    public struct NavMeshBuildDebugSettings
    {
        public NavMeshBuildDebugFlags flags { get { return (NavMeshBuildDebugFlags)m_Flags; } set { m_Flags = (byte)value; } }

        private byte m_Flags;
    }
}
