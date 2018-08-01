// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Mono.Cecil;
using UnityEngine.Scripting;

namespace UnityEditor
{
    static class AssemblyValidation
    {
        // Keep in sync with AssemblyValidationFlags in MonoManager.cpp
        [Flags]
        public enum ErrorFlags
        {
            None = 0,
            ReferenceHasErrors = (1 << 0),
            UnresolvableReference = (1 << 1),
            IncompatibleWithEditor = (1 << 2),
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Error
        {
            public ErrorFlags flags;
            public string message;

            public void Add(ErrorFlags newFlags, string newMessage)
            {
                flags |= newFlags;

                if (message == null)
                {
                    message = newMessage;
                }
                else
                {
                    message += string.Format("\n{0}", newMessage);
                }
            }

            public bool HasFlag(ErrorFlags testFlags)
            {
                return (flags & testFlags) == testFlags;
            }

            public void ClearFlags(ErrorFlags clearFlags)
            {
                flags &= ~clearFlags;
            }
        }

        public struct AssemblyAndReferences
        {
            public int assemblyIndex;
            public int[] referenceIndicies;
        }

        class AssemblyResolver : BaseAssemblyResolver
        {
            readonly IDictionary<string, AssemblyDefinition> cache;

            public AssemblyResolver()
            {
                cache = new Dictionary<string, AssemblyDefinition>(StringComparer.Ordinal);
            }

            public override AssemblyDefinition Resolve(AssemblyNameReference name)
            {
                if (name == null)
                    throw new ArgumentNullException("name");

                AssemblyDefinition assembly;
                if (cache.TryGetValue(name.Name, out assembly))
                    return assembly;

                assembly = base.Resolve(name);
                cache[name.Name] = assembly;

                return assembly;
            }

            public void RegisterAssembly(AssemblyDefinition assembly)
            {
                if (assembly == null)
                    throw new ArgumentNullException("assembly");

                var name = assembly.Name.Name;
                if (cache.ContainsKey(name))
                    return;

                cache[name] = assembly;
            }

            protected override void Dispose(bool disposing)
            {
                foreach (var assembly in cache.Values)
                    assembly.Dispose();

                cache.Clear();

                base.Dispose(disposing);
            }
        }

        [RequiredByNativeCode]
        public static Error[] ValidateAssemblies(string[] assemblyPaths, bool enableLogging)
        {
            var searchPaths = AssemblyHelper.GetDefaultAssemblySearchPaths();

            var assemblyDefinitions = LoadAssemblyDefinitions(assemblyPaths, searchPaths);

            if (enableLogging)
            {
                // Prints assemblies and their references to the Editor.log
                PrintAssemblyDefinitions(assemblyDefinitions);

                foreach (var searchPath in searchPaths)
                {
                    Console.WriteLine("[AssemblyValidation] Search Path: '" + searchPath + "'");
                }
            }

            var errors = ValidateAssemblyDefinitions(assemblyPaths,
                assemblyDefinitions,
                PluginCompatibleWithEditor);

            return errors;
        }

        public static bool PluginCompatibleWithEditor(string path)
        {
            var pluginImporter = AssetImporter.GetAtPath(path) as PluginImporter;

            if (pluginImporter == null)
                return true;

            if (pluginImporter.GetCompatibleWithAnyPlatform())
                return true;

            return pluginImporter.GetCompatibleWithEditor();
        }

        public static void PrintAssemblyDefinitions(AssemblyDefinition[] assemblyDefinitions)
        {
            foreach (var assemblyDefinition in assemblyDefinitions)
            {
                Console.WriteLine("[AssemblyValidation] Assembly: " + assemblyDefinition.Name);

                var assemblyReferences = GetAssemblyNameReferences(assemblyDefinition);

                foreach (var reference in assemblyReferences)
                {
                    Console.WriteLine("[AssemblyValidation]   Reference: " + reference);
                }
            }
        }

        public static Error[] ValidateAssembliesInternal(string[] assemblyPaths,
            string[] searchPaths,
            Func<string, bool> compatibleWithEditor)
        {
            var assemblyDefinitions = LoadAssemblyDefinitions(assemblyPaths, searchPaths);
            return ValidateAssemblyDefinitions(assemblyPaths, assemblyDefinitions, compatibleWithEditor);
        }

        public static Error[] ValidateAssemblyDefinitions(string[] assemblyPaths,
            AssemblyDefinition[] assemblyDefinitions,
            Func<string, bool> compatibleWithEditor)
        {
            var errors = new Error[assemblyPaths.Length];

            CheckAssemblyReferences(assemblyPaths,
                errors,
                assemblyDefinitions,
                compatibleWithEditor);

            return errors;
        }

        public static AssemblyDefinition[] LoadAssemblyDefinitions(string[] assemblyPaths, string[] searchPaths)
        {
            var assemblyResolver = new AssemblyResolver();

            foreach (var asmpath in searchPaths)
                assemblyResolver.AddSearchDirectory(asmpath);

            var readerParameters = new ReaderParameters
            {
                AssemblyResolver = assemblyResolver
            };

            var assemblyDefinitions = new AssemblyDefinition[assemblyPaths.Length];

            for (int i = 0; i < assemblyPaths.Length; ++i)
            {
                assemblyDefinitions[i] = AssemblyDefinition.ReadAssembly(assemblyPaths[i], readerParameters);
                // Cecil tries to resolve references by filename, since Unity force loads
                // assemblies, then assembly reference will resolve even if the assembly name
                // does not match the assembly filename. So we register all assemblies in
                // in the resolver.
                assemblyResolver.RegisterAssembly(assemblyDefinitions[i]);
            }

            return assemblyDefinitions;
        }

        public static void CheckAssemblyReferences(string[] assemblyPaths,
            Error[] errors,
            AssemblyDefinition[] assemblyDefinitions,
            Func<string, bool> compatibleWithEditor)
        {
            SetupEditorCompatibility(assemblyPaths, errors, compatibleWithEditor);

            var assemblyDefinitionNameToIndex = new Dictionary<string, int>();
            var assembliesAndReferencesArray = new AssemblyAndReferences[assemblyPaths.Length];

            for (int i = 0; i < assemblyDefinitions.Length; ++i)
            {
                assemblyDefinitionNameToIndex[assemblyDefinitions[i].Name.Name] = i;
                assembliesAndReferencesArray[i] = new AssemblyAndReferences
                {
                    assemblyIndex = i,
                    referenceIndicies = new int[0]
                };
            }

            for (int i = 0; i < assemblyPaths.Length; ++i)
            {
                if (errors[i].HasFlag(ErrorFlags.IncompatibleWithEditor))
                    continue;

                ResolveAndSetupReferences(i,
                    errors,
                    assemblyDefinitions,
                    assemblyDefinitionNameToIndex,
                    assembliesAndReferencesArray);
            }

            // Check assemblies for references to assemblies with errors
            int referenceErrorCount;

            do
            {
                referenceErrorCount = 0;

                foreach (var assemblyAndReferences in assembliesAndReferencesArray)
                {
                    var assemblyIndex = assemblyAndReferences.assemblyIndex;

                    foreach (var referenceIndex in assemblyAndReferences.referenceIndicies)
                    {
                        var referenceError = errors[referenceIndex];
                        if (errors[assemblyIndex].flags == ErrorFlags.None &&
                            referenceError.flags != ErrorFlags.None)
                        {
                            if (referenceError.HasFlag(ErrorFlags.IncompatibleWithEditor))
                            {
                                errors[assemblyIndex].Add(ErrorFlags.ReferenceHasErrors | ErrorFlags.IncompatibleWithEditor,
                                    string.Format("Reference '{0}' is incompatible with the editor.",
                                        assemblyDefinitions[referenceIndex].Name.Name));
                            }
                            else
                            {
                                errors[assemblyIndex].Add(ErrorFlags.ReferenceHasErrors | referenceError.flags,
                                    string.Format("Reference has errors '{0}'.",
                                        assemblyDefinitions[referenceIndex].Name.Name));
                            }

                            referenceErrorCount++;
                        }
                    }
                }
            }
            while (referenceErrorCount > 0);
        }

        public static void SetupEditorCompatibility(string[] assemblyPaths,
            Error[] errors,
            Func<string, bool> compatibleWithEditor)
        {
            for (int i = 0; i < assemblyPaths.Length; ++i)
            {
                var assemblyPath = assemblyPaths[i];

                if (!compatibleWithEditor(assemblyPath))
                {
                    errors[i].Add(ErrorFlags.IncompatibleWithEditor,
                        "Assembly is incompatible with the editor");
                }
            }
        }

        public static void ResolveAndSetupReferences(int index,
            Error[] errors,
            AssemblyDefinition[] assemblyDefinitions,
            Dictionary<string, int> assemblyDefinitionNameToIndex,
            AssemblyAndReferences[] assemblyAndReferences)
        {
            var assemblyDefinition = assemblyDefinitions[index];
            var assemblyResolver = assemblyDefinition.MainModule.AssemblyResolver;

            var assemblyReferences = GetAssemblyNameReferences(assemblyDefinition);

            var referenceIndieces = new List<int>
            {
                Capacity = assemblyReferences.Length
            };

            foreach (var reference in assemblyReferences)
            {
                try
                {
                    var referenceAssemblyDefinition = assemblyResolver.Resolve(reference);

                    int referenceAssemblyDefinitionIndex;

                    if (assemblyDefinitionNameToIndex.TryGetValue(referenceAssemblyDefinition.Name.Name,
                        out referenceAssemblyDefinitionIndex))
                    {
                        referenceIndieces.Add(referenceAssemblyDefinitionIndex);
                    }
                }
                catch (AssemblyResolutionException)
                {
                    errors[index].Add(ErrorFlags.UnresolvableReference,
                        string.Format("Unable to resolve reference '{0}'. Is the assembly missing or incompatible with the current platform?",
                            reference.Name));
                }
            }

            assemblyAndReferences[index].referenceIndicies = referenceIndieces.ToArray();
        }

        public static AssemblyNameReference[] GetAssemblyNameReferences(AssemblyDefinition assemblyDefinition)
        {
            List<AssemblyNameReference> result = new List<AssemblyNameReference>
            {
                Capacity = 16
            };

            foreach (ModuleDefinition module in assemblyDefinition.Modules)
            {
                var references = module.AssemblyReferences;

                foreach (var reference in references)
                {
                    result.Add(reference);
                }
            }

            return result.ToArray();
        }
    }
}
