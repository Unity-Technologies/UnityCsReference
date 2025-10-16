// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace Unity.DataModel;

internal struct PureEntitySet
{
    internal EntityId[] pureEntityInstanceIds;
    internal UdmObjectId[] pureEntityObjectIds;
}

internal sealed class PureEntityFactory
{
    private PureEntitySet pureEntities = new PureEntitySet();

    internal void AddObjectsToBatch(PureEntityRtti rtti, UdmObjectId[] objectIds)
    {
}

    internal PureEntitySet FlushBatch()
    {
        return pureEntities;
    }
}
