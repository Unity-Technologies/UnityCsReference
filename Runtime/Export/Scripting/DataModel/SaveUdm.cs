// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.EntitiesLike;
using Unity.Profiling;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;

namespace Unity.DataModel;

internal static class SaveUdm
{
    private static readonly ProfilerMarker s_SerializeContextMarker = new ProfilerMarker("SerializeContextMarker");
    private static readonly ProfilerMarker s_SerializeCallMarker = new ProfilerMarker("Serialize");
    private static readonly ProfilerMarker s_RemappingMarker = new ProfilerMarker("Remapping");

    internal static DocumentModel CreateDocumentFromLiveObjectsInternal(ReadOnlySpan<UdmWriteData> objectsToWrite, ReadOnlySpan<EntityId> pureEntitiesIds, SerializeInstructionFlags options, bool remapInstanceIds, bool preserveUnresolvedInstanceIds, string resourcePath = "", UnityEngine.GUID assetGUID = default, bool serializeEcsData = true)
    {
        var document = DocumentModel.CreateNew();

        SerializeContext serializeContext;

        var randSeed = assetGUID.GetHashCode();
        using var udmObjectIDGenerator = new NoCollisionUDMObjectIDGenerator(randSeed);
        using (s_SerializeContextMarker.Auto())
        {
            serializeContext = BuildSerializeContext(document, resourcePath, udmObjectIDGenerator, objectsToWrite, pureEntitiesIds, options, serializeEcsData);
        }

        using (s_SerializeCallMarker.Auto())
        {
            serializeContext.Serialize();
        }

        if (remapInstanceIds)
        {
            using (s_RemappingMarker.Auto())
            using (var referenceRemapper = new InstanceIDToReferenceRemapper(document, serializeContext.ReferenceRegistry, preserveUnresolvedInstanceIds))
            {
                referenceRemapper.RemapReferences();
            }
        }

        return document;
    }

    [RequiredByNativeCode]
    internal static IntPtr CreateDocumentFromLiveObjectsInternalBindings(UdmWriteData[] objectsToWrite, EntityId[] pureEntitiesIds, ulong options, bool remapInstanceIds, bool preserveUnresolvedInstanceIds, string resourcePath, UnityEngine.GUID assetGUID, bool serializeEcsData)
    {
        SerializeInstructionFlags serializeOptions = (SerializeInstructionFlags)options;

        var document = CreateDocumentFromLiveObjectsInternal(objectsToWrite.AsSpan(), pureEntitiesIds.AsSpan(), serializeOptions, remapInstanceIds, preserveUnresolvedInstanceIds, resourcePath, assetGUID, serializeEcsData);
        return document.DocumentPtr;
    }

    internal static DocumentModel CreateDocumentFromUnityObjectInternal(UnityEngine.Object obj, UdmObjectId objectId, SerializeInstructionFlags options)
    {
        UdmWriteData udmWriteData;
        udmWriteData.objectId = objectId;
        obj.GetInstanceIdFast(out udmWriteData.instanceId); // GetInstanceIdFast() can be called on any thread
        udmWriteData.isStrippedObject = 0;

        return CreateDocumentFromLiveObjectsInternal(MemoryMarshal.CreateReadOnlySpan(ref udmWriteData, 1), ReadOnlySpan<EntityId>.Empty, options, false, false);
    }

    [RequiredByNativeCode]
    internal static IntPtr CreateDocumentFromUnityObjectInternalBindings(UnityEngine.Object obj, ulong objectId, ulong options)
    {
        SerializeInstructionFlags serializeOptions = (SerializeInstructionFlags)options;

        var document = CreateDocumentFromUnityObjectInternal(obj, objectId, serializeOptions);
        return document.DocumentPtr;
    }

    private static SerializeContext BuildSerializeContext(DocumentModel documentModel, string resourcePath, NoCollisionUDMObjectIDGenerator udmObjectIDGenerator, ReadOnlySpan<UdmWriteData> objectsToWrite, ReadOnlySpan<EntityId> pureEntitiesIds, SerializeInstructionFlags options, bool serializeEcsData)
    {
        EntityManager entityManager = default;
        if (ActiveGameObjectWorld.World != null)
        {
            entityManager = ActiveGameObjectWorld.World.EntityManager;
        }
        SerializeContext serializeContext = new SerializeContext(documentModel, options, udmObjectIDGenerator, entityManager, serializeEcsData);
        if (options.HasFlag(SerializeInstructionFlags.ResolveStreamedResourceSources))
        {
            serializeContext.ResourcePath = resourcePath;
        }

        foreach (var writeData in objectsToWrite)
        {
            UnityEngine.Object obj = Resources.EntityIdToObject(writeData.instanceId);
            if (obj == null)
            {
                continue;
            }
            serializeContext.AddUnityObjectForSerialization(obj, writeData.instanceId, writeData.objectId, writeData.isStrippedObject != 0);
            udmObjectIDGenerator.AddUsedUDMObjectID(writeData.objectId);
        }

        foreach (var entityId in pureEntitiesIds)
        {
            serializeContext.AddEntityForSerialization(entityId, udmObjectIDGenerator.NextUDMObjectID());
        }

        return serializeContext;
    }
}
