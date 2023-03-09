// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEngine
{
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    [NativeHeader("Runtime/Utilities/DiagnosticSwitch.h")]
    [NativeClass("DiagnosticSwitch", "struct DiagnosticSwitch;")]
    [NativeAsStruct]
    internal class DiagnosticSwitch
    {
        private IntPtr m_Ptr;

        private DiagnosticSwitch(IntPtr ptr)
        {
            m_Ptr = ptr;
        }

        // Keep this in sync with DiagnosticSwitch::SwitchFlags in C++
        [Flags]
        internal enum Flags
        {
            None = 0,
            CanChangeAfterEngineStart = (1 << 0),
            PropagateToAssetImportWorkerProcess = (1 << 1),
        }

        public extern string name { get; }
        public extern string description { get; }
        [NativeName("OwningModuleName")] public extern string owningModule { get; }
        public extern Flags flags { get; }

        public object value { get => GetScriptingValue(); set => SetScriptingValue(value, false); }
        [NativeName("ScriptingDefaultValue")] public extern object defaultValue { get; }
        [NativeName("ScriptingMinValue")] public extern object minValue { get; }
        [NativeName("ScriptingMaxValue")] public extern object maxValue { get; }
        public object persistentValue { get => GetScriptingPersistentValue(); set => SetScriptingValue(value, true); }
        [NativeName("ScriptingEnumInfo")] public extern EnumInfo enumInfo { get; }

        private extern object GetScriptingValue();
        private extern object GetScriptingPersistentValue();

        [NativeThrows]
        private extern void SetScriptingValue(object value, bool setPersistent);

        public bool isSetToDefault => Equals(persistentValue, defaultValue);
        public bool needsRestart => !Equals(value, persistentValue);

        public override string ToString()
        {
            var valueText = value == null ? "<null>" : value.ToString();
            return $"{name} = {valueText}";
        }

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(DiagnosticSwitch diagnosticSwitch) => diagnosticSwitch.m_Ptr;
            public static DiagnosticSwitch ConvertToManaged(IntPtr ptr) => new DiagnosticSwitch(ptr);
        }
    }
}
