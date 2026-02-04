// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Core
{
    static class DescriptorLoader
    {
#pragma warning disable CS0649

        [Serializable]
        private sealed class SerializedDescriptor
        {
            public string id;
            public string title;
            public string defaultSeverity;
            public string[] areas;
            public string[] platforms;
            public string description;
            public string recommendation;
            public string documentationUrl;
            public string minimumVersion;
            public string maximumVersion;
            public string type;
            public string method;
            public string returnType;
            public string value;

            internal SerializedDescriptor()
            {
                // only for json serialization purposes.
                type = string.Empty;
                method = string.Empty;
                returnType = string.Empty;
                defaultSeverity = Severity.Default.ToString();
            }

            /// <summary>Returns the hash code for the Descriptor's Issue ID.</summary>
            /// <returns>The computed hash code.</returns>
            public override int GetHashCode()
            {
                return id.GetHashCode();
            }
        }

        [Serializable]
        private class SerializedDescriptorCollection
        {
            public SerializedDescriptor[] descriptors;
        }

#pragma warning restore CS0649

        internal static List<Descriptor> LoadFromJson(string path, string name)
        {
            var json = File.ReadAllText(Path.Combine(path, name + ".json"));
            var rawDescriptors = JsonUtility.FromJson<SerializedDescriptorCollection>("{\"descriptors\":" + json + "}").descriptors;
            var descriptors = new List<Descriptor>(rawDescriptors.Length);
            foreach (var rawDescriptor in rawDescriptors)
            {
                if (string.IsNullOrEmpty(rawDescriptor.id))
                    throw new Exception("Descriptor with null id loaded from " + name);

                var areas = (Areas)Enum.Parse(typeof(Areas), string.Join(", ", rawDescriptor.areas));
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                var platforms = (rawDescriptor.platforms != null) ? rawDescriptor.platforms.Select(p => (BuildTarget)Enum.Parse(typeof(BuildTarget), p)).ToSerializableArray() : null;
#pragma warning restore UA2001

                var desc = new Descriptor(rawDescriptor.id, rawDescriptor.title, areas, rawDescriptor.description, rawDescriptor.recommendation)
                {
                    Type = rawDescriptor.type ?? string.Empty,
                    Method = rawDescriptor.method ?? string.Empty,
                    ReturnType = rawDescriptor.returnType ?? string.Empty,
                    Value = rawDescriptor.value,
                    Platforms = platforms,
                    DefaultSeverity = rawDescriptor.defaultSeverity == Severity.Default.ToString() ? Severity.Moderate : (Severity)Enum.Parse(typeof(Severity), rawDescriptor.defaultSeverity),
                    DocumentationUrl = rawDescriptor.documentationUrl ?? string.Empty,
                    MinimumVersion = rawDescriptor.minimumVersion ?? string.Empty,
                    MaximumVersion = rawDescriptor.maximumVersion ?? string.Empty
                };
                if (string.IsNullOrEmpty(desc.Title))
                {
                    if (string.IsNullOrEmpty(desc.Type) || string.IsNullOrEmpty(desc.Method))
                        desc.Title = string.Empty;
                    else
                        desc.Title = desc.GetFullTypeName();
                }

                descriptors.Add(desc);
            }

            return descriptors;
        }
    }
}
