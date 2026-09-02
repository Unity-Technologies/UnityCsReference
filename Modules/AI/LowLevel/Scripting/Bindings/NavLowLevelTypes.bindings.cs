// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.AI.Navigation.LowLevel;

// Keep in sync with the values in NavMeshTypes.h
///<summary>Bit flags representing the resulting state of NavWorld operations.</summary>
///<remarks>The main values are <c>Success</c>, <c>Failure</c> and <c>InProgress</c>. A status usually has only one of these main flags set. Unity sets the secondary flags (details) when it encounters specific issues during the operation. Apply <c>StatusDetailMask</c> as a bitmask to retain only the active secondary flags.</remarks>
///<example><code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/LowLevelQueries/LowLevel/CodeExamples/NavQueryStatusDetailMaskExample.cs}]]></code></example>
[Flags]
public enum NavQueryStatus
{
    // High level status.
    ///<summary>The NavWorld operation didn't complete successfully and produced no usable result.</summary>
    ///<remarks>An invalid <see cref="NavNode" /> most often causes the failure. The node was important for the operation either because it was given as an input parameter, or because the operation stored it inside a <see cref="NavQueryBuffer" /> at a time when the node was still valid. In these cases the <c>NavQueryStatus</c> value has the <see cref="NavQueryStatus.InvalidParameter" /> flag set.
    ///
    ///Another cause for an operation to report a <c>Failure</c> status is a <c>NavQueryBuffer</c> object used as input although it isn't in the correct state that the operation expects it to be. As an example, a call to <see cref="NavWorld.ContinueFindPath(NavQueryBuffer, int, out int)" /> needs as input parameter a <c>NavQueryBuffer</c> that has been initialized correctly in a call to <see cref="NavWorld.BeginFindPath(NavQueryBuffer, NavLocation, NavLocation, int, Unity.Collections.NativeArray{float})" />, otherwise the pathfinding operation can't continue.</remarks>
    Failure = 1 << 31,

    ///<summary>The NavWorld operation completed successfully and produced a valid result for the caller to consume.</summary>
    ///<remarks>Check the status for secondary flags that might provide more details about the result. Apply <see cref="NavQueryStatus.StatusDetailMask" /> as a bitmask to keep only those flags.</remarks>
    Success = 1 << 30,

    ///<summary>The NavWorld operation has started but hasn't yet finished and requires additional calls to advance.</summary>
    ///<remarks>Call <see cref="NavWorld.ContinueFindPath(NavQueryBuffer, int, out int)" /> repeatedly while the operation reports this status, until it returns a different one. The search must have been started by a call to <see cref="NavWorld.BeginFindPath(NavQueryBuffer, NavLocation, NavLocation, int, Unity.Collections.NativeArray{float})" />.</remarks>
    InProgress = 1 << 29,

    // Detail information for status.
    ///<summary>Bitmask that has 0 set for the Success, Failure and InProgress bits and 1 set for all the other flags.</summary>
    ///<remarks>Use it to separate the detail flags from the main status flags.
    ///
    ///Use its bitwise complement to retain only the main status flags, which are mutually exclusive.</remarks>
    StatusDetailMask = 0x0ffffff,

    ///<summary>A parameter didn't contain valid information, useful for carrying out the NavMesh query.</summary>
    ///<remarks>This is a detail flag that accompanies a main status. Use <see cref="NavWorld.IsValid()" /> to check a NavWorld, a <see cref="NavNode" />, or a <see cref="NavLocation" /> before you pass it to an operation.</remarks>
    InvalidParameter = 1 << 3, // An input parameter was invalid.

    ///<summary>The output buffer provided to the query was too small to hold all the results.</summary>
    ///<remarks>Provide a larger output buffer to the operation to solve the issue. <see cref="NavWorld.GetEdgesAndNeighbors(NavNode, Unity.Collections.NativeSlice{UnityEngine.Vector3}, Unity.Collections.NativeSlice{NavNode}, Unity.Collections.NativeSlice{byte}, out int, out int)" /> and the <see cref="NavWorld.Raycast(out UnityEngine.AI.NavMeshHit, Unity.Collections.NativeSlice{NavNode}, out int, NavLocation, UnityEngine.Vector3, int, Unity.Collections.NativeArray{float})" /> overload that fills a path buffer both report this flag.</remarks>
    MoreDataAvailable = 1 << 4, // Result buffer for the query was too small to store all results.

    ///<summary>Query ran out of node stack space during a search.</summary>
    ///<remarks>This happens when the query has visited more nodes than there is room in the <see cref="NavQueryBuffer" />. To fix this issue, use a larger value for the <c>maxNodesToVisit</c> parameter when you create the NavQueryBuffer. <see cref="NavWorld.ContinueFindPath(NavQueryBuffer, int, out int)" /> reports this flag alongside its main status.</remarks>
    MaxNodesToVisitExceeded = 1 << 5, // Query ran out of nodes during search.

    ///<summary>Query didn't reach the end location, returning best guess.</summary>
    ///<remarks>Unity sets this flag on the status that <see cref="NavWorld.EndFindPath(NavQueryBuffer, out int)" /> returns when the search reached the closest position it could find instead of the requested end location. The resulting path is still usable.</remarks>
    PartialResult = 1 << 6 // Query did not reach the end location, returning best guess.
}

// Flags describing node properties. Keep in sync with the enum declared in NavMesh.h
///<summary>The types of nodes in the navigation data.</summary>
///<remarks>Geometrically, navigation data is made up of polygons and segments connected together. Polygon nodes describe the walkable surfaces that an agent moves across, while link nodes describe the point-to-point connections between two positions on those surfaces. Use <see cref="NavWorld.GetNodeType" /> to determine which of the two a given <see cref="NavNode" /> represents.</remarks>
///<seealso cref="NavWorld.GetNodeType" />
///<seealso cref="UnityEngine.AI.NavMeshData" />
///<seealso cref="UnityEngine.AI.NavMeshLinkData" />
///<example><code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/LowLevelQueries/LowLevel/CodeExamples/GetNodeTypeExample.cs}]]></code></example>
public enum NavNodeType
{
    ///<summary>Represents a node that has no type, because the NavNode is null.</summary>
    ///<remarks><see cref="NavWorld.GetNodeType" /> returns this value for a node that <see cref="NavNode.IsNull">is null</see>, which happens when the <see cref="NavNode" /> was never obtained from a NavWorld operation.</remarks>
    Undefined = -1,

    ///<summary>Represents a node in the NavMesh that is a single surface polygon.</summary>
    Polygon = 0, // Regular ground polygons.

    ///<summary>Represents a node in the NavMesh that is a point-to-point connection between two positions on the NavMesh surface.</summary>
    Link = 1 // Off-mesh connections.
}
