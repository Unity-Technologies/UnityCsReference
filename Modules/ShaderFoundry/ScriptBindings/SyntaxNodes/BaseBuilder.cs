// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.ShaderFoundry
{
    internal class BaseBuilder
    {
        public SyntaxTree syntaxTree { get; private set; }
        public BaseBuilder(SyntaxTree syntaxTree)
        {
            if (syntaxTree == null || !syntaxTree.IsValid)
                throw new InvalidOperationException("SyntaxTree is not valid. Did you destroy it?");
            this.syntaxTree = syntaxTree;
        }

        internal static SyntaxNode Validate<SyntaxNode>(SyntaxNode node) where SyntaxNode : ISyntaxNode
        {
            if (!node.IsValid)
                throw new InvalidOperationException("Node is not valid.");
            return node;
        }

        internal static bool IsAscii(char c)
        {
            return c >= '\u0000' && c <= '\u007f';
        }

        internal static bool IsAsciiLetter(char c)
        {
            return 'A' <= c && c <= 'Z' || 'a' <= c && c <= 'z';
        }

        internal static string ValidateIdentifierString(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
                throw new InvalidOperationException($"The string '{identifier}' is not a valid identifier.");
            if (!IsAsciiLetter(identifier[0]) && identifier[0] != '_')
                throw new InvalidOperationException($"The string '{identifier}' is not a valid identifier.");
            for (var i = 1; i < identifier.Length; ++i)
            {
                if (!IsAsciiLetter(identifier[i]) && !char.IsDigit(identifier[i]) && identifier[i] != '_')
                    throw new InvalidOperationException($"The string '{identifier}' is not a valid identifier.");
            }
            return identifier;
        }

        // Raises an exception if the given string contains unicode characters.
        // fieldName is the semantic name of str, to be used in the exception's message.
        // If the given string is null, returns the empty string. Otherwise, returns the given string.
        internal static string ValidateString(string str, string fieldName)
        {
            if (string.IsNullOrEmpty(str))
                return string.Empty;

            foreach (char c in str)
            {
                if (!IsAscii(c))
                {
                    throw new ArgumentException($"{fieldName} does not allow unicode characters");
                }
            }
            return str;
        }
    }
}
