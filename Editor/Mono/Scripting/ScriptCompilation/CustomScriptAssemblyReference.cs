// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Scripting.ScriptCompilation
{
    [Serializable]
    class CustomScriptAssemblyReferenceData
    {
        // Disable the `x is never assigned to, and will always have its default value' warning (CS0649)
        #pragma warning disable 649
        public string reference;

        public static CustomScriptAssemblyReferenceData FromJson(string json)
        {
            CustomScriptAssemblyReferenceData assemblyRefData = new CustomScriptAssemblyReferenceData();
            UnityEngine.JsonUtility.FromJsonOverwrite(json, assemblyRefData);

            if (assemblyRefData == null)
                throw new Exception("Json file does not contain an assembly reference definition");

            return assemblyRefData;
        }

        public static string ToJson(CustomScriptAssemblyReferenceData data)
        {
            return UnityEngine.JsonUtility.ToJson(data, true);
        }
    }

    class CustomScriptAssemblyReference
    {
        public bool Equals(CustomScriptAssemblyReference other)
        {
            return string.Equals(FilePath, other.FilePath, StringComparison.Ordinal)
                   && string.Equals(PathPrefix, other.PathPrefix, StringComparison.Ordinal)
                   && string.Equals(Reference, other.Reference, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((CustomScriptAssemblyReference) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(FilePath, PathPrefix, Reference);
        }

        public string FilePath { get; set; }
        public string PathPrefix { get; set; }
        public string Reference { get; set; } // Name or GUID

        public static CustomScriptAssemblyReference FromCustomScriptAssemblyReferenceData(string path, CustomScriptAssemblyReferenceData customScriptAssemblyReferenceData)
        {
            if (customScriptAssemblyReferenceData == null)
                return null;

            var pathPrefix = path.Substring(0, path.Length - AssetPath.GetFileName(path).Length);

            var customScriptAssemblyReference = new CustomScriptAssemblyReference();
            customScriptAssemblyReference.FilePath = path;
            customScriptAssemblyReference.PathPrefix = pathPrefix;
            customScriptAssemblyReference.Reference = customScriptAssemblyReferenceData.reference;

            return customScriptAssemblyReference;
        }

        public static CustomScriptAssemblyReference FromPathAndReference(string path, string reference)
        {
            var pathPrefix = path.Substring(0, path.Length - AssetPath.GetFileName(path).Length);

            var customScriptAssemblyReference = new CustomScriptAssemblyReference();
            customScriptAssemblyReference.FilePath = path;
            customScriptAssemblyReference.PathPrefix = pathPrefix;
            customScriptAssemblyReference.Reference = reference;

            return customScriptAssemblyReference;
        }

        public CustomScriptAssemblyReferenceData CreateData()
        {
            return new CustomScriptAssemblyReferenceData() { reference = Reference };
        }
    }
}
