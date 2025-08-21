// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Unity.VectorGraphics
{
    internal class SVGPropertySheet : Dictionary<string, string> { }

    internal class SVGStyleSheet
    {
        private List<KeyValuePair<string, SVGPropertySheet>> m_Selectors = new List<KeyValuePair<string, SVGPropertySheet>>();

        public SVGPropertySheet this[string key]
        {
            get
            {
                int i = m_Selectors.FindIndex(x => x.Key == key);
                if (i != -1)
                    return m_Selectors[i].Value;
                return null;
            }
            set
            {
                var v = new KeyValuePair<string, SVGPropertySheet>(key, value);
                int i = m_Selectors.FindIndex(x => x.Key == key);
                if (i != -1)
                    m_Selectors[i] = v;
                m_Selectors.Add(v);
            }
        }

        public IEnumerable<string> selectors
        {
            get
            {
                foreach (var kvp in m_Selectors)
                    yield return kvp.Key;
            }
        }

        public int Count
        {
            get { return m_Selectors.Count; }
        }

        public void Clear()
        {
            m_Selectors.Clear();
        }
    };

    internal static class SVGStyleSheetUtils
    {
        public static SVGStyleSheet Parse(string cssText)
        {
            var result = new SVGStyleSheet();
            var tokens = Tokenize(cssText);

            var sheet = new SVGStyleSheet();
            while (ParseSelector(tokens, sheet))
            {
                var resultList = new List<string>(result.selectors);
                foreach (var sel in sheet.selectors)
                {
                    if (resultList.Contains(sel))
                        CombineProperties(result[sel], sheet[sel]);
                    else
                        result[sel] = sheet[sel];
                }
                sheet.Clear();
            }

            return result;
        }

        public static SVGPropertySheet ParseInline(string cssText)
        {
            var tokens = Tokenize(cssText);
            var props = new SVGPropertySheet();
            ParseProperties(tokens, props);
            return props;
        }

        private static bool ParseSelector(List<string> tokens, SVGStyleSheet sheet)
        {
            if (tokens.Count == 0)
                return false;

            var newSheet = new SVGStyleSheet();
            while (true)
            {
                var selectorName = PopToken(tokens);
                newSheet[selectorName] = new SVGPropertySheet();

                while (PeekToken(tokens) == ",")
                    PopToken(tokens);
                
                if (PeekToken(tokens) == "" || PeekToken(tokens) == "{")
                    break;
            }

            var sep = PopToken(tokens);
            if (sep != "{")
            {
                Debug.LogError("Invalid CSS selector opening bracket: \"" + sep + "\"");
                return false;
            }

            var props = new SVGPropertySheet();
            ParseProperties(tokens, props);

            // Transfer properties to the new selectors
            foreach (var key in newSheet.selectors)
                sheet[key] = CopyProperties(props);
            
            sep = PopToken(tokens);
            if (sep != "}")
            {
                Debug.LogError("Invalid CSS selector closing bracket: \"" + sep + "\"");
                return false;
            }
            
            return true;
        }

        private static void CombineProperties(SVGPropertySheet first, SVGPropertySheet second)
        {
            foreach (var key in second.Keys)
                first[key] = second[key];
        }

        private static SVGPropertySheet CopyProperties(SVGPropertySheet props)
        {
            var newProps = new SVGPropertySheet();
            foreach (var v in props)
                newProps[v.Key] = v.Value;
            return newProps;
        }

        private static bool ParseProperties(List<string> tokens, SVGPropertySheet props)
        {
            string name;
            string value;
            while (ParseProperty(tokens, out name, out value))
            {
                props[name] = value;
                while (PeekToken(tokens) == ";")
                    PopToken(tokens);
            }
            return true;
        }

        private static bool ParseProperty(List<string> tokens, out string name, out string value)
        {
            name = null;
            value = null;

            if (PeekToken(tokens) == "" || PeekToken(tokens) == "}")
                return false;

            name = PopToken(tokens);

            var sep = PopToken(tokens);
            if (sep != ":")
            {
                Debug.LogError("Invalid CSS property separator: \"" + sep + "\"");
                return false;
            }

            value = "";
            while (PeekToken(tokens) != "" && PeekToken(tokens) != ";" && PeekToken(tokens) != "}")
            {
                value = (value == "") ? PopToken(tokens) : value + " " + PopToken(tokens);
                if (PeekToken(tokens) == "(")
                    value += ParseParenValue(tokens);
            }

            return true;
        }

        private static string ParseParenValue(List<string> tokens)
        {
            var opening = PopToken(tokens);
            if (opening != "(")
            {
                Debug.LogError("Invaid CSS value opening");
                return "";
            }

            var value = opening;
            while (PeekToken(tokens) != "" && PeekToken(tokens) != ")")
                value += PopToken(tokens);
            
            if (PeekToken(tokens) != ")")
            {
                Debug.LogError("Invaid CSS value closing");
                return "";
            }

            value += PopToken(tokens);
            return value;
        }

        /// <summary>Breaks a CSS input into tokens</summary>
        /// <param name="cssText">The CSS text</param>
        /// <returns>A list of tokens</returns>
        public static List<string> Tokenize(string cssText)
        {
            var tokens = new List<string>();

            cssText = cssText.Replace(System.Environment.NewLine, ""); // Remove newlines
            cssText = Regex.Replace(cssText, @"/\*.*?\*/", ""); // Remove CSS comments
            cssText = Regex.Replace(cssText, @"<!--.*?-->", ""); // Remove XML comments

            int from = 0;
            int to = 0;

            while (from < cssText.Length)
            {
                while (from < cssText.Length && IsWhitespace(cssText[from]))
                    ++from;

                to = from;

                while (to < cssText.Length && !IsSeparator(cssText[to]))
                    ++to;

                if (from == to)
                {
                    if (from < cssText.Length)
                        tokens.Add(cssText[from].ToString());
                    ++to;
                }
                else
                {
                    tokens.Add(cssText.Substring(from, to-from));
                }

                from = to;
            }
            
            return tokens;
        }

        private static string PeekToken(List<string> tokens)
        {
            if (tokens.Count == 0)
                return "";
            return tokens[0];
        }

        private static string PopToken(List<string> tokens)
        {
            if (tokens.Count == 0)
                return "";
            var tok = tokens[0];
            tokens.RemoveAt(0);
            return tok;
        }

        private static bool IsSeparator(char ch)
        {
            return IsWhitespace(ch) || ch == ';' || ch == ':' || ch == '{' || ch == '}' || ch == '(' || ch == ')' || ch == ',';
        }

        private static bool IsWhitespace(char ch)
        {
            return ch == ' ' || ch == '\n' || ch == '\t';
        }
    }
} // namespace
