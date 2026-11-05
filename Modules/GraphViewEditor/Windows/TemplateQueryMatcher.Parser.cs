// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.Experimental.GraphView
{
    partial class TemplateQueryMatcher
    {
        // A null expression has no terms: it is dropped from and/or and matches everything on its own.
        class Parser
        {
            const int k_MaxDepth = 64;

            readonly List<string> m_Tokens;
            readonly string m_ToolKey;
            readonly TemplateQueryMatcher m_Matcher;
            int m_Index;
            int m_Depth;
            int m_NegationDepth;

            public Parser(List<string> tokens, string toolKey, TemplateQueryMatcher matcher)
            {
                m_Tokens = tokens;
                m_ToolKey = toolKey;
                m_Matcher = matcher;
            }

            public Func<Dictionary<string, List<string>>, bool> ParseQuery()
            {
                Func<Dictionary<string, List<string>>, bool> result = null;
                while (m_Index < m_Tokens.Count)
                {
                    result = And(result, ParseOr());
                    if (m_Index < m_Tokens.Count)
                        ++m_Index;
                }
                return result;
            }

            Func<Dictionary<string, List<string>>, bool> ParseOr()
            {
                var result = ParseAnd();
                while (m_Index < m_Tokens.Count && IsKeyword(m_Tokens[m_Index], k_Or))
                {
                    ++m_Index;
                    result = Or(result, ParseAnd());
                }
                return result;
            }

            Func<Dictionary<string, List<string>>, bool> ParseAnd()
            {
                Func<Dictionary<string, List<string>>, bool> result = null;
                while (m_Index < m_Tokens.Count)
                {
                    var token = m_Tokens[m_Index];
                    if (IsToken(token, k_GroupClose) || IsKeyword(token, k_Or))
                        break;

                    if (IsKeyword(token, k_And))
                    {
                        ++m_Index;
                        continue;
                    }

                    result = And(result, ParseUnary());
                }
                return result;
            }

            Func<Dictionary<string, List<string>>, bool> ParseUnary()
            {
                if (m_Depth >= k_MaxDepth)
                {
                    m_Index = m_Tokens.Count;
                    return null;
                }

                ++m_Depth;
                var result = ParseUnaryAtDepth();
                --m_Depth;
                return result;
            }

            Func<Dictionary<string, List<string>>, bool> ParseUnaryAtDepth()
            {
                var token = m_Tokens[m_Index++];
                if (IsToken(token, k_GroupOpen))
                {
                    var group = ParseOr();
                    if (m_Index < m_Tokens.Count && IsToken(m_Tokens[m_Index], k_GroupClose))
                        ++m_Index;
                    return group;
                }

                if (IsNegation(token))
                {
                    if (m_Index >= m_Tokens.Count || IsToken(m_Tokens[m_Index], k_GroupClose))
                        return null;
                    ++m_NegationDepth;
                    var operand = ParseUnary();
                    --m_NegationDepth;
                    return Not(operand);
                }

                if (token.Length > 1 && token[0] == k_NegationPrefix)
                {
                    ++m_NegationDepth;
                    var term = ParseTerm(token.Substring(1));
                    --m_NegationDepth;
                    return Not(term);
                }

                return ParseTerm(token);
            }

            Func<Dictionary<string, List<string>>, bool> ParseTerm(string token)
            {
                if (TryParseArea(token, out var area))
                {
                    if (m_NegationDepth == 0)
                        m_Matcher.Area = area;
                    return null;
                }

                if (!TryParseTerm(token, m_ToolKey, out var term))
                    return null;
                return document => MatchTerm(term, document);
            }

            static bool IsNegation(string token) => IsToken(token, k_NegationPrefix) || IsKeyword(token, k_Not);

            static bool IsToken(string token, char c) => token.Length == 1 && token[0] == c;

            static bool IsKeyword(string token, string keyword) =>
                string.Equals(token, keyword, StringComparison.OrdinalIgnoreCase);

            static Func<Dictionary<string, List<string>>, bool> And(
                Func<Dictionary<string, List<string>>, bool> lhs,
                Func<Dictionary<string, List<string>>, bool> rhs)
            {
                if (lhs == null)
                    return rhs;
                if (rhs == null)
                    return lhs;
                return document => lhs(document) && rhs(document);
            }

            static Func<Dictionary<string, List<string>>, bool> Or(
                Func<Dictionary<string, List<string>>, bool> lhs,
                Func<Dictionary<string, List<string>>, bool> rhs)
            {
                if (lhs == null)
                    return rhs;
                if (rhs == null)
                    return lhs;
                return document => lhs(document) || rhs(document);
            }

            static Func<Dictionary<string, List<string>>, bool> Not(Func<Dictionary<string, List<string>>, bool> operand)
            {
                if (operand == null)
                    return null;
                return document => !operand(document);
            }
        }
    }
}
