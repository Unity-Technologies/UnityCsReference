using System.Collections.Generic;

namespace UnityEngine.UIElements.StyleSheets.Syntax
{
    internal enum StyleSyntaxTokenType
    {
        Unknown,
        String,
        Number,
        Space,
        SingleBar,
        DoubleBar,
        DoubleAmpersand,
        Comma,
        SingleQuote,
        Asterisk,
        Plus,
        QuestionMark,
        HashMark,
        ExclamationPoint,
        OpenBracket,
        CloseBracket,
        OpenBrace,
        CloseBrace,
        LessThan,
        GreaterThan,
        End
    }

    internal struct StyleSyntaxToken
    {
        public StyleSyntaxTokenType type;
        public string text;
        public int number;

        public StyleSyntaxToken(StyleSyntaxTokenType t)
        {
            type = t;
            text = null;
            this.number = 0;
        }

        public StyleSyntaxToken(StyleSyntaxTokenType type, string text)
        {
            this.type = type;
            this.text = text;
            this.number = 0;
        }

        public StyleSyntaxToken(StyleSyntaxTokenType type, int number)
        {
            this.type = type;
            this.text = null;
            this.number = number;
        }
    }

    internal class StyleSyntaxTokenizer
    {
        private List<StyleSyntaxToken> m_Tokens = new List<StyleSyntaxToken>();
        private int m_CurrentTokenIndex = -1;

        public StyleSyntaxToken current
        {
            get
            {
                if (m_CurrentTokenIndex < 0 || m_CurrentTokenIndex >= m_Tokens.Count)
                    return new StyleSyntaxToken(StyleSyntaxTokenType.Unknown);

                return m_Tokens[m_CurrentTokenIndex];
            }
        }

        public StyleSyntaxToken MoveNext()
        {
            var token = current;
            if (token.type == StyleSyntaxTokenType.Unknown)
                return token;

            m_CurrentTokenIndex++;
            token = current;

            // Last token?
            if (m_CurrentTokenIndex == m_Tokens.Count)
                m_CurrentTokenIndex = -1;

            return token;
        }

        public StyleSyntaxToken PeekNext()
        {
            int nextIndex = m_CurrentTokenIndex + 1;
            if (m_CurrentTokenIndex < 0 || nextIndex >= m_Tokens.Count)
                return new StyleSyntaxToken(StyleSyntaxTokenType.Unknown);

            return m_Tokens[nextIndex];
        }

        public void Tokenize(string syntax)
        {
            m_Tokens.Clear();
            m_CurrentTokenIndex = 0;

            syntax = syntax.Trim(' ').ToLower();

            for (int i = 0; i < syntax.Length; i++)
            {
                var c = syntax[i];
                switch (c)
                {
                    case ' ':
                        i = GlobCharacter(syntax, i, ' ');
                        m_Tokens.Add(new StyleSyntaxToken(StyleSyntaxTokenType.Space));
                        break;
                    case '|':
                        if (IsNextCharacter(syntax, i, '|'))
                        {
                            m_Tokens.Add(new StyleSyntaxToken(StyleSyntaxTokenType.DoubleBar));
                            ++i;
                        }
                        else
                        {
                            m_Tokens.Add(new StyleSyntaxToken(StyleSyntaxTokenType.SingleBar));
                        }
                        break;
                    case '&':
                        if (!IsNextCharacter(syntax, i, '&'))
                        {
                            string nextChar = i + 1 < syntax.Length ? syntax[i + 1].ToString() : "EOF";
                            Debug.LogAssertionFormat("Expected '&' got '{0}'", nextChar);
                            m_Tokens.Add(new StyleSyntaxToken(StyleSyntaxTokenType.Unknown));
                        }
                        else
                        {
                            m_Tokens.Add(new StyleSyntaxToken(StyleSyntaxTokenType.DoubleAmpersand));
                            ++i;
                        }
                        break;
                    case ',':
                        m_Tokens.Add(new StyleSyntaxToken(StyleSyntaxTokenType.Comma));
                        break;
                    case '\'':
                        m_Tokens.Add(new StyleSyntaxToken(StyleSyntaxTokenType.SingleQuote));
                        break;
                    case '*':
                        m_Tokens.Add(new StyleSyntaxToken(StyleSyntaxTokenType.Asterisk));
                        break;
                    case '+':
                        m_Tokens.Add(new StyleSyntaxToken(StyleSyntaxTokenType.Plus));
                        break;
                    case '?':
                        m_Tokens.Add(new StyleSyntaxToken(StyleSyntaxTokenType.QuestionMark));
                        break;
                    case '#':
                        m_Tokens.Add(new StyleSyntaxToken(StyleSyntaxTokenType.HashMark));
                        break;
                    case '!':
                        m_Tokens.Add(new StyleSyntaxToken(StyleSyntaxTokenType.ExclamationPoint));
                        break;
                    case '[':
                        m_Tokens.Add(new StyleSyntaxToken(StyleSyntaxTokenType.OpenBracket));
                        break;
                    case ']':
                        m_Tokens.Add(new StyleSyntaxToken(StyleSyntaxTokenType.CloseBracket));
                        break;
                    case '{':
                        m_Tokens.Add(new StyleSyntaxToken(StyleSyntaxTokenType.OpenBrace));
                        break;
                    case '}':
                        m_Tokens.Add(new StyleSyntaxToken(StyleSyntaxTokenType.CloseBrace));
                        break;
                    case '<':
                        m_Tokens.Add(new StyleSyntaxToken(StyleSyntaxTokenType.LessThan));
                        break;
                    case '>':
                        m_Tokens.Add(new StyleSyntaxToken(StyleSyntaxTokenType.GreaterThan));
                        break;
                    default:
                        if (char.IsNumber(c))
                        {
                            int subStrStart = i;
                            int subStrLength = 1;
                            while (IsNextNumber(syntax, i))
                            {
                                ++i;
                                ++subStrLength;
                            }

                            string tokenText = syntax.Substring(subStrStart, subStrLength);
                            int tokenNumber = int.Parse(tokenText);
                            m_Tokens.Add(new StyleSyntaxToken(StyleSyntaxTokenType.Number, tokenNumber));
                        }
                        else if (char.IsLetter(c))
                        {
                            int subStrStart = i;
                            int subStrLength = 1;
                            while (IsNextLetterOrDash(syntax, i))
                            {
                                ++i;
                                ++subStrLength;
                            }

                            string tokenText = syntax.Substring(subStrStart, subStrLength);
                            m_Tokens.Add(new StyleSyntaxToken(StyleSyntaxTokenType.String, tokenText));
                        }
                        else
                        {
                            // Got unsupported character
                            Debug.LogAssertionFormat("Expected letter or number got '{0}'", c);
                            m_Tokens.Add(new StyleSyntaxToken(StyleSyntaxTokenType.Unknown));
                        }
                        break;
                }
            }

            m_Tokens.Add(new StyleSyntaxToken(StyleSyntaxTokenType.End));
        }

        private static bool IsNextCharacter(string s, int index, char c)
        {
            return index + 1 < s.Length && s[index + 1] == c;
        }

        private static bool IsNextLetterOrDash(string s, int index)
        {
            return index + 1 < s.Length && (char.IsLetter(s[index + 1]) || s[index + 1] == '-');
        }

        private static bool IsNextNumber(string s, int index)
        {
            return index + 1 < s.Length && char.IsNumber(s[index + 1]);
        }

        private static int GlobCharacter(string s, int index, char c)
        {
            while (IsNextCharacter(s, index, c))
            {
                ++index;
            }
            return index;
        }
    }
}
