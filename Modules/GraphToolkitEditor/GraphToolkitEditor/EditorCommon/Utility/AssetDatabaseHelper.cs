// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    [UnityRestricted]
    internal static class AssetDatabaseHelper
    {
        public static bool TryGetGUIDAndLocalFileIdentifier(Object obj, out GUID guid, out long localId)
        {
            var success = AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out var guidStr, out localId);
            GUID.TryParse(guidStr, out guid);
            return success;
        }
    }
}
