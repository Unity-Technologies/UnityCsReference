// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace UnityEditor.Search
{
    delegate SearchExpression SearchExpressionParserWithStructHandler(SearchExpressionParserArgs args);
    delegate SearchExpression SearchExpressionParserWithContextHandler(string text, SearchContext context);
    delegate SearchExpression SearchExpressionParserHandler(string text);
    delegate SearchExpression SearchExpressionParserHandlerStringView(StringView text);

    [Flags]
    enum SearchExpressionParserFlags
    {
        None = 0,
        ImplicitLiterals = 1 << 0,
        CanContainAliases = 1 << 1,
        ValidateSignature = 1 << 2,

        Default = CanContainAliases | ValidateSignature
    }

    enum BuiltinParserPriority : int
    {
        Alias = 1,
        Named = 5,
        Set = 9,
        Number = 10,
        Bool = 20,
        Expand = 21,
        ImplicitStringLiteral = 29,
        String = 30,
        Query = 1000
    }

    [AttributeUsage(AttributeTargets.Method)]
    class SearchExpressionParserAttribute : Attribute
    {
        public string name;
        public int priority;

        public SearchExpressionParserAttribute(string name, int priority)
        {
            this.name = name;
            this.priority = priority;
        }

        internal SearchExpressionParserAttribute(string name, BuiltinParserPriority priority)
        {
            this.name = name;
            this.priority = (int)priority;
        }
    }

    readonly struct SearchExpressionParserArgs
    {
        public readonly StringView text;
        public readonly SearchContext context;
        public readonly SearchExpressionParserFlags options;

        public SearchExpressionParserArgs(StringView text, SearchContext context = null, SearchExpressionParserFlags options = SearchExpressionParserFlags.Default)
        {
            this.text = text.Trim();
            this.context = context;
            this.options = options;
        }

        public SearchExpressionParserArgs(SearchContext context, SearchExpressionParserFlags options = SearchExpressionParserFlags.Default)
            : this(new StringView(context.searchText), context, options)
        {
        }

        internal SearchExpressionParserArgs With(StringView newText, SearchExpressionParserFlags optionsToAdd = SearchExpressionParserFlags.None)
        {
            return new SearchExpressionParserArgs(newText, context, options | optionsToAdd);
        }

        public SearchExpressionParserArgs With(SearchExpressionParserFlags optionsToAdd)
        {
            return With(text, optionsToAdd);
        }

        public SearchExpressionParserArgs Without(SearchExpressionParserFlags optionsToRemove)
        {
            return new SearchExpressionParserArgs(text, context, options & ~optionsToRemove);
        }

        public bool HasOption(SearchExpressionParserFlags option)
        {
            return options.HasFlag(option);
        }

        public override string ToString()
        {
            return $"{text} ({options})";
        }
    }

    readonly struct SearchExpressionParser
    {
        public readonly int priority;
        public readonly string name;
        public readonly SearchExpressionParserWithStructHandler handler;

        public SearchExpressionParser(string name, int priority, SearchExpressionParserWithStructHandler handler)
        {
            this.name = name;
            this.priority = priority;
            this.handler = handler;
        }

        public override string ToString()
        {
            return $"{name} | {handler.Method.DeclaringType.FullName}.{handler.Method.Name} | priority: {priority}";
        }
    }

    class SearchExpressionParseException : Exception
    {
        public int index;
        public int length;
        public SearchExpressionParseException(string message, int index, int length)
            : base(message)
        {
            this.index = index;
            this.length = length;
        }
    }

    static class ParserManager
    {
        public static List<SearchExpressionParser> parsers;

        static ParserManager()
        {
            RefreshParsers();
        }

        public static void RefreshParsers()
        {
            var supportedSignatures = new[]
            {
                MethodSignature.FromDelegate<SearchExpressionParserHandler>(),
                MethodSignature.FromDelegate<SearchExpressionParserHandlerStringView>(),
                MethodSignature.FromDelegate<SearchExpressionParserWithContextHandler>(),
                MethodSignature.FromDelegate<SearchExpressionParserWithStructHandler>()
            };
            parsers = ReflectionUtils.LoadAllMethodsWithAttribute<SearchExpressionParserAttribute, SearchExpressionParser>(
                (mi, attribute, handler) =>
                {
                    if (handler is SearchExpressionParserWithStructHandler handlerWithStruct)
                        return new SearchExpressionParser(attribute.name, attribute.priority, handlerWithStruct);
                    if (handler is SearchExpressionParserWithContextHandler handlerWithContext)
                        return new SearchExpressionParser(attribute.name, attribute.priority, args => handlerWithContext(args.text.ToString(), args.context));
                    if (handler is SearchExpressionParserHandlerStringView handlerStringView)
                        return new SearchExpressionParser(attribute.name, attribute.priority, (args) => handlerStringView(args.text));
                    if (handler is SearchExpressionParserHandler handlerNoContext)
                        return new SearchExpressionParser(attribute.name, attribute.priority, (args) => handlerNoContext(args.text.ToString()));
                    throw new CustomAttributeFormatException($"Invalid parser handler {attribute.name} using {mi.DeclaringType.FullName}.{mi.Name}");
                },
                supportedSignatures).ToList();
            parsers.Sort((a, b) => a.priority.CompareTo(b.priority));
        }

        public static SearchExpression Parse(SearchExpressionParserArgs args)
        {
            foreach (var parser in parsers)
            {
                var expr = parser.handler(args);
                if (expr != null)
                    return expr;
            }

            // TODO: this is an error : we were not able to parse the root expression
            throw new SearchExpressionParseException($"Expression `{args.text}` cannot be parsed.\n" +
                $"{string.Join("\n", args.context.GetAllErrors().Select(e => e.reason))}".Trim(), args.text.startIndex, args.text.Length);
        }
    }

    static class ParserUtils
    {
        private static readonly char[] k_Quotes = { '\'', '"' };
        private static readonly char[] k_Openers = { '[', '{' };
        private static readonly char[] k_Closers = { ']', '}' };

        public static readonly string k_QueryWithSelectorPattern = @"(^|\s|[^\w])([@$][\w\d]+([.][\w\d]+)*)";
        public static readonly Regex namedExpressionStartRegex = new Regex(@"(?<name>[a-zA-Z0-9_\-]*?){");

        public static StringView[] ExtractArguments(StringView text, string errorPrefix = "")
        {
            var paramsBlock = text;
            var expressionParams = new List<StringView>();
            var paramStartIndex = -1;
            var lastCommaIndex = -1;
            Stack<char> openersStack = new Stack<char>();
            openersStack.Push(paramsBlock[0]);
            int currentStringTokenIndex = -1;
            var i = 1;
            for (; i < paramsBlock.Length; ++i)
            {
                if (paramsBlock[i] == ' ')
                    continue;
                if (paramStartIndex == -1)
                    paramStartIndex = i;

                // In case of a string, we must find the end of the string before checking any nested levels or ,
                if (k_Quotes.Any(c => c == paramsBlock[i]))
                {
                    if (currentStringTokenIndex == -1)
                        currentStringTokenIndex = i;
                    // Already tested in GetExpressionsStartAndLength
                    //else if (paramsBlock[currentStringTokenIndex] != paramsBlock[i])
                    //throw new SearchExpressionParseException($"Nested strings are not allowed);
                    else currentStringTokenIndex = -1;
                }
                if (currentStringTokenIndex != -1) // is in string
                    continue;

                if (k_Openers.Any(c => c == paramsBlock[i]))
                {
                    openersStack.Push(paramsBlock[i]);
                    continue;
                }

                if (k_Closers.Any(c => c == paramsBlock[i]))
                {
                    if (CharMatchOpener(openersStack.Peek(), paramsBlock[i]))
                    {
                        openersStack.Pop();
                        // We found the final closer, that means we found the end of the expression
                        if (openersStack.Count == 0)
                        {
                            var startIndex = GetExpressionStartIndex(paramStartIndex, lastCommaIndex);
                            if (i - startIndex != 0)
                                expressionParams.Add(paramsBlock.Substring(startIndex, i - startIndex).Trim());
                            else if (lastCommaIndex != -1)
                                throw new SearchExpressionParseException($"Last argument missing in \"{errorPrefix + text.Substring(0, i + 1)}\"", text.startIndex + lastCommaIndex, i - lastCommaIndex + 1);
                            ++i;
                            break;
                        }
                        continue;
                    }
                    // Already tested in GetExpressionsStartAndLength
                    //else throw new SearchExpressionParseException($"Missing \"{GetCorrespondingCloser(openersStack.Peek())}\" in \"{errorPrefix + args.text.Substring(0, i + 1)}\"", args.text.startIndex, i + 1);
                }
                if (paramsBlock[i] == ',' && openersStack.Count == 1)
                {
                    var startIndex = GetExpressionStartIndex(paramStartIndex, lastCommaIndex);
                    if (i - startIndex == 0)
                    {
                        var position = lastCommaIndex == -1 ? 0 : lastCommaIndex;
                        throw new SearchExpressionParseException($"The argument is not defined before \",\" in \"{errorPrefix + text.Substring(0, i)}\"", text.startIndex + position, i - position + 1);
                    }
                    lastCommaIndex = i;
                    paramStartIndex = -1;
                    expressionParams.Add(paramsBlock.Substring(startIndex, lastCommaIndex - startIndex).Trim());
                }
            }

            // Already tested in GetExpressionsStartAndLength
            //if (currentStringTokenIndex != -1)
            //    throw new SearchExpressionParseException($"The string \"{args.text.Substring(currentStringTokenIndex)}\" is not closed correctly", args.text.startIndex + currentStringTokenIndex, args.text.Length - currentStringTokenIndex);
            //if (openersStack.Count != 0)
            //    throw new SearchExpressionParseException($"Missing \"{GetCorrespondingCloser(openersStack.Peek())}\" in \"{errorPrefix + args.text}\"", args.text.startIndex - errorPrefix.Length, args.text.Length);

            return expressionParams.ToArray();
        }

        private static bool CharMatchOpener(char openingChar, char charToTest)
        {
            char? correspondingCloser = GetCorrespondingCloser(openingChar);
            if (correspondingCloser.HasValue && charToTest == correspondingCloser)
                return true;
            return false;
        }

        public static char? GetCorrespondingCloser(char openingChar)
        {
            if (openingChar == '{') return '}';
            if (openingChar == '[') return ']';
            return null;
        }

        private static int GetExpressionStartIndex(int paramStartIndex, int lastCommaIndex)
        {
            int startIndex;
            if (paramStartIndex == -1)
                startIndex = lastCommaIndex + 1;
            else
                startIndex = paramStartIndex;
            return startIndex;
        }

        public static bool IsQuote(char c)
        {
            return Array.IndexOf(k_Quotes, c) != -1;
        }

        public static bool IsOpener(char c)
        {
            return Array.IndexOf(k_Openers, c) != -1;
        }

        public static bool IsCloser(char c)
        {
            return Array.IndexOf(k_Closers, c) != -1;
        }

        public static bool HasQuotes(StringView sv)
        {
            if (sv.Length < 2)
                return false;
            var c = sv[0];
            if (!IsQuote(c))
                return false;
            return c == sv.Last();
        }

        public static StringView[] GetExpressionsStartAndLength(StringView text, out bool rootHasParameters)
        {
            rootHasParameters = false;
            var openersStack = new Stack<char>();
            var expressions = new List<StringView>();
            int firstOpenerIndex = -1;
            int currentStringTokenIndex = -1;
            for (int i = 0; i < text.Length; ++i)
            {
                if (text[i] == ' ')
                    continue;

                // In case of a string, we must find the end of the string before checking any nested levels or ,
                if (k_Quotes.Any(c => c == text[i]))
                {
                    if (currentStringTokenIndex == -1)
                        currentStringTokenIndex = i;
                    else if (text[currentStringTokenIndex] != text[i])
                        throw new SearchExpressionParseException($"Nested strings are not allowed, \"{text[i]}\" found instead of \"{text[currentStringTokenIndex]}\" in \"{text.Substring(currentStringTokenIndex, i + 1 - currentStringTokenIndex)}\"", text.startIndex + currentStringTokenIndex, i - currentStringTokenIndex + 1);
                    else currentStringTokenIndex = -1;
                }
                if (currentStringTokenIndex != -1) // is in string
                    continue;

                if (k_Openers.Any(c => c == text[i]))
                {
                    if (openersStack.Count == 0)
                        firstOpenerIndex = i;
                    openersStack.Push(text[i]);
                    continue;
                }

                if (k_Closers.Any(c => c == text[i]))
                {
                    if (openersStack.Count == 0)
                        throw new SearchExpressionParseException($"Extra \"{text[i]}\" found", text.startIndex + i, 1);
                    if (CharMatchOpener(openersStack.Peek(), text[i]))
                    {
                        openersStack.Pop();
                        // We found the final closer, that means we found the end of the expression
                        if (openersStack.Count == 0)
                        {
                            expressions.Add(text.Substring(firstOpenerIndex, i - firstOpenerIndex + 1));
                        }
                        continue;
                    }
                    else
                        throw new SearchExpressionParseException($"Missing \"{GetCorrespondingCloser(openersStack.Peek())}\" in \"{text.Substring(0, i + 1)}\"", text.startIndex + firstOpenerIndex, i - firstOpenerIndex + 1);
                }

                if (openersStack.Count == 0 && text[i] == ',')
                    rootHasParameters = true;
            }
            if (currentStringTokenIndex != -1)
                throw new SearchExpressionParseException($"The string \"{text.Substring(currentStringTokenIndex)}\" is not closed correctly", text.startIndex + currentStringTokenIndex, text.Length - currentStringTokenIndex);
            if (openersStack.Any())
                throw new SearchExpressionParseException($"Missing \"{GetCorrespondingCloser(openersStack.Peek())}\" in \"{text}\"", text.startIndex + firstOpenerIndex, text.Length - firstOpenerIndex);
            return expressions.ToArray();
        }

        public static StringView SimplifyExpression(StringView outerText, bool trimLastWhiteSpaces = true)
        {
            // First we look for the closers that could be trimmed
            Stack<int> nestedLevelsEnd = new Stack<int>();
            Stack<int> nestedLevelsStart = new Stack<int>();
            for (int i = outerText.Length - 1; i >= 0; --i)
            {
                if (char.IsWhiteSpace(outerText[i]))
                    continue;
                if (outerText[i] == '}')
                {
                    nestedLevelsEnd.Push(i);
                    continue;
                }
                break;
            }
            // Then we parse the string to check how many we can trim
            bool hasTrimmedText = false;
            if (nestedLevelsEnd.Any())
            {
                bool inNonTrimmableText = false;
                int nonTrimmableOpeners = 0;
                bool isInString = false;
                for (int i = 0; i < outerText.Length; ++i)
                {
                    if (char.IsWhiteSpace(outerText[i]))
                        continue;
                    if (k_Quotes.Contains(outerText[i]))
                    {
                        isInString = !isInString;
                    }
                    if (isInString)
                        continue;
                    if (outerText[i] == '{')
                    {
                        nestedLevelsStart.Push(i);
                        // if part can't be trimmed we must not keep the { so we keep track of how many
                        if (inNonTrimmableText)
                            ++nonTrimmableOpeners;
                        continue;
                    }
                    if (!nestedLevelsStart.Any() || !nestedLevelsEnd.Any())
                        break;
                    if (outerText[i] == '}')
                    {
                        if (nonTrimmableOpeners == 0 && i == nestedLevelsEnd.Peek() && nestedLevelsStart.Count == nestedLevelsEnd.Count)
                        {
                            hasTrimmedText = true;
                            break;
                        }
                        nestedLevelsStart.Pop();
                        if (i == nestedLevelsEnd.Peek())
                            nestedLevelsEnd.Pop();
                        // In that case that means there was one expression that was closed and no more remaining so we should exit
                        if (!nestedLevelsStart.Any())
                            break;
                        if (inNonTrimmableText && nonTrimmableOpeners > 0)
                            --nonTrimmableOpeners;
                    }
                    // if we find other characters that means that part can't be trimmed, in that case we must not keep the next { we might find
                    inNonTrimmableText = true;
                }
            }
            var result = outerText;
            if (hasTrimmedText)
            {
                int start = nestedLevelsStart.Peek() + 1;
                int length = nestedLevelsEnd.Peek() - start;
                result = outerText.Substring(start, length);
            }
            return trimLastWhiteSpaces ? result.Trim() : result;
        }
    }
}
