// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.UIElements;

namespace Unity.Localization.Editor;

static class MarkedAttribute
{
    // A name that already looks like a marker is written inside the plain marker, so reading it back cannot mistake it
    // for the other kind. Without this a collection called "Guid(x)" would come back empty.
    public static string Wrap(string value, string marker, string plainMarker)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        return Looks(value, marker) || Looks(value, plainMarker) ? $"{plainMarker}({value})" : value;
    }

    public static bool TryUnwrap(string value, string marker, out string inner)
    {
        inner = null;
        if (string.IsNullOrEmpty(value) || !Looks(value, marker))
            return false;
        inner = value.Substring(marker.Length + 1, value.Length - marker.Length - 2);
        return true;
    }

    static bool Looks(string value, string marker)
        => value.StartsWith(marker + "(", StringComparison.Ordinal) && value.EndsWith(")", StringComparison.Ordinal);
}

sealed class TableReferenceConverter : UxmlAttributeConverter<TableReference>
{
    const string k_Guid = "Guid";
    const string k_Name = "Name";

    public override TableReference FromString(string value)
    {
        if (string.IsNullOrEmpty(value))
            return default;
        if (MarkedAttribute.TryUnwrap(value, k_Guid, out var guid))
            return TableReference.FromGuid(guid);
        return MarkedAttribute.TryUnwrap(value, k_Name, out var name) ? name : value;
    }

    public override string ToString(TableReference value) => value.ReferenceType switch
    {
        TableReference.Type.Guid => $"{k_Guid}({value.TableCollectionNameGuid})",
        TableReference.Type.Name => MarkedAttribute.Wrap(value.TableCollectionName, k_Guid, k_Name),
        _ => string.Empty
    };
}

sealed class TableEntryReferenceConverter : UxmlAttributeConverter<TableEntryReference>
{
    const string k_Id = "Id";
    const string k_Key = "Key";

    public override TableEntryReference FromString(string value)
    {
        if (string.IsNullOrEmpty(value))
            return default;
        if (MarkedAttribute.TryUnwrap(value, k_Id, out var id) && long.TryParse(id, out var keyId))
            return keyId;
        return MarkedAttribute.TryUnwrap(value, k_Key, out var key) ? key : value;
    }

    public override string ToString(TableEntryReference value) => value.ReferenceType switch
    {
        TableEntryReference.Type.Id => $"{k_Id}({value.KeyId})",
        TableEntryReference.Type.Name => MarkedAttribute.Wrap(value.Key, k_Id, k_Key),
        _ => string.Empty
    };
}
