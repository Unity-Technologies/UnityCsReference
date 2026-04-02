// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.AI.Navigation.LowLevel;

// Keep in sync with the values in NavMeshTypes.h
[Flags]
public enum NavQueryStatus
{
    // High level status.
    Failure = 1 << 31,
    Success = 1 << 30,
    InProgress = 1 << 29,

    // Detail information for status.
    StatusDetailMask = 0x0ffffff,
    InvalidParameter = 1 << 3, // An input parameter was invalid.
    MoreDataAvailable = 1 << 4, // Result buffer for the query was too small to store all results.
    MaxNodesToVisitExceeded = 1 << 5, // Query ran out of nodes during search.
    PartialResult = 1 << 6 // Query did not reach the end location, returning best guess.
}

// Flags describing node properties. Keep in sync with the enum declared in NavMesh.h
public enum NavNodeType
{
    Undefined = -1,
    Polygon = 0, // Regular ground polygons.
    Link = 1 // Off-mesh connections.
}
