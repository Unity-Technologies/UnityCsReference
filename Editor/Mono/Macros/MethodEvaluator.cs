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

        public static object ExecuteExternalCode(string parcel)
        {
            using (var stream = new MemoryStream(Convert.FromBase64String(parcel)))
            {
                var header = (string)s_Formatter.Deserialize(stream);
                if (header != "com.unity3d.automation")
                    throw new Exception("Invalid parcel for external code execution.");
                var assemblyPath = (string)s_Formatter.Deserialize(stream);
                var resolver = new AssemblyResolver(Path.GetDirectoryName(assemblyPath));
                AppDomain.CurrentDomain.AssemblyResolve += resolver.AssemblyResolve;
                var assembly = Assembly.LoadFrom(assemblyPath);
                try
                {
                    var type = (Type)s_Formatter.Deserialize(stream);
                    var methodName = (string)s_Formatter.Deserialize(stream);
                    const BindingFlags methodVisibility = BindingFlags.Public |
                        BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
                    var methodParametersTypes = (Type[])s_Formatter.Deserialize(stream);
                    var method = type.GetMethod(methodName, methodVisibility, null,
                            methodParametersTypes, null);
                    if (method == null)
                        throw new Exception(string.Format(
                                "Could not find method {0}.{1} in assembly {2} located in {3}.",
                                type.FullName, methodName, assembly.GetName().Name, assemblyPath));
                    var arguments = (object[])s_Formatter.Deserialize(stream);
                    return ExecuteCode(type, method, arguments);
                }
                finally
                {
                    AppDomain.CurrentDomain.AssemblyResolve -= resolver.AssemblyResolve;
                }
            }
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
