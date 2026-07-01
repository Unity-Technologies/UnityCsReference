// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor.Experimental;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Core
{
    internal static class RoslynTextLookup
    {
        [Serializable]
        struct RawStringLookup
        {
#pragma warning disable 649 // Disable warning CS0649. The fields are assigned during JSON deserialization
            public string id;
            public string description;
            public string recommendation;
#pragma warning restore 649
        }

        struct StringLookup
        {
            public string description;
            public string recommendation;

            public StringLookup(string description, string recommendation)
            {
                this.description = description;
                this.recommendation = recommendation;
            }
        }

        private static Dictionary<string, StringLookup> m_StringLookup;

        public static void Initialize()
        {
            m_StringLookup = new Dictionary<string, StringLookup>();

            var json = EditorResources.Load<TextAsset>(Path.Combine(ProjectAuditor.s_RulesDataPath, "RoslynTextLookup.json")).text;
            var rawDescriptors = Json.DeserializeArray<RawStringLookup>(json);

            foreach (var rawDescriptor in rawDescriptors)
            {
                m_StringLookup[rawDescriptor.id] = new StringLookup(rawDescriptor.description, rawDescriptor.recommendation);
            }
        }

        public static bool GetDescription(string id, out string description, out string recommendation)
        {
            if (m_StringLookup == null)
                Initialize();

            if (m_StringLookup.TryGetValue(id, out var result))
            {
                description = result.description;
                recommendation = result.recommendation;
                return true;
            }

            description = string.Empty;
            recommendation = string.Empty;
            return false;
        }
    }
}
