// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.AdaptivePerformance.Editor.Metadata;
using UnityEditor.AdaptivePerformance.UI.Editor;
using UnityEngine;
using UnityEngine.AdaptivePerformance;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Profile.AdaptivePerformance;

internal class AdaptivePerformanceSettingProvider : IBuildProfileSettingsProvider
{
    public string GetDisplayName() => BuildProfileAdaptivePerformanceToggle.adaptivePerformanceLabelText;
    public string GetTooltip() => string.Empty;

    public bool CanAddSettings(BuildProfile profile)
    {
        return BuildProfileModuleUtil.IsModuleInstalled(profile.platformGuid) && profile.platformBuildProfile != null && profile.GetComponent<AdaptivePerformanceGeneralSettings>() == null;
    }

    public bool HasSettings(BuildProfile profile)
    {
        return profile.platformBuildProfile?.adaptivePerformanceEnabled == true || profile.GetComponent<AdaptivePerformanceGeneralSettings>() != null;
    }

    public void OnAdd(BuildProfile profile)
    {
        if (profile == null || profile.platformBuildProfile == null) return;
        profile.platformBuildProfile.adaptivePerformanceEnabled = true;
    }

    public void OnRemove(BuildProfile profile)
    {
        if (profile == null || profile.platformBuildProfile == null) return;
        profile.platformBuildProfile.adaptivePerformanceEnabled = false;
        BuildProfileAdaptivePerformanceToggle.RemoveAllSettingsFromBuildProfile(profile);
    }

    public Action<BuildProfile> GetResetAction() => OnReset;

    static void OnReset(BuildProfile profile)
    {
        if (profile == null || profile.platformBuildProfile == null) return;
        profile.platformBuildProfile.adaptivePerformanceEnabled = true; // When AP is disabled and reset, it should be enabled again
        BuildProfileAdaptivePerformanceToggle.RemoveAllSettingsFromBuildProfile(profile);
    }

    public Action<BuildProfile> OnCopy() => OnCopyProfile;
    public Action<BuildProfile> OnPaste() => OnPasteProfile;

    public VisualElement CreateInspectorGUI(BuildProfile profile, SerializedObject serializedObject)
    {
        return new BuildProfileAdaptivePerformanceToggle(profile);
    }

    [Serializable]
    class ClipboardRef
    {
        public string propertyPath;
        public int targetIndex;
    }

    [Serializable]
    class ClipboardEntry
    {
        public string typeName;
        public string json;
        public List<ClipboardRef> refs = new();
    }

    [Serializable]
    class ClipboardPayload
    {
        public string format;
        public bool adaptivePerformanceEnabled;
        public List<ClipboardEntry> entries = new();
    }

    const string k_PayloadFormat = "UnityEditor.Build.Profile.AdaptivePerformance/1";

    const string k_ForeignClipboardMessage =
        "Adaptive Performance paste: the clipboard does not contain Adaptive Performance settings.";

    internal static void OnCopyProfile(BuildProfile profile)
    {
        var payload = BuildPayload(profile);
        if (payload != null)
            GUIUtility.systemCopyBuffer = payload;
    }

    internal static string BuildPayload(BuildProfile profile)
    {
        if (profile == null) return null;
        var profilePath = AssetDatabase.GetAssetPath(profile);
        if (string.IsNullOrEmpty(profilePath)) return null;

        var payload = new ClipboardPayload
        {
            format = k_PayloadFormat,
            adaptivePerformanceEnabled = profile.platformBuildProfile?.adaptivePerformanceEnabled ?? false
        };

        // Pass 1: enumerate the known AP sub-assets in a fixed order and assign each an
        // index. Enumerating explicitly (rather than BFS-ing over ObjectReferences) scopes
        // the payload to exactly the six AP types and prevents an unrelated ScriptableObject
        // sub-asset reachable through some future ObjectReference from being copied along.
        var orderedSubAssets = new List<ScriptableObject>();
        var indexByEntityId = new Dictionary<EntityId, int>();
        foreach (var subAsset in BuildProfileAdaptivePerformanceToggle.EnumerateAdaptivePerformanceSubAssets(profile))
        {
            if (subAsset == null) continue;
            var id = subAsset.GetEntityId();
            if (indexByEntityId.ContainsKey(id)) continue;
            indexByEntityId[id] = orderedSubAssets.Count;
            orderedSubAssets.Add(subAsset);
        }

        // Pass 2: serialize each sub-asset. Any ObjectReference whose target is in our
        // indexed set is a cross-ref within the AP graph and gets recorded so paste can
        // rewire it to the target profile's freshly-created instance. Refs to anything
        // else (external assets, MonoScripts) pass through the JSON unchanged.
        foreach (var so in orderedSubAssets)
        {
            var soType = so.GetType();
            var entry = new ClipboardEntry
            {
                // FullName + short assembly name (no Version / Culture / PublicKeyToken)
                // so a payload copied under one Unity build can still be pasted after the
                // AP assembly's version bumps in a later build.
                typeName = soType.FullName + ", " + soType.Assembly.GetName().Name,
                json = EditorJsonUtility.ToJson(so)
            };
            using var serializedObject = new SerializedObject(so);
            var iterator = serializedObject.GetIterator();
            while (iterator.Next(true))
            {
                if (iterator.propertyType != SerializedPropertyType.ObjectReference) continue;
                var target = iterator.objectReferenceValue;
                if (target == null) continue;
                if (!indexByEntityId.TryGetValue(target.GetEntityId(), out var targetIndex)) continue;
                entry.refs.Add(new ClipboardRef
                {
                    propertyPath = iterator.propertyPath,
                    targetIndex = targetIndex
                });
            }
            payload.entries.Add(entry);
        }

        return EditorJsonUtility.ToJson(payload);
    }

    internal static void OnPasteProfile(BuildProfile profile)
    {
        ApplyPayload(profile, GUIUtility.systemCopyBuffer);
    }

    internal static void ApplyPayload(BuildProfile profile, string clipboard)
    {
        if (profile == null) return;
        if (string.IsNullOrEmpty(clipboard)) return;

        var payload = new ClipboardPayload();
        try
        {
            EditorJsonUtility.FromJsonOverwrite(clipboard, payload);
        }
        catch
        {
            Debug.LogWarning(k_ForeignClipboardMessage);
            return;
        }

        if (payload.format != k_PayloadFormat)
        {
            Debug.LogWarning(k_ForeignClipboardMessage);
            return;
        }

        payload.entries ??= new List<ClipboardEntry>();

        var entryTypes = new Type[payload.entries.Count];
        for (int i = 0; i < payload.entries.Count; i++)
        {
            var type = Type.GetType(payload.entries[i].typeName);
            if (type == null || !typeof(ScriptableObject).IsAssignableFrom(type))
            {
                Debug.LogWarning($"Adaptive Performance paste: could not resolve type '{payload.entries[i].typeName}'; skipping paste.");
                return;
            }

            if (type.IsAbstract || type.ContainsGenericParameters)
            {
                Debug.LogWarning($"Adaptive Performance paste: type '{type.FullName}' cannot be instantiated; skipping paste.");
                return;
            }
            entryTypes[i] = type;
        }

        // Validate all loader/provider entries against the destination profile's target platform
        // BEFORE mutating anything, so an unsupported payload leaves the target untouched.
        for (int i = 0; i < payload.entries.Count; i++)
        {
            var type = entryTypes[i];
            var isLoader = typeof(AdaptivePerformanceLoader).IsAssignableFrom(type);
            var isProvider = typeof(IAdaptivePerformanceSettings).IsAssignableFrom(type);
            if (!isLoader && !isProvider)
            {
                // Anything BuildPayload cannot have produced is someone else's object.
                if (!IsAdaptivePerformancePayloadType(type))
                {
                    Debug.LogWarning($"Adaptive Performance paste: '{type.FullName}' is not an Adaptive Performance type; skipping paste.");
                    return;
                }
                continue;
            }
            if (!BuildProfileAdaptivePerformanceToggle.IsLoaderOrProviderSettingsSupportedForBuildTarget(type.FullName, profile.buildTarget))
            {
                Debug.LogWarning($"Adaptive Performance paste: '{type.FullName}' is not supported on the destination profile's target platform ({profile.buildTarget}); skipping paste.");
                return;
            }
        }

        // Build every replacement instance BEFORE touching the profile since CreateInstance can still return null
        var newInstances = new ScriptableObject[payload.entries.Count];
        for (int i = 0; i < payload.entries.Count; i++)
        {
            var created = ScriptableObject.CreateInstance(entryTypes[i]);
            if (created == null)
            {
                // Nothing has been mutated yet, so discarding what we made leaves the profile intact.
                for (int j = 0; j < i; j++)
                    UnityEngine.Object.DestroyImmediate(newInstances[j]);
                Debug.LogWarning($"Adaptive Performance paste: could not create an instance of '{entryTypes[i].FullName}'; skipping paste.");
                return;
            }
            newInstances[i] = created;
        }

        // Group every mutation below into a single undo step so Ctrl+Z after a paste
        // fully restores the previous Adaptive Performance graph.
        const string undoName = "Paste Adaptive Performance Settings";
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName(undoName);
        int undoGroup = Undo.GetCurrentGroup();

        // Capture the profile itself so the adaptivePerformanceEnabled flag change is undoable.
        Undo.RegisterCompleteObjectUndo(profile, undoName);

        BuildProfileAdaptivePerformanceToggle.RemoveAllSettingsFromBuildProfile(profile, registerUndo: true);
        if (profile.platformBuildProfile != null)
            profile.platformBuildProfile.adaptivePerformanceEnabled = payload.adaptivePerformanceEnabled;

        // Attach the instances built above, in the same order as they were copied.
        // This will also paste over the original references, which will be restored in the next loop.
        for (int i = 0; i < payload.entries.Count; i++)
        {
            var so = newInstances[i];
            AssetDatabase.AddObjectToAsset(so, profile);
            so.hideFlags |= HideFlags.HideInHierarchy;
            EditorJsonUtility.FromJsonOverwrite(payload.entries[i].json, so);
            // Register creation AFTER FromJsonOverwrite so the recorded initial state
            // includes the populated fields (redo re-creates the object in that state).
            Undo.RegisterCreatedObjectUndo(so, undoName);
        }
        // Restore the references between the ScriptableObjects in the payload to the newly created instances
        for (int i = 0; i < newInstances.Length; i++)
        {
            var refs = payload.entries[i].refs;
            if (refs == null || refs.Count == 0) continue;

            using var serializedObject = new SerializedObject(newInstances[i]);
            foreach (var refEntry in refs)
            {
                if (refEntry.targetIndex < 0 || refEntry.targetIndex >= newInstances.Length) continue;
                var property = serializedObject.FindProperty(refEntry.propertyPath);
                if (property == null || property.propertyType != SerializedPropertyType.ObjectReference) continue;
                property.objectReferenceValue = newInstances[refEntry.targetIndex];
            }
            // ApplyModifiedProperties (not WithoutUndo) so ref rewires are captured in the undo group.
            serializedObject.ApplyModifiedProperties();
        }

        Undo.CollapseUndoOperations(undoGroup);
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssetIfDirty(profile);
    }

    // The non-loader, non-provider types EnumerateAdaptivePerformanceSubAssets can yield.
    static bool IsAdaptivePerformancePayloadType(Type type) =>
        typeof(AdaptivePerformanceGeneralSettings).IsAssignableFrom(type) ||
        typeof(AdaptivePerformanceManagerSettings).IsAssignableFrom(type) ||
        typeof(BuildProfileProviderContainer).IsAssignableFrom(type) ||
        typeof(AdaptivePerformanceScaler).IsAssignableFrom(type);
}
