// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Unity.Scripting.LifecycleManagement;

namespace Unity.ProjectAuditor.Editor.Core
{
    [Serializable]
    partial class DescriptorLibrary : ISerializationCallbackReceiver
    {
        [AutoStaticsCleanupOnCodeReload] // Lazy-initialized descriptor registry; must be reset on code reload so descriptors are re-registered
        static Dictionary<int, Descriptor> s_Descriptors;

        [NoAutoStaticsCleanup] // Lazy-initialized cache of area strings; data is still valid after code reload
        static Dictionary<Areas, string> s_DescriptorAreaStrings;

        [SerializeField]
        internal List<Descriptor> m_SerializedDescriptors;

        public static bool RegisterDescriptor(string id, Descriptor descriptor)
        {
            return RegisterDescriptor(new DescriptorId(id), descriptor);
        }

        public static bool RegisterDescriptor(DescriptorId id, Descriptor descriptor)
        {
            if (s_Descriptors == null)
                s_Descriptors = new Dictionary<int, Descriptor>();

            bool alreadyFound = s_Descriptors.ContainsKey(id);
            s_Descriptors[id] = descriptor;
            return alreadyFound;
        }

        public static Descriptor GetDescriptor(int idAsInt)
        {
            if (!s_Descriptors.TryGetValue(idAsInt, out var descriptor))
                throw new InvalidOperationException($"Descriptor with id {idAsInt} is not registered. Ensure Initialize() registers all descriptors used in Analyze(). This can happen if you report an issue without checking context.IsDescriptorEnabled(descriptor), for example if the issue is only applicable on a subset of platforms.");
            return descriptor;
        }

        public static string GetAreasString(Areas areas)
        {
            if (s_DescriptorAreaStrings == null)
                s_DescriptorAreaStrings = new Dictionary<Areas, string>();

            if (s_DescriptorAreaStrings.TryGetValue(areas, out string desc))
                return desc;

            desc = ObjectNames.NicifyVariableName(areas.ToString());
            s_DescriptorAreaStrings[areas] = desc;
            return desc;
        }

        public void OnBeforeSerialize()
        {
            // update list from dictionary

            // TODO: Serialization is needed to survive domain reload, and when writing a Report out to file.
            // In both cases the list only really needs to contain the Descriptors that correspond to ProjectIssues
            // actually found in the report, so if we had the report object we could potentially do some filtering here.
            if (s_Descriptors != null)
                m_SerializedDescriptors = new List<Descriptor>(s_Descriptors.Values);
        }

        public void OnAfterDeserialize()
        {
            // update dictionary from list
            if (m_SerializedDescriptors != null)
            {
                if (s_Descriptors == null)
                    s_Descriptors = new Dictionary<int, Descriptor>();

                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                var deserializedDescriptors = m_SerializedDescriptors.ToDictionary(m => new DescriptorId(m.Id).AsInt(), m => m);
#pragma warning restore UA2001
                foreach (var deserializedDescriptor in deserializedDescriptors)
                    s_Descriptors.TryAdd(deserializedDescriptor.Key, deserializedDescriptor.Value); // Only add items that don't exist, otherwise we lose all the non-serialized data, eg Fixer

                m_SerializedDescriptors = null;
            }
        }

        // For testing purposes only
        internal static void Reset()
        {
            s_Descriptors?.Clear();
        }
    }
}
