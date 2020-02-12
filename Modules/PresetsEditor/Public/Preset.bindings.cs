// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
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

        public extern PropertyModification[] PropertyModifications
        {
            [NativeName("GetManagedPropertyModifications")]
            get;
        }

        public extern string[] excludedProperties { get; set; }

        public bool ApplyTo(Object target)
        {
            return ApplyTo(target, s_EmptyArray);
        }

        public extern bool ApplyTo([NotNull] Object target, string[] selectedPropertyPaths);

        public extern bool DataEquals([NotNull] Object target);

        public extern bool UpdateProperties([NotNull] Object source);

        public extern PresetType GetPresetType();

        public string GetTargetFullTypeName()
        {
            return GetPresetType().GetManagedTypeName();
        }

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

        [FreeFunction("GetDefaultPresetsForObject")]
        public static extern Preset[] GetDefaultPresetsForObject([NotNull] Object target);

        [Obsolete("Use GetDefaultPresetsForObject to get the full ordered list of default Presets that would be applied to that Object")]
        public static Preset GetDefaultForObject(Object target)
        {
            var defaults = GetDefaultPresetsForObject(target);
            return defaults.Length > 0 ? defaults[0] : null;
        }

        [Obsolete("Use GetDefaultPresetsForType to get the full list of default Presets for a given PresetType.")]
        public static Preset GetDefaultForPreset(Preset preset)
        {
            if (preset == null)
                throw new ArgumentNullException(nameof(preset));
            var defaults = GetDefaultPresetsForType(preset.GetPresetType());
            foreach (var defaultPreset in defaults)
            {
                if (defaultPreset.m_Filter == string.Empty)
                    return defaultPreset.m_Preset;
            }

            return null;
        }

        [FreeFunction("GetAllDefaultTypes")]
        public static extern PresetType[] GetAllDefaultTypes();

        [FreeFunction("GetDefaultPresetsForType")]
        public static extern DefaultPreset[] GetDefaultPresetsForType(PresetType type);

        [FreeFunction("SetDefaultPresetsForType")]
        public static extern bool SetDefaultPresetsForType(PresetType type, DefaultPreset[] presets);

        [Obsolete("Use SetDefaultPresetsForType instead.")]
        public static bool SetAsDefault(Preset preset)
        {
            if (preset == null)
                throw new ArgumentNullException(nameof(preset));
            return SetDefaultPresetsForType(preset.GetPresetType(), new[] {new DefaultPreset {m_Filter = string.Empty, m_Preset = preset}});
        }

        public static void RemoveFromDefault(Preset preset)
        {
            var type = preset.GetPresetType();
            var list = GetDefaultPresetsForType(type);
            var newList = list.Where(d => d.preset != preset);
            if (newList.Count() != list.Length)
                SetDefaultPresetsForType(type, newList.ToArray());
        }

        [Obsolete("Use PresetType.IsValidDefault instead.")]
        public static bool IsPresetExcludedFromDefaultPresets(Preset preset)
        {
            if (preset == null)
                throw new ArgumentNullException(nameof(preset));
            return !preset.GetPresetType().IsValidDefault();
        }

        [Obsolete("Use PresetType.IsValidDefault instead.")]
        public static bool IsObjectExcludedFromDefaultPresets(Object target)
        {
            return !new PresetType(target).IsValidDefault();
        }

        [Obsolete("Use PresetType.IsValid instead.")]
        public static bool IsObjectExcludedFromPresets(Object target)
        {
            return !new PresetType(target).IsValid();
        }
    }
}
