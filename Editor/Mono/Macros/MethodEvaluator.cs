// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml;
using UnityEngine.Assemblies;

namespace UnityEditor.Macros
{
    public static class MethodEvaluator
    {
        [DataContract(Name = "EvalDataParcel", Namespace = "com.unity3d.automation")]
        internal class EvalDataParcel
        {
            /// <summary>
            /// Paths to assembly file which contains the known type at the same index as KnownTypeNames.
            /// </summary>
            [DataMember]
            public string[] KnownTypeAssemblyPaths;
            /// <summary>
            /// Assembly qualified name of the known types which are required for arguments deserialization of EvalRemoteMethodCallParcel.
            /// </summary>
            [DataMember]
            public string[] KnownTypeNames;

            /// <summary>
            /// EvalRemoteMethodCallParcel binary data.
            /// </summary>
            [DataMember]
            public byte[] RemoteMethodCallParcelData;
        }

        [DataContract(Name = "EvalRemoteMethodCallParcel", Namespace = "com.unity3d.automation")]
        [KnownType(typeof(MethodEvaluatorSurrogateProvider.EnumSurrogate))]
        internal class EvalRemoteMethodCallParcel : IExtensibleDataObject
        {
            public ExtensionDataObject ExtensionData { get; set; }

            [DataMember]
            public string AssemblyPath;

            [DataMember]
            public string TypeName;

            [DataMember]
            public string MethodName;

            [DataMember]
            public string[] ParameterTypeNames;

            [DataMember]
            public object[] Arguments;
        }

        private class AssemblyResolver
        {
            private readonly string m_AssemblyDirectory;

            public AssemblyResolver(string assemblyDirectory)
            {
                m_AssemblyDirectory = assemblyDirectory;
            }

            public Assembly AssemblyResolve(object sender, ResolveEventArgs args)
            {
                var simpleName = args.Name.Split(',')[0];
                var assemblyFile = Path.Combine(m_AssemblyDirectory, simpleName + ".dll");
                if (File.Exists(assemblyFile))
                    return Assembly.LoadFrom(assemblyFile);

                return null;
            }


            public Assembly TypeResolve(object sender, ResolveEventArgs args)
            {
                var scriptAssembliesPath = Path.Combine(Directory.GetCurrentDirectory(), "Library/ScriptAssemblies/");
                if (!Directory.Exists(scriptAssembliesPath))
                    return null;

                //This removes (or at least aleviates) the requirement to qualify the type with the assembly name in tests.
                var dlls = Directory.GetFiles(scriptAssembliesPath, "*.dll");

                foreach (var dllPath in dlls)
                {
                    try
                    {
                        var assembly = CurrentAssemblies.LoadFromPath(dllPath);
                        if (assembly != null && Array.Exists(assembly.GetTypes(), t => t.Name == args.Name))
                            return assembly;
                    }
                    catch (Exception)
                    {
                    }
                }
                return null;
            }
        }

        public static object Eval(string assemblyFile, string typeName,
            string methodName, Type[] paramTypes, object[] args)
        {
            if (!File.Exists(assemblyFile))
            {
                throw new FileNotFoundException("The specified assembly file could not be found.", assemblyFile);
            }
            var assemblyDirectory = Path.GetDirectoryName(assemblyFile);
            // AssemblyResolve may be called from the context of Default AssemblyLoadContext
            // That breaks Code Reload on CoreCLR
            var assembly = CurrentAssemblies.LoadFromPath(assemblyFile);
            var method = assembly.GetType(typeName, true).GetMethod(methodName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                null, paramTypes, null);
            if (method == null)
                throw new ArgumentException(string.Format(
                    "Method {0}.{1}({2}) not found in assembly {3}!", typeName,
                    methodName, ToCommaSeparatedString(paramTypes),
                    assembly.FullName));
            return method.Invoke(null, args);
        }

        private static Type TypeOrTypeName(object o)
        {
            switch (o)
            {
                case Type t:
                    return t;
                case string s:
                    return Type.GetType(s);
                default:
                    throw new FormatException($"Expected {o} to be a string or a Type. It is a {o.GetType()}");
            }
        }

        private static Type[] TypesOrTypeNames(object o)
        {

            switch (o)
            {
                case Type[] t:
                    return t;
                case string[] s:
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    return s.Select(Type.GetType).ToArray();
#pragma warning restore UA2001
                default:
                    throw new FormatException($"Expected {o} to be a string[] or a Type[]. It is a {o.GetType()}");
            }
        }

        public static object ExecuteExternalCode(string parcel)
        {
            EvalDataParcel evalDataParcel;
            // Process EvalDataParcel to extract known types information which is required for
            // deserialization of the main EvalRemoteMethodCallParcel data.
            using (var dataStream = new MemoryStream(Convert.FromBase64String(parcel)))
            {
                // Setting quotas to max to avoid issues with large data.
                using var reader = XmlDictionaryReader.CreateTextReader(dataStream, XmlDictionaryReaderQuotas.Max);
                var serializer = new DataContractSerializer(typeof(EvalDataParcel));
                try
                {
                    evalDataParcel = (EvalDataParcel)serializer.ReadObject(reader, true);
                }
                catch (SerializationException)
                {
                    throw new Exception("Invalid EvalDataParcel data for external code execution.");
                }
            }

            // Prepare list of known types information. Add EnumSurrogate to support int to enum remapping.
            var knownTypeNames = new List<Type>() { typeof(MethodEvaluatorSurrogateProvider.EnumSurrogate) };
            for (var i = 0; i < evalDataParcel.KnownTypeNames.Length; ++i)
            {
                // Avoid loading assemblies which are already loaded - empty path means that this type is in system assembly.
                if (!string.IsNullOrEmpty(evalDataParcel.KnownTypeAssemblyPaths[i]) && File.Exists(evalDataParcel.KnownTypeAssemblyPaths[i]))
                    CurrentAssemblies.LoadFromPath(evalDataParcel.KnownTypeAssemblyPaths[i]);

                var type = Type.GetType(evalDataParcel.KnownTypeNames[i]);
                if (type == null)
                {
                    throw new Exception($"Invalid EvalDataParcel. Could not find type {evalDataParcel.KnownTypeNames[i]} in for external code execution arguments.");
                }
                knownTypeNames.Add(type);
            }

            // Now deserialize the main method call data including arguments
            EvalRemoteMethodCallParcel evalRemoteMethodCallParcel;
            using (var methodStream = new MemoryStream(evalDataParcel.RemoteMethodCallParcelData))
            {
                // Setting quotas to max to avoid issues with large data.
                using var reader = XmlDictionaryReader.CreateTextReader(methodStream, XmlDictionaryReaderQuotas.Max);
                var serializer = new DataContractSerializer(typeof(EvalRemoteMethodCallParcel), knownTypeNames);
                serializer.SetSerializationSurrogateProvider(new MethodEvaluatorSurrogateProvider());
                try
                {
                    evalRemoteMethodCallParcel = (EvalRemoteMethodCallParcel)serializer.ReadObject(reader, true);
                }
                catch (SerializationException)
                {
                    throw new Exception("Invalid EvalRemoteMethodCallParcel data for external code execution.");
                }
            }

            // Use AssemblyPath to load the assembly which contains the type we need to execute the method on.
            var assemblyPath = evalRemoteMethodCallParcel.AssemblyPath;
            AssemblyResolver resolver = null;
            if (assemblyPath != "netstandard")
            {
                resolver = new AssemblyResolver(Path.GetDirectoryName(assemblyPath));
                // TypeResolve is used to find types which are not defined in the Assembly.GetExecutingAssembly.
                // E.g. test methods which are called with MethodEvaluator are defined in the test assembly IntegrationTests.dll (explicitly or as generated delegates),
                // and thus are called but are called within executing assembly IntegrationTests.dll assembly.
                // However Unity scripts we generate in tests are copied to the generated project and are compiled into the project assembly (e.g. Assembly-CSharp.dll).
                // That makes expression like Type.GetType("DependencyScriptForPrefabs") fail, because DependencyScriptForPrefabs is defined in Assembly-CSharp.dll, not in IntegrationTests.dll
                // when the test runs in the Unity Editor.
                // So to fix that we add a TypeResolve handler which will try to find the type in the assemblies in the project itself.
#pragma warning disable UAC0006 // AppDomain usage - we remove the resolver in finally block
                AppDomain.CurrentDomain.AssemblyResolve += resolver.AssemblyResolve;
                AppDomain.CurrentDomain.TypeResolve += resolver.TypeResolve;
#pragma warning restore UAC0006 // AppDomain usage

            }

            try
            {
                // Retrieve the method's type.
                var type = TypeOrTypeName(evalRemoteMethodCallParcel.TypeName);
                if (type == null)
                {
                    throw new Exception($"Could not find type {evalRemoteMethodCallParcel.TypeName} in assembly {assemblyPath}.");
                }

                // Lookup the method itself using specified method name and parameter types.
                var methodName = evalRemoteMethodCallParcel.MethodName;
                const BindingFlags methodVisibility = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
                var methodParametersTypes = TypesOrTypeNames(evalRemoteMethodCallParcel.ParameterTypeNames);
                var method = type.GetMethod(methodName, methodVisibility, null, methodParametersTypes, null);
                if (method == null)
                {
                    throw new Exception(string.Format(
                        "Could not find method {0}.{1} in assembly {2} located in {3}.",
                        type.FullName, methodName, type.Assembly.GetName().Name, assemblyPath));
                }

                // Prepare object[] arguments transforming some of those from strings to object type.
                var arguments = evalRemoteMethodCallParcel.Arguments;
                TransformArgumentsBack(methodParametersTypes, arguments);

                // And finally invoke the method with all parameters processed.
                var returnValue = ExecuteCode(type, method, arguments);
                return returnValue;
            }
            finally
            {
                if (resolver != null)
                {
                    // Important touch to not leak the resolvers to Default AssemblyLoadContext.
#pragma warning disable UAC0006 // AppDomain usage
                AppDomain.CurrentDomain.AssemblyResolve -= resolver.AssemblyResolve;
                    AppDomain.CurrentDomain.TypeResolve -= resolver.TypeResolve;
#pragma warning restore UAC0006 // AppDomain usage
                }
            }
        }

        private static void  TransformArgumentsBack(Type[] argTypes, object[] args)
        {
            for(int i = 0; i<argTypes.Length; i++)
            {
                if (argTypes[i] == typeof(Type) && args[i] is string)
                {
                    args[i] = TypeOrTypeName(args[i]);
                }
                if (argTypes[i].IsSubclassOf(typeof(Delegate)) && args[i] is string str)
                {
                    args[i] = DeserializeDelegate(argTypes[i], str);
                }
            }
        }

        private static Delegate DeserializeDelegate(Type t, string serialized)
        {
            var parts = serialized.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var declaringType = TypeOrTypeName(parts[0]);
            var methodName = parts[1];
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var parameterTypes = TypesOrTypeNames(parts.Skip(2).ToArray());
#pragma warning restore UA2001
            const BindingFlags methodVisibility = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            var method = declaringType.GetMethod(methodName, methodVisibility, null, parameterTypes, null);
            if (method == null)
                throw new Exception(string.Format(
                    "Could not find method {0}.{1} in assembly {2}.",
                    declaringType.FullName, methodName, declaringType.Assembly.GetName().Name));
            return method.CreateDelegate(t);
        }

        private static object ExecuteCode(Type target, MethodInfo method, object[] args)
        {
            return method.Invoke(method.IsStatic ? null : GetActor(target), args);
        }

        private static object GetActor(Type type)
        {
            var ctorInfo = type.GetConstructor(Array.Empty<Type>());
            return ctorInfo != null ? ctorInfo.Invoke(Array.Empty<object>()) : null;
        }

        private static string ToCommaSeparatedString<T>(IEnumerable<T> items)
        {
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return string.Join(", ", items.Select(o => o.ToString()).ToArray());
#pragma warning restore UA2001
        }
    }
}
