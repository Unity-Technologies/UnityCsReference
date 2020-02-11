// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace UnityEditor.Scripting.ScriptCompilation
{
    internal class IllegalNamespaceParsing : Exception
    {
        public IllegalNamespaceParsing(string className, Exception cause)
            : base($"Searching for classname: '{className}' caused error in CSharpNameParser", cause)
        {
        }
    }

    internal class UnsupportedDefineExpression : Exception
    {
        public UnsupportedDefineExpression(string message) : base(message) {}
    }

    internal static class CSharpNamespaceParser
    {
        static readonly Regex k_ReDefineExpr = new Regex(@"r'\s+|([=!]=)\s*(true|false)|([_a-zA-Z][_a-zA-Z0-9]*)|([()!]|&&|\|\|)", RegexOptions.Compiled);
        static readonly Regex k_BlockComments = new Regex(@"((?:\/\*(?:[^*]|(?:\*+[^*\/]))*\*+\/)|(?:\/\/.*))", RegexOptions.Compiled);
        static readonly Regex k_LineComments = new Regex(@"//.*?\n", RegexOptions.Compiled);
        static readonly Regex k_Strings = new Regex(@"""((\\[^\n]|[^""\n])*)""", RegexOptions.Compiled);
        static readonly Regex k_VerbatimStrings = new Regex(@"@(""[^""]*"")+", RegexOptions.Compiled);
        static readonly Regex k_NewlineRegex = new Regex("\r\n?", RegexOptions.Compiled);
        static readonly Regex k_SingleQuote = new Regex(@"(?<![\\])'(\\[uUx]\w+|\\.|.)'", RegexOptions.Compiled);
        static readonly Regex k_ConditionalCompilation = new Regex(@"[\t ]*#[\t ]*(if|else|elif|endif|define|undef)([\t !(]+[^/\n]*)?", RegexOptions.Compiled);
        static string s_ClassName;

        public static string GetNamespace(string sourceCode, string className, params string[] defines)
        {
            s_ClassName = className;

            sourceCode = k_NewlineRegex.Replace(sourceCode, "\n");
            sourceCode = k_SingleQuote.Replace(sourceCode, "");
            sourceCode = k_Strings.Replace(sourceCode, "");
            sourceCode = k_BlockComments.Replace(sourceCode, "");
            sourceCode = k_LineComments.Replace(sourceCode, "\n");
            sourceCode = k_VerbatimStrings.Replace(sourceCode, "");
            try
            {
                sourceCode = RemoveUnusedDefines(sourceCode, defines.ToList());

                return FindNamespaceForMono(className, sourceCode);
            }
            catch (Exception e)
            {
                throw new IllegalNamespaceParsing(className, e);
            }
        }

        static string FindNamespaceForMono(string className, string source)
        {
            source = FixBraces(source);
            var split = source.Split(new[] { ' ', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            var parent = new Node { Name = "-1" };
            var builder = new StringBuilder(source.Length);
            var buildingNode = false;
            var buildingClass = false;
            var level = 0;
            var resNamespace = "";
            foreach (var token in split)
            {
                switch (token)
                {
                    case "{":
                        if (buildingNode)
                        {
                            parent = AddCurrent(level, builder.ToString(), parent);
                            builder = new StringBuilder();
                            buildingNode = false;
                        }

                        level++;

                        break;
                    case "}":
                        if (parent.Level > --level)
                        {
                            parent = parent.Parent;
                            builder.Clear();
                        }
                        break;
                    case "class":
                        buildingClass = true;
                        buildingNode = true;
                        break;
                    case "namespace":
                        buildingNode = true;
                        break;
                    default:
                        if (buildingNode)
                        {
                            var strippedClassname = StripClassName(token);
                            if (buildingClass && strippedClassname.Equals(className))
                            {
                                buildingClass = false;
                                resNamespace = CollectNamespace(parent);
                            }
                            else
                            {
                                builder.Append(token);
                            }
                        }

                        break;
                }
            }

            return resNamespace;
        }

        static string StripClassName(string classname)
        {
            var strippedClassname = classname.Contains(":") ? classname.Split(':')[0] : classname;
            strippedClassname = strippedClassname.StartsWith("@") ? strippedClassname.Split('@')[1] : strippedClassname;

            return strippedClassname;
        }

        static string FixBraces(string sourceCode)
        {
            var stringBuilder = new StringBuilder(sourceCode.Length * 2);
            var lastChar = '-';
            foreach (var c in sourceCode.ToCharArray())
            {
                if ((c == '{' || c == '}') && (lastChar != '\n' || lastChar != ' '))
                    stringBuilder.Append(' ');
                if ((lastChar == '{' || lastChar == '}') && (c != '\n' || c != ' '))
                    stringBuilder.Append(' ');
                stringBuilder.Append(c);
                lastChar = c;
            }

            return stringBuilder.ToString();
        }

        static string CollectNamespace(Node parent)
        {
            var list = new List<string>();
            for (var par = parent; par.Name != "-1"; par = par.Parent) { list.Add(par.Name); }

            if (list.Count == 0) return "";

            list.Reverse();
            return list.Aggregate((a, b) => a + "." + b);
        }

        static Node AddCurrent(int level, string s, Node parent)
        {
            return new Node { Level = level + 1, Name = s, Parent = parent };
        }

        class Node
        {
            public int Level;
            public string Name;
            public Node Parent;
        }

        static string RemoveUnusedDefines(string source, List<string> defines)
        {
            var stack = new Stack<Tuple<bool, bool>>();
            var split = source.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
            var longest = split.Aggregate("", (max, cur) => max.Length > cur.Length ? max : cur);
            var stringBuilder = new StringBuilder(split.Length * longest.Length);
            foreach (var s in split)
            {
                if (s.IndexOf("#", StringComparison.Ordinal) < 0)
                {
                    if (stack.Count == 0 || stack.Peek().Item1)
                    {
                        stringBuilder.Append(s).Append("\n");
                    }

                    continue;
                }

                var match = k_ConditionalCompilation.Match(s);
                var directive = match.Groups[1].Value;
                if (directive == "else")
                {
                    var elseEmitting = stack.Peek().Item2;
                    stack.Pop();
                    stack.Push(new Tuple<bool, bool>(elseEmitting, false));
                    continue;
                }
                if (directive == "endif")
                {
                    stack.Pop();
                    continue;
                }

                var arg = match.Groups[2].Value.Trim();
                if (directive.Length > 0 && arg.Length == 0)
                {
                    throw new UnsupportedDefineExpression(s);
                }

                if (directive == "define")
                {
                    if (!defines.Contains(arg) && (stack.Count == 0 || stack.Peek().Item1))
                    {
                        defines.Add(arg);
                    }
                }
                else if (directive == "undefine")
                {
                    if (stack.Count == 0 || stack.Peek().Item1)
                    {
                        defines.Remove(arg);
                    }
                }
                else if (directive == "if")
                {
                    var evalResult = EvaluateDefine(arg.Trim(), defines);
                    var isEmitting = stack.Count == 0 || stack.Peek().Item1;
                    stack.Push(new Tuple<bool, bool>(isEmitting && evalResult, isEmitting && !evalResult));
                }
                else if (directive == "elif")
                {
                    var evalResult = EvaluateDefine(arg, defines);
                    var elseEmitting = stack.Peek().Item2;
                    stack.Pop();
                    stack.Push(new Tuple<bool, bool>(elseEmitting && evalResult, elseEmitting && !evalResult));
                }
            }

            return stringBuilder.ToString();
        }

        static bool IsNullOrWhiteSpace(string value)
        {
            return value == null || value.All(char.IsWhiteSpace);
        }

        public static bool EvaluateDefine(string expr, ICollection<string> defines)
        {
            var res = new List<string>();
            var pos = 0;
            while (pos < expr.Length)
            {
                var match = k_ReDefineExpr.Match(expr, pos); // eq_operator, bool_val, symbol, operator
                // TODO: C# 4.0+ Replace with string.IsNullOrWhiteSpace when available
                if (IsNullOrWhiteSpace(expr.Substring(pos)))
                    break;

                if (!match.Success)
                    throw new InvalidOperationException($"Error while searching for {s_ClassName}: invalid #ifdef expression: {expr} (error while searching for {expr.Substring(pos)}");

                pos = match.Index + match.Length;

                if (match.Groups[1].Success)
                    res.Add(match.Groups[1].Value + (match.Groups[2].Value == "true").ToString().ToLower());
                else if (match.Groups[3].Value == "true" || match.Groups[3].Value == "false")
                    res.Add((match.Groups[3].Value == "true").ToString().ToLower());
                else if (match.Groups[3].Success)
                    res.Add(defines.Contains(match.Groups[3].Value).ToString().ToLower());
                else if (match.Groups[4].Success)
                    res.Add(match.Groups[4].Value);
            }

            try
            {
                return EvaluateBooleanExpression(string.Join(" ", res.ToArray()));
            }
            catch (InvalidOperationException)
            {
                throw new UnsupportedDefineExpression($"{expr}: caused an error in CSharpNamespaceParser");
            }
        }

        static bool EvaluateBooleanExpression(string expression)
        {
            expression = expression.Replace("&&", "&").Replace("||", "|").Replace("==", "=");
            expression = expression.Replace("true", "1").Replace("false", "0");
            expression = expression.Replace(" ", string.Empty);

            return EvaluateBool(expression);
        }

        static bool EvaluateBool(string expression)
        {
            var tokens = expression.ToCharArray();
            var values = new Stack<bool>();
            var ops = new Stack<char>();

            foreach (var token in tokens)
            {
                if (token == '0' || token == '1') values.Push(token == '1');
                else if (token == '(') ops.Push(token);
                else if (token == ')')
                {
                    for (var nextOp = ops.Pop(); nextOp != '('; nextOp = ops.Pop())
                        values.Push(ApplyOp(nextOp, values));
                }
                else if (token == '&' || token == '|' || token == '!' || token == '=')
                {
                    while (ops.Count != 0 && HasPrecedence(token, ops.Peek()))
                    {
                        values.Push(ApplyOp(ops.Pop(), values));
                    }

                    ops.Push(token);
                }
            }

            while (ops.Count != 0)
            {
                values.Push(ApplyOp(ops.Pop(), values));
            }

            return values.Pop();
        }

        static bool ApplyOp(char op, Stack<bool> values)
        {
            var val1 = values.Pop();
            switch (op)
            {
                case '&':
                {
                    var val2 = values.Pop();
                    return val1 && val2;
                }
                case '|':
                {
                    var val2 = values.Pop();
                    return val1 || val2;
                }
                case '!': return !val1;
                case '=': return val1 == values.Pop();
                default:
                    throw new NotImplementedException($"{op}: unrecognized operator");
            }
        }

        /// <returns>Returns whether 'op2' has higher or same precedence as 'op1'.</returns>
        static bool HasPrecedence(char op1, char op2)
        {
            if (op2 == '(' || op2 == ')') return false;
            if (op1 == '!' && op2 == '&') return false;
            if (op1 == '!' && op2 == '|') return false;
            if (op1 == '&' && op2 == '|') return false;
            if (op1 == '=') return false;

            return true;
        }
    }
}
