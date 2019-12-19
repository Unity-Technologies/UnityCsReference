// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEditor.VisualStudioIntegration;
using UnityEngine.Bindings;
using UnityEngine.Internal;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    // Must be a struct in order to have correct comparison behaviour
    [StructLayout(LayoutKind.Sequential)]
    [ExcludeFromDocs]
    public struct ExternalVersionControl
    {
        private readonly string m_Value;

        public static readonly string Disabled = "Hidden Meta Files";
        public static readonly string AutoDetect = "Auto detect";
        public static readonly string Generic = "Visible Meta Files";


        [Obsolete("Asset Server VCS support has been removed.")]
        public static readonly string AssetServer = "Asset Server";

        public ExternalVersionControl(string value)
        {
            m_Value = value;
        }

        // User-defined conversion
        public static implicit operator string(ExternalVersionControl d)
        {
            return d.ToString();
        }

        // User-defined conversion
        public static implicit operator ExternalVersionControl(string d)
        {
            return new ExternalVersionControl(d);
        }

        public override string ToString()
        {
            return m_Value;
        }
    }

    [NativeHeader("Editor/Src/VersionControlSettings.h")]
    [NativeHeader("Editor/Src/EditorUserSettings.h")]
    public sealed class VersionControlSettings : Object
    {
        private VersionControlSettings()
        {
        }

        [StaticAccessor("GetVersionControlSettings()", StaticAccessorType.Dot)]
        [ExcludeFromDocs]
        public static extern string mode
        {
            [NativeMethod("GetMode")]
            get;
            [NativeMethod("SetMode")]
            set;
        }

        [StaticAccessor("GetEditorUserSettings()", StaticAccessorType.Dot)]
        private static extern string GetConfigValue(string name);

        [StaticAccessor("GetEditorUserSettings()", StaticAccessorType.Dot)]
        private static extern void SetConfigValue(string name, string value);
    }
}
