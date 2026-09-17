// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEngine;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditorInternal
{
    internal static class ManagedCaptureOtaConfig
    {
        // Test seam: overrides the persisted OTA config location. Null uses the default Library path.
        [NoAutoStaticsCleanup]
        internal static string OtaConfigPathOverride { get; set; }

        // Under Library/ so the override is machine-local and survives restarts, letting builds run
        // offline from the last fetched config.
        static string OtaConfigPath => OtaConfigPathOverride
            ?? Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Library", "ManagedCapture", "config.json"));

        // Rejects an invalid payload rather than persisting it, so a bad OTA push cannot break the
        // build or widen capture. The network fetch is the caller's responsibility.
        internal static bool SetOtaConfig(string configJson)
        {
            if (!IsValidConfig(configJson, out var reason))
            {
                Debug.LogWarning($"[ManagedCapture] Rejected OTA config override: {reason}");
                return false;
            }

            var path = OtaConfigPath;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, configJson);
            return true;
        }

        internal static void ClearOtaConfig()
        {
            var path = OtaConfigPath;
            if (File.Exists(path))
                File.Delete(path);
        }

        internal static string ReadPersistedOtaConfig()
        {
            try
            {
                var path = OtaConfigPath;
                if (!File.Exists(path))
                    return null;

                var json = File.ReadAllText(path);
                if (IsValidConfig(json, out var reason))
                    return json;

                Debug.LogWarning($"[ManagedCapture] Ignoring persisted OTA config: {reason}");
                return null;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ManagedCapture] Failed to read persisted OTA config: {e.Message}");
                return null;
            }
        }

        // GetConfig() prefers the persisted file, so an accepted-but-malformed override would poison
        // every later build. The authoritative schema parse happens in the linker. Internal so
        // ManagedCaptureStripping can also use it to decide whether the resolved config (baked or OTA)
        // is worth forwarding to the linker at all.
        internal static bool IsValidConfig(string configJson, out string reason)
        {
            reason = null;
            if (string.IsNullOrWhiteSpace(configJson)) { reason = "empty"; return false; }

            // JsonUtility throws on malformed JSON and leaves the field null for a non-array
            // "assemblies", so a well-formed config is the only way to get a non-null array.
            ConfigShape parsed;
            try
            {
                parsed = JsonUtility.FromJson<ConfigShape>(configJson);
            }
            catch (Exception e)
            {
                reason = $"not valid JSON ({e.Message})";
                return false;
            }

            if (parsed.assemblies == null) { reason = "missing or non-array 'assemblies'"; return false; }
            return true;
        }

        // Only the assemblies array is inspected here; the linker validates the rest of the schema.
        [Serializable]
        struct ConfigShape
        {
#pragma warning disable 0649 // assigned by JsonUtility via reflection
            public AssemblyShape[] assemblies;
#pragma warning restore 0649
        }

        [Serializable]
        struct AssemblyShape
        {
#pragma warning disable 0649 // assigned by JsonUtility via reflection
            public string assembly;
#pragma warning restore 0649
        }
    }
}
