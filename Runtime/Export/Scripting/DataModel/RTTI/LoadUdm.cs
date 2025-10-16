// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.DataModel;
using System.Collections.Generic;
using System;


using Unity.Collections;
using Unity.EntitiesLike;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;

namespace Unity.DataModel
{
    internal enum LoadMode { Normal, Merge }

    internal static class LoadUdm
    {
        [NativeHeader("Runtime/Export/Scripting/DataModel/RTTI/LoadUdm.h")]
        [FreeFunction("SetAwakeFromLoadQueueBinding")]
        [ThreadSafe]
        extern internal static void SetAwakeFromLoadQueue(UnityEngine.Object[] objects, IntPtr awakeFromLoadQueuePtr);

        internal static unsafe ConstructedObjectSets CreateLiveObjectsFromDocumentInternal(DocumentModel document, EntityManager entityManager, SerializeInstructionFlags serializeOptions, bool preserveMissingInternalReferences, bool registerObjects = false)
        {
            return CreateAndMergeLiveObjectsFromDocumentInternal(document, entityManager, serializeOptions, LoadMode.Normal, ReadOnlySpan<UdmReadData>.Empty, preserveMissingInternalReferences, registerObjects);
        }

        internal static ConstructedObjectSets CreateAndMergeLiveObjectsFromDocumentInternal(DocumentModel documentModel, EntityManager entityManager, SerializeInstructionFlags serializeOptions, LoadMode loadMode, ReadOnlySpan<UdmReadData> objectsToMerge, bool preserveMissingInternalReferences, bool registerObjects)
        {
            // Load external documents
            List<UdmGuid> externalRefs = new List<UdmGuid>();
            documentModel.GetExternalDocumentIDs(externalRefs);
            var span = NoAllocHelpers.CreateSpan(externalRefs);

            var objectsBySchema = DocumentInstanceFactory.GetObjectsBySchema(documentModel);

            var nativeObjectIds = DocumentInstanceFactory.GetNativeObjectIds(objectsBySchema);

            var instanceIdDictionary = new Dictionary<UdmObjectId, EntityId>();
            HashSet<UdmObjectId> existingObjects = null;

            bool hasExistingObjects = false;
            if (loadMode == LoadMode.Merge)
            {
                existingObjects = new HashSet<UdmObjectId>();

                if (!objectsToMerge.IsEmpty)
                {
                    hasExistingObjects = true;
                }
            }

            if (!hasExistingObjects)
            {
            }

            // We are only allowed to register objects on the main thread
            var constructedObjectSets = DocumentInstanceFactory.ConstructAndMergeObjectsInternal(loadMode, objectsBySchema, instanceIdDictionary, existingObjects, entityManager, registerObjects);

            return constructedObjectSets;
        }

        internal static void MergeUnityObjectFromDocumentInternal(DocumentModel documentModel, UnityEngine.Object obj, UdmObjectId objectId, SerializeInstructionFlags serializeOptions)
        {
            var objectsBySchema = DocumentInstanceFactory.GetObjectsBySchema(documentModel);

            EntityId instanceId;
            obj.GetInstanceIdFast(out instanceId); // GetInstanceIdFast() can be called on any thread

            var instanceIdDictionary = new Dictionary<UdmObjectId, EntityId>();
            var existingObjects = new HashSet<UdmObjectId>();

            instanceIdDictionary[objectId] = instanceId;
            existingObjects.Add(objectId);

            EntityManager entityManager = default;

            // We are only allowed to register objects on the main thread
            var constructedObjectSets = DocumentInstanceFactory.ConstructAndMergeObjectsInternal(LoadMode.Merge, objectsBySchema, instanceIdDictionary, existingObjects, entityManager, false);

        }

        [RequiredByNativeCode]
        internal static unsafe void MergeUnityObjectFromDocumentInternalBindings(IntPtr documentPtr, UnityEngine.Object obj, ulong objectId, ulong options)
        {
            DocumentModel document = documentPtr;
            SerializeInstructionFlags serializeOptions = (SerializeInstructionFlags)options;

            MergeUnityObjectFromDocumentInternal(document, obj, objectId, serializeOptions);
        }

        internal static void RegisterInstances(DeserializeContext context, ConstructedObjectSets constructedObjectSets)
        {
            var reference = new Reference();
            reference.DocumentId = default;

            foreach (var objectGroup in constructedObjectSets.pureManagedObjects.objectGroups)
            {
                for (int i = 0; i < objectGroup.objectIds.Length; i++)
                {
                    reference.UdmObjectId = objectGroup.objectIds[i];
                    context.SetInstance(reference, objectGroup.instances[i]);
                }
            }

            for (int i = 0; i < constructedObjectSets.unityObjects.allObjectIds.Length; i++)
            {
                reference.UdmObjectId = constructedObjectSets.unityObjects.allObjectIds[i];
                context.SetInstance(reference, constructedObjectSets.unityObjects.allInstances[i]);
            }

            foreach (var objectGroup in constructedObjectSets.simpleNativeTypeObjects.objectGroups)
            {
                for (int i = 0; i < objectGroup.objectIds.Length; i++)
                {
                    reference.UdmObjectId = objectGroup.objectIds[i];
                    context.SetInstance(reference, objectGroup.instances[i]);
                }
            }
        }
    }
}
