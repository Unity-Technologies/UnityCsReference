// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
namespace Unity.DataModel;

internal sealed class InstanceRegistry
{
    internal Dictionary<Reference, object> UdmReferenceToObject { get; private set; } = new();

    internal bool TryGetInstance(Reference reference, out object obj)
    {
        return UdmReferenceToObject.TryGetValue(reference, out obj);
    }

    internal void SetInstance(Reference reference, object obj)
    {
        if (!reference.IsValid())
            throw new InvalidOperationException("Trying to call SetInstance with invalid reference");
        UdmReferenceToObject[reference] = obj;
    }
}
