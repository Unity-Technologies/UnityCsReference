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
            x86,
            x86_64
        };

        internal enum EditorPluginOSArchitecture
        {
            AnyOS,
            OSX,
            Windows,
            Linux
        };

        EditorPluginCPUArchitecture cpu;
        EditorPluginOSArchitecture os;

        public EditorPluginImporterExtension() : base(GetProperties())
        {
        }

        private static Property[] GetProperties()
        {
            return new[]
            {
                new Property(EditorGUIUtility.TextContent("CPU|Is plugin compatible with 32bit or 64bit Editor?"), "CPU", EditorPluginCPUArchitecture.AnyCPU, BuildPipeline.GetEditorTargetName()),
                new Property(EditorGUIUtility.TextContent("OS|Is plugin compatible with Windows, OS X or Linux Editor?"), "OS", EditorPluginOSArchitecture.AnyOS, BuildPipeline.GetEditorTargetName()),
            };
        }
    }
}
