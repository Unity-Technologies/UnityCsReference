// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.DataModel;

// This object group handles immutable types (strings and arrays) that cannot be initialized with RuntimeHelpers.GetUninitializedObject(type).
// Unlike other factories, we create an instance of the object during deserialization and add it to the InstanceRegistry afterwards.
// Immutable objects must be deserialized first, so that the correct object instances are available in case they are referenced by other objects.
internal static class ImmutablePureManagedObjectSerializer
{
    internal static ObjectModel SerializeObject(ImmutablePureManagedObjectRtti rtti, SerializeContext context, object data, UdmObjectId objectId, bool isStrippedObject, SerializeInstructionFlags options)
    {
        if (data is not string && !typeof(Array).IsAssignableFrom(data.GetType()))
            throw new InvalidOperationException($"{nameof(ImmutablePureManagedObjectSerializer)} is only valid for string and array objects.");

        ObjectModel objectModel = context.Document.CreateObjectModel(rtti.Schema, objectId);

        if (!objectModel.IsValid())
        {
            throw new InvalidOperationException($"Unable to create object model for objectId {objectId}");
        }

        var accessor = objectModel.GetAccessor();
        UdmManagedSerialization.WriteToAccessor(rtti.TransferData, accessor, data, context);

        return objectModel;
    }

    internal static object InstantiateFromModel(ImmutablePureManagedObjectRtti rtti, ConstAccessor accessor, DeserializeContext context)
    {
        var transferData = rtti.TransferData[0];

        if (transferData.RttiDataType == RttiDataType.String)
        {
            return accessor.GetUTF8StringValue().ToString();
        }
        else if (transferData.RttiDataType == RttiDataType.Array)
        {
            return UdmManagedSerialization.ArrayFromObjModelToLive(accessor, transferData, context);
        }
        else
        {
            throw new InvalidOperationException($"Unsupported data type {transferData.RttiDataType} for ImmutablePureManagedObject.");
        }
    }

    internal static void DeserializeObjects(DeserializeContext context, ConstructedImmutablePureManagedObjectSet objects)
    {
        foreach (var objectGroup in objects.objectGroups)
        {
            var accessor = new ConstAccessor {
                Schema = objectGroup.rtti.Schema,
                Data = default,
            };

            for (int i = 0; i < objectGroup.objectPtrs.Length; i++)
            {
                accessor.Data = objectGroup.objectPtrs[i];
                objectGroup.instances[i] = InstantiateFromModel(objectGroup.rtti, accessor, context);

                var reference = new Reference(objectGroup.objectIds[i]);
                context.SetInstance(reference, objectGroup.instances[i]);
            }
        }
    }
}
