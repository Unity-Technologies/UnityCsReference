// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.Search
{
    struct QueryMarkerArgument
    {
        public StringView rawText;
        public object value;
        public SearchExpression expression;

        public bool needsEvaluation => expression != null && value == null;

        public IEnumerable<object> Evaluate(SearchContext context, bool reevaluateLiterals = false)
        {
            if (expression == null)
                return new[] { rawText.ToString() };
            if (expression.types.HasAny(SearchExpressionType.Literal) && !reevaluateLiterals)
                return new[] { value };
            return expression.Execute(context).Where(item => item != null).Select(item => item.value);
        }
    }

    // <$CONTROL_TYPE:[VALUE,]EXPRESSION_ARGS$>.
    struct QueryMarker
    {
        const string k_StartToken = "<$";
        const string k_EndToken = "$>";

        public string type;        // Control Editor Type
        public StringView text;
        public object value => args.Length > 0 ? args[0].value : null;
        public QueryMarkerArgument[] args;

        public static QueryMarker none = new QueryMarker();

        public bool valid => text.valid;

        public static bool TryParse(string text, out QueryMarker marker)
        {
            return TryParse(text.GetStringView(), out marker);
        }

        public static bool TryParse(StringView text, out QueryMarker marker)
        {
            marker = none;
            if (!IsQueryMarker(text))
                return false;

            var innerText = text.Substring(k_StartToken.Length, text.Length - k_StartToken.Length - k_EndToken.Length);

            var indexOfColon = innerText.IndexOf(':');
            if (indexOfColon < 0)
                return false;

            var controlType = innerText.Substring(0, indexOfColon).Trim().ToString();
            var args = new List<StringView>();
            var level = 0;
            var currentArgStart = indexOfColon + 1;
            for (var i = currentArgStart; i < innerText.Length; ++i)
            {
                if (ParserUtils.IsOpener(innerText[i]))
                    ++level;
                if (ParserUtils.IsCloser(innerText[i]))
                    --level;
                if (innerText[i] != ',' || level != 0)
                    continue;
                if (i+1 == innerText.Length)
                    return false;

                args.Add(innerText.Substring(currentArgStart, i - currentArgStart).Trim());
                currentArgStart = i + 1;
            }

            if (currentArgStart == innerText.Length)
                return false; // No arguments

            // Extract the final argument, since there is no comma after the last argument
            args.Add(innerText.Substring(currentArgStart, innerText.Length - currentArgStart).Trim());

            var queryMarkerArguments = new List<QueryMarkerArgument>();
            using (var context = SearchService.CreateContext(""))
            {
                foreach (var arg in args)
                {
                    var parserArgs = new SearchExpressionParserArgs(arg, context, SearchExpressionParserFlags.ImplicitLiterals);
                    SearchExpression expression = null;
                    try
                    {
                        expression = SearchExpression.Parse(parserArgs);
                    }
                    catch (SearchExpressionParseException)
                    { }

                    if (expression == null || !expression.types.HasAny(SearchExpressionType.Literal))
                    {
                        queryMarkerArguments.Add(new QueryMarkerArgument { rawText = arg, expression = expression, value = expression == null ? arg.ToString() : null });
                        continue;
                    }
                    var results = expression.Execute(context);
                    var resolvedValue = results.FirstOrDefault(item => item != null);
                    var resolvedValueObject = resolvedValue?.value;
                    queryMarkerArguments.Add(new QueryMarkerArgument { rawText = arg, expression = expression, value = resolvedValueObject });
                }
            }

            marker = new QueryMarker { type = controlType, text = text, args = queryMarkerArguments.ToArray() };
            return true;
        }

        public static bool IsQueryMarker(StringView text)
        {
            return text.StartsWith(k_StartToken) && text.EndsWith(k_EndToken);
        }

        public static QueryMarker[] ParseAllMarkers(string text)
        {
            return ParseAllMarkers(text.GetStringView());
        }

        public static QueryMarker[] ParseAllMarkers(StringView text)
        {
            if (text.Length <= k_StartToken.Length + k_EndToken.Length)
                return null;

            List<QueryMarker> markers = new List<QueryMarker>();
            var startIndex = -1;
            var endIndex = -1;
            var nestedLevel = 0;
            bool inQuotes = false;
            for (var i = 0; i < text.Length; ++i)
            {
                if (i + k_StartToken.Length > text.Length || i + k_EndToken.Length > text.Length)
                    break;

                if (ParserUtils.IsOpener(text[i]) && !inQuotes)
                    ++nestedLevel;
                if (ParserUtils.IsCloser(text[i]) && !inQuotes)
                    --nestedLevel;
                if (text[i] == '"' || text[i] == '\'')
                    inQuotes = !inQuotes;

                if (startIndex == -1 && nestedLevel == 0 && !inQuotes)
                {
                    var openerSv = text.Substring(i, k_StartToken.Length);
                    if (openerSv == k_StartToken)
                        startIndex = i;
                }

                if (endIndex == -1 && nestedLevel == 0 && !inQuotes)
                {
                    var closerSv = text.Substring(i, k_EndToken.Length);
                    if (closerSv == k_EndToken)
                        endIndex = i + k_EndToken.Length;
                }

                if (startIndex != -1 && endIndex != -1)
                {
                    var markerSv = text.Substring(startIndex, endIndex - startIndex);
                    if (TryParse(markerSv, out var marker))
                        markers.Add(marker);
                    startIndex = -1;
                    endIndex = -1;
                }
            }

            return markers.ToArray();
        }

        public static string ReplaceMarkersWithRawValues(string text, out QueryMarker[] markers)
        {
            return ReplaceMarkersWithRawValues(text.GetStringView(), out markers);
        }

        public static string ReplaceMarkersWithRawValues(StringView text, out QueryMarker[] markers)
        {
            markers = ParseAllMarkers(text);
            if (markers == null || markers.Length == 0)
                return text.ToString();

            var modifiedSv = text.GetSubsetStringView();
            foreach (var queryMarker in markers)
            {
                if (!queryMarker.valid || queryMarker.args == null || queryMarker.args.Length == 0)
                    continue;
                var valueArg = queryMarker.args[0];
                modifiedSv.ReplaceWithSubset(queryMarker.text.startIndex, queryMarker.text.endIndex, valueArg.rawText.startIndex, valueArg.rawText.endIndex);
            }

            return modifiedSv.ToString();
        }

        public static string ReplaceMarkersWithEvaluatedValues(string text, SearchContext context)
        {
            var allMarkers = ParseAllMarkers(text);
            var modifiedText = text;
            var offset = 0;
            foreach (var queryMarker in allMarkers)
            {
                if (!queryMarker.valid || queryMarker.args == null || queryMarker.args.Length == 0)
                    continue;
                var valueArg = queryMarker.args[0];
                var value = valueArg.value;
                if (valueArg.needsEvaluation)
                    value = valueArg.Evaluate(context).FirstOrDefault();
                if (value == null)
                    continue;

                var valueStr = value.ToString();
                modifiedText = modifiedText.Remove(queryMarker.text.startIndex + offset, queryMarker.text.Length);
                modifiedText = modifiedText.Insert(queryMarker.text.startIndex + offset, valueStr);
                offset += valueStr.Length - queryMarker.text.Length;
            }

            return modifiedText;
        }

        public static string ReplaceMarkersWithEvaluatedValues(StringView text, SearchContext context)
        {
            return ReplaceMarkersWithEvaluatedValues(text.ToString(), context);
        }

        // Evaluate arguments as search expressions
        public IEnumerable<object> EvaluateArgs(bool reevaluateLiterals = false)
        {
            using (var context = SearchService.CreateContext(""))
            {
                return EvaluateArgs(context, reevaluateLiterals).ToList(); // Calling ToList to force evaluation so context is not used after disposal.
            }
        }

        public IEnumerable<object> EvaluateArgs(SearchContext context, bool reevaluateLiterals = false)
        {
            return args.SelectMany(qma => qma.Evaluate(context, reevaluateLiterals));
        }

        public override string ToString()
        {
            return $"{k_StartToken}{type}:{string.Join(",", EvaluateArgs())}{k_EndToken}";
        }
    }
}
