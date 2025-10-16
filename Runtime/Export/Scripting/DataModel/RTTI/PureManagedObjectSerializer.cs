// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace Unity.DataModel;

internal static class PureManagedObjectSerializer
{
    internal static ObjectModel SerializeObject(PureManagedObjectRtti rtti, SerializeContext context, object data, UdmObjectId objectId, bool isStrippedObject, SerializeInstructionFlags options)
    {
        if (data is UnityEngine.Object)
            throw new InvalidOperationException($"{nameof(PureManagedObjectSerializer)} is not valid for objects derived from UnityEngine.Object. It's only valid for pure managed objects.");

        ObjectModel objectModel = context.Document.CreateObjectModel(rtti.Schema, objectId);

        if (!objectModel.IsValid())
        {
            throw new InvalidOperationException($"Unable to create object model for objectId {objectId}");
        }

        var accessor = objectModel.GetAccessor();
        UdmManagedSerialization.WriteToAccessor(rtti.TransferData, accessor, data, context);
        return objectModel;
    }

    internal static void DeserializeObjects(DeserializeContext context, ConstructedPureManagedObjectSet objects)
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
                UdmManagedSerialization.ReadFromAccessor(objectGroup.rtti.TransferData, accessor, objectGroup.instances[i], context);
            }
        }
    }
}
