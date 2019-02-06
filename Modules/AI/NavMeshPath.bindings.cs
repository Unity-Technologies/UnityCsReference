// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.AI
{
    // Keep this enum in sync with the one defined in "NavMeshBindingTypes.h"
    // Status of path.
    [MovedFrom("UnityEngine")]
    public enum NavMeshPathStatus
    {
        PathComplete = 0,   // The path terminates at the destination.
        PathPartial = 1,    // The path cannot reach the destination.
        PathInvalid = 2     // The path is invalid.
    }

    // Path navigation.
    [NativeHeader("Modules/AI/NavMeshPath.bindings.h")]
    [StructLayout(LayoutKind.Sequential)]
    [MovedFrom("UnityEngine")]
    public sealed class NavMeshPath
    {
        internal IntPtr m_Ptr;
        internal Vector3[] m_Corners;

        public NavMeshPath()
        {
            m_Ptr = InitializeNavMeshPath();
        }

        ~NavMeshPath()
        {
            DestroyNavMeshPath(m_Ptr);
            m_Ptr = IntPtr.Zero;
        }

        [FreeFunction("NavMeshPathScriptBindings::InitializeNavMeshPath")]
        static extern IntPtr InitializeNavMeshPath();

        [FreeFunction("NavMeshPathScriptBindings::DestroyNavMeshPath", IsThreadSafe = true)]
        static extern void DestroyNavMeshPath(IntPtr ptr);

        [FreeFunction("NavMeshPathScriptBindings::GetCornersNonAlloc", HasExplicitThis = true)]
        public extern int GetCornersNonAlloc([Out] Vector3[] results);

        [FreeFunction("NavMeshPathScriptBindings::CalculateCornersInternal", HasExplicitThis = true)]
        extern Vector3[] CalculateCornersInternal();

        [FreeFunction("NavMeshPathScriptBindings::ClearCornersInternal", HasExplicitThis = true)]
        extern void ClearCornersInternal();

        // Erase all corner points from path.
        public void ClearCorners()
        {
            ClearCornersInternal();
            m_Corners = null;
        }

        void CalculateCorners()
        {
            if (m_Corners == null)
                m_Corners = CalculateCornersInternal();
        }

        // Corner points of path. (RO)
        public Vector3[] corners { get { CalculateCorners(); return m_Corners; } }

        // Status of the path. (RO)
        public extern NavMeshPathStatus status { get; }
    }
}
