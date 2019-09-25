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
        static readonly Regex k_BlockComments = new Regex(@"((?:\/\*(?:[^*]|(?:\*+[^*\/]))*\*+\/)|(?:\/\/.*))", RegexOptions.Compiled);
        static readonly Regex k_LineComments = new Regex(@"//.*?\n", RegexOptions.Compiled);
        static readonly Regex k_Strings = new Regex(@"""((\\[^\n]|[^""\n])*)""", RegexOptions.Compiled);
        static readonly Regex k_VerbatimStrings = new Regex(@"@(""[^""]*"")+", RegexOptions.Compiled);
        static readonly Regex k_NewlineRegex = new Regex("\r\n?", RegexOptions.Compiled);
        static readonly Regex k_SingleQuote = new Regex(@"((?<![\\])['])(?:.(?!(?<![\\])\1))*.?\1", RegexOptions.Compiled);
        static readonly Regex k_ConditionalCompilation = new Regex(@"[\t ]*#[\t ]*(if|else|elif|endif|define|undef)([\t !(]+[^/\n]*)?", RegexOptions.Compiled);
        static readonly Regex k_GenerateAuthoringComponentClassName = new Regex(@"\[GenerateAuthoringComponent\][\s|\n]*.*struct[\s|\n]*(\S*)", RegexOptions.Compiled);
        static readonly Regex k_Namespace = new Regex(@"\s*namespace\s.", RegexOptions.Compiled);
        static readonly string k_GenerateAuthoringComponentAttribute = "[GenerateAuthoringComponent]";
        static readonly string k_AuthoringComponentSuffix = "Authoring";

        // Used for detecting warning in PureCSharpTests
        public static Action<string> s_LogWarningAction;
        static CSharpNamespaceParser()
        {
            s_LogWarningAction = Debug.LogWarning;
        }

        public static void GetClassAndNamespace(string sourceCode, string className,
            out string outClassName, out string outNamespace)
        {
            bool namespaceParsed = false;
            outClassName = className;
            outNamespace = string.Empty;

            // Check for authoring component and try to parse it as class name with namespace if present
            var authoringComponentCodeIndex = sourceCode.IndexOf(k_GenerateAuthoringComponentAttribute,
                StringComparison.Ordinal);
            if (authoringComponentCodeIndex != -1)
            {
                string foundClassName = string.Empty;
                var codeFromAttribute = sourceCode.Substring(authoringComponentCodeIndex);
                var match = k_GenerateAuthoringComponentClassName.Match(codeFromAttribute);
                if (match.Groups.Count <= 1)
                    s_LogWarningAction($"Code contains {k_GenerateAuthoringComponentAttribute} attributes but no valid following struct.");
                else
                {
                    foundClassName = match.Groups[1].Value;
                    outClassName = foundClassName + k_AuthoringComponentSuffix;
                    outNamespace = FindNamespace(sourceCode, foundClassName, true);
                    namespaceParsed = true;
                }
            }

            // No authoring component attribute found, or we couldn't parse it, do normal namespace parsing
            if (!namespaceParsed)
            {
                outClassName = className;
                outNamespace = FindNamespace(sourceCode, className, false);
            }
        }

        static string FindNamespace(string sourceCode, string className, bool acceptStruct)
        {
            sourceCode = k_NewlineRegex.Replace(sourceCode, "\n");
            sourceCode = k_SingleQuote.Replace(sourceCode, "");
            sourceCode = k_Strings.Replace(sourceCode, "");
            sourceCode = k_BlockComments.Replace(sourceCode, "");
            sourceCode = k_LineComments.Replace(sourceCode, "\n");
            sourceCode = k_VerbatimStrings.Replace(sourceCode, "");
            try
            {
                sourceCode = ReduceCodeAndCheckForNamespacesModification(sourceCode, className);
                return FindClassAndNamespace(className, sourceCode, acceptStruct);
            }
            catch (Exception e)
            {
                throw new IllegalNamespaceParsing(className, e);
            }
        }

        static string FindClassAndNamespace(string className, string source, bool acceptStruct = false)
        {
            source = FixBraces(source);
            var split = source.Split(new[] { ' ', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            var parent = new Node { Name = "-1" };
            var builder = new StringBuilder(source.Length);
            var buildingNode = false;
            var buildingClass = false;
            var classAlreadyFoundInOtherNamespace = false;
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
                    case "struct":
                        if (acceptStruct)
                        {
                            buildingClass = true;
                            buildingNode = true;
                        }
                        break;
                    case "namespace":
                        buildingNode = true;
                        break;
                    default:
                        if (buildingNode)
                        {
                            var strippedClassname = StripClassName(token);
                            if (buildingClass)
                            {
                                buildingClass = false;
                                if (strippedClassname.Equals(className))
                                {
                                    var foundNamespace = CollectNamespace(parent);
                                    if (classAlreadyFoundInOtherNamespace && foundNamespace != resNamespace)
                                    {
                                        s_LogWarningAction(
                                            $"Class {className} can not exist in multiple namespaces in the same file, even if one is excluded with preprocessor directives. Please move these to separate files if this is the case.");
                                    }

                                    resNamespace = foundNamespace;
                                    classAlreadyFoundInOtherNamespace = true;
                                }
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

        static bool CheckForNamespaceModification(Stack<Tuple<bool, int>> namespaceScopeStack, int stackCount)
        {
            foreach (var tuple in namespaceScopeStack)
            {
                if (tuple.Item1 && (stackCount == -1 || tuple.Item2 == stackCount))
                    return true;
            }
            return false;
        }

        // Reduce code to path that assumes all definitions are true
        // Also check for the case where we have a namespace keyword inside any non-outter #if statement.
        static string ReduceCodeAndCheckForNamespacesModification(string source, string className)
        {
            var stack = new Stack<Tuple<bool, bool>>();
            var namespaceScopeStack = new Stack<Tuple<bool, int>>(); // <true if namespace scope, stack depth>
            var split = source.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
            var longest = split.Aggregate("", (max, cur) => max.Length > cur.Length ? max : cur);
            var stringBuilder = new StringBuilder(split.Length * longest.Length);
            bool foundNamespace = false;
            bool namespaceModificationFound = false;

            foreach (var s in split)
            {
                // Check for new namespace declarations, when we have are inside of multiple #ifs,
                // and also have seen previous namespace declarations (likely namespace modification).
                if (k_Namespace.IsMatch(s))
                {
                    namespaceModificationFound |= (stack.Count > 1 && CheckForNamespaceModification(namespaceScopeStack, -1));
                    foundNamespace = true;
                }
                if (s.IndexOf("{", StringComparison.Ordinal) >= 0)
                {
                    namespaceScopeStack.Push(new Tuple<bool, int>(foundNamespace, stack.Count));
                    foundNamespace = false;
                }
                if (s.IndexOf("}", StringComparison.Ordinal) >= 0)
                {
                    if (namespaceScopeStack.Count > 0)
                        namespaceScopeStack.Pop();
                }

                // Handle directives from here on down
                if (s.IndexOf("#", StringComparison.Ordinal) < 0)
                {
                    if (stack.Count == 0 || stack.Peek().Item1)
                        stringBuilder.Append(s).Append("\n");
                    continue;
                }

                var match = k_ConditionalCompilation.Match(s);
                var directive = match.Groups[1].Value;
                if (directive == "else")
                {
                    namespaceModificationFound |= CheckForNamespaceModification(namespaceScopeStack, stack.Count);
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
                else if (directive == "if")
                {
                    var evalResult = true;
                    var isEmitting = stack.Count == 0 || stack.Peek().Item1;
                    stack.Push(new Tuple<bool, bool>(isEmitting && evalResult, isEmitting && !evalResult));
                }
                else if (directive == "elif")
                {
                    namespaceModificationFound |= CheckForNamespaceModification(namespaceScopeStack, stack.Count);
                    var evalResult = true;
                    var elseEmitting = stack.Peek().Item2;
                    stack.Pop();
                    stack.Push(new Tuple<bool, bool>(elseEmitting && evalResult, elseEmitting && !evalResult));
                }
            }

            if (namespaceModificationFound)
                s_LogWarningAction(
                    $"While looking for class {className} a namespace modification was detected. Namespace modification with preprocessor directives is not supported. Please ensure that all directives do not change the namespaces of types.");

            return stringBuilder.ToString();
        }

        static bool IsNullOrWhiteSpace(string value)
        {
            return value == null || value.All(char.IsWhiteSpace);
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
