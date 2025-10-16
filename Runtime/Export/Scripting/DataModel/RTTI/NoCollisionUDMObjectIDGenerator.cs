// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace Unity.DataModel;

internal class NoCollisionUDMObjectIDGenerator : IUDMObjectIDGenerator, IDisposable
{
    internal NoCollisionUDMObjectIDGenerator()
    {
        LocalRandom = new System.Random();
// TODO: Re-enable once Entities is part of U6
//#if !UNITY_EXTERNAL_TOOL
//        UsedIDs = new Unity.Collections.NativeHashSet<ulong>(1000, Unity.Collections.Allocator.Temp);
//#else
        UsedIDs = new HashSet<ulong>();
//#endif
    }

    internal NoCollisionUDMObjectIDGenerator(int randSeed)
    {
        LocalRandom = new System.Random(randSeed);
// TODO: Re-enable once Entities is part of U6
//#if !UNITY_EXTERNAL_TOOL
//      UsedIDs = new Unity.Collections.NativeHashSet<ulong>(1000, Unity.Collections.Allocator.Temp);
//#else
        UsedIDs = new HashSet<ulong>();
//#endif
    }

    internal void AddUsedUDMObjectID(UdmObjectId udmObjectID)
    {
        UsedIDs.Add(udmObjectID.Id);
    }

    public UdmObjectId NextUDMObjectID()
    {
        UInt64 nextID;
        do
        {
            nextID = NextRandomULong();
        }
        while (UsedIDs.Contains(nextID));

        UsedIDs.Add(nextID);
        return new UdmObjectId(nextID);
    }

    private UInt64 NextRandomULong()
    {
        Span<byte> resultBytes = stackalloc byte[8];
        LocalRandom.NextBytes(resultBytes);
        return BitConverter.ToUInt64(resultBytes);
    }

    public void Dispose()
    {
        // TODO: Re-enable once
        // Entities is part of U6
//#if !UNITY_EXTERNAL_TOOL
        //UsedIDs.Dispose();
//#endif
    }

    private System.Random LocalRandom;
// TODO: Re-enable once Entities is part of U6
//#if !UNITY_EXTERNAL_TOOL
    //private Unity.Collections.NativeHashSet<ulong> UsedIDs;
//#else
    private HashSet<ulong> UsedIDs;
//#endif
}
