// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Unity.GraphToolsFoundation
{
    class MathExpressionReader_Internal
    {
        readonly string m_Input;
        int m_I;

        public MathExpressionReader_Internal(string input)
        {
            m_Input = input.Trim();
            m_I = 0;
        }

        void SkipWhitespace()
        {
            while (!Done && char.IsWhiteSpace(m_Input[m_I]))
                m_I++;
        }

        bool Done => m_I >= m_Input.Length;
        char NextChar => m_Input[m_I];
        char ConsumeChar() => m_Input[m_I++];

        public string CurrentToken;
        public Token CurrentTokenType;
        public Token PrevTokenType;

        public void ReadToken()
        {
            CurrentToken = null;
            PrevTokenType = CurrentTokenType;
            CurrentTokenType = Token.None;
            if (Done)
                return;
            switch (NextChar)
            {
                case '(':
                    ConsumeChar();
                    CurrentTokenType = Token.LeftParens;
                    break;
                case ')':
                    ConsumeChar();
                    CurrentTokenType = Token.RightParens;
                    break;
                case ',':
                    ConsumeChar();
                    CurrentTokenType = Token.Coma;
                    break;
                default:
                {
                    if (char.IsDigit(NextChar) || NextCharIsPoint())
                    {
                        var foundPoint = false;
                        var sb = new StringBuilder();
                        do
                        {
                            foundPoint |= NextCharIsPoint();
                            sb.Append(ConsumeChar());
                        }
                        while (!Done && (char.IsDigit(NextChar) || (NextChar == '.' && !foundPoint)));
                        if (!Done && foundPoint && NextCharIsPoint())     // 1.2.3
                            throw new InvalidDataException($"Invalid number: '{sb}.'");

                        CurrentToken = sb.ToString();
                        CurrentTokenType = Token.Number;
                    }
                    else
                    {
                        if (MatchOp(out var op))
                        {
                            CurrentToken = op.Str;
                            CurrentTokenType = Token.Op;
                            for (var i = 0; i < CurrentToken.Length; i++)
                                ConsumeChar();
                        }
                        else
                        {
                            CurrentTokenType = Token.Identifier;
                            var sb = new StringBuilder();
                            while (!Done && NextChar != ')' && NextChar != ',' && !MatchOp(out _) && !char.IsWhiteSpace(NextChar))
                                sb.Append(ConsumeChar());
                            CurrentToken = sb.ToString();
                        }
                    }
                    break;
                }
            }

            SkipWhitespace();

            bool MatchOp(out Operator_Internal desc)
            {
                foreach (var pair in MathExpressionParser.Ops_Internal.Where(pair => m_Input.IndexOf(pair.Value.Str, m_I, StringComparison.Ordinal) == m_I))
                {
                    desc = pair.Value;
                    return true;
                }

                desc = default;
                return false;
            }

            bool NextCharIsPoint() => NextChar == '.';
        }
    }
}
