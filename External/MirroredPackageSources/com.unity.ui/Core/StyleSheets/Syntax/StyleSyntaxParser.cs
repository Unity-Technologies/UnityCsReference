using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements.StyleSheets.Syntax
{
    internal class StyleSyntaxParser
    {
        private List<Expression> m_ProcessExpressionList = new List<Expression>();
        private Stack<Expression> m_ExpressionStack = new Stack<Expression>();
        private Stack<ExpressionCombinator> m_CombinatorStack = new Stack<ExpressionCombinator>();

        private Dictionary<string, Expression> m_ParsedExpressionCache = new Dictionary<string, Expression>();

        public Expression Parse(string syntax)
        {
            if (string.IsNullOrEmpty(syntax))
                return null;

            Expression tree = null;

            // All parsed syntax string are stored into cache
            // A bunch of properties have the same syntax so it's worth caching the result
            if (!m_ParsedExpressionCache.TryGetValue(syntax, out tree))
            {
                StyleSyntaxTokenizer tokenizer = new StyleSyntaxTokenizer();
                tokenizer.Tokenize(syntax);

                try
                {
                    tree = ParseExpression(tokenizer);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }

                m_ParsedExpressionCache[syntax] = tree;
            }

            return tree;
        }

        // Implementation of the shunting yard algorithm
        private Expression ParseExpression(StyleSyntaxTokenizer tokenizer)
        {
            StyleSyntaxToken token = tokenizer.current;
            while (!IsExpressionEnd(token))
            {
                // Step 1 : either a term or group
                Expression expression = null;
                if (token.type == StyleSyntaxTokenType.String || token.type == StyleSyntaxTokenType.LessThan)
                {
                    expression = ParseTerm(tokenizer);
                }
                else if (token.type == StyleSyntaxTokenType.OpenBracket)
                {
                    expression = ParseGroup(tokenizer);
                }
                else
                {
                    throw new Exception($"Unexpected token '{token.type}' in expression");
                }

                m_ExpressionStack.Push(expression);

                // Step 2 : either a combinator or the end of the expression
                var nextCombinatorType = ParseCombinatorType(tokenizer);
                if (nextCombinatorType != ExpressionCombinator.None)
                {
                    if (m_CombinatorStack.Count > 0)
                    {
                        var previousCombinator = m_CombinatorStack.Peek();
                        int previousPrecedence = (int)previousCombinator;
                        int currentPrecedence = (int)nextCombinatorType;

                        // Higher precedence means the previous combinator needs to be created first
                        while (previousPrecedence > currentPrecedence && previousCombinator != ExpressionCombinator.Group)
                        {
                            ProcessCombinatorStack();
                            previousCombinator = m_CombinatorStack.Count > 0 ? m_CombinatorStack.Peek() : ExpressionCombinator.None;
                            previousPrecedence = (int)previousCombinator;
                        }
                    }

                    m_CombinatorStack.Push(nextCombinatorType);
                }

                token = tokenizer.current;
            }

            // Step 3 : Rollback the stack of combinators and create associated expressions
            while (m_CombinatorStack.Count > 0)
            {
                // When a group is encountered the current expression is done
                var combinatorType = m_CombinatorStack.Peek();
                if (combinatorType == ExpressionCombinator.Group)
                {
                    m_CombinatorStack.Pop();
                    break;
                }
                else
                {
                    ProcessCombinatorStack();
                }
            }

            return m_ExpressionStack.Pop();
        }

        private void ProcessCombinatorStack()
        {
            var combinatorType = m_CombinatorStack.Pop();
            var exp2 = m_ExpressionStack.Pop();
            var exp1 = m_ExpressionStack.Pop();

            m_ProcessExpressionList.Clear();

            m_ProcessExpressionList.Add(exp1);
            m_ProcessExpressionList.Add(exp2);

            // Small optimization that merge combinator of the same type from the stack into one
            while (m_CombinatorStack.Count > 0 && combinatorType == m_CombinatorStack.Peek())
            {
                // The expression on the stack was encountered before so insert it at
                // the beginning of the combinator sub expressions
                var e = m_ExpressionStack.Pop();
                m_ProcessExpressionList.Insert(0, e);

                m_CombinatorStack.Pop();
            }

            var c = new Expression(ExpressionType.Combinator);
            c.combinator = combinatorType;
            c.subExpressions = m_ProcessExpressionList.ToArray();

            m_ExpressionStack.Push(c);
        }

        private Expression ParseTerm(StyleSyntaxTokenizer tokenizer)
        {
            Expression exp = null;

            StyleSyntaxToken token = tokenizer.current;
            if (token.type == StyleSyntaxTokenType.LessThan)
            {
                // Data type or property
                exp = ParseDataType(tokenizer);
            }
            else if (token.type == StyleSyntaxTokenType.String)
            {
                // Keyword
                exp = new Expression(ExpressionType.Keyword);
                exp.keyword = token.text.ToLower();

                tokenizer.MoveNext();
            }
            else
            {
                throw new Exception($"Unexpected token '{token.type}' in expression. Expected term token");
            }

            ParseMultiplier(tokenizer, ref exp.multiplier);
            return exp;
        }

        private ExpressionCombinator ParseCombinatorType(StyleSyntaxTokenizer tokenizer)
        {
            var type = ExpressionCombinator.None;

            StyleSyntaxToken token = tokenizer.current;
            while (!IsExpressionEnd(token) && type == ExpressionCombinator.None)
            {
                StyleSyntaxToken next = tokenizer.PeekNext();
                switch (token.type)
                {
                    case StyleSyntaxTokenType.Space:
                        if (!IsCombinator(next) && next.type != StyleSyntaxTokenType.CloseBracket)
                        {
                            type = ExpressionCombinator.Juxtaposition;
                        }
                        break;
                    case StyleSyntaxTokenType.SingleBar:
                        type = ExpressionCombinator.Or;
                        break;
                    case StyleSyntaxTokenType.DoubleBar:
                        type = ExpressionCombinator.OrOr;
                        break;
                    case StyleSyntaxTokenType.DoubleAmpersand:
                        type = ExpressionCombinator.AndAnd;
                        break;
                    default:
                        throw new Exception($"Unexpected token '{token.type}' in expression. Expected combinator token");
                }

                token = tokenizer.MoveNext();
            }

            // Remove space after the combinator if any
            EatSpace(tokenizer);

            return type;
        }

        private Expression ParseGroup(StyleSyntaxTokenizer tokenizer)
        {
            StyleSyntaxToken token = tokenizer.current;
            if (token.type != StyleSyntaxTokenType.OpenBracket)
                throw new Exception($"Unexpected token '{token.type}' in group expression. Expected '[' token");

            m_CombinatorStack.Push(ExpressionCombinator.Group);

            tokenizer.MoveNext();
            EatSpace(tokenizer);

            var subExpression = ParseExpression(tokenizer);

            token = tokenizer.current;
            if (token.type != StyleSyntaxTokenType.CloseBracket)
                throw new Exception($"Unexpected token '{token.type}' in group expression. Expected ']' token");

            // Do not eat the space after the ] token because it could be a juxtaposition or multiplier
            tokenizer.MoveNext();

            var group = new Expression(ExpressionType.Combinator);
            group.combinator = ExpressionCombinator.Group;
            group.subExpressions = new Expression[] { subExpression };

            ParseMultiplier(tokenizer, ref group.multiplier);

            return group;
        }

        private Expression ParseDataType(StyleSyntaxTokenizer tokenizer)
        {
            Expression exp = null;

            StyleSyntaxToken token = tokenizer.current;
            if (token.type != StyleSyntaxTokenType.LessThan)
                throw new Exception($"Unexpected token '{token.type}' in data type expression. Expected '<' token");

            token = tokenizer.MoveNext();
            switch (token.type)
            {
                case StyleSyntaxTokenType.String:
                    DataType dataType = DataType.None;
                    try
                    {
                        object enumValue = Enum.Parse(typeof(DataType), token.text, true);
                        if (enumValue != null)
                            dataType = (DataType)enumValue;
                    }
                    catch (Exception)
                    {
                        throw new Exception($"Unknown data type '{token.text}'");
                    }

                    exp = new Expression(ExpressionType.Data);
                    exp.dataType = dataType;

                    tokenizer.MoveNext();
                    break;
                case StyleSyntaxTokenType.SingleQuote:
                    exp = ParseProperty(tokenizer);
                    break;
                default:
                    throw new Exception($"Unexpected token '{token.type}' in data type expression");
            }

            token = tokenizer.current;
            if (token.type != StyleSyntaxTokenType.GreaterThan)
                throw new Exception($"Unexpected token '{token.type}' in data type expression. Expected '>' token");

            tokenizer.MoveNext();

            return exp;
        }

        private Expression ParseProperty(StyleSyntaxTokenizer tokenizer)
        {
            Expression exp = null;

            StyleSyntaxToken token = tokenizer.current;
            if (token.type != StyleSyntaxTokenType.SingleQuote)
                throw new Exception($"Unexpected token '{token.type}' in property expression. Expected ''' token");

            token = tokenizer.MoveNext();
            if (token.type != StyleSyntaxTokenType.String)
                throw new Exception($"Unexpected token '{token.type}' in property expression. Expected 'string' token");

            string propertyName = token.text;
            string syntax;
            if (!StylePropertyCache.TryGetSyntax(propertyName, out syntax))
                throw new Exception($"Unknown property '{propertyName}' <''> expression.");

            // Check if it's in the cache first
            if (!m_ParsedExpressionCache.TryGetValue(syntax, out exp))
            {
                // Expanded property are in a group to honor the precedence rule
                // Pushing the ExpressionCombinator.Group allow the next call to Parse to stop at this location
                m_CombinatorStack.Push(ExpressionCombinator.Group);

                // Recursively call Parse to expand the property syntax
                exp = Parse(syntax);
            }

            token = tokenizer.MoveNext();
            if (token.type != StyleSyntaxTokenType.SingleQuote)
                throw new Exception($"Unexpected token '{token.type}' in property expression. Expected ''' token");

            token = tokenizer.MoveNext();
            if (token.type != StyleSyntaxTokenType.GreaterThan)
                throw new Exception($"Unexpected token '{token.type}' in property expression. Expected '>' token");

            var group = new Expression(ExpressionType.Combinator);
            group.combinator = ExpressionCombinator.Group;
            group.subExpressions = new Expression[] { exp };

            return group;
        }

        private void ParseMultiplier(StyleSyntaxTokenizer tokenizer, ref ExpressionMultiplier multiplier)
        {
            StyleSyntaxToken token = tokenizer.current;
            if (IsMultiplier(token))
            {
                switch (token.type)
                {
                    case StyleSyntaxTokenType.Asterisk:
                        multiplier.type = ExpressionMultiplierType.ZeroOrMore;
                        break;
                    case StyleSyntaxTokenType.Plus:
                        multiplier.type = ExpressionMultiplierType.OneOrMore;
                        break;
                    case StyleSyntaxTokenType.QuestionMark:
                        multiplier.type = ExpressionMultiplierType.ZeroOrOne;
                        break;
                    case StyleSyntaxTokenType.HashMark:
                        multiplier.type = ExpressionMultiplierType.OneOrMoreComma;
                        break;
                    case StyleSyntaxTokenType.ExclamationPoint:
                        multiplier.type = ExpressionMultiplierType.GroupAtLeastOne;
                        break;
                    case StyleSyntaxTokenType.OpenBrace:
                        multiplier.type = ExpressionMultiplierType.Ranges;
                        break;
                    default:
                        throw new Exception($"Unexpected token '{token.type}' in expression. Expected multiplier token");
                }

                token = tokenizer.MoveNext();
            }

            if (multiplier.type == ExpressionMultiplierType.Ranges)
                ParseRanges(tokenizer, out multiplier.min, out multiplier.max);
        }

        private void ParseRanges(StyleSyntaxTokenizer tokenizer, out int min, out int max)
        {
            min = -1;
            max = -1;

            var token = tokenizer.current;

            bool foundComma = false;
            while (token.type != StyleSyntaxTokenType.CloseBrace)
            {
                switch (token.type)
                {
                    case StyleSyntaxTokenType.Number:
                        if (!foundComma)
                        {
                            min = token.number;
                        }
                        else
                        {
                            max = token.number;
                        }

                        break;
                    case StyleSyntaxTokenType.Comma:
                        foundComma = true;
                        break;
                    default:
                        throw new Exception($"Unexpected token '{token.type}' in expression. Expected ranges token");
                }

                token = tokenizer.MoveNext();
            }

            tokenizer.MoveNext();
        }

        private static void EatSpace(StyleSyntaxTokenizer tokenizer)
        {
            var token = tokenizer.current;
            if (token.type == StyleSyntaxTokenType.Space)
                tokenizer.MoveNext();
        }

        private static bool IsExpressionEnd(StyleSyntaxToken token)
        {
            switch (token.type)
            {
                case StyleSyntaxTokenType.End:
                case StyleSyntaxTokenType.CloseBracket:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsCombinator(StyleSyntaxToken token)
        {
            switch (token.type)
            {
                case StyleSyntaxTokenType.Space:
                case StyleSyntaxTokenType.SingleBar:
                case StyleSyntaxTokenType.DoubleBar:
                case StyleSyntaxTokenType.DoubleAmpersand:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsMultiplier(StyleSyntaxToken token)
        {
            switch (token.type)
            {
                case StyleSyntaxTokenType.Asterisk:
                case StyleSyntaxTokenType.Plus:
                case StyleSyntaxTokenType.QuestionMark:
                case StyleSyntaxTokenType.HashMark:
                case StyleSyntaxTokenType.ExclamationPoint:
                case StyleSyntaxTokenType.OpenBrace:
                    return true;
                default:
                    return false;
            }
        }
    }
}
