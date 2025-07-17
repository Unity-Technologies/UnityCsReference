// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;


namespace UnityEditor.Search
{
    static class Icons
    {
        public static string iconFolder = $"Icons/QuickSearch";
        public static Texture2D quicksearch = EditorGUIUtility.LoadIcon("Search Icon");
        public static Texture2D shortcut = EditorGUIUtility.LoadIcon("Shortcut Icon");
        public static Texture2D staticAPI = EditorGUIUtility.LoadIcon("cs Script Icon");
        public static Texture2D settings = EditorGUIUtility.LoadIcon("Settings Icon");
        public static Texture2D favorite = EditorGUIUtility.LoadIcon("Favorite Icon");
        public static Texture2D store = EditorGUIUtility.LoadIcon("AssetStore Icon");
        public static Texture2D help = EditorGUIUtility.LoadIcon($"Icons/_Help.png");
        public static Texture2D clear = EditorGUIUtility.LoadIcon("StyleSheets/Northstar/Images/clear.png");
        public static Texture2D quickSearchWindow = EditorGUIUtility.LoadIcon($"{iconFolder}/SearchWindow.png");
        public static Texture2D more = EditorGUIUtility.LoadIcon($"{iconFolder}/more.png");
        public static Texture2D logInfo = EditorGUIUtility.LoadIcon("UIPackageResources/Images/console.infoicon.png");
        public static Texture2D logWarning = EditorGUIUtility.LoadIcon("UIPackageResources/Images/cconsole.warnicon.png");
        public static Texture2D logError = EditorGUIUtility.LoadIcon("UIPackageResources/Images/console.erroricon.png");
        public static Texture2D packageInstalled = EditorGUIUtility.LoadIcon($"{iconFolder}/package_installed.png");
        public static Texture2D packageUpdate = EditorGUIUtility.LoadIcon($"{iconFolder}/package_update.png");
        public static Texture2D dependencies = EditorGUIUtility.LoadIcon("UnityEditor.FindDependencies");
        public static Texture2D toggles = EditorGUIUtility.LoadIcon("MoreOptions");
    }
}
