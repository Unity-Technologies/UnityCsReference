// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    [NativeType(Header = "Modules/PresetsEditor/Preset.h")]
    [UsedByNativeCode]
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

        public extern string GetTargetTypeName();

        public extern bool IsValid();

        [FreeFunction]
        public static extern bool IsExcludedFromPresets(Object reference);

        public extern bool CanBeAppliedTo(Object target);
    }
}
