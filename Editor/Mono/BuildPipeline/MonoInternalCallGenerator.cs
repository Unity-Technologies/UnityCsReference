// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using UnityEditor.Utils;
using UnityEditorInternal;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Mono.Cecil;

namespace UnityEditor
{
    internal class MonoAOTRegistration
    {
        static void ExtractNativeMethodsFromTypes(ICollection<TypeDefinition> types, ArrayList res)
        {
            foreach (TypeDefinition typ in types)
            {
                foreach (MethodDefinition method in typ.Methods)
                {
                    if (method.IsStatic && method.IsPInvokeImpl && method.PInvokeInfo.Module.Name.Equals("__Internal"))
                    {
                        if (res.Contains(method.Name))
                            throw new SystemException("Duplicate native method found : " + method.Name + ". Please check your source carefully.");

                        res.Add(method.Name);
                    }
                }

                if (typ.HasNestedTypes)
                    ExtractNativeMethodsFromTypes(typ.NestedTypes, res);
            }
        }

        // Builds list of extern methods that are marked with DllImport("__Internal") attribute
        // @TODO: This needs to be rewritten using cecil
        static ArrayList BuildNativeMethodList(AssemblyDefinition[] assemblies)
        {
            ArrayList res = new ArrayList();

            foreach (AssemblyDefinition ass in assemblies)
            {
                //string assname = ass.Name.Name;
                if (!"System".Equals(ass.Name.Name))
                {
                    ExtractNativeMethodsFromTypes(ass.MainModule.Types, res);
                }
            }

            return res;
        }

        static public HashSet<string> BuildReferencedTypeList(AssemblyDefinition[] assemblies)
        {
            HashSet<string> res = new HashSet<string>();

            foreach (AssemblyDefinition ass in assemblies)
            {
                //string assname = ass.Name.Name;
                if (!ass.Name.Name.StartsWith("System") && !ass.Name.Name.Equals("UnityEngine"))
                {
                    foreach (TypeReference typ in ass.MainModule.GetTypeReferences())
                    {
                        res.Add(typ.FullName);
                    }
                }
            }

            return res;
        }

        static public void WriteCPlusPlusFileForStaticAOTModuleRegistration(BuildTarget buildTarget, string file,
            CrossCompileOptions crossCompileOptions,
            bool advancedLic, string targetDevice, bool stripping, RuntimeClassRegistry usedClassRegistry,
            AssemblyReferenceChecker checker, string stagingAreaDataManaged)
        {
            WriteCPlusPlusFileForStaticAOTModuleRegistration(buildTarget, file, crossCompileOptions, advancedLic, targetDevice, stripping, usedClassRegistry, checker, stagingAreaDataManaged, null);
        }

        static public void WriteCPlusPlusFileForStaticAOTModuleRegistration(BuildTarget buildTarget, string file,
            CrossCompileOptions crossCompileOptions,
            bool advancedLic, string targetDevice, bool stripping, RuntimeClassRegistry usedClassRegistry,
            AssemblyReferenceChecker checker, string stagingAreaDataManaged, IIl2CppPlatformProvider platformProvider)
        {
            // generate the Interal Call Summary file
            var icallSummaryPath = Path.Combine(stagingAreaDataManaged, "ICallSummary.txt");
            var dlls = Directory.GetFiles(stagingAreaDataManaged, "UnityEngine.*Module.dll").Concat(new[] {Path.Combine(stagingAreaDataManaged, "UnityEngine.dll")});
            var exe = Path.Combine(MonoInstallationFinder.GetFrameWorksFolder(), "Tools/InternalCallRegistrationWriter/InternalCallRegistrationWriter.exe");
            var args = string.Format("-assembly=\"{0}\" -summary=\"{1}\"",
                    dlls.Aggregate((dllArg, next) => dllArg + ";" + next), icallSummaryPath
                    );
            Runner.RunManagedProgram(exe, args);

            HashSet<UnityType> nativeClasses;
            HashSet<string> nativeModules;
            CodeStrippingUtils.GenerateDependencies(Path.GetDirectoryName(stagingAreaDataManaged), icallSummaryPath, usedClassRegistry, stripping, out nativeClasses, out nativeModules, platformProvider);

            using (TextWriter w = new StreamWriter(file))
            {
                string[] fileNames = checker.GetAssemblyFileNames();
                AssemblyDefinition[] assemblies = checker.GetAssemblyDefinitions();

                bool fastICall = (crossCompileOptions & CrossCompileOptions.FastICall) != 0;

                ArrayList nativeMethods = BuildNativeMethodList(assemblies);

                if (buildTarget == BuildTarget.iOS)
                {
                    w.WriteLine("#include \"RegisterMonoModules.h\"");
                    w.WriteLine("#include <stdio.h>");
                }

                w.WriteLine("");
                w.WriteLine("#if defined(TARGET_IPHONE_SIMULATOR) && TARGET_IPHONE_SIMULATOR");
                w.WriteLine("    #define DECL_USER_FUNC(f) void f() __attribute__((weak_import))");
                w.WriteLine("    #define REGISTER_USER_FUNC(f)\\");
                w.WriteLine("        do {\\");
                w.WriteLine("        if(f != NULL)\\");
                w.WriteLine("            mono_dl_register_symbol(#f, (void*)f);\\");
                w.WriteLine("        else\\");
                w.WriteLine("            ::printf_console(\"Symbol \'%s\' not found. Maybe missing implementation for Simulator?\\n\", #f);\\");
                w.WriteLine("        }while(0)");
                w.WriteLine("#else");
                w.WriteLine("    #define DECL_USER_FUNC(f) void f() ");
                w.WriteLine("    #if !defined(__arm64__)");
                w.WriteLine("    #define REGISTER_USER_FUNC(f) mono_dl_register_symbol(#f, (void*)&f)");
                w.WriteLine("    #else");
                w.WriteLine("        #define REGISTER_USER_FUNC(f)");
                w.WriteLine("    #endif");
                w.WriteLine("#endif");
                w.WriteLine("extern \"C\"\n{");

                w.WriteLine("    typedef void* gpointer;");
                w.WriteLine("    typedef int gboolean;");

                if (buildTarget == BuildTarget.iOS)
                {
                    w.WriteLine("    const char*         UnityIPhoneRuntimeVersion = \"{0}\";", Application.unityVersion);
                    w.WriteLine("    void                mono_dl_register_symbol (const char* name, void *addr);");

                    w.WriteLine("#if !defined(__arm64__)");
                    w.WriteLine("    extern int          mono_ficall_flag;");
                    w.WriteLine("#endif");
                }

                w.WriteLine("    void                mono_aot_register_module(gpointer *aot_info);");

                w.WriteLine("#if __ORBIS__ || SN_TARGET_PSP2");
                w.WriteLine("#define DLL_EXPORT __declspec(dllexport)"); // ps4 and psp2 need dllexport.
                w.WriteLine("#else");
                w.WriteLine("#define DLL_EXPORT");
                w.WriteLine("#endif");

                w.WriteLine("#if !(TARGET_IPHONE_SIMULATOR)");
                w.WriteLine("    extern gboolean     mono_aot_only;");

                for (int q = 0; q < fileNames.Length; ++q)
                {
                    string fileName = fileNames[q];
                    string assemblyName = assemblies[q].Name.Name;
                    assemblyName = assemblyName.Replace(".", "_");
                    assemblyName = assemblyName.Replace("-", "_");
                    assemblyName = assemblyName.Replace(" ", "_");

                    w.WriteLine("    extern gpointer*    mono_aot_module_{0}_info; // {1}", assemblyName, fileName);
                }
                w.WriteLine("#endif // !(TARGET_IPHONE_SIMULATOR)");
                foreach (string nmethod in nativeMethods)
                {
                    w.WriteLine("    DECL_USER_FUNC({0});", nmethod);
                }
                w.WriteLine("}");

                w.WriteLine("DLL_EXPORT void RegisterMonoModules()");
                w.WriteLine("{");

                w.WriteLine("#if !(TARGET_IPHONE_SIMULATOR) && !defined(__arm64__)");
                w.WriteLine("    mono_aot_only = true;");

                if (buildTarget == BuildTarget.iOS)
                {
                    w.WriteLine("    mono_ficall_flag = {0};", fastICall ? "true" : "false");
                }

                foreach (AssemblyDefinition definition in assemblies)
                {
                    string assemblyName = definition.Name.Name;
                    assemblyName = assemblyName.Replace(".", "_");
                    assemblyName = assemblyName.Replace("-", "_");
                    assemblyName = assemblyName.Replace(" ", "_");
                    w.WriteLine("    mono_aot_register_module(mono_aot_module_{0}_info);", assemblyName);
                }

                w.WriteLine("#endif // !(TARGET_IPHONE_SIMULATOR) && !defined(__arm64__)");
                w.WriteLine("");

                if (buildTarget == BuildTarget.iOS)
                {
                    foreach (string nmethod in nativeMethods)
                    {
                        w.WriteLine("    REGISTER_USER_FUNC({0});", nmethod);
                    }
                }
                w.WriteLine("}");
                w.WriteLine("");


                if (buildTarget == BuildTarget.iOS)
                {
                    var inputAssemblies = new List<AssemblyDefinition>();

                    for (int i = 0; i < assemblies.Length; i++)
                    {
                        if (AssemblyHelper.IsUnityEngineModule(assemblies[i].Name.Name))
                            inputAssemblies.Add(assemblies[i]);
                    }

                    GenerateRegisterInternalCalls(inputAssemblies.ToArray(), w);

                    GenerateRegisterModules(nativeClasses, nativeModules, w, stripping);

                    if (stripping && usedClassRegistry != null)
                        GenerateRegisterClassesForStripping(nativeClasses, w);
                    else
                        GenerateRegisterClasses(nativeClasses, w);
                }

                w.Close();
            }
        }

        public static void GenerateRegisterModules(HashSet<UnityType> nativeClasses, HashSet<string> nativeModules, TextWriter output, bool strippingEnabled)
        {
            output.WriteLine("void InvokeRegisterStaticallyLinkedModuleClasses()");
            output.WriteLine("{");
            if (nativeClasses == null)
            {
                output.WriteLine("\tvoid RegisterStaticallyLinkedModuleClasses();");
                output.WriteLine("\tRegisterStaticallyLinkedModuleClasses();");
            }
            else
            {
                output.WriteLine("\t// Do nothing (we're in stripping mode)");
            }
            output.WriteLine("}");
            output.WriteLine();

            output.WriteLine("void RegisterStaticallyLinkedModulesGranular()");
            output.WriteLine("{");
            foreach (string module in nativeModules)
            {
                output.WriteLine("\tvoid RegisterModule_" + module + "();");
                output.WriteLine("\tRegisterModule_" + module + "();");
                output.WriteLine();
            }
            output.WriteLine("}\n");
        }

        public static void GenerateRegisterClassesForStripping(HashSet<UnityType> nativeClassesAndBaseClasses, TextWriter output)
        {
            // Forward declare template function
            output.WriteLine("template <typename T> void RegisterClass();");
            output.WriteLine("template <typename T> void RegisterStrippedType(int, const char*, const char*);");
            output.WriteLine();

            // Forward declaration
            foreach (var type in UnityType.GetTypes())
            {
                if (type.baseClass == null || type.isEditorOnly)
                    continue;

                if (!type.hasNativeNamespace)
                {
                    output.WriteLine("class {0};", type.name);
                }
                else
                {
                    output.WriteLine("namespace {0} {{ class {1}; }}", type.nativeNamespace, type.name);
                }

                output.WriteLine();
            }

            output.Write("void RegisterAllClasses() \n{\n");
            output.WriteLine("\tvoid RegisterBuiltinTypes();");
            output.WriteLine("\tRegisterBuiltinTypes();");

            // Write non stripped class registration
            output.WriteLine("\t// {0} Non stripped classes\n", nativeClassesAndBaseClasses.Count);
            int count = 1;
            foreach (var type in UnityType.GetTypes())
            {
                if (type.baseClass == null || type.isEditorOnly || !nativeClassesAndBaseClasses.Contains(type))
                    continue;

                output.WriteLine("\t// {0}. {1}", count++, type.qualifiedName);
                output.WriteLine("\tRegisterClass<{0}>();\n", type.qualifiedName);
            }

            output.WriteLine();

            // Write stripped class registration

            // TODO (ulfj ) 2016-08-15 : Right now we cannot deal with types that are compiled into the editor
            // but not the player due to other defines than UNITY_EDITOR in them module definition file
            // (for example WorldAnchor only being there if ENABLE_HOLOLENS_MODULE_API). Doing this would
            // require either some non trivial changes to the module registration macros or a way for these
            // conditionals to be included in the RTTI so we can emit them when generating the code, so we
            // disabling the registration of stripped classes for now and will get back to this when we have
            // landed the remaining changes to the type system.

            //output.WriteLine("\t// Stripped classes");
            //foreach (var type in UnityType.GetTypes())
            //{
            //  if (type.baseClass == null || type.isEditorOnly || nativeClassesAndBaseClasses.Contains(type))
            //      continue;

            //  output.WriteLine("\tRegisterStrippedType<{0}>();", type.qualifiedName);
            //}

            output.Write("\n}\n");
        }

        public static void GenerateRegisterClasses(HashSet<UnityType> allClasses, TextWriter output)
        {
            output.WriteLine("void RegisterAllClasses() \n{");
            output.WriteLine("\tvoid RegisterAllClassesGranular();");
            output.WriteLine("\tRegisterAllClassesGranular();");
            output.WriteLine("}");
        }

        // Generate a cpp file that registers all exposed internal calls
        // for example: Register_UnityEngine_AnimationClip_get_length
        //
        public static void GenerateRegisterInternalCalls(AssemblyDefinition[] assemblies, TextWriter output)
        {
            output.Write("void RegisterAllStrippedInternalCalls ()\n{\n");

            foreach (AssemblyDefinition definition in assemblies)
                GenerateRegisterInternalCallsForTypes(definition.MainModule.Types, output);

            output.Write("}\n\n");
        }

        private static void GenerateRegisterInternalCallsForTypes(IEnumerable<TypeDefinition> types, TextWriter output)
        {
            foreach (TypeDefinition typeDefinition in types)
            {
                foreach (MethodDefinition method in typeDefinition.Methods)
                    GenerateInternalCallMethod(typeDefinition, method, output);

                GenerateRegisterInternalCallsForTypes(typeDefinition.NestedTypes, output);
            }
        }

        static void GenerateInternalCallMethod(TypeDefinition typeDefinition, MethodDefinition method, TextWriter output)
        {
            if (!method.IsInternalCall)
                return;

            string registerName = typeDefinition.FullName + "_" + method.Name;
            registerName = registerName.Replace('/', '_');
            registerName = registerName.Replace('.', '_');

            if (registerName.Contains("UnityEngine_Serialization"))
                return;

            output.WriteLine("\tvoid Register_{0} ();", registerName);
            output.WriteLine("\tRegister_{0} ();", registerName);
        }

        /*
        [MenuItem("Test/GenerateRegisterInternalCalls")]
        public static void GenerateRegisterInternalCallsTest()
        {
            AssemblyDefinition[] definition = { AssemblyFactory.GetAssembly("/Users/joe/Sources/unity-trunk/build/iPhonePlayer/Managed/UnityEngine.dll")};
            GenerateRegisterInternalCalls(definition, new StreamWriter("/test.txt"));
        }
        */
    }
}
