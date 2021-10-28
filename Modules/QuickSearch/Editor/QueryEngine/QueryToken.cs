// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.Search
{
    /// <summary>
    /// Represents a token of a query string.
    /// </summary>
    public readonly struct QueryToken
    {
        /// <summary>
        /// The text representing the token.
        /// </summary>
        public string text { get; }

        /// <summary>
        /// The position of the token in the entire query string.
        /// </summary>
        public int position { get; }

        /// <summary>
        /// The length of the token. Can be different than the length of the text.
        /// </summary>
        public int length { get; }

        internal readonly StringView stringView;

        /// <summary>
        /// Creates a token from a string and a position.
        /// </summary>
        /// <param name="text">The value of the token.</param>
        /// <param name="position">The position of the token in the entire query string.</param>
        public QueryToken(string text, int position)
            : this(text, position, text?.Length ?? 0)
        {}

        /// <summary>
        /// Creates a token from a string, a position and a length.
        /// </summary>
        /// <param name="text">The value of the token.</param>
        /// <param name="position">The position of the token in the entire query string.</param>
        /// <param name="length">The length of the token.</param>
        public QueryToken(string text, int position, int length)
        {
            this.position = position;
            this.length = text.Length;
            this.stringView = new StringView(text);
            this.text = text;
        }

        internal QueryToken(in StringView stringView, int position)
            : this(stringView, position, stringView.Length)
        {}

        internal QueryToken(in StringView stringView, int position, int length)
        {
            this.stringView = stringView;
            this.text = stringView.ToString();
            this.position = position;
            this.length = length;
        }

        internal QueryToken(int position, int length)
            : this(StringView.Empty, position, length)
        {}
    }
}
