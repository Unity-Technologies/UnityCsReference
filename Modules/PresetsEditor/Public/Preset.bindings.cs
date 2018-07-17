// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using Object = UnityEngine.Object;

namespace UnityEditor.Presets
{
    [NativeType(Header = "Modules/PresetsEditor/Public/Preset.h")]
    [NativeHeader("Modules/PresetsEditor/Public/PresetManager.h")]
    [UsedByNativeCode]
    [ExcludeFromPreset]
    public sealed class Preset : Object
    {
        static readonly string[] s_EmptyArray = new string[0];
        public Preset(Object source)
        {
            Internal_Create(this, source);
        }

        static extern void Internal_Create([Writable] Preset notSelf, [NotNull] Object source);

        public extern PropertyModification[] PropertyModifications { get; }

        public bool ApplyTo(Object target)
        {
            return ApplyTo(target, s_EmptyArray);
        }

        public extern bool ApplyTo([NotNull] Object target, string[] selectedPropertyPaths);

        public extern bool DataEquals([NotNull] Object target);

        public extern bool UpdateProperties([NotNull] Object source);

        public extern string GetTargetFullTypeName();

        public string GetTargetTypeName()
        {
            string fullTypeName = GetTargetFullTypeName();
            int lastDot = fullTypeName.LastIndexOf(".");
            if (lastDot == -1)
                lastDot = fullTypeName.LastIndexOf(":");
            if (lastDot != -1)
                return fullTypeName.Substring(lastDot + 1);
            return fullTypeName;
        }

        public extern bool IsValid();

        public extern bool CanBeAppliedTo(Object target);

        // Components returned by this method will have their GameObject hideFlags set with DontAllowDestruction.
        // The HideFlags must be set to HideFlags.None before calling Destroy or DestroyImmediate on it.
        internal extern Object GetReferenceObject();

        [FreeFunction("PresetManager::GetDefaultForObject")]
        public static extern Preset GetDefaultForObject([NotNull] Object target);

        [FreeFunction("PresetManager::GetDefaultForPreset")]
        public static extern Preset GetDefaultForPreset([NotNull] Preset preset);

        [FreeFunction("PresetManager::SetAsDefault")]
        public static extern bool SetAsDefault([NotNull] Preset preset);

        [FreeFunction("PresetManager::RemoveFromDefault")]
        public static extern void RemoveFromDefault([NotNull] Preset preset);

        [FreeFunction]
        public static extern bool IsPresetExcludedFromDefaultPresets(Preset preset);

        [FreeFunction]
        public static extern bool IsObjectExcludedFromDefaultPresets(Object target);

        [FreeFunction]
        public static extern bool IsObjectExcludedFromPresets(Object reference);

        [FreeFunction]
        internal static extern bool IsExcludedFromPresetsByTypeID(int nativeTypeID);
    }
}
