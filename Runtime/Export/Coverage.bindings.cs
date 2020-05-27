// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using System.Reflection;
using System.Linq;
using System.Text;
using UnityEngine.Scripting;

namespace UnityEngine.TestTools
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Struct)]
    [UsedByNativeCode]
    public class ExcludeFromCoverageAttribute : Attribute
    {
    }

    [NativeType(CodegenOptions.Custom, "ManagedCoveredSequencePoint", Header = "Runtime/Scripting/ScriptingCoverage.bindings.h")]
    public struct CoveredSequencePoint
    {
        public MethodBase method;
        public UInt32 ilOffset;
        public UInt32 hitCount;
        public string filename;
        public UInt32 line;
        public UInt32 column;
    }

    [NativeType(CodegenOptions.Custom, "ManagedCoveredMethodStats", Header = "Runtime/Scripting/ScriptingCoverage.bindings.h")]
    public struct CoveredMethodStats
    {
        public MethodBase method;
        public int totalSequencePoints;
        public int uncoveredSequencePoints;

        private string GetTypeDisplayName(Type t)
        {
            if (t == typeof(int))
                return "int";
            if (t == typeof(bool))
                return "bool";
            if (t == typeof(float))
                return "float";
            if (t == typeof(double))
                return "double";
            if (t == typeof(void))
                return "void";
            if (t == typeof(string))
                return "string";

            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(System.Collections.Generic.List<>))
            {
                return "System.Collections.Generic.List<" + GetTypeDisplayName(t.GetGenericArguments()[0]) + ">";
            }

            if (t.IsArray && t.GetArrayRank() == 1)
            {
                return GetTypeDisplayName(t.GetElementType()) + "[]";
            }

            return t.FullName;
        }

        public override string ToString()
        {
            if (method == null)
                return "<no method>";

            var sb = new StringBuilder();
            sb.Append(GetTypeDisplayName(method.DeclaringType));
            sb.Append(".");
            sb.Append(method.Name);
            sb.Append("(");

            bool didAppendParam = false;
            foreach (var param in method.GetParameters())
            {
                if (didAppendParam)
                    sb.Append(", ");
                sb.Append(GetTypeDisplayName(param.ParameterType));
                sb.Append(" ");
                sb.Append(param.Name);
                didAppendParam = true;
            }

            sb.Append(")");

            return sb.ToString();
        }
    }

    [NativeType("Runtime/Scripting/ScriptingCoverage.h")]
    [NativeClass("ScriptingCoverage")]
    public static class Coverage
    {
        public extern static bool enabled { get; set; }

        [FreeFunction("ScriptingCoverageGetCoverageForMethodInfoObject", ThrowsException = true)]
        private extern static CoveredSequencePoint[] GetSequencePointsFor_Internal(MethodBase method);

        [FreeFunction("ScriptingCoverageResetForMethodInfoObject", ThrowsException = true)]
        private extern static void ResetFor_Internal(MethodBase method);

        [FreeFunction("ScriptingCoverageGetStatsForMethodInfoObject", ThrowsException = true)]
        private extern static CoveredMethodStats GetStatsFor_Internal(MethodBase method);

        public static CoveredSequencePoint[] GetSequencePointsFor(MethodBase method)
        {
            if (method == null)
                throw new ArgumentNullException("method");

            return GetSequencePointsFor_Internal(method);
        }

        public static CoveredMethodStats GetStatsFor(MethodBase method)
        {
            if (method == null)
                throw new ArgumentNullException("method");

            return GetStatsFor_Internal(method);
        }

        public static CoveredMethodStats[] GetStatsFor(MethodBase[] methods)
        {
            if (methods == null)
                throw new ArgumentNullException("methods");

            var result = new CoveredMethodStats[methods.Length];
            for (int i = 0; i < methods.Length; ++i)
                result[i] = GetStatsFor(methods[i]);
            return result;
        }

        public static CoveredMethodStats[] GetStatsFor(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            return GetStatsFor(type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                BindingFlags.Static | BindingFlags.DeclaredOnly).OfType<MethodBase>().ToArray());
        }

        [FreeFunction("ScriptingCoverageGetStatsForAllCoveredMethodsFromScripting", ThrowsException = true)]
        public static extern CoveredMethodStats[] GetStatsForAllCoveredMethods();

        public static void ResetFor(MethodBase method)
        {
            if (method == null)
                throw new ArgumentNullException("method");

            ResetFor_Internal(method);
        }

        [FreeFunction("ScriptingCoverageResetAllFromScripting", ThrowsException = true)]
        public extern static void ResetAll();
    }
}
