// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;

namespace UnityEditorInternal
{
    public static class Il2CppNativeCodeBuilderUtils
    {
        public static IEnumerable<string> AddBuilderArguments(Il2CppNativeCodeBuilder builder, string outputRelativePath, IEnumerable<string> includeRelativePaths, bool debugBuild)
        {
            var arguments = new List<string>();

            arguments.Add("--compile-cpp");
            if (builder.LinkLibIl2CppStatically)
                arguments.Add("--libil2cpp-static");
            arguments.Add(FormatArgument("platform", builder.CompilerPlatform));
            arguments.Add(FormatArgument("architecture", builder.CompilerArchitecture));

            if (debugBuild)
                arguments.Add(FormatArgument("configuration", "Debug"));
            else
                arguments.Add(FormatArgument("configuration", "Release"));

            arguments.Add(FormatArgument("outputpath", builder.ConvertOutputFileToFullPath(outputRelativePath)));

            if (!string.IsNullOrEmpty(builder.CacheDirectory))
                arguments.Add(FormatArgument("cachedirectory", CacheDirectoryPathFor(builder.CacheDirectory)));

            if (!string.IsNullOrEmpty(builder.CompilerFlags))
                arguments.Add(FormatArgument("compiler-flags", builder.CompilerFlags));

            if (!string.IsNullOrEmpty(builder.LinkerFlags))
                arguments.Add(FormatArgument("linker-flags", builder.LinkerFlags));

            if (!string.IsNullOrEmpty(builder.PluginPath))
                arguments.Add(FormatArgument("plugin", builder.PluginPath));

            foreach (var includePath in builder.ConvertIncludesToFullPaths(includeRelativePaths))
                arguments.Add(FormatArgument("additional-include-directories", includePath));

            arguments.AddRange(builder.AdditionalIl2CPPArguments);

            return arguments;
        }

        public static void ClearAndPrepareCacheDirectory(Il2CppNativeCodeBuilder builder)
        {
            var currentEditorVersion = InternalEditorUtility.GetFullUnityVersion();
            ClearCacheIfEditorVersionDiffers(builder, currentEditorVersion);
            PrepareCacheDirectory(builder, currentEditorVersion);
        }

        public static void ClearCacheIfEditorVersionDiffers(Il2CppNativeCodeBuilder builder, string currentEditorVersion)
        {
            var cacheDirectoryPath = CacheDirectoryPathFor(builder.CacheDirectory);
            if (Directory.Exists(cacheDirectoryPath))
            {
                if (!File.Exists(Path.Combine(builder.CacheDirectory, EditorVersionFilenameFor(currentEditorVersion))))
                    Directory.Delete(cacheDirectoryPath, true);
            }
        }

        public static void PrepareCacheDirectory(Il2CppNativeCodeBuilder builder, string currentEditorVersion)
        {
            var cacheDirectoryPath = CacheDirectoryPathFor(builder.CacheDirectory);
            Directory.CreateDirectory(cacheDirectoryPath);
            var versionFilePath = Path.Combine(builder.CacheDirectory, EditorVersionFilenameFor(currentEditorVersion));
            if (!File.Exists(versionFilePath))
                File.Create(versionFilePath).Dispose();
        }

        public static string ObjectFilePathInCacheDirectoryFor(string builderCacheDirectory)
        {
            return CacheDirectoryPathFor(builderCacheDirectory);
        }

        private static string CacheDirectoryPathFor(string builderCacheDirectory)
        {
            return builderCacheDirectory + "/il2cpp_cache";
        }

        private static string FormatArgument(string name, string value)
        {
            return string.Format("--{0}=\"{1}\"", name, EscapeEmbeddedQuotes(value));
        }

        private static string EditorVersionFilenameFor(string editorVersion)
        {
            return string.Format("il2cpp_cache {0}", editorVersion);
        }

        private static string EscapeEmbeddedQuotes(string value)
        {
            return value.Replace("\"", "\\\"");
        }
    }
}
