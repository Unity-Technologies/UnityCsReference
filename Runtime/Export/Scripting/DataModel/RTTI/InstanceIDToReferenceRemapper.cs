// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.EntitiesLike;
using UnityEngine;

namespace Unity.DataModel;

internal sealed class InstanceIDToReferenceRemapper : DocumentReferenceRemapper, IDisposable
{
    private Dictionary<EntityId, Reference> ReferenceMap;
    private List<Accessor> ExternalReferences;

    // In some cases we want to preserve references to non-persistent objects outside of the document, like during cloning or prefab merging.
    private bool PreserveUnresolvedInstanceIds;

    internal InstanceIDToReferenceRemapper(DocumentModel document, ReferenceRegistry referenceRegistry, bool preserveUnresolvedInstanceIds) : base(document)
    {
        ReferenceMap = new Dictionary<EntityId, Reference>(referenceRegistry.ObjectToUDMReference.Count);
        ExternalReferences = new List<Accessor>(referenceRegistry.ObjectToUDMReference.Count);
        PreserveUnresolvedInstanceIds = preserveUnresolvedInstanceIds;

        foreach (var item in referenceRegistry.ObjectToUDMReference)
        {
            if (item.Key is UnityEngine.Object unityObj)
            {
                unityObj.GetInstanceIdFast(out var instanceID); // GetInstanceIdFast() can be called on any thread
                ReferenceMap.TryAdd(instanceID, item.Value);
            }
            else
            {
                ReferenceMap.TryAdd(EntityId.From((int) item.Value.UdmObjectId.Id), item.Value);
            }
        }

        ReferenceMap.TryAdd(EntityId.None, Reference.Default);
    }

    internal override void RemapReference(Accessor referenceAccessor)
    {
        var reference = referenceAccessor.GetReferenceValue();
        if (!ReferenceMap.TryGetValue(EntityId.From((int) reference.UdmObjectId.Id), out var udmReference))
        {
            ExternalReferences.Add(referenceAccessor);
        }
        else
        {
            referenceAccessor.SetReferenceValue(udmReference);
        }
    }

    internal override void RemapExternalReferences()
    {
        NativeArray<EntityId> unmappedInstanceIDs = new(ExternalReferences.Count, Allocator.Temp);
        for (int index = 0; index < ExternalReferences.Count; ++index)
        {
            unmappedInstanceIDs[index] = EntityId.From((int) ExternalReferences[index].GetReferenceValue().UdmObjectId.Id);
        }

        NativeArray<Reference> references = new(ExternalReferences.Count, Allocator.Temp);
        AssetObjectRegistry.GetOrCreateReferencesForObjects(unmappedInstanceIDs, references);

        for (int i = 0; i < ExternalReferences.Count; ++i)
        {
            Reference reference;
            if (references[i].IsValid())
            {
                reference = references[i];
            }
            else if (PreserveUnresolvedInstanceIds)
            {
                reference = new Reference { UdmObjectId = unmappedInstanceIDs[i].GetRawData() };
            }
            else
            {
                reference = Reference.Default;
            }

            ExternalReferences[i].SetReferenceValue(reference);
        }

        unmappedInstanceIDs.Dispose();
        references.Dispose();
    }

    public void Dispose()
    {
        // TODO: When ReferenceMap & ExternalReferences
        // become NativeHashMap and NativeList, call
        // Dispose on them
        //ReferenceMap.Dispose();
        //ExternalReferences.Dispose();
    }
}
