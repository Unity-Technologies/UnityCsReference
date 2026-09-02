// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Unity.AI.Navigation.LowLevel;

///<summary>A world position that is guaranteed to be on the navigation data, either on a NavMesh surface or on a link.</summary>
///<remarks>The NavLocation stores that position together with the <see cref="NavNode" /> of the node containing it, which is either a <see cref="NavNodeType.Polygon">polygon</see> of a NavMesh surface or a <see cref="NavNodeType.Link">link</see>. Using NavLocations with <see cref="NavWorld" /> operations removes the need to project the desired world position onto the NavMesh at the beginning of each operation.
///
///A NavLocation can be invalid in the following scenarios:
///
///1. When it has been created empty, instead of being the result of a NavWorld operation.
///2. When a <see cref="NavWorld.MapLocation(Vector3, Vector3, int, int)" /> call finds no polygon that matches the requested position, extents, agent type, or area mask.
///3. When the NavMesh has been removed or modified at the indicated position or in its close vicinity.
///
///If a <see cref="UnityEngine.AI.NavMeshObstacle" /> carving the NavMesh in its vicinity makes a NavLocation invalid, the NavLocation returns to a valid state once all NavMeshObstacle objects stop carving the NavMesh tile where the NavLocation is. This is because removing all NavMeshObstacle objects restores the NavMesh to its original form without regenerating it.</remarks>
///<seealso cref="NavWorld.MapLocation(Vector3, Vector3, int, int)" />
///<seealso cref="NavWorld.IsValid(NavLocation)" />
///<seealso cref="NavNode" />
///<seealso cref="NavNodeType" />
///<example><code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/LowLevelQueries/LowLevel/CodeExamples/NavLocationExample.cs}]]></code></example>
public readonly struct NavLocation : IEquatable<NavLocation>
{
    ///<summary>The unique identifier for the node in the NavMesh to which the world position has been mapped.</summary>
    public NavNode node { get; }
    ///<summary>A world position that sits precisely on the surface of the NavMesh or along its links.</summary>
    ///<remarks>For more information about how the navigation system represents surfaces and links, refer to <see href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/NavInnerWorkings.html">Inner Workings of the Navigation System</see>.</remarks>
    public Vector3 position { get; }

    internal NavLocation(Vector3 position, NavNode node)
    {
        this.position = position;
        this.node = node;
    }

    ///<summary>Checks whether two NavLocation objects have the same position and refer to the same NavMesh node.</summary>
    ///<remarks>This operator is a syntactic shortcut equivalent to calling <see cref="NavLocation.Equals(NavLocation)" />. Use it inside expressions where the <c>==</c> syntax is more concise than an explicit method call. Both the <see cref="NavLocation.position">position</see> and the <see cref="NavLocation.node">node</see> are compared.</remarks>
    ///<param name="left">The <see cref="NavLocation" /> on the left side of the operator.</param>
    ///<param name="right">The <see cref="NavLocation" /> on the right side of the operator.</param>
    ///<returns><c>true</c> if the two <see cref="NavLocation" /> values share the same node and position, <c>false</c> otherwise.</returns>
    ///<example><code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/LowLevelQueries/LowLevel/CodeExamples/NavLocationEqualOperatorExample.cs}]]></code></example>
    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    public static bool operator ==(NavLocation left, NavLocation right)
    {
        return left.node.Equals(right.node) && left.position.Equals(right.position);
    }

    ///<summary>Checks whether two NavLocation objects differ in position or refer to different NavMesh nodes.</summary>
    ///<remarks>This operator is a syntactic shortcut equivalent to negating the result of <see cref="NavLocation.Equals(NavLocation)" />. Use it inside expressions where the <c>!=</c> syntax is more concise than an explicit method call. Any difference in the <see cref="NavLocation.position">position</see> or <see cref="NavLocation.node">node</see> makes the two values unequal.</remarks>
    ///<param name="left">The <see cref="NavLocation" /> on the left side of the operator.</param>
    ///<param name="right">The <see cref="NavLocation" /> on the right side of the operator.</param>
    ///<returns><c>true</c> if the two <see cref="NavLocation" /> values differ in node or position, <c>false</c> otherwise.</returns>
    ///<example><code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/LowLevelQueries/LowLevel/CodeExamples/NavLocationNotEqualOperatorExample.cs}]]></code></example>
    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    public static bool operator !=(NavLocation left, NavLocation right)
    {
        return !left.node.Equals(right.node) || !left.position.Equals(right.position);
    }

    ///<summary>Checks whether two NavLocation objects represent the same position on the same NavMesh node.</summary>
    ///<remarks>The comparison checks the <see cref="NavLocation.node">node</see> identifier and the exact <see cref="NavLocation.position">position</see> coordinates. Two <see cref="NavLocation" /> values that share the same node but have slightly different positions are considered different. Use this method to compare <see cref="NavLocation" /> values instead of relying on reference equality.</remarks>
    ///<param name="other">A NavLocation to compare this one with.</param>
    ///<returns><c>true</c> if both the <see cref="NavLocation.node">node</see> and the <see cref="NavLocation.position">position</see> of the two NavLocation values are equal, and <c>false</c> otherwise.</returns>
    ///<example><code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/LowLevelQueries/LowLevel/CodeExamples/NavLocationEqualsExample.cs}]]></code></example>
    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    public readonly bool Equals(NavLocation other)
    {
        return node.Equals(other.node) && position.Equals(other.position);
    }

    ///<summary>Checks whether two NavLocation objects represent the same position on the same NavMesh node.</summary>
    ///<remarks>The comparison checks the <see cref="NavLocation.node">node</see> identifier and the exact <see cref="NavLocation.position">position</see> coordinates. Two <see cref="NavLocation" /> values that share the same node but have slightly different positions are considered different. Use this method to compare <see cref="NavLocation" /> values instead of relying on reference equality.</remarks>
    ///<param name="obj">An object to interpret as a NavLocation and to compare this one with.</param>
    ///<returns><c>true</c> if both the <see cref="NavLocation.node">node</see> and the <see cref="NavLocation.position">position</see> of the two NavLocation values are equal, and <c>false</c> otherwise.</returns>
    ///<example><code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/LowLevelQueries/LowLevel/CodeExamples/NavLocationEqualsExample.cs}]]></code></example>
    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    public readonly override bool Equals(object obj)
    {
        return obj is NavLocation other && node.Equals(other.node) && position.Equals(other.position);
    }

    ///<summary>Returns the hash code for use in collections.</summary>
    ///<remarks>The returned value combines the <see cref="NavLocation.node">node</see> identifier and the <see cref="NavLocation.position">position</see> of this <see cref="NavLocation" />. The hash code is suitable for storing locations in hash-based collections such as Dictionary or HashSet. Two <see cref="NavLocation" /> values that compare equal through <see cref="NavLocation.Equals(NavLocation)" /> also produce the same hash code.</remarks>
    ///<returns>The hash code representing the node identifier and position of this location.</returns>
    ///<example><code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/LowLevelQueries/LowLevel/CodeExamples/NavLocationHashExample.cs}]]></code></example>
    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    public readonly override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = (hash * 31) + node.GetHashCode();
            hash = (hash * 31) + position.GetHashCode();
            return hash;
        }
    }
}
