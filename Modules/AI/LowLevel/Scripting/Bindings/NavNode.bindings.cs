// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Unity.AI.Navigation.LowLevel;

///<summary>Represents a compact identifier for the data of a navigation graph node.</summary>
///<remarks>It is used in <see cref="NavWorld" /> operations for pinpointing and accessing relevant nodes in the NavMesh or NavMesh Links. Each node can be used by only one type of agent.
///
///This identifier becomes invalid once the node is removed from the NavMesh, either by completely removing the surface or by modifying the surface in the node's immediate vicinity.
///
///For more information about the surfaces that nodes describe, refer to <see href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/NavInnerWorkings.html#walkable-areas">Walkable Areas</see>.</remarks>
///<seealso cref="NavWorld.IsValid(NavNode)" />
///<example><code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/LowLevelQueries/LowLevel/CodeExamples/NavNodeExample.cs}]]></code></example>
public struct NavNode : IEquatable<NavNode>
{
    internal ulong m_PolyRef;

    ///<summary>Determines whether two NavNode objects refer to the same NavMesh node.</summary>
    ///<remarks>This operator is a syntactic shortcut equivalent to calling <see cref="NavNode.Equals(NavNode)" />. Use it inside expressions where the <c>==</c> syntax is more concise than an explicit method call. Two null <see cref="NavNode" /> values are considered equal.</remarks>
    ///<param name="left">The <see cref="NavNode" /> on the left side of the operator.</param>
    ///<param name="right">The NavNode on the right side of the operator.</param>
    ///<returns><c>true</c> if the two <see cref="NavNode" /> values share the same identifier or are both null, <c>false</c> otherwise.</returns>
    ///<seealso cref="NavNode.IsNull" />
    ///<example><code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/LowLevelQueries/LowLevel/CodeExamples/NavNodeEqualOperatorExample.cs}]]></code></example>
    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    public static bool operator ==(NavNode left, NavNode right) { return left.m_PolyRef == right.m_PolyRef; }

    ///<summary>Determines whether two NavNode objects refer to different NavMesh nodes.</summary>
    ///<remarks>This operator is a syntactic shortcut equivalent to negating the result of <see cref="NavNode.Equals(NavNode)" />. Use it inside expressions where the <c>!=</c> syntax is more concise than an explicit method call. A null <see cref="NavNode" /> is considered different from any non-null one.</remarks>
    ///<param name="left">The <see cref="NavNode" /> on the left side of the operator.</param>
    ///<param name="right">The NavNode on the right side of the operator.</param>
    ///<returns><c>true</c> if the two <see cref="NavNode" /> values reference different nodes or differ in null state, <c>false</c> otherwise.</returns>
    ///<seealso cref="NavNode.IsNull" />
    ///<example><code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/LowLevelQueries/LowLevel/CodeExamples/NavNodeNotEqualOperatorExample.cs}]]></code></example>
    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    public static bool operator !=(NavNode left, NavNode right) { return left.m_PolyRef != right.m_PolyRef; }

    ///<summary>Determines whether this NavNode object refers to the same navigation node as another.</summary>
    ///<remarks>The comparison is performed on the internal identifier of the node. Two <see cref="NavNode" /> values are equal even if they refer to a node that has since been removed from the NavMesh, because the identifier itself doesn't change. Use <see cref="NavWorld.IsValid(NavNode)" /> to determine whether the node is still active in the NavMesh world.</remarks>
    ///<param name="other">A NavNode to compare this one with.</param>
    ///<returns><c>true</c> if two <see cref="NavNode" /> objects refer to the same NavMesh node, and <c>false</c> otherwise.</returns>
    ///<example><code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/LowLevelQueries/LowLevel/CodeExamples/NavNodeEqualsExample.cs}]]></code></example>
    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    public readonly bool Equals(NavNode other) { return m_PolyRef == other.m_PolyRef; }

    ///<summary>Determines whether this NavNode object refers to the same navigation node as another.</summary>
    ///<remarks>The comparison is performed on the internal identifier of the node. Two <see cref="NavNode" /> values are equal even if they refer to a node that has since been removed from the NavMesh, because the identifier itself doesn't change. Use <see cref="NavWorld.IsValid(NavNode)" /> to determine whether the node is still active in the NavMesh world.</remarks>
    ///<param name="obj">An object to interpret as a NavNode and to compare this one with.</param>
    ///<returns><c>true</c> if two <see cref="NavNode" /> objects refer to the same NavMesh node, and <c>false</c> otherwise.</returns>
    ///<example><code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/LowLevelQueries/LowLevel/CodeExamples/NavNodeEqualsExample.cs}]]></code></example>
    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    public readonly override bool Equals(object obj)
    {
        return obj is NavNode other && m_PolyRef == other.m_PolyRef;
    }

    ///<summary>Obtains the hash code of this NavNode for use in collections.</summary>
    ///<remarks>The returned value is derived from the internal identifier of the <see cref="NavNode" /> and is suitable for storing nodes in hash-based collections such as Dictionary or HashSet. Two <see cref="NavNode" /> values that compare equal through <see cref="NavNode.Equals(NavNode)" /> also produce the same hash code.</remarks>
    ///<returns>The hash code derived from the node identifier, suitable for use in hash-based collections.</returns>
    ///<example><code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/LowLevelQueries/LowLevel/CodeExamples/NavNodeHashExample.cs}]]></code></example>
    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    public readonly override int GetHashCode() { return m_PolyRef.GetHashCode(); }

    ///<summary>Determines whether this NavNode is empty of any information.</summary>
    ///<remarks>A null <see cref="NavNode" /> is the default value of the struct and represents the absence of a reference to any node. This is distinct from a <see cref="NavNode" /> that once referenced a real node that has since been removed; in that case <see cref="NavNode.IsNull" /> still returns false. Use <see cref="NavWorld.IsValid(NavNode)" /> to check whether a non-null node is currently active in the NavMesh.</remarks>
    ///<returns><c>true</c> if the <see cref="NavNode" /> has been created empty and has never pointed to any node in the NavMesh, or <c>false</c> otherwise.</returns>
    ///<example><code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/LowLevelQueries/LowLevel/CodeExamples/NavNodeIsNullExample.cs}]]></code></example>
    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    public readonly bool IsNull() { return m_PolyRef == 0; }
}
