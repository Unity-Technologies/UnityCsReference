// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using System.Runtime.CompilerServices;

using UnityEngine;
using UnityEngine.Bindings;

namespace Unity.DataModel;

// Minimal data we need to create a placeholder null UnityObject during managed object deserialization.
// This is used when a referenced UnityObject is missing. We keep this data in the DeserializeContext.
internal struct NullObjectData
{
    internal Schema HostSchema;
    internal uint FieldIndex;

    internal NullObjectData(Schema hostSchema, uint fieldIndex = uint.MaxValue)
    {
        HostSchema = hostSchema;
        FieldIndex = fieldIndex;
    }

    internal bool IsValid()
    {
        return HostSchema.IsValid() && FieldIndex != uint.MaxValue;
    }
}

// When deserializing a managed object that references a missing UnityObject, a placeholder null UnityObject is created.
// We keep the EntityId, so the reference can be resurrected when the asset re-appears.
// ScriptableObjects and MonoBehaviours can never be resurrected.
// This implements the same logic as GetNullUnityEngineObjectReplacement in TransferPPtrToMonoObject.cpp for valid EntityIds.
internal static class NullUnityObjectFactory
{
    // As the managed TypeCache is editor-only, we create our own bindings here.


    internal static UnityEngine.Object CreateNullUnityObject(EntityId entityId, NullObjectData nullObjectData)
    {
        var hostSchema = nullObjectData.HostSchema;
        var fieldIndex = nullObjectData.FieldIndex;

        var field = hostSchema.GetFields().GetFieldByIndex(fieldIndex);
        if (!field.IsValid())
            return null;

        var hostName = hostSchema.GetTypeName().ToString();
        var hostType = RttiResolver.GetRTTI(hostSchema).Type;

        // We don't want to use reflection here https://jira.unity3d.com/browse/UDM-462.
        // We cannot get the field type from the field schema, as that is the generic reference schema.
        var fieldName = field.GetName().ToString();
        var fieldInfo = hostType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (fieldInfo == null)
            return null;

        var fieldType = fieldInfo.FieldType;

        if (fieldType.IsArray)
        {
            if (fieldType.GetArrayRank() > 1)
                return null;

            fieldType = fieldType.GetElementType();
        }

        if (!typeof(UnityEngine.Object).IsAssignableFrom(fieldType))
            return null;

        // This mimics runtime behaviour and makes references to missing objects easier to detect (UUM-34447).
        if (Application.isPlaying && (typeof(MonoBehaviour).IsAssignableFrom(fieldType) || typeof(ScriptableObject).IsAssignableFrom(fieldType)))
            return null;


        string errorMessage = $"MissingReferenceException:The variable '{fieldName}' of '{hostName}' doesn't exist anymore.\n" +
            $"You probably need to reassign the '{fieldName}' variable of the '{hostName}' script in the inspector.";

        return null;
    }
}
