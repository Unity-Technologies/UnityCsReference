// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor.AssetImporters;

namespace UnityEditor.Search
{
    class SearchDatabaseTemplates
    {
        public static readonly string @default =
@"{
    ""name"": ""Assets"",
    ""roots"": [""Assets""],
    ""includes"": [],
    ""excludes"": [""Temp/"", ""External/""],
    ""options"": {
        ""types"": true,
        ""properties"": false,
        ""extended"": false,
        ""dependencies"": false
    },
    ""baseScore"": 999
}";

        public static readonly string assets =
@"{
    ""roots"": [],
    ""includes"": [],
    ""excludes"": [""Temp/"", ""External/""],
    ""options"": {
        ""types"": true,
        ""properties"": false,
        ""extended"": false,
        ""dependencies"": false
    },
    ""baseScore"": 100
}";

        public static readonly string packages =
@"{
    ""roots"": [""Packages""],
    ""includes"": [],
    ""excludes"": [],
    ""options"": {
        ""types"": true,
        ""properties"": false,
        ""extended"": false,
        ""dependencies"": false
    },
    ""baseScore"": 9999
}";

        public static readonly string prefabs = @"{
    ""type"": ""prefab"",
    ""roots"": [],
    ""includes"": ["".prefab""],
    ""excludes"": [],
    ""options"": {
        ""types"": true,
        ""properties"": true,
        ""extended"": true,
        ""dependencies"": false
    },
    ""baseScore"": 150
}";
        public static readonly string scenes = @"{
    ""type"": ""scene"",
    ""roots"": [],
    ""includes"": ["".unity""],
    ""excludes"": [],
    ""options"": {
        ""types"": true,
        ""properties"": false,
        ""extended"": true,
        ""dependencies"": false
    },
    ""baseScore"": 155
}";

        public static readonly Dictionary<string, string> all = new Dictionary<string, string>()
        {
            { "Assets", assets },
            { "Packages", packages },
            { "Prefabs", prefabs },
            { "Scenes", scenes },
            { "_Default", @default }
        };
    }

    [ExcludeFromPreset, ScriptedImporter(version: SearchDatabase.version, ext: "index", importQueueOffset: 1999)] // kImportOrderPrefabs = 1500
    class SearchDatabaseImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            try
            {
                var db = ScriptableObject.CreateInstance<SearchDatabase>();
                db.Import(ctx.assetPath);
                ctx.AddObjectToAsset("index", db);
                ctx.SetMainObject(db);

                ctx.DependsOnCustomDependency(nameof(CustomObjectIndexerAttribute));

                hideFlags |= HideFlags.HideInInspector;
            }
            catch (SearchDatabaseException ex)
            {
                ctx.LogImportError(ex.Message, AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(ex.guid)));
            }
        }

        public static string CreateTemplateIndex(string template, string path, string name = null, string settings = null)
        {
            if (settings == null && !SearchDatabaseTemplates.all.ContainsKey(template))
                return null;

            var dirPath = path;
            var templateContent = settings ?? SearchDatabaseTemplates.all[template];

            if (File.Exists(path))
            {
                dirPath = Path.GetDirectoryName(path);
                if (Selection.assetGUIDs.Length > 1)
                    path = dirPath;
            }

            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);

            var indexFileName = string.IsNullOrEmpty(name) ? Path.GetFileNameWithoutExtension(path) : name;
            var indexPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(dirPath, $"{indexFileName}.index")).Replace("\\", "/");

            SearchAnalytics.SendEvent(null, SearchAnalytics.GenericEventType.CreateIndexFromTemplate, template);

            File.WriteAllText(indexPath, templateContent);
            AssetDatabase.ImportAsset(indexPath);
            Providers.AssetProvider.reloadAssetIndexes = true;

            return indexPath;
        }

        private static bool ValidateTemplateIndexCreation<T>() where T : UnityEngine.Object
        {
            var asset = Selection.activeObject as T;
            if (asset)
                return true;
            return CreateIndexProjectValidation();
        }

        private static string GetSelectionFolderPath()
        {
            var folderPath = "Assets";
            if (Selection.activeObject != null)
                folderPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (File.Exists(folderPath))
                folderPath = Path.GetDirectoryName(folderPath);
            return folderPath;
        }

        [MenuItem("Assets/Create/Search/Assets Index")]
        internal static void CreateIndexProject()
        {
            CreateTemplateIndex("Assets", GetSelectionFolderPath());
        }

        [MenuItem("Assets/Create/Search/Assets Index", validate = true)]
        internal static bool CreateIndexProjectValidation()
        {
            return Directory.Exists(GetSelectionFolderPath());
        }

        [MenuItem("Assets/Create/Search/Prefabs Index")]
        internal static void CreateIndexPrefab()
        {
            CreateTemplateIndex("Prefabs", GetSelectionFolderPath());
        }

        [MenuItem("Assets/Create/Search/Prefabs Index", validate = true)]
        internal static bool CreateIndexPrefabValidation()
        {
            return ValidateTemplateIndexCreation<GameObject>();
        }

        [MenuItem("Assets/Create/Search/Scenes Index")]
        internal static void CreateIndexScene()
        {
            CreateTemplateIndex("Scenes", GetSelectionFolderPath());
        }

        [MenuItem("Assets/Create/Search/Scenes Index", validate = true)]
        internal static bool CreateIndexSceneValidation()
        {
            return ValidateTemplateIndexCreation<SceneAsset>();
        }
    }
}
