// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace UnityEditor.Macros
{
    public static class MacroEvaluator
    {
        public static string Eval(string macro)
        {
            //TODO: Make sure we still need these 2 ways to run code. I can't find any reference to these names (ExecuteMethod, ExecuteMethod2)
            if (macro.StartsWith("ExecuteMethod: ") || macro.StartsWith("ExecuteMethod2: "))
                return ExecuteMethodThroughReflection(macro);

            var ret = MethodEvaluator.ExecuteExternalCode(macro);
            return ret == null ? "Null" : ret.ToString();
        }

        private static string ExecuteMethodThroughReflection(string macro)
        {
            try
            {
                var regex = new Regex("ExecuteMethod: (?<type>.*)\\.(?<method>.*)");
                var match = regex.Match(macro);
                var typename = match.Groups["type"].ToString();
                var methodname = match.Groups["method"].ToString();

                var type = EditorAssemblies.loadedAssemblies.SelectMany(a => a.GetTypes()).Where(t => t.FullName == typename).FirstOrDefault();
                if (type == null)
                {
                    var regex2 = new Regex(@"ExecuteMethod2: (?<assembly>[^,]+),(?<type>.*)\.(?<method>.*)");
                    match = regex2.Match(macro);

                    typename = match.Groups["type"].ToString();
                    methodname = match.Groups["method"].ToString();
                    var assembly = match.Groups["assembly"].ToString();

                    var aa = Assembly.LoadFrom(assembly);
                    type = aa.GetTypes().Where(t => t.FullName == typename).FirstOrDefault();
                    if (type == null)
                    {
                        var sb = new System.Text.StringBuilder();
                        foreach (var tt in aa.GetTypes())
                        {
                            sb.AppendFormat("\t{0}\r\n", tt.FullName);
                        }

                        throw new ArgumentException(String.Format("cannot find Type {0}. Looked int: \r\n{1}", typename, sb));
                    }
                }
                var method = type.GetMethod(methodname, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (method == null)
                    throw new ArgumentException(String.Format("cannot find method {0} in type {1}", methodname, typename));
                if (method.GetParameters().Length > 0)
                    throw new ArgumentException("You can only invoke static methods with no arguments");
                var result = method.Invoke(null, new object[0]);
                return result == null ? "Null" : result.ToString();
            }
            catch (Exception ex)
            {
                while (ex != null)
                {
                    Console.WriteLine("{0}\r\n{1}", ex.Message, ex.StackTrace);
                    ex = ex.InnerException;
                }

                throw;
            }
        }
    }
}
