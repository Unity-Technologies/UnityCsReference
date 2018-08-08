// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Runtime.InteropServices;

namespace UnityEditor.VersionControl
{
    [NativeHeader("Editor/Src/VersionControl/VCConfigField.h")]
    [NativeHeader("Editor/Src/VersionControl/VC_bindings.h")]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public class ConfigField
    {
        // The bindings generator will set the instance pointer in this field
        IntPtr m_Self;

        internal ConfigField() {}

        ~ConfigField()
        {
            Dispose();
        }

        public void Dispose()
        {
            Destroy(m_Self);
            m_Self = IntPtr.Zero;
        }

        [FreeFunction("VersionControlBindings::ConfigField::Destroy", IsThreadSafe = true)]
        static extern void Destroy(IntPtr configField);

        public extern string name { get; }
        public extern string label { get; }
        public extern string description { get; }

        public extern bool isRequired
        {
            [NativeMethod("IsRequired")]
            get;
        }

        public extern bool isPassword
        {
            [NativeMethod("IsPassword")]
            get;
        }
    }

    [NativeHeader("Editor/Src/VersionControl/VCPlugin.h")]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public class Plugin
    {
        // The bindings generator will set the instance pointer in this field
        IntPtr m_Self;

        internal Plugin() {}

        ~Plugin()
        {
            Dispose();
        }

        public void Dispose()
        {
            Destroy(m_Self);
            m_Self = IntPtr.Zero;
        }

        [FreeFunction("VersionControlBindings::Plugin::Destroy", IsThreadSafe = true)]
        static extern void Destroy(IntPtr plugin);

        static public extern Plugin[] availablePlugins
        {
            [FreeFunction("VersionControlBindings::Plugin::GetAvailablePlugins")]
            get;
        }

        public extern string name { get; }
        public ConfigField[] configFields
        {
            get
            {
                return GetConfigFields(m_Self);
            }
        }

        [FreeFunction("VersionControlBindings::Plugin::GetConfigFields")]
        static extern ConfigField[] GetConfigFields(IntPtr plugin);
    }
}
