// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor
{
    // Helper class to access Unity documentation.
    [NativeHeader("Editor/Src/Panels/HelpPanel.h")]
    [NativeHeader("Editor/Platform/Interface/EditorUtility.h")]
    public class Help
    {
        // Is there a help page for this object?
        public static bool HasHelpForObject(Object obj) { return HasHelpForObject(obj, true); }

        // Intentionally internal. Extra argument only used to make doc authoring easier.
        [FreeFunction]
        internal static extern bool HasHelpForObject(Object obj, bool defaultToMonoBehaviour);

        // Intentionally internal.
        internal static string GetNiceHelpNameForObject(Object obj)
        {
            return GetNiceHelpNameForObject(obj, true);
        }

        // Intentionally internal.
        [FreeFunction]
        internal static extern string GetNiceHelpNameForObject(Object obj, bool defaultToMonoBehaviour);

        public static string GetHelpURLForObject(Object obj)
        {
            return GetHelpURLForObject(obj, true);
        }

        [FreeFunction]
        private static extern string GetHelpURLForObject(Object obj, bool defaultToMonoBehaviour);
        // Show help page for this object.
        [FreeFunction]
        public static extern void ShowHelpForObject(Object obj);
        // Show a help page.
        [FreeFunction("ShowNamedHelp")]
        public static extern void ShowHelpPage(string page);
        // Open /url/ in the default web browser.
        [FreeFunction("OpenURLInWebbrowser")]
        public static extern void BrowseURL(string url);
    }
}
