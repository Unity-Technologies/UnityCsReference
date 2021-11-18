// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;

namespace UnityEditor.Macros
{
    public static class MethodEvaluator
    {
        private static readonly BinaryFormatter s_Formatter =
            new BinaryFormatter { AssemblyFormat = FormatterAssemblyStyle.Simple };

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
                //This removes (or at least aleviates) the requirement to qualify the type with the assembly name in tests.
                var dlls = Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "Library/ScriptAssemblies/"), "*.dll");
                foreach (var dllPath in dlls)
                {
                    try
                    {
                        var assembly = Assembly.LoadFrom(dllPath);
                        if (assembly != null && assembly.GetTypes().Any(t => t.Name == args.Name))
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
            var assemblyDirectory = Path.GetDirectoryName(assemblyFile);
            var resolver = new AssemblyResolver(assemblyDirectory);
            AppDomain.CurrentDomain.AssemblyResolve += resolver.AssemblyResolve;
            try
            {
                var assembly = Assembly.LoadFrom(assemblyFile);
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
            finally
            {
                AppDomain.CurrentDomain.AssemblyResolve -= resolver.AssemblyResolve;
            }
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
                    return s.Select(Type.GetType).ToArray();
                default:
                    throw new FormatException($"Expected {o} to be a string[] or a Type[]. It is a {o.GetType()}");
            }
        }

        public static object ExecuteExternalCode(string parcel)
        {
            using (var stream = new MemoryStream(Convert.FromBase64String(parcel)))
            {
                var header = (string)s_Formatter.Deserialize(stream);
                if (header != "com.unity3d.automation")
                    throw new Exception("Invalid parcel for external code execution.");
                var assemblyPath = (string)s_Formatter.Deserialize(stream);
                AssemblyResolver resolver = null;
                Assembly assembly = null;
                if (assemblyPath != "netstandard") {
                    resolver = new AssemblyResolver(Path.GetDirectoryName(assemblyPath));

                    AppDomain.CurrentDomain.AssemblyResolve += resolver.AssemblyResolve;
                    AppDomain.CurrentDomain.TypeResolve += resolver.TypeResolve;
                    assembly = Assembly.LoadFrom(assemblyPath);
                }
                try
                {
                    var type = TypeOrTypeName(s_Formatter.Deserialize(stream));

                    var methodName = (string)s_Formatter.Deserialize(stream);

                    const BindingFlags methodVisibility = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
                    var methodParametersTypes = TypesOrTypeNames(s_Formatter.Deserialize(stream));

                    var method = type.GetMethod(methodName, methodVisibility, null, methodParametersTypes, null);
                    if (method == null)
                        throw new Exception(string.Format(
                            "Could not find method {0}.{1} in assembly {2} located in {3}.",
                            type.FullName, methodName, type.Assembly.GetName().Name, assemblyPath));

                    var arguments = (object[])s_Formatter.Deserialize(stream);
                    TransformArgumentsBack(methodParametersTypes, arguments);
                    var returnValue = ExecuteCode(type, method, arguments);
                    return returnValue;
                }
                finally
                {
                    if (resolver != null)
                    {
                        AppDomain.CurrentDomain.AssemblyResolve -= resolver.AssemblyResolve;
                    }
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
            var parameterTypes = TypesOrTypeNames(parts.Skip(2).ToArray());
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
            var ctorInfo = type.GetConstructor(new Type[] {});
            return ctorInfo != null ? ctorInfo.Invoke(new object[] {}) : null;
        }

        private static string ToCommaSeparatedString<T>(IEnumerable<T> items)
        {
            return string.Join(", ", items.Select(o => o.ToString()).ToArray());
        }
    }
}
