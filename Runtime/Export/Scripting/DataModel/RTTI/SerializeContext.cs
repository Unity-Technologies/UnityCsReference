// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// Define ENABLE_PROFILE_SERIALIZERS to enable profiling, as this could be expensive at scale

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Unity.EntitiesLike;
using Unity.Profiling;

namespace Unity.DataModel;

internal sealed class SerializeContext
{

    // We need to implement IEquatable so data structures like Hashset doesn't allocate in functions like Contains
    private struct SerializeItemInfo : IEquatable<SerializeItemInfo>
    {
        internal object Obj;
        internal EntityId EntityId;
        internal UdmObjectId UdmObjectId;
        internal bool IsStrippedObject;

        public bool Equals(SerializeItemInfo other)
        {
            return  Obj == other.Obj &&
                    EntityId == other.EntityId &&
                    UdmObjectId == other.UdmObjectId &&
                    IsStrippedObject == other.IsStrippedObject;
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            return obj is SerializeItemInfo && Equals((SerializeItemInfo)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return UdmObjectId.GetHashCode();
            }
        }
    }

    internal SerializeContext(DocumentModel documentModel,
        SerializeInstructionFlags options,
        IUDMObjectIDGenerator objectIDGenerator,
        EntityManager entityManager = default,
        bool serializeEcsData = true)
    {
        Document = documentModel;
        Options = options;
        EntityManager = entityManager;
        ObjectIDGenerator = objectIDGenerator;

        ResourcePath = string.Empty;
        ReferenceRegistry = new();
        CollectedRecords = new();
        SerializedRecords = new();
        SerializingRecords = new();

        SerializeEcsData = serializeEcsData;
    }

    internal Reference ResolveReference(object obj)
    {
        if (obj == null)
        {
            return Reference.Default;
        }

        // Unity object for this reference shouldn't be added for Serialization at this point.
        if (obj is UnityEngine.Object unityObj)
        {
            unityObj.GetInstanceIdFast(out var instanceID); // GetInstanceIdFast() can be called on any thread

                return new Reference();
        }

        var reference = GetOrCreateReferenceForObject(obj);

        // We only need to serialize objects that live in this document
        if (reference.IsInternal())
        {
            AddObjectForSerialization(obj, reference.UdmObjectId);
        }
        return reference;
    }

    internal void AddEntityForSerialization(EntityId instanceID, UdmObjectId objectId)
    {
        if (!SerializedRecords.Add(objectId))
            return;

        var entry = new SerializeItemInfo
        {
            Obj = null,
            EntityId = instanceID,
            UdmObjectId = objectId,
            IsStrippedObject = false,
        };
        AddObjectForSerializationInternal(entry);
    }

    internal void AddObjectForSerialization(object obj, UdmObjectId objectId, bool isStrippedObject=false)
    {
        if (!SerializedRecords.Add(objectId))
            return;

        var entry = new SerializeItemInfo
        {
            Obj = obj,
            UdmObjectId = objectId,
            IsStrippedObject = isStrippedObject,
        };

        // Unity object for this reference shouldn't be added for Serialization at this point.
        if (obj is UnityEngine.Object unityObj)
        {
            entry.EntityId = unityObj.GetEntityId();
        }
        AddObjectForSerializationInternal(entry);
    }

    internal void AddUnityObjectForSerialization(UnityEngine.Object obj, EntityId instanceId, UdmObjectId objectId, bool isStrippedObject = false)
    {
        if (!SerializedRecords.Add(objectId))
            return;

        var entry = new SerializeItemInfo
        {
            Obj = obj,
            UdmObjectId = objectId,
            IsStrippedObject = isStrippedObject,
            EntityId = instanceId
        };
        AddObjectForSerializationInternal(entry);
    }

    private void AddObjectForSerializationInternal(SerializeItemInfo entry)
    {
        CollectedRecords.Add(entry);

        var reference = new Reference
        {
            DocumentId = default,
            UdmObjectId = entry.UdmObjectId
        };

        if (entry.Obj != null)
        {
            ReferenceRegistry.SetReference(entry.Obj, reference);
        }
    }


    internal void Serialize()
    {
        while (CollectedRecords.Count != 0)
        {
            SerializingRecords.Clear();
            (SerializingRecords, CollectedRecords) = (CollectedRecords, SerializingRecords);

            SerializeRecords();
        }
    }

    private void SerializeRecords()
    {
        var serializeECS = SerializeEcsData && EntityManager.IsCreated;
        Type GameObjectType = typeof(GameObject);
        var pureEntityRtti = (PureEntityRtti)RttiResolver.GetRTTI(typeof(PureEntity));

        foreach (var entry in SerializingRecords)
        {
            // Serialize
            var obj = entry.Obj;
            if (obj != null)
            {
                Rtti rtti;
                {
                    rtti = RttiResolver.GetOrAddRTTI(obj);
                }
                if (rtti != null)
                {
                    switch (rtti.RttiGroup)
                    {
                        case RttiGroup.PureManaged:
                        {
                            {
                                PureManagedObjectSerializer.SerializeObject((PureManagedObjectRtti)rtti, this, obj, entry.UdmObjectId, entry.IsStrippedObject, Options);
                            }
                            break;
                        }
                        case RttiGroup.ImmutablePureManaged:
                        {
                            {
                                ImmutablePureManagedObjectSerializer.SerializeObject((ImmutablePureManagedObjectRtti)rtti, this, obj, entry.UdmObjectId, entry.IsStrippedObject, Options);
                            }
                            break;
                        }
                        case RttiGroup.UnityObject:
                        {
                            {
                                UnityEngine.Object unityObj = (UnityEngine.Object)obj;
                                UnityObjectSerializer.SerializeObject((UnityObjectRtti)rtti, this, unityObj, entry.UdmObjectId, entry.IsStrippedObject, Options);

                                // Serialize ECS side of the GameObject
                                if (serializeECS && rtti.Type == GameObjectType)
                                {
                                    EcsComponentSerializer.SerializeEntityComponents(entry.EntityId, entry.UdmObjectId, this);
                                }
                            }
                            break;
                        }
                        case RttiGroup.PureEntity:
                        {
                            throw new NotSupportedException("We shouldn't have a managed object with the RttiGroup PureEntity");
                        }
                        case RttiGroup.SimpleNativeType:
                        {
                            {
                                SimpleNativeTypeObjectSerializer.SerializeObject(rtti, this, (ISimpleNativeType)obj, entry.UdmObjectId, entry.IsStrippedObject, Options);
                            }
                            break;
                        }
                        default: break;
                    }
                }
            }
            else if (serializeECS && entry.EntityId != EntityId.None)
            {
                {
                    // It's a pure entity because it's not an object but has an instance id.
                    EcsComponentSerializer.SerializePureEntity(pureEntityRtti, entry.EntityId, entry.UdmObjectId, this);
                }
            }
        }
    }

    private Reference GetOrCreateReferenceForObject(object obj)
    {
        Reference reference;
        bool found;
        try
        {
            found = ReferenceRegistry.TryGetReference(obj, out reference);
        }
        catch (Exception e)
        {
            // Catch exceptions thrown during hashing
            EngineHelper.LogWarning($"Unable to create reference for object: {e.ToString()}");
            return Reference.Default;
        }

        if (!found)
        {
            // There is no entry in the registry, so we need to create a new reference
            UdmObjectId objId = ObjectIDGenerator.NextUDMObjectID();
            reference = new Reference
            {
                DocumentId = default,
                UdmObjectId = objId
            };
            ReferenceRegistry.SetReference(obj, reference);
        }
        return reference;
    }

    internal IUDMObjectIDGenerator ObjectIDGenerator { get; private set; }

    internal readonly DocumentModel Document;
    internal EntityManager EntityManager;
    internal readonly SerializeInstructionFlags Options;

    internal bool SerializeEcsData;

    internal string ResourcePath { get; set; }
    private List<SerializeItemInfo> CollectedRecords;
    private HashSet<UdmObjectId> SerializedRecords;
    private List<SerializeItemInfo> SerializingRecords;
    internal ReferenceRegistry ReferenceRegistry { get; private set; }
}
