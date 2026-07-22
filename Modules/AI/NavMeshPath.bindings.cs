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
    ///<summary>Status of path.</summary>
    [MovedFrom("UnityEngine")]
    public enum NavMeshPathStatus
    {
        ///<summary>The path terminates at the destination.</summary>
        PathComplete = 0,
        ///<summary>The path cannot reach the destination.</summary>
        PathPartial = 1,
        ///<summary>The path is not valid.</summary>
        ///<remarks>Refer to the documentation of the returning method or property for more information. , <see cref="NavMeshPath.status" />.</remarks>
        ///<seealso cref="NavMeshAgent.pathStatus" />
        PathInvalid = 2
    }

    ///<summary>A path as calculated by the navigation system.</summary>
    ///<remarks>The path is represented as a list of waypoints stored in the <see cref="corners" /> array. These points are not set directly from user scripts but a NavMeshPath with points correctly assigned is returned by the <see cref="AI.NavMesh.CalculatePath" /> function and the <see cref="AI.NavMeshAgent.path" /> property.</remarks>
    [NativeHeader("Modules/AI/NavMeshPath.bindings.h")]
    [StructLayout(LayoutKind.Sequential)]
    [MovedFrom("UnityEngine")]
    public sealed class NavMeshPath
    {
        internal IntPtr m_Ptr;
        internal Vector3[] m_Corners;

        ///<summary>NavMeshPath constructor.</summary>
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

        ///<summary>Calculate the corners for the path.</summary>
        ///<remarks>This function is similar to the <see cref="corners" /> property except that the results are returned in the supplied array.
        ///
        ///Note that this function expects the supplied array to have at least 2 elements.</remarks>
        ///<param name="results">Array to store path corners.</param>
        ///<returns>The number of corners along the path - including start and end points.</returns>
        [FreeFunction("NavMeshPathScriptBindings::GetCornersNonAlloc", HasExplicitThis = true)]
        public extern int GetCornersNonAlloc([Out] Vector3[] results);

        [FreeFunction("NavMeshPathScriptBindings::CalculateCornersInternal", HasExplicitThis = true)]
        extern Vector3[] CalculateCornersInternal();

        [FreeFunction("NavMeshPathScriptBindings::ClearCornersInternal", HasExplicitThis = true)]
        extern void ClearCornersInternal();

        ///<summary>Erase all corner points from path.</summary>
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

        ///<summary>Corner points of the path.</summary>
        ///<remarks>Also known as "waypoints", the corners define the places along a path where it changes direction (ie, the path consists of a number of straight-line moves between corners).</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.AI;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour {
        ///    float PathLength(NavMeshPath path) {
        ///        if (path.corners.Length < 2)
        ///            return 0;
        ///
        ///        float lengthSoFar = 0.0F;
        ///        for (int i = 1; i < path.corners.Length; i++) {
        ///            lengthSoFar += Vector3.Distance(path.corners[i - 1], path.corners[i]);
        ///        }
        ///        return lengthSoFar;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public Vector3[] corners { get { CalculateCorners(); return m_Corners; } }

        ///<summary>Status of the path.</summary>
        ///<remarks>This reports whether the path reaches the target, reaches part of the way to the target, or is just not valid. Among other reasons, a path returns <see cref="NavMeshPathStatus.PathInvalid" /> if it can't determine the nearest polygon of the source or target position, or if the path would have been a partial result, but the point closest to the target on the final polygon could not be determined. These situations are rare, and may arise if the navigation mesh is being changed while a path is being calculated.</remarks>
        public extern NavMeshPathStatus status { get; }

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(NavMeshPath navMeshPath) => navMeshPath.m_Ptr;
        }
    }
}
