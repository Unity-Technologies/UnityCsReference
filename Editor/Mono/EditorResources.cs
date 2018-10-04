// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.StyleSheets;
using UnityEngine;
using UnityEngine.Internal;

namespace UnityEditor.Experimental
{
    [ExcludeFromDocs]
    public partial class EditorResources
    {
        // Global editor styles
        private static StyleCatalog s_StyleCatalog;

        static EditorResources()
        {
            s_StyleCatalog = new StyleCatalog();
            s_StyleCatalog.AddPaths(GetDefaultStyleCatalogPaths());
            s_StyleCatalog.Refresh();
        }

        private static bool IsEditorStyleSheet(string path)
        {
            var pathLowerCased = path.ToLower();
            return pathLowerCased.Contains("/editor/") && (pathLowerCased.EndsWith("common.uss") ||
                pathLowerCased.EndsWith(EditorGUIUtility.isProSkin ? "dark.uss" : "light.uss"));
        }

        private static List<string> GetDefaultStyleCatalogPaths()
        {
            bool useDarkTheme = EditorGUIUtility.isProSkin;
            var catalogFiles = new List<string>
            {
                "StyleSheets/Extensions/common.uss",
                useDarkTheme ? "StyleSheets/Extensions/dark.uss" : "StyleSheets/Extensions/light.uss"
            };

            return catalogFiles;
        }

        internal static void BuildCatalog()
        {
            s_StyleCatalog = new StyleCatalog();
            var paths = GetDefaultStyleCatalogPaths();

            foreach (var editorUssPath in AssetDatabase.GetAllAssetPaths().Where(IsEditorStyleSheet))
                paths.Add(editorUssPath);

            s_StyleCatalog.AddPaths(paths);
            s_StyleCatalog.Refresh();
        }

        // Returns the editor resources absolute file system path.
        public static string dataPath => Application.dataPath;

        // Resolve an editor resource asset path.
        public static string ExpandPath(string path)
        {
            return path.Replace("\\", "/");
        }

        // Returns the full file system path of an editor resource asset path.
        public static string GetFullPath(string path)
        {
            if (File.Exists(path))
                return path;
            return new FileInfo(ExpandPath(path)).FullName;
        }

        // Checks if an editor resource asset path exists.
        public static bool Exists(string path)
        {
            if (File.Exists(path))
                return true;
            return File.Exists(ExpandPath(path));
        }

        // Loads an editor resource asset.
        public static T Load<T>(string assetPath, bool isRequired = true) where T : UnityEngine.Object
        {
            var obj = Load(assetPath, typeof(T));
            if (!obj && isRequired)
                throw new FileNotFoundException("Could not find editor resource " + assetPath);
            return obj as T;
        }

        // Returns a globally defined style element by name.
        internal static StyleBlock GetStyle(string selectorName, params StyleState[] states) { return s_StyleCatalog.GetStyle(selectorName, states); }
        internal static StyleBlock GetStyle(int selectorKey, params StyleState[] states) { return s_StyleCatalog.GetStyle(selectorKey, states); }

        // Loads an USS asset into the global style catalog
        internal static void LoadStyles(string ussPath)
        {
            s_StyleCatalog.Load(ussPath);
        }

        // Creates a new style catalog from a set of USS assets.
        internal static StyleCatalog LoadCatalog(string[] ussPaths)
        {
            var catalog = new StyleCatalog();
            foreach (var path in ussPaths)
                catalog.AddPath(path);
            catalog.Refresh();
            return catalog;
        }
    }
}
