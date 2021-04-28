// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;

namespace UnityEditor.Search
{
    internal static class Icons
    {
        public static string iconFolder = $"Icons/QuickSearch";
        public static Texture2D quicksearch = EditorGUIUtility.FindTexture("Search Icon");
        public static Texture2D shortcut = EditorGUIUtility.FindTexture("Shortcut Icon");
        public static Texture2D staticAPI = EditorGUIUtility.FindTexture("cs Script Icon");
        public static Texture2D settings = EditorGUIUtility.FindTexture("Settings Icon");
        public static Texture2D favorite = EditorGUIUtility.FindTexture("Favorite Icon");
        public static Texture2D store = EditorGUIUtility.FindTexture("AssetStore Icon");
        public static Texture2D help = EditorGUIUtility.LoadIcon($"Icons/_Help.png");
        public static Texture2D clear = EditorGUIUtility.LoadIcon("StyleSheets/Northstar/Images/clear.png");
        public static Texture2D quickSearchWindow = EditorGUIUtility.LoadIcon($"{iconFolder}/SearchWindow.png");
        public static Texture2D more = EditorGUIUtility.LoadIcon($"{iconFolder}/more.png");
        public static Texture2D logInfo = EditorGUIUtility.LoadIcon("UIPackageResources/Images/console.infoicon.png");
        public static Texture2D logWarning = EditorGUIUtility.LoadIcon("UIPackageResources/Images/cconsole.warnicon.png");
        public static Texture2D logError = EditorGUIUtility.LoadIcon("UIPackageResources/Images/console.erroricon.png");
        public static Texture2D packageInstalled = EditorGUIUtility.LoadIcon($"{iconFolder}/package_installed.png");
        public static Texture2D packageUpdate = EditorGUIUtility.LoadIcon($"{iconFolder}/package_update.png");
    }
}
