// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.Scripting;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.RestService
{
    internal class ProjectStateRestHandler : JSONHandler
    {
        protected override JSONValue HandleGet(Request request, JSONValue payload)
        {
            AssetDatabase.Refresh();
            return JsonForProject();
        }

        public class Island
        {
            public MonoIsland MonoIsland { get; set; }
            public String Name { get; set; }
            public List<String> References { get; set; }
        }

        private static JSONValue JsonForProject()
        {
            var islands = EditorCompilationInterface.GetAllMonoIslands().Select(i => new Island
            {
                MonoIsland = i,
                Name = Path.GetFileNameWithoutExtension(i._output),
                References = i._references.ToList()
            }).ToList();

            // Mono islands have references to each others output location. For MD we want to have the reference
            // be to the actual project, so we are going to update the references here
            foreach (Island island in islands)
            {
                var toAdd = new List<string>();
                var toRemove = new List<string>();

                foreach (var reference in island.References)
                {
                    var refName = Path.GetFileNameWithoutExtension(reference);

                    if (reference.StartsWith("Library/") && islands.Any(i => i.Name == refName))
                    {
                        toAdd.Add(refName);
                        toRemove.Add(reference);
                    }

                    if (reference.EndsWith("/UnityEditor.dll") || reference.EndsWith("/UnityEngine.dll")
                        || reference.EndsWith("\\UnityEditor.dll") || reference.EndsWith("\\UnityEngine.dll"))
                        toRemove.Add(reference);
                }

                island.References.Add(InternalEditorUtility.GetEditorAssemblyPath());
                island.References.Add(InternalEditorUtility.GetEngineAssemblyPath());

                foreach (var a in toAdd)
                    island.References.Add(a);

                foreach (var r in toRemove)
                    island.References.Remove(r);
            }

            var files = islands.SelectMany(i => i.MonoIsland._files).Concat(GetAllSupportedFiles()).Distinct().ToArray();
            var emptyDirectories = RelativeToProjectPath(FindEmptyDirectories(AssetsPath, files));

            var result = new JSONValue();
            result["islands"] = new JSONValue(islands.Select(i => JsonForIsland(i)).Where(i2 => !i2.IsNull()).ToList());
            result["basedirectory"] = ProjectPath;

            var assetDatabase = new JSONValue();
            assetDatabase["files"] = ToJSON(files);
            assetDatabase["emptydirectories"] = ToJSON(emptyDirectories);

            result["assetdatabase"] = assetDatabase;
            return result;
        }

        static bool IsSupportedExtension(string extension)
        {
            if (extension.StartsWith("."))
                extension = extension.Substring(1);
            var all = EditorSettings.projectGenerationBuiltinExtensions.Concat(EditorSettings.projectGenerationUserExtensions);
            return all.Any(s => string.Equals(s, extension, StringComparison.InvariantCultureIgnoreCase));
        }

        private static IEnumerable<string> GetAllSupportedFiles()
        {
            return AssetDatabase.GetAllAssetPaths().Where(asset => IsSupportedExtension(Path.GetExtension(asset)));
        }

        private static JSONValue JsonForIsland(Island island)
        {
            if (island.Name == "UnityEngine" || island.Name == "UnityEditor")
                return null;

            var result = new JSONValue();
            result["name"] = island.Name;
            result["language"] = island.Name.Contains("Boo") ? "Boo" : island.Name.Contains("UnityScript") ? "UnityScript" : "C#";
            result["files"] = ToJSON(island.MonoIsland._files);
            result["defines"] = ToJSON(island.MonoIsland._defines);
            result["references"] = ToJSON(island.MonoIsland._references);
            result["basedirectory"] = ProjectPath;
            return result;
        }

        private static void FindPotentialEmptyDirectories(string path, ICollection<string> result)
        {
            var directories = Directory.GetDirectories(path);

            if (directories.Length == 0)
            {
                result.Add(path.Replace('\\', '/'));
                return;
            }

            foreach (var directory in directories)
                FindPotentialEmptyDirectories(directory, result);
        }

        private static IEnumerable<string> FindPotentialEmptyDirectories(string path)
        {
            var result = new List<string>();
            FindPotentialEmptyDirectories(path, result);
            return result;
        }

        private static string[] FindEmptyDirectories(string path, string[] files)
        {
            var potentialEmptyDirectories = FindPotentialEmptyDirectories(path);
            return potentialEmptyDirectories.Where(d => !files.Any(f => f.StartsWith(d))).ToArray();
        }

        private static string[] RelativeToProjectPath(string[] paths)
        {
            var projectPath = ProjectPath.EndsWith("/") ? ProjectPath : ProjectPath + "/";
            return paths.Select(d => d.StartsWith(projectPath) ? d.Substring(projectPath.Length) : d).ToArray();
        }

        static string ProjectPath
        {
            get { return Path.GetDirectoryName(Application.dataPath); }
        }

        static string AssetsPath
        {
            get { return ProjectPath + "/Assets"; }
        }

        internal static void Register()
        {
            Router.RegisterHandler("/unity/projectstate", new ProjectStateRestHandler());
        }
    }
}
