// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.AI
{
    // Keep this enum in sync with the one defined in "NavMeshBindingTypes.h"
    [Flags]
    public enum NavMeshBuildDebugFlags
    {
        None = 0,
        InputGeometry = 1 << 0,
        Voxels = 1 << 1,
        Regions = 1 << 2,
        RawContours = 1 << 3,
        SimplifiedContours = 1 << 4,
        PolygonMeshes = 1 << 5,
        PolygonMeshesDetail = 1 << 6,
        All = unchecked((int)(~(~0U << 7)))
    }

    // Keep this enum in sync with the one defined in "NavMeshBindingTypes.h"
    public enum NavMeshBuildSourceShape
    {
        Mesh = 0,
        Terrain = 1,
        Box = 2,
        Sphere = 3,
        Capsule = 4,
        ModifierBox = 5
    }

    // Keep this enum in sync with the one defined in "NavMeshBindingTypes.h"
    public enum NavMeshCollectGeometry
    {
        RenderMeshes = 0,
        PhysicsColliders = 1
    }

    // Struct containing source geometry data and annotation for runtime navmesh building
    [UsedByNativeCode]
    [NativeHeader("Modules/AI/Public/NavMeshBindingTypes.h")]
    public struct NavMeshBuildSource
    {
        public Matrix4x4 transform { get { return m_Transform; } set { m_Transform = value; } }
        public Vector3 size { get { return m_Size; } set { m_Size = value; } }
        public NavMeshBuildSourceShape shape { get { return m_Shape; } set { m_Shape = value; } }
        public int area { get { return m_Area; } set { m_Area = value; } }
        public Object sourceObject { get { return InternalGetObject(m_InstanceID); } set { m_InstanceID = value != null ? value.GetInstanceID() : 0; } }
        public Component component { get { return InternalGetComponent(m_ComponentID); } set { m_ComponentID = value != null ? value.GetInstanceID() : 0; } }

        private Matrix4x4 m_Transform;
        private Vector3 m_Size;
        private NavMeshBuildSourceShape m_Shape;
        private int m_Area;
        private int m_InstanceID;
        private int m_ComponentID;

        [StaticAccessor("NavMeshBuildSource", StaticAccessorType.DoubleColon)]
        private static extern Component InternalGetComponent(int instanceID);

        [StaticAccessor("NavMeshBuildSource", StaticAccessorType.DoubleColon)]
        private static extern Object InternalGetObject(int instanceID);
    }

    // Struct containing source geometry data and annotation for runtime navmesh building
    [NativeHeader("Modules/AI/Public/NavMeshBindingTypes.h")]
    public struct NavMeshBuildMarkup
    {
        public bool overrideArea { get { return m_OverrideArea != 0; } set { m_OverrideArea = value ? 1 : 0; } }
        public int area { get { return m_Area; } set { m_Area = value; } }
        public bool ignoreFromBuild { get { return m_IgnoreFromBuild != 0; } set { m_IgnoreFromBuild = value ? 1 : 0; } }
        public Transform root { get { return InternalGetRootGO(m_InstanceID); } set { m_InstanceID = value != null ? value.GetInstanceID() : 0; } }

        private int m_OverrideArea;
        private int m_Area;
        private int m_IgnoreFromBuild;
        private int m_InstanceID;

        [StaticAccessor("NavMeshBuildMarkup", StaticAccessorType.DoubleColon)]
        private static extern Transform InternalGetRootGO(int instanceID);
    }
}
