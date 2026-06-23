// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor.Utilities;

[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
internal static class UIAnimationClipFactory
{
    const string k_CreateAssetUndoName = "Create UI Animation Clip";

    // Asset creation isn't undoable (matches Unity's "Create > X" menus); only the field assignment reverts.
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    public static UIAnimationClip CreateAssetAndAssignToField(string path, Action<UIAnimationClip> assignToField)
    {
        if (assignToField == null)
            return null;

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName(k_CreateAssetUndoName);
        var undoGroup = Undo.GetCurrentGroup();

        var (loaded, _) = CreateOrReplaceAsset(path);

        try
        {
            if (loaded != null)
                assignToField(loaded);
        }
        finally
        {
            Undo.CollapseUndoOperations(undoGroup);
        }

        return loaded;
    }

    static (UIAnimationClip clip, bool wasNewlyCreated) CreateOrReplaceAsset(string path)
    {
        if (string.IsNullOrEmpty(path))
            return (null, false);

        // Normalize once so AssetDatabase APIs (which expect '/') see the same path validation did.
        path = path.Replace('\\', '/');

        if (!IsValidProjectAssetPath(path))
        {
            Debug.LogWarning($"Cannot create UI Animation Clip at '{path}': path must be under 'Assets/' or 'Packages/'.");
            return (null, false);
        }

        if (AssetDatabase.AssetPathExists(path))
        {
            // Overwrite is confirmed earlier by EditorUtility.SaveFilePanel*; the in-place mutation
            // below keeps GUID + sub-asset identity so inbound refs and Undo stay valid.
            var existingAsset = AssetDatabase.LoadMainAssetAtPath(path);
            if (existingAsset is UIAnimationClip existing)
            {
                ReplaceClipContentsInPlace(existing);
                return (existing, false);
            }

            var existingTypeName = existingAsset != null ? existingAsset.GetType().Name : "Unknown";
            Debug.LogWarning($"Cannot create UI Animation Clip at '{path}': an asset of type '{existingTypeName}' already exists. Pick a different name to avoid replacing an unrelated asset.");
            return (null, false);
        }

        return (CreateNewAsset(path), true);
    }

    static UIAnimationClip CreateNewAsset(string path)
    {
        var innerClip = BuildPristineInnerClip();
        var newClip = new UIAnimationClip { animationClip = innerClip };
        AssetDatabase.CreateAsset(newClip, path);
        innerClip.name = newClip.name; // Sub-asset name follows the parent, which is named by CreateAsset.
        // The inner clip is an implementation detail of the UIAnimationClip; hide it so it doesn't
        // show as a separate sub-asset row in the Project window or a second Inspector section.
        innerClip.hideFlags = HideFlags.HideInHierarchy;
        AssetDatabase.AddObjectToAsset(innerClip, newClip);
        AssetDatabase.SaveAssets();
        return AssetDatabase.LoadAssetAtPath<UIAnimationClip>(path);
    }

    // CopySerialized covers everything ClearCurves doesn't (events, frameRate, wrapMode, ...).
    static void ReplaceClipContentsInPlace(UIAnimationClip existing)
    {
        var inner = existing.animationClip;
        if (inner != null)
        {
            var pristine = BuildPristineInnerClip();
            try
            {
                Undo.RegisterCompleteObjectUndo(new UnityEngine.Object[] { existing, inner }, k_CreateAssetUndoName);
                var preservedName = inner.name; // CopySerialized would clobber the sub-asset name.
                EditorUtility.CopySerialized(pristine, inner);
                inner.name = preservedName;
                // Re-assert in case CopySerialized cleared the flag, and to retro-fit clips authored
                // before the inner clip was hidden.
                inner.hideFlags = HideFlags.HideInHierarchy;
                EditorUtility.SetDirty(inner);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(pristine);
            }
        }
        else
        {
            // Corrupt asset: sub-asset went missing; recreate it.
            Undo.RegisterCompleteObjectUndo(existing, k_CreateAssetUndoName);
            var freshInner = BuildPristineInnerClip();
            freshInner.name = existing.name;
            freshInner.hideFlags = HideFlags.HideInHierarchy;
            existing.animationClip = freshInner;
            AssetDatabase.AddObjectToAsset(freshInner, existing);
        }
        EditorUtility.SetDirty(existing);
        AssetDatabase.SaveAssetIfDirty(existing);
    }

    static AnimationClip BuildPristineInnerClip()
    {
        var clip = new AnimationClip();
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        return clip;
    }

    static bool IsValidProjectAssetPath(string path)
    {
        return !string.IsNullOrEmpty(path)
               && (path.StartsWith("Assets/", StringComparison.Ordinal)
                   || path.StartsWith("Packages/", StringComparison.Ordinal));
    }
}
