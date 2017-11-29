// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using Object = UnityEngine.Object;

namespace UnityEditor.Presets
{
    [NativeType(Header = "Modules/PresetsEditor/Preset.h")]
    [NativeHeader("Modules/PresetsEditor/PresetManager.h")]
    [UsedByNativeCode]
    [ExcludeFromPreset]
    public sealed class Preset : Object
    {
        public Preset(Object source)
        {
            Internal_Create(this, source);
        }

        static extern void Internal_Create([Writable] Preset notSelf, [NotNull] Object source);

        public extern PropertyModification[] PropertyModifications { get; }

        public extern bool ApplyTo([NotNull] Object target);

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
