// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using Mono.Cecil;

namespace Unity.UNetWeaver
{
    public static class Log
    {
        public static Action<string> WarningMethod;
        public static Action<string> ErrorMethod;

        public static void Warning(string msg)
        {
            WarningMethod("UNetWeaver warning: " + msg);
        }

        public static void Error(string msg)
        {
            ErrorMethod("UNetWeaver error: " + msg);
        }
    }

    public class Program
    {
        public static bool Process(string unityEngine, string unetDLL, string outputDirectory, string[] assemblies, string[] extraAssemblyPaths, IAssemblyResolver assemblyResolver, Action<string> printWarning, Action<string> printError)
        {
            CheckDLLPath(unityEngine);
            CheckDLLPath(unetDLL);
            CheckOutputDirectory(outputDirectory);
            CheckAssemblies(assemblies);
            Log.WarningMethod = printWarning;
            Log.ErrorMethod = printError;
            return Weaver.WeaveAssemblies(assemblies, extraAssemblyPaths, assemblyResolver, outputDirectory, unityEngine, unetDLL);
        }

        private static void CheckDLLPath(string path)
        {
            if (!File.Exists(path))
                throw new Exception("dll could not be located at " + path + "!");
        }

        private static void CheckAssemblies(IEnumerable<string> assemblyPaths)
        {
            foreach (var assemblyPath in assemblyPaths)
                CheckAssemblyPath(assemblyPath);
        }

        private static void CheckAssemblyPath(string assemblyPath)
        {
            if (!File.Exists(assemblyPath))
                throw new Exception("Assembly " + assemblyPath + " does not exist!");
        }

        private static void CheckOutputDirectory(string outputDir)
        {
            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);
        }
    }
}
