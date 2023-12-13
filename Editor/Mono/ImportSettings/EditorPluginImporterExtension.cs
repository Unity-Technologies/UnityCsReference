// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using UnityEditor.Modules;

namespace UnityEditor
{
    internal class EditorPluginImporterExtension : DefaultPluginImporterExtension
    {
        internal enum EditorPluginCPUArchitecture
        {
            AnyCPU,
            [InspectorName("Intel 64-bit")]
            x86_64,
            [InspectorName("Apple silicon / Arm64")]
            ARM64
        }

        internal enum EditorPluginOSArchitecture
        {
            AnyOS,
            [InspectorName("macOS")]
            OSX,
            Windows,
            Linux
        }

        internal class EditorProperty : Property
        {
            public EditorProperty(GUIContent name, string key, object defaultValue)
                : base(name, key, defaultValue, BuildPipeline.GetEditorTargetName())
            {
            }

            internal override void Reset(PluginImporterInspector inspector)
            {
                string valueString = inspector.importer.GetEditorData(key);
                ParseStringValue(inspector, valueString);
            }

            internal override void Apply(PluginImporterInspector inspector)
            {
                inspector.importer.SetEditorData(key, value.ToString());
            }
        }

        internal class CPUProperty : Property
        {
            private readonly Func<Enum, bool> validArch;

            public CPUProperty(GUIContent name, string key, EditorPluginCPUArchitecture defaultValue, Func<Enum, bool> validArch)
                : base(name, key, defaultValue, BuildPipeline.GetEditorTargetName())
            {
                this.validArch = validArch;
            }

            internal override void Reset(PluginImporterInspector inspector)
            {
                string valueString = inspector.importer.GetEditorData(key);
                ParseStringValue(inspector, valueString);
            }

            internal override void Apply(PluginImporterInspector inspector)
            {
                inspector.importer.SetEditorData(key, value.ToString());
            }

            internal override void OnGUI(PluginImporterInspector inspector)
            {
                value = EditorGUILayout.EnumPopup(name, (Enum)value, validArch, false);
            }
        }

        private CPUProperty editorCPUProperty;
        private EditorProperty editorOSProperty;

        public EditorPluginImporterExtension() : base(null)
        {
            editorCPUProperty = new CPUProperty(EditorGUIUtility.TrTextContent("CPU", "The processor architectiure that this plugin is compatible with"), "CPU", EditorPluginCPUArchitecture.AnyCPU, (Enum e) => CanSelectArch(e));
            editorOSProperty = new EditorProperty(EditorGUIUtility.TrTextContent("OS", "The Editor operating system that this plugin is compatible with"), "OS", EditorPluginOSArchitecture.AnyOS);

            properties = new Property[] { editorOSProperty, editorCPUProperty };
        }

        public override void OnPlatformSettingsGUI(PluginImporterInspector inspector)
        {
            EditorGUI.BeginChangeCheck();

            editorOSProperty.OnGUI(inspector);
            editorCPUProperty.OnGUI(inspector);

            if (EditorGUI.EndChangeCheck())
            {
                if (!CanSelectArch(editorCPUProperty.value as Enum))
                    editorCPUProperty.value = EditorPluginCPUArchitecture.AnyCPU;

                hasModified = true;
            }
        }

        private bool CanSelectArch(Enum value)
        {
            var arch = (EditorPluginCPUArchitecture)value;
            var os = (EditorPluginOSArchitecture)editorOSProperty.value;

            switch (os)
            {
                case EditorPluginOSArchitecture.AnyOS:
                    return arch == EditorPluginCPUArchitecture.AnyCPU;
                case EditorPluginOSArchitecture.OSX:
                case EditorPluginOSArchitecture.Windows:
                    return true;
                case EditorPluginOSArchitecture.Linux:
                    return arch != EditorPluginCPUArchitecture.ARM64;
                default:
                    return false;
            }
        }
    }
}
