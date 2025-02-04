// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace UnityEditor.Scripting.ScriptCompilation
{
    internal interface IAssemblyGraphBuilder
    {
        Dictionary<CustomScriptAssembly, List<string>> Match(
            IReadOnlyCollection<string> sources, bool logWarningOnMiss = true);
    }

    internal class AssemblyGraphBuilder : IAssemblyGraphBuilder
    {
        private readonly string _projectPath;
        private readonly CustomScriptAssembly _globalAssemblyDefinition;
        private readonly CustomScriptAssembly _editorAssebmlyDefinition;
        private readonly CustomScriptAssembly _globalFirstpassAssemblyDefinition;
        private readonly CustomScriptAssembly _editorFirstpassAssemblyDefinition;
        static readonly string _guidFormat = "N";

        private readonly CustomScriptAssemblyReference[] _globalFirstpassAssemblyReferences;

        private PathMultidimensionalDivisionTree<CustomScriptAssembly> _npp =
            new PathMultidimensionalDivisionTree<CustomScriptAssembly>();

        public AssemblyGraphBuilder(string projectPath)
        {
            _projectPath = projectPath;
            _globalAssemblyDefinition = new CustomScriptAssembly
            {
                Name = "Assembly-CSharp",
                PathPrefix = projectPath,
                FilePath = Path.Combine(projectPath, "main.asmdef"),
                GUID = CreateNewUnityGuid(),
                IsPredefined = true,
            };

            _editorAssebmlyDefinition = new CustomScriptAssembly
            {
                Name = "Assembly-CSharp-Editor",
                PathPrefix = Path.Combine(projectPath, "Editor"),
                FilePath = Path.Combine(projectPath, "Editor/main-editor.asmdef"),
                GUID = CreateNewUnityGuid(),
                IsPredefined = true,
            };

            _globalFirstpassAssemblyDefinition = new CustomScriptAssembly
            {
                Name = "Assembly-CSharp-firstpass",
                PathPrefix = Path.Combine(projectPath, "Plugins"),
                FilePath = Path.Combine(projectPath, "Plugins/firstpass.asmdef"),
                GUID = CreateNewUnityGuid(),
                IsPredefined = true,
            };

            _editorFirstpassAssemblyDefinition = new CustomScriptAssembly
            {
                Name = "Assembly-CSharp-Editor-firstpass",
                PathPrefix = Path.Combine(projectPath, "Plugins/Editor"),
                FilePath = Path.Combine(projectPath, "Plugins/Editor/firstpass-editor.asmdef"),
                GUID = CreateNewUnityGuid(),
                IsPredefined = true,
            };

            _globalFirstpassAssemblyReferences = new[]
            {
                CustomScriptAssemblyReference.FromPathAndReference(
                    Path.Combine(projectPath, "standard assets/standard assets.asmref"),
                    _globalFirstpassAssemblyDefinition.Name),
                CustomScriptAssemblyReference.FromPathAndReference(
                    Path.Combine(projectPath, "pro standard assets/pro standard assets.asmref"),
                    _globalFirstpassAssemblyDefinition.Name),
                CustomScriptAssemblyReference.FromPathAndReference(
                    Path.Combine(projectPath, "iphone standard assets/iphone standard assets.asmref"),
                    _globalFirstpassAssemblyDefinition.Name),
            };
        }

        public void Initialize(IReadOnlyCollection<CustomScriptAssembly> assemblies,
            IReadOnlyCollection<CustomScriptAssemblyReference> customScriptAssemblyReferences)
        {
            var assemblyByNameLookup = assemblies.ToDictionary(x => x.Name, x => x);
            var assemblyByGuidLookup = assemblies.ToDictionary(x => x.GUID, x => x);

            bool rootOverridden = assemblies.Any(x => AssetPath.ComparePaths(x.PathPrefix, _projectPath));
            if (!rootOverridden)
            {
                _npp.Insert(_globalAssemblyDefinition.PathPrefix, _globalAssemblyDefinition);
                _npp.Insert(_editorAssebmlyDefinition.PathPrefix, _editorAssebmlyDefinition);
                _npp.Insert(_globalFirstpassAssemblyDefinition.PathPrefix, _globalFirstpassAssemblyDefinition);
                _npp.Insert(_editorFirstpassAssemblyDefinition.PathPrefix, _editorFirstpassAssemblyDefinition);
            }

            foreach (var assemblyDef in assemblies)
            {
                _npp.Insert(assemblyDef.PathPrefix, assemblyDef);
            }

            if (!rootOverridden)
            {
                foreach (var globalFirstpassAssemblyReference in _globalFirstpassAssemblyReferences)
                {
                    _npp.Insert(globalFirstpassAssemblyReference.PathPrefix, _globalFirstpassAssemblyDefinition);
                }
            }

            foreach (var assemblyReference in customScriptAssemblyReferences)
            {
                CustomScriptAssembly foundAssemblyDef = null;
                var foundAssemblyDefinition = GUIDReference.IsGUIDReference(assemblyReference.Reference)
                    ? assemblyByGuidLookup.TryGetValue(GUIDReference.GUIDReferenceToGUID(assemblyReference.Reference),
                        out foundAssemblyDef)
                    : assemblyByNameLookup.TryGetValue(assemblyReference.Reference, out foundAssemblyDef);

                if (foundAssemblyDefinition)
                {
                    _npp.Insert(assemblyReference.PathPrefix, foundAssemblyDef);
                }
                else
                {
                    Console.WriteLine(
                        $"Assembly reference {assemblyReference.FilePath} has no target assembly definition");
                }
            }
        }

        public Dictionary<CustomScriptAssembly, List<string>> Match(
            IReadOnlyCollection<string> sources, bool logWarningOnMiss = true)
        {
            var assemblyNameSources = new Dictionary<CustomScriptAssembly, List<string>>();

            foreach (var source in sources)
            {
                var sourceSpan = source.AsSpan();
                var currentMatchingAssemblyDefinition = _npp.MatchClosest(sourceSpan, out var matchedBy);
                currentMatchingAssemblyDefinition =
                    CheckAndUpdateEditorSpecialFolder(currentMatchingAssemblyDefinition, sourceSpan, matchedBy);

                if (currentMatchingAssemblyDefinition == null)
                {
                    if (logWarningOnMiss)
                    {
                        Console.WriteLine(
                            $"Script '{source}' will not be compiled because it exists outside the Assets folder and does not to belong to any assembly definition file.");
                    }

                    continue;
                }

                if (!assemblyNameSources.TryGetValue(currentMatchingAssemblyDefinition, out var sourceList))
                {
                    sourceList = new List<string>();
                    assemblyNameSources[currentMatchingAssemblyDefinition] = sourceList;
                }

                sourceList.Add(source);
            }

            return assemblyNameSources;
        }

        internal static string CreateNewUnityGuid()
        {
            return Guid.NewGuid().ToString(_guidFormat);
        }

        internal static ReadOnlySpan<char> GetRelativePathFromAsmdefOrAsmref(CustomScriptAssembly currentMatchingAssemblyDefinition, ReadOnlySpan<char> sourceSpan, string matchedBy)
        {
            if(currentMatchingAssemblyDefinition == null)
            {
                return sourceSpan;
            }
            return sourceSpan[matchedBy.Length..];
        }

        private CustomScriptAssembly CheckAndUpdateEditorSpecialFolder(
            CustomScriptAssembly currentMatchingAssemblyDefinition, ReadOnlySpan<char> sourceSpan, string matchedBy)
        {
            var relativeSourceSpan = GetRelativePathFromAsmdefOrAsmref(currentMatchingAssemblyDefinition, sourceSpan, matchedBy);

            if (HasEditorSpecialFolder(relativeSourceSpan))
            {
                if (currentMatchingAssemblyDefinition == null ||
                    currentMatchingAssemblyDefinition == _globalAssemblyDefinition)
                {
                    currentMatchingAssemblyDefinition = _editorAssebmlyDefinition;
                }
                else if (currentMatchingAssemblyDefinition == _globalFirstpassAssemblyDefinition)
                {
                    currentMatchingAssemblyDefinition = _editorFirstpassAssemblyDefinition;
                }
            }

            return currentMatchingAssemblyDefinition;
        }

        internal static bool HasEditorSpecialFolder(ReadOnlySpan<char> remainingPath)
        {
            const string editorLower = "editor";
            const string editorUpper = "EDITOR";

            if (remainingPath.Length < editorLower.Length)
            {
                return false;
            }

            int matchOffset = 0;
            for (int i = 0; i < remainingPath.Length; i++)
            {
                if (editorLower[matchOffset] == remainingPath[i] || editorUpper[matchOffset] == remainingPath[i])
                {
                    matchOffset++;
                    if (matchOffset < editorLower.Length)
                    {
                        continue;
                    }

                    // We have match the "editor" folder, if we are at the end of the
                    // match or do we have a separator as our next character
                    if (i + 1 >= remainingPath.Length
                        || Utility.IsPathSeparator(remainingPath[i + 1]))
                    {
                        return true;
                    }
                }

                // forward to next path separator or end
                for (; i < remainingPath.Length; i++)
                {
                    if (Utility.IsPathSeparator(remainingPath[i]))
                    {
                        break;
                    }
                }

                matchOffset = 0;
                if (remainingPath.Length - i < editorLower.Length)
                {
                    return false;
                }
            }

            return matchOffset == editorLower.Length;
        }
    }
}
