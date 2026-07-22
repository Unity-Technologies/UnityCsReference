// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.AI.Navigation.LowLevel;

// Keep in sync with the values in NavMeshTypes.h
///<summary>The state of a navigation query after running an operation.</summary>

[Flags]
public enum NavQueryStatus
{
    // High level status.
    ///<exclude />
    Failure = 1 << 31,
    ///<exclude />
    Success = 1 << 30,
    ///<exclude />
    InProgress = 1 << 29,

    // Detail information for status.
    ///<exclude />
    StatusDetailMask = 0x0ffffff,
    ///<exclude />
    InvalidParameter = 1 << 3, // An input parameter was invalid.
    ///<exclude />
    MoreDataAvailable = 1 << 4, // Result buffer for the query was too small to store all results.
    ///<exclude />
    MaxNodesToVisitExceeded = 1 << 5, // Query ran out of nodes during search.
    ///<exclude />
    PartialResult = 1 << 6 // Query did not reach the end location, returning best guess.
}

// Flags describing node properties. Keep in sync with the enum declared in NavMesh.h
///<summary>Describes whether the navigation node is created by a NavMesh or a NavMeshLink.</summary>

public enum NavNodeType
{
    ///<exclude />
    Undefined = -1,
    ///<exclude />
    Polygon = 0, // Regular ground polygons.
    ///<exclude />
    Link = 1 // Off-mesh connections.
}
