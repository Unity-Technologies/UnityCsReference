// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Compilation;

namespace UnityEditor.Scripting.ScriptCompilation
{
    interface ILoadingAssemblyDefinition
    {
        CustomScriptAssembly[] CustomScriptAssemblies { get; }
        Dictionary<string, CustomScriptAssembly> FilePathToCustomScriptAssemblies { get; }
        List<Exception> Exceptions { get; }
        List<CustomScriptAssemblyReference> CustomScriptAssemblyReferences { get; }

        void SetAllCustomScriptAssemblyJsonContents(string[] paths, string[] contents, string[] guids);
        void SetAllCustomScriptAssemblyReferenceJsonsContents(string[] paths, string[] contents);
        void ClearCustomScriptAssemblies();
        void Refresh(
            CompilationSetupErrorsTrackerBase trackerBase,
            bool skipCustomScriptAssemblyGraphValidation,
            string projectDirectory);
    }

    class LoadingAssemblyDefinition : ILoadingAssemblyDefinition
    {
        CompilationSetupErrorsTrackerBase m_CompilationSetupErrorsTracker;
        bool m_SkipCustomScriptAssemblyGraphValidation;
        string m_ProjectDirectory;

        public CustomScriptAssembly[] CustomScriptAssemblies { get; private set; } = new CustomScriptAssembly[0];
        public List<CustomScriptAssemblyReference> CustomScriptAssemblyReferences { get; private set; } = new List<CustomScriptAssemblyReference>();

        public Dictionary<string, CustomScriptAssembly> FilePathToCustomScriptAssemblies { get; private set; } = new Dictionary<string, CustomScriptAssembly>();

        public List<Exception> Exceptions { get; private set; } = new List<Exception>();

        public void Refresh(
            CompilationSetupErrorsTrackerBase trackerBase,
            bool skipCustomScriptAssemblyGraphValidation,
            string projectDirectory)
        {
            m_CompilationSetupErrorsTracker = trackerBase;
            m_SkipCustomScriptAssemblyGraphValidation = skipCustomScriptAssemblyGraphValidation;
            m_ProjectDirectory = projectDirectory;
        }

        public void SetAllCustomScriptAssemblyJsonContents(string[] paths, string[] contents, string[] guids)
        {
            var assemblies = new List<CustomScriptAssembly>();
            var filePathToAssembly = new Dictionary<string, CustomScriptAssembly>();
            var assemblyLowercaseNamesLookup = new Dictionary<string, CustomScriptAssembly>();
            var exceptions = new List<Exception>();
            var guidsToAssemblies = new Dictionary<string, CustomScriptAssembly>();
            HashSet<string> predefinedAssemblyNames = null;

            // To check if a path prefix is already being used we use a Dictionary where the key is the prefix and the value is the file path.
            var prefixToFilePathLookup = CustomScriptAssemblyReferences.ToDictionary(x => x.PathPrefix,
                x => new List<string> {x.FilePath}, StringComparer.OrdinalIgnoreCase);

            m_CompilationSetupErrorsTracker.ClearCompilationSetupErrors(CompilationSetupErrors.LoadError);

            // Load first to setup guidsToAssemblies dictionary and convert guids to assembly names
            // before checking for assembly reference errors, so errors emit assembly names instead of guids.
            for (var i = 0; i < paths.Length; ++i)
            {
                var path = paths[i];
                var guid = guids[i];

                string lowerCaseName = null;
                CustomScriptAssembly loadedCustomScriptAssembly = null;

                try
                {
                    var fullPath = AssetPath.IsPathRooted(path)
                        ? AssetPath.GetFullPath(path)
                        : AssetPath.Combine(m_ProjectDirectory, path);

                    loadedCustomScriptAssembly =
                        contents != null
                        ? LoadCustomScriptAssemblyFromJson(fullPath, contents[i], guid)
                        : LoadCustomScriptAssemblyFromJsonPath(fullPath, guid);

                    loadedCustomScriptAssembly.References = loadedCustomScriptAssembly.References ?? new string[0];

                    lowerCaseName = Utility.FastToLower(loadedCustomScriptAssembly.Name);
                    guidsToAssemblies[Utility.FastToLower(guid)] = loadedCustomScriptAssembly;

                    if (!m_SkipCustomScriptAssemblyGraphValidation)
                    {
                        predefinedAssemblyNames = AppleSauce(
                            predefinedAssemblyNames,
                            assemblyLowercaseNamesLookup,
                            lowerCaseName,
                            prefixToFilePathLookup,
                            ref loadedCustomScriptAssembly);
                    }
                }
                catch (Exception e)
                {
                    m_CompilationSetupErrorsTracker.SetCompilationSetupErrors(CompilationSetupErrors.LoadError);
                    exceptions.Add(e);
                }

                if (loadedCustomScriptAssembly == null || m_SkipCustomScriptAssemblyGraphValidation && assemblyLowercaseNamesLookup.ContainsKey(lowerCaseName))
                {
                    continue;
                }

                loadedCustomScriptAssembly.References = loadedCustomScriptAssembly.References ?? new string[0];
                assemblyLowercaseNamesLookup[lowerCaseName] = loadedCustomScriptAssembly;
                assemblies.Add(loadedCustomScriptAssembly);
                filePathToAssembly.Add(loadedCustomScriptAssembly.FilePath, loadedCustomScriptAssembly);

                if (!prefixToFilePathLookup.TryGetValue(loadedCustomScriptAssembly.PathPrefix, out var duplicateFilePaths))
                {
                    duplicateFilePaths = new List<string>();
                    prefixToFilePathLookup[loadedCustomScriptAssembly.PathPrefix] = duplicateFilePaths;
                }

                duplicateFilePaths.Add(loadedCustomScriptAssembly.FilePath);
            }

            // Convert GUID references to assembly names
            foreach (var assembly in assemblies)
            {
                for (int i = 0; i < assembly.References.Length; ++i)
                {
                    var reference = assembly.References[i];

                    if (!GUIDReference.IsGUIDReference(reference))
                    {
                        continue;
                    }

                    var guid = Utility.FastToLower(GUIDReference.GUIDReferenceToGUID(reference));

                    reference = guidsToAssemblies.TryGetValue(guid, out var referenceAssembly)
                        ? referenceAssembly.Name
                        : string.Empty;

                    assembly.References[i] = reference;
                }
            }

            // Check loaded assemblies for assembly reference errors after all GUID references have been
            // converted to names.
            if (!m_SkipCustomScriptAssemblyGraphValidation)
            {
                foreach (var loadedCustomScriptAssembly in assemblies)
                {
                    try
                    {
                        var references = loadedCustomScriptAssembly.References.Where(r => !string.IsNullOrEmpty(r))
                            .ToArray();

                        if (references.Length == references.Distinct().Count())
                        {
                            continue;
                        }

                        var duplicateRefs = references.GroupBy(r => r).SelectMany(g => g.Skip(1)).ToArray();
                        var duplicateRefsString = string.Join(",", duplicateRefs);

                        throw new AssemblyDefinitionException(
                            $"Assembly has duplicate references: {duplicateRefsString}",
                            loadedCustomScriptAssembly.FilePath);
                    }
                    catch (Exception e)
                    {
                        m_CompilationSetupErrorsTracker.SetCompilationSetupErrors(CompilationSetupErrors.LoadError);
                        exceptions.Add(e);
                    }
                }
            }

            CustomScriptAssemblies = assemblies.ToArray();
            FilePathToCustomScriptAssemblies = filePathToAssembly;
            Exceptions = exceptions;
        }

        public void SetAllCustomScriptAssemblyReferenceJsonsContents(string[] paths, string[] contents)
        {
            var assemblyRefs = new List<CustomScriptAssemblyReference>();
            var exceptions = new List<Exception>();

            // We only construct this lookup if it is required, which is when we are using guids instead of assembly names.
            Dictionary<string, CustomScriptAssembly> guidsToAssemblies = null;

            // To check if a path prefix is already being used we use a Dictionary where the key is the prefix and the value is the file path.
            var prefixToFilePathLookup = m_SkipCustomScriptAssemblyGraphValidation ?
                null :
                CustomScriptAssemblies.GroupBy(x => x.PathPrefix).ToDictionary(x => x.First().PathPrefix, x => new List<string>() { x.First().FilePath }, StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < paths.Length; ++i)
            {
                var path = paths[i];

                CustomScriptAssemblyReference loadedCustomScriptAssemblyReference = null;

                try
                {
                    var fullPath = AssetPath.IsPathRooted(path) ? AssetPath.GetFullPath(path) : AssetPath.Combine(m_ProjectDirectory, path);

                    if (contents != null)
                    {
                        var jsonContents = contents[i];
                        loadedCustomScriptAssemblyReference = LoadCustomScriptAssemblyReferenceFromJson(fullPath, jsonContents);
                    }
                    else
                    {
                        loadedCustomScriptAssemblyReference = LoadCustomScriptAssemblyReferenceFromJsonPath(fullPath);
                    }

                    if (!m_SkipCustomScriptAssemblyGraphValidation)
                    {
                        // Check both asmdef and asmref files.
                        if (prefixToFilePathLookup.TryGetValue(loadedCustomScriptAssemblyReference.PathPrefix, out var duplicateFilePaths))
                        {
                            var filePaths = new List<string> {loadedCustomScriptAssemblyReference.FilePath};
                            filePaths.AddRange(duplicateFilePaths);

                            throw new AssemblyDefinitionException(
                                $"Folder '{loadedCustomScriptAssemblyReference.PathPrefix}' contains multiple assembly definition files", filePaths.ToArray());
                        }
                    }

                    // Convert GUID references to assembly names
                    if (GUIDReference.IsGUIDReference(loadedCustomScriptAssemblyReference.Reference))
                    {
                        // Generate the guid to assembly lookup?
                        guidsToAssemblies = guidsToAssemblies ?? CustomScriptAssemblies.ToDictionary(x => x.GUID);

                        var guid = Utility.FastToLower(GUIDReference.GUIDReferenceToGUID(loadedCustomScriptAssemblyReference.Reference));
                        if (guidsToAssemblies.TryGetValue(guid, out var foundAssembly))
                        {
                            loadedCustomScriptAssemblyReference.Reference = foundAssembly.Name;
                        }
                    }
                }
                catch (Exception e)
                {
                    m_CompilationSetupErrorsTracker.SetCompilationSetupErrors(CompilationSetupErrors.LoadError);
                    exceptions.Add(e);
                }

                if (loadedCustomScriptAssemblyReference != null)
                {
                    assemblyRefs.Add(loadedCustomScriptAssemblyReference);

                    if (!m_SkipCustomScriptAssemblyGraphValidation)
                    {
                        if (!prefixToFilePathLookup.TryGetValue(loadedCustomScriptAssemblyReference.PathPrefix, out var duplicateFilePaths))
                        {
                            duplicateFilePaths = new List<string>();
                            prefixToFilePathLookup[loadedCustomScriptAssemblyReference.PathPrefix] = duplicateFilePaths;
                        }

                        duplicateFilePaths.Add(loadedCustomScriptAssemblyReference.FilePath);
                    }
                }
            }

            CustomScriptAssemblyReferences = assemblyRefs;
            Exceptions = exceptions;
        }

        public void ClearCustomScriptAssemblies()
        {
            CustomScriptAssemblies = new CustomScriptAssembly[0];
            CustomScriptAssemblyReferences.Clear();
        }

        static HashSet<string> AppleSauce(HashSet<string> predefinedAssemblyNames,
            Dictionary<string, CustomScriptAssembly> assemblyLowercaseNamesLookup,
            string lowerCaseName, Dictionary<string, List<string>> prefixToFilePathLookup,
            ref CustomScriptAssembly loadedCustomScriptAssembly)
        {
            if (predefinedAssemblyNames == null)
            {
                predefinedAssemblyNames = new HashSet<string>(EditorBuildRules.PredefinedTargetAssemblyNames);
                var net46 = MonoLibraryHelpers
                    .GetSystemLibraryReferences(ApiCompatibilityLevel.NET_4_6,
                    ScriptCompilers.CSharpSupportedLanguage).Select(Path.GetFileNameWithoutExtension);
                var netstandard20 = MonoLibraryHelpers
                    .GetSystemLibraryReferences(ApiCompatibilityLevel.NET_Standard_2_0,
                    ScriptCompilers.CSharpSupportedLanguage).Select(Path.GetFileNameWithoutExtension);
                predefinedAssemblyNames.UnionWith(net46);
                predefinedAssemblyNames.UnionWith(netstandard20);
            }

            if (predefinedAssemblyNames.Contains(loadedCustomScriptAssembly.Name))
            {
                throw new AssemblyDefinitionException(
                    $"Assembly cannot be have reserved name '{loadedCustomScriptAssembly.Name}'",
                    loadedCustomScriptAssembly.FilePath);
            }

            if (assemblyLowercaseNamesLookup.TryGetValue(lowerCaseName, out var duplicate))
            {
                var filePaths = new[]
                {
                    loadedCustomScriptAssembly.FilePath,
                    duplicate.FilePath
                };
                var errorMsg = $"Assembly with name '{loadedCustomScriptAssembly.Name}' already exists";
                loadedCustomScriptAssembly = null; // Set to null to prevent it being added.
                throw new AssemblyDefinitionException(errorMsg, filePaths);
            }

            // Check both asmdef and asmref files.
            if (prefixToFilePathLookup.TryGetValue(loadedCustomScriptAssembly.PathPrefix,
                out var duplicateFilePaths))
            {
                var filePaths = new List<string> {loadedCustomScriptAssembly.FilePath};
                filePaths.AddRange(duplicateFilePaths);

                throw new AssemblyDefinitionException(
                    $"Folder '{loadedCustomScriptAssembly.PathPrefix}' contains multiple assembly definition files",
                    filePaths.ToArray());
            }

            return predefinedAssemblyNames;
        }

        public static CustomScriptAssembly LoadCustomScriptAssemblyFromJson(string path, string json, string guid)
        {
            try
            {
                var customScriptAssemblyData = CustomScriptAssemblyData.FromJson(json);
                return CustomScriptAssembly.FromCustomScriptAssemblyData(path, guid, customScriptAssemblyData);
            }
            catch (Exception e)
            {
                throw new AssemblyDefinitionException(e.Message, path);
            }
        }

        public static CustomScriptAssembly LoadCustomScriptAssemblyFromJsonPath(string path, string guid)
        {
            var json = Utility.ReadTextAsset(path);

            try
            {
                var customScriptAssemblyData = CustomScriptAssemblyData.FromJson(json);
                return CustomScriptAssembly.FromCustomScriptAssemblyData(path, guid, customScriptAssemblyData);
            }
            catch (Exception e)
            {
                throw new AssemblyDefinitionException(e.Message, path);
            }
        }

        static CustomScriptAssemblyReference LoadCustomScriptAssemblyReferenceFromJsonPath(string path)
        {
            var json = Utility.ReadTextAsset(path);
            return LoadCustomScriptAssemblyReferenceFromJson(path, json);
        }

        static CustomScriptAssemblyReference LoadCustomScriptAssemblyReferenceFromJson(string path, string json)
        {
            try
            {
                var customScriptAssemblyRefData = CustomScriptAssemblyReferenceData.FromJson(json);
                return CustomScriptAssemblyReference.FromCustomScriptAssemblyReferenceData(path, customScriptAssemblyRefData);
            }
            catch (Exception e)
            {
                throw new Compilation.AssemblyDefinitionException(e.Message, path);
            }
        }
    }
}
