// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.UIElements
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal static class UXMLConstants
    {
        const string k_WindowsNewlineChar = "\r\n";
        public const string UnixNewlineChar = "\n";

        public static readonly string UxmlEngineNamespaceDefaultPrefix = "ui";
        public static readonly string UxmlEditorNamespaceDefaultPrefix = "uie";
        public static readonly string UxmlEngineNamespace = "UnityEngine.UIElements";
        public static readonly string UxmlEditorNamespace = "UnityEditor.UIElements";
        public static readonly string UxmlRootElementTypeName = "UXML";
        public static readonly string TemplateTypeName = "Template";
        public static readonly string StyleTypeName = "Style";
        public static readonly string AttributeOverridesTypeName = "AttributeOverrides";
        public static readonly string NameAttributeName = "name";
        public static readonly string SrcAttributeName = "src";

        public static string newlineCharFromEditorSettings
        {
            get
            {
                string preferredLineEndings;
                switch (EditorSettings.lineEndingsForNewScripts)
                {
                    case LineEndingsMode.OSNative:
                        if (Application.platform == RuntimePlatform.WindowsEditor)
                            preferredLineEndings = k_WindowsNewlineChar;
                        else
                            preferredLineEndings = UnixNewlineChar;
                        break;
                    case LineEndingsMode.Unix:
                        preferredLineEndings = UnixNewlineChar;
                        break;
                    case LineEndingsMode.Windows:
                        preferredLineEndings = k_WindowsNewlineChar;
                        break;
                    default:
                        preferredLineEndings = UnixNewlineChar;
                        break;
                }
                return preferredLineEndings;
            }
        }
    }
}
