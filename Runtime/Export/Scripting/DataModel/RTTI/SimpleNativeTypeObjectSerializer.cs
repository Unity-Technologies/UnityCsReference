// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.DataModel;

internal static class SimpleNativeTypeObjectSerializer
{
    internal static ObjectModel SerializeObject(Rtti rtti, SerializeContext context, ISimpleNativeType data, UdmObjectId objectId, bool isStrippedObject, SerializeInstructionFlags options)
    {
        ObjectModel objectModel = context.Document.CreateObjectModel(rtti.Schema, objectId);
        if (!objectModel.IsValid())
        {
            throw new InvalidOperationException($"Unable to create object model for objectId {objectId}");
        }

        var accessor = objectModel.GetAccessor();
        data.UDMWriteNativeObject(accessor, options);
        return objectModel;
    }

    internal static void DeserializeObjects(DeserializeContext context, ConstructedSimpleNativeTypeObjectSet objects)
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
                objectGroup.instances[i].UDMReadNativeObject(accessor, context.Options, context.Resolver);
            }
        }
    }
}
