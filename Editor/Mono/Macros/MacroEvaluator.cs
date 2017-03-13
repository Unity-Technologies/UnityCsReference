// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityScript.Scripting;

namespace UnityEditor.Macros
{
    public static class MacroEvaluator
    {
        public static string Eval(string macro)
        {
            if (macro.StartsWith("ExecuteMethod: "))
                return ExecuteMethodThroughReflection(macro);


            var value = Evaluator.Eval(EditorEvaluationContext, macro);
            return value == null ? "Null" : value.ToString();
        }

        private static string ExecuteMethodThroughReflection(string macro)
        {
            var regex = new Regex("ExecuteMethod: (?<type>.*)\\.(?<method>.*)");
            var match = regex.Match(macro);
            var typename = match.Groups["type"].ToString();
            var methodname = match.Groups["method"].ToString();

            var type = EditorAssemblies.loadedAssemblies.Select(a => a.GetType(typename, false)).Where(t => t != null).First();
            var method = type.GetMethod(methodname, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null)
                throw new ArgumentException(String.Format("cannot find method {0} in type {1}", methodname, typename));
            if (method.GetParameters().Length > 0)
                throw new ArgumentException("You can only invoke static methods with no arguments");
            var result = method.Invoke(null, new object[0]);
            return result == null ? "Null" : result.ToString();
        }

        private static readonly EvaluationContext EditorEvaluationContext = new EvaluationContext(new EditorEvaluationDomainProvider());

        private class EditorEvaluationDomainProvider : SimpleEvaluationDomainProvider
        {
            private static readonly string[] DefaultImports = new[] { "UnityEditor", "UnityEngine" };

            public EditorEvaluationDomainProvider() : base(DefaultImports)
            {
            }

            public override Assembly[] GetAssemblyReferences()
            {
                var editorAssemblies = EditorAssemblies.loadedAssemblies;
                var referencedAssemblies = editorAssemblies.SelectMany(a => a.GetReferencedAssemblies()).Select(a => TryToLoad(a)).Where(a => a != null);
                return editorAssemblies.Concat(referencedAssemblies).ToArray();
            }

            private static Assembly TryToLoad(AssemblyName a)
            {
                try
                {
                    return Assembly.Load(a);
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }
    }
}
