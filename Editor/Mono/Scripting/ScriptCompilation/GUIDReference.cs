// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Scripting.ScriptCompilation
{
    static class GUIDReference
    {
        private static readonly string GUIDReferencePrefix = "GUID:";
        private static readonly string GUIDReferencePrefixLowerCase = "guid:";

        public static bool IsGUIDReference(string reference)
        {
            return Utility.FastStartsWith(reference, GUIDReferencePrefix, GUIDReferencePrefixLowerCase);
        }

        public static string GUIDReferenceToGUID(string reference)
        {
            return reference.Substring(GUIDReferencePrefix.Length);
        }

        public static string GUIDToGUIDReference(string guid)
        {
            return $"{GUIDReferencePrefix}{guid}";
        }
    }
}
