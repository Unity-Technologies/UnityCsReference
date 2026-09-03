// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;

namespace Unity.ProjectAuditor.Editor.Core
{
    static partial class DescriptorLibrary
    {
        [AutoStaticsCleanupOnCodeReload] // Lazy-initialized descriptor registry; must be reset on code reload so descriptors are re-registered
        static Dictionary<int, Descriptor> s_Descriptors;

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

        public static bool UnregisterDescriptor(string id)
        {
            if (s_Descriptors != null)
                return s_Descriptors.Remove(new DescriptorId(id));
            return false;
        }

        // Every currently registered descriptor. Order is unspecified.
        public static IReadOnlyCollection<Descriptor> GetAllDescriptors()
        {
            if (s_Descriptors == null)
                return Array.Empty<Descriptor>();
            return s_Descriptors.Values;
        }

        public static Descriptor GetDescriptor(int idAsInt)
        {
            if (!s_Descriptors.TryGetValue(idAsInt, out var descriptor))
                throw new InvalidOperationException($"Descriptor with id {idAsInt} is not registered. Ensure Initialize() registers all descriptors used in Analyze(). This can happen if you report an issue without checking context.IsDescriptorEnabled(descriptor), for example if the issue is only applicable on a subset of platforms.");
            return descriptor;
        }

        public static bool HasDescriptor(int idAsInt)
        {
            return s_Descriptors.ContainsKey(idAsInt);
        }

        // Builds the list of Descriptors to serialize into a Report. Serialization is needed to survive domain
        // reload and when writing a Report out to file; in both cases the list only needs the Descriptors a report
        // actually references via its issues, so the caller passes the set of referenced ids. A null set means
        // "no scope": serialize the whole registry (larger, but still correct).
        internal static List<Descriptor> CollectForSerialization(HashSet<int> referencedIds)
        {
            if (s_Descriptors == null)
                return new List<Descriptor>();

            if (referencedIds == null)
                return new List<Descriptor>(s_Descriptors.Values);

            var result = new List<Descriptor>(referencedIds.Count);
            foreach (var id in referencedIds)
            {
                if (s_Descriptors.TryGetValue(id, out var descriptor))
                    result.Add(descriptor);
            }
            return result;
        }

        // Merges Descriptors deserialized from a Report back into the registry. Only adds ids that don't already
        // exist, otherwise we lose all the non-serialized data on the live descriptor, eg Fixer.
        internal static void RegisterDeserialized(List<Descriptor> descriptors)
        {
            if (descriptors == null)
                return;

            if (s_Descriptors == null)
                s_Descriptors = new Dictionary<int, Descriptor>();

            foreach (var descriptor in descriptors)
            {
                if (descriptor == null || string.IsNullOrEmpty(descriptor.Id))
                    continue;
                s_Descriptors.TryAdd(new DescriptorId(descriptor.Id).AsInt(), descriptor);
            }
        }

        // For testing purposes only
        internal static void Reset()
        {
            s_Descriptors?.Clear();
        }
    }
}
