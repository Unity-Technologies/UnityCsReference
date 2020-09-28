// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Xml;
using UnityEditorInternal;
using Mono.Cecil;
using UnityEditor.Build.Reporting;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEngine.Networking;
using UnityEditor.Utils;

namespace UnityEditor
{
    internal class CodeStrippingUtils
    {
        private static UnityType s_GameManagerTypeInfo = null;
        internal static UnityType GameManagerTypeInfo
        {
            get
            {
                if (s_GameManagerTypeInfo == null)
                    s_GameManagerTypeInfo = FindTypeByNameChecked("GameManager", "initializing code stripping utils");
                return s_GameManagerTypeInfo;
            }
        }

        private static UnityType FindTypeByNameChecked(string name, string msg)
        {
            UnityType result = UnityType.FindTypeByName(name);
            if (result == null)
                throw new ArgumentException(string.Format("Could not map typename '{0}' to type info ({1})", name, msg ?? "no context"));
            return result;
        }

        public static void GenerateDependencies(string strippedAssemblyDir, bool doStripping, out HashSet<UnityType> nativeClasses,
            out HashSet<string> nativeModules)
        {
            var dataFromLinker = AssemblyStripper.ReadLinkerToEditorData(strippedAssemblyDir);
            nativeClasses = doStripping ? new HashSet<UnityType>() : null;
            nativeModules = new HashSet<string>();
            foreach (var module in dataFromLinker.report.modules)
            {
                nativeModules.Add(module.name);

                if (doStripping)
                {
                    foreach (var dependency in module.dependencies)
                    {
                        var unityType = UnityType.FindTypeByName(dependency.name);

                        if (unityType != null)
                            nativeClasses.Add(unityType);
                    }
                }
            }
        }

        public static void WriteModuleAndClassRegistrationFile(string strippedAssemblyDir, string icallsListFile, string outputDir, RuntimeClassRegistry rcr, IEnumerable<UnityType> classesToSkip, IIl2CppPlatformProvider platformProvider, bool writeModuleRegistration = true, bool writeClassRegistration = true)
        {
            HashSet<UnityType> nativeClasses;
            HashSet<string> nativeModules;
            // by default, we only care about il2cpp
            bool doStripping = PlayerSettings.stripEngineCode;
            GenerateDependencies(strippedAssemblyDir, doStripping, out nativeClasses, out nativeModules);

            var outputClassRegistration = Path.Combine(outputDir, "UnityClassRegistration.cpp");
            using (TextWriter w = new StreamWriter(outputClassRegistration))
            {
                if (writeModuleRegistration)
                    WriteFunctionRegisterStaticallyLinkedModulesGranular(w, nativeModules);
                if (writeClassRegistration)
                    WriteStaticallyLinkedModuleClassRegistration(w, nativeClasses, new HashSet<UnityType>(classesToSkip));
                w.Close();
            }
        }

        public class ModuleDependencyComparer : IComparer<string>
        {
            public int Compare(string stringA, string stringB)
            {
                return ModuleMetadata.GetModuleDependencies(stringA).Contains(stringB) ? 1 : ModuleMetadata.GetModuleDependencies(stringB).Contains(stringA) ? -1 : 0;
            }
        }

        private static void WriteFunctionInvokeRegisterStaticallyLinkedModuleClasses(TextWriter w, HashSet<UnityType> nativeClasses)
        {
            w.WriteLine("void InvokeRegisterStaticallyLinkedModuleClasses()");
            w.WriteLine("{");
            if (nativeClasses == null)
            {
                w.WriteLine("\tvoid RegisterStaticallyLinkedModuleClasses();");
                w.WriteLine("\tRegisterStaticallyLinkedModuleClasses();");
            }
            else
            {
                w.WriteLine("\t// Do nothing (we're in stripping mode)");
            }
            w.WriteLine("}");
            w.WriteLine();
        }

        private static void WriteFunctionRegisterStaticallyLinkedModulesGranular(TextWriter w, HashSet<string> nativeModules)
        {
            w.WriteLine("extern \"C\" void RegisterStaticallyLinkedModulesGranular()");
            w.WriteLine("{");

            var nativeModulesSorted = nativeModules.OrderBy(x => x, new ModuleDependencyComparer());
            foreach (string module in nativeModulesSorted)
            {
                w.WriteLine("\tvoid RegisterModule_" + module + "();");
                w.WriteLine("\tRegisterModule_" + module + "();");
                w.WriteLine();
            }
            w.WriteLine("}");
            w.WriteLine();
        }

        private static void WriteStaticallyLinkedModuleClassRegistration(TextWriter w, HashSet<UnityType> nativeClasses, HashSet<UnityType> classesToSkip)
        {
            w.WriteLine("template <typename T> void RegisterUnityClass(const char*);");
            w.WriteLine("template <typename T> void RegisterStrippedType(int, const char*, const char*);");
            w.WriteLine();

            WriteFunctionInvokeRegisterStaticallyLinkedModuleClasses(w, nativeClasses);

            // Forward declare types
            if (nativeClasses != null)
            {
                foreach (var type in UnityType.GetTypes())
                {
                    if (type.baseClass == null || type.isEditorOnly || classesToSkip.Contains(type))
                        continue;

                    if (type.hasNativeNamespace)
                        w.Write("namespace {0} {{ class {1}; }} ", type.nativeNamespace, type.name);
                    else
                        w.Write("class {0}; ", type.name);

                    if (nativeClasses.Contains(type))
                        w.WriteLine("template <> void RegisterUnityClass<{0}>(const char*);", type.qualifiedName);
                    else
                        w.WriteLine();
                }
                w.WriteLine();
            }

            // Class registration function
            w.WriteLine("void RegisterAllClasses()");
            w.WriteLine("{");

            if (nativeClasses == null)
            {
                w.WriteLine("\tvoid RegisterAllClassesGranular();");
                w.WriteLine("\tRegisterAllClassesGranular();");
            }
            else
            {
                w.WriteLine("void RegisterBuiltinTypes();");
                w.WriteLine("RegisterBuiltinTypes();");
                w.WriteLine("\t//Total: {0} non stripped classes", nativeClasses.Count);

                int index = 0;
                foreach (var klass in nativeClasses)
                {
                    w.WriteLine("\t//{0}. {1}", index, klass.qualifiedName);
                    if (classesToSkip.Contains(klass) || (klass.baseClass == null))
                        w.WriteLine("\t//Skipping {0}", klass.qualifiedName);
                    else
                        w.WriteLine("\tRegisterUnityClass<{0}>(\"{1}\");", klass.qualifiedName, klass.module);
                    ++index;
                }
                w.WriteLine();

                // Register stripped classes

                // TODO (ulfj ) 2016-08-15 : Right now we cannot deal with types that are compiled into the editor
                // but not the player due to other defines than UNITY_EDITOR in them module definition file
                // (for example WorldAnchor only being there if ENABLE_HOLOLENS_MODULE_API). Doing this would
                // require either some non trivial changes to the module registration macros or a way for these
                // conditionals to be included in the RTTI so we can emit them when generating the code, so we
                // disabling the registration of stripped classes for now and will get back to this when we have
                // landed the remaining changes to the type system.

                //w.WriteLine("\t//Stripped classes");
                //foreach (var type in UnityType.GetTypes())
                //{
                //  if (type.baseClass == null || type.isEditorOnly || classesToSkip.Contains(type) || nativeClasses.Contains(type))
                //      continue;

                //  w.WriteLine("\tRegisterStrippedType<{0}>({1}, \"{2}\", \"{3}\");", type.qualifiedName, type.persistentTypeID, type.name, type.nativeNamespace);
                //}
            }
            w.WriteLine("}");
        }

        private static readonly string[] s_TreatedAsUserAssemblies =
        {
            // Treat analytics as we user assembly. If it is not used, it won't be in the directory,
            // so this should not add to the build size unless it is really used.
            "Unity.Analytics.dll",
        };

        public static string[] UserAssemblies
        {
            get
            {
                EditorCompilation.TargetAssemblyInfo[] allTargetAssemblies = EditorCompilationInterface.GetTargetAssemblyInfos();

                string[] targetAssemblyNames = new string[allTargetAssemblies.Length + s_TreatedAsUserAssemblies.Length];

                for (int i = 0; i < allTargetAssemblies.Length; ++i)
                {
                    targetAssemblyNames[i] = allTargetAssemblies[i].Name;
                }
                for (int i = 0; i < s_TreatedAsUserAssemblies.Length; ++i)
                {
                    targetAssemblyNames[allTargetAssemblies.Length + i] = s_TreatedAsUserAssemblies[i];
                }
                return targetAssemblyNames;
            }
        }
    } //CodeStrippingUtils
} //UnityEditor
