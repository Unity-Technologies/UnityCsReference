// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Scripting;

namespace UnityEditor.Shaders
{
    [RequiredByNativeCode]
    internal static class RenderPipelineSubShaderStripping
    {
        const string k_GraphicsSettingsPath = "ProjectSettings/GraphicsSettings.asset";
        const string k_QualitySettingsPath = "ProjectSettings/QualitySettings.asset";

        // RenderPipeline shader tags for the persisted RP configuration of the build target group.
        // non-empty: resolved tag set. empty: no RP configured, strip tagged subshaders (UUM-141340).
        // null: an RP asset is referenced but not imported yet (cold Library) - don't strip/cache, let
        // the tag custom dependency reimport the shader once it resolves.
        [RequiredByNativeCode]
        internal static string[] GetActiveRenderPipelineShaderTagsForPlatform(string buildTargetGroupName)
        {
            var unique = UnityEngine.Pool.HashSetPool<string>.Get();
            try
            {
                // Typed overload avoids substituting the live (runtime-overridable) default RP.
                QualitySettings.GetRenderPipelineAssetsForPlatform<RenderPipelineAsset>(
                    buildTargetGroupName, out HashSet<RenderPipelineAsset> perQualityAssets, out bool allLevelsAreOverridden);

                foreach (var asset in perQualityAssets)
                    AddTagIfPersistent(unique, asset);

                // Typed overload above drops not-yet-imported per-quality assets, so check them directly.
                if (AnyPersistentPerQualityRenderPipelineUnresolved())
                    return null;

                if (!allLevelsAreOverridden || perQualityAssets.Count == 0)
                {
                    if (!TryAddPersistentDefaultRenderPipelineTag(unique))
                        return null; // referenced but not imported yet
                }

                if (unique.Count == 0)
                    return Array.Empty<string>();

                var result = new string[unique.Count];
                unique.CopyTo(result);
                return result;
            }
            finally
            {
                UnityEngine.Pool.HashSetPool<string>.Release(unique);
            }
        }

        static void AddTagIfPersistent(HashSet<string> tags, RenderPipelineAsset asset)
        {
            // In-memory assets (e.g. test pipelines) can't be part of a build; ignore for stable results.
            if (asset == null || !EditorUtility.IsPersistent(asset))
                return;

            string tag = asset.renderPipelineShaderTag;
            if (!string.IsNullOrEmpty(tag))
                tags.Add(tag);
        }

        // Reads the default RP from disk (not the runtime-overridable live one). Returns false when it's
        // referenced but not importable yet (unresolved); true when resolved or genuinely unconfigured.
        static bool TryAddPersistentDefaultRenderPipelineTag(HashSet<string> tags)
        {
            return InspectPersistentSettings(k_GraphicsSettingsPath, ifMissing: true, loaded =>
            {
                foreach (var obj in loaded)
                {
                    if (obj == null)
                        continue;

                    var property = new SerializedObject(obj).FindProperty("m_CustomRenderPipeline");
                    if (property == null)
                        continue;

                    var asset = property.objectReferenceValue as RenderPipelineAsset;
                    if (asset != null)
                    {
                        AddTagIfPersistent(tags, asset);
                        return true;
                    }

                    return !IsUnresolvedRenderPipelineReference(property);
                }
                return true;
            });
        }

        // Conservative across all quality levels (not just the current platform's): worst case is one
        // extra reimport cycle, never a wrong strip.
        static bool AnyPersistentPerQualityRenderPipelineUnresolved()
        {
            return InspectPersistentSettings(k_QualitySettingsPath, ifMissing: false, loaded =>
            {
                foreach (var obj in loaded)
                {
                    if (obj == null)
                        continue;

                    var levels = new SerializedObject(obj).FindProperty("m_QualitySettings");
                    if (levels == null || !levels.isArray)
                        continue;

                    for (int i = 0; i < levels.arraySize; ++i)
                    {
                        var rp = levels.GetArrayElementAtIndex(i).FindPropertyRelative("customRenderPipeline");
                        if (rp != null && IsUnresolvedRenderPipelineReference(rp))
                            return true;
                    }
                    return false;
                }
                return false;
            });
        }

        // Loads a persisted settings file as detached copies, passes them to 'inspect', and always
        // destroys them. Returns 'ifMissing' when the file can't be loaded.
        static bool InspectPersistentSettings(string path, bool ifMissing, Func<UnityEngine.Object[], bool> inspect)
        {
            UnityEngine.Object[] loaded = InternalEditorUtility.LoadSerializedFileAndForget(path);
            if (loaded == null)
                return ifMissing;

            try
            {
                return inspect(loaded);
            }
            finally
            {
                // Detached copies are owned by us.
                foreach (var obj in loaded)
                {
                    if (obj != null)
                        UnityEngine.Object.DestroyImmediate(obj);
                }
            }
        }

        // Null object with a live entity id == referenced but not imported yet.
        static bool IsUnresolvedRenderPipelineReference(SerializedProperty property)
        {
            return property.objectReferenceValue == null && property.objectReferenceEntityIdValue != EntityId.None;
        }
    }
}
