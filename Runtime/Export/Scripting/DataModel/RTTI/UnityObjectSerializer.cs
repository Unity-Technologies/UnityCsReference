// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.DataModel;

internal static class UnityObjectSerializer
{
    internal static ObjectModel SerializeObject(UnityObjectRtti rtti, SerializeContext context, UnityEngine.Object data, UdmObjectId objectId, bool isStrippedObject, SerializeInstructionFlags options)
    {
        ObjectModel objectModel = context.Document.CreateObjectModel(rtti.Schema, objectId);
        if (!objectModel.IsValid())
        {
            throw new InvalidOperationException($"Unable to create object model for objectId {objectId}");
        }

        if (rtti is UnityHybridObjectRtti hybridRtti)
        {
            var accessor = objectModel.GetAccessor();
            if (!isStrippedObject)
            {
                UdmManagedSerialization.WriteToAccessor(hybridRtti.TransferData, accessor, data, context);
            }
        }
        UnityEngine.Object obj = (UnityEngine.Object)data;
        return objectModel;
    }

    internal static void DeserializeObject(UnityObjectRtti rtti, DeserializeContext context, ConstObjectModel dataObjectModel, UnityEngine.Object data)
    {
        if (rtti is UnityHybridObjectRtti hybridRtti)
        {
            var constAccessor = dataObjectModel.GetAccessor();
            UdmManagedSerialization.ReadFromAccessor(hybridRtti.TransferData, constAccessor, data, context);
        }
    }

    internal static void DeserializeObjects(DeserializeContext context, ConstructedUnityObjectSet objects)
    {
        var allObjectsIndex = 0;

        for (int i = 0; i < objects.schemaObjectCounts.Length; i++)
        {
            var schemaObjectCount = objects.schemaObjectCounts[i];
            var rtti = objects.allRtti[i];

            if (rtti is UnityHybridObjectRtti hybridRtti)
            {
                unsafe
                {
                    var constAccessor = new ConstAccessor()
                    {
                        Schema = rtti.Schema,
                        Data = default,
                        References = (IntPtr)context.DocumentModel.GetReferences()
                    };

                    for (int j = 0; j < schemaObjectCount.ObjectCount; j++)
                    {
                        constAccessor.Data = objects.allObjectPtrs[allObjectsIndex];
                        UdmManagedSerialization.ReadFromAccessor(hybridRtti.TransferData, constAccessor, objects.allInstances[allObjectsIndex], context);

                        allObjectsIndex++;
                    }
                }
            }
            else
            {
                allObjectsIndex += schemaObjectCount.ObjectCount;
            }
        }
    }
}
