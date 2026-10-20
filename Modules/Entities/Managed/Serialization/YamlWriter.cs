// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Unity.Mathematics;

namespace Unity.Entities.Serialization
{
    /// <summary>
    /// Small and efficient class to writer in the Yaml format
    /// </summary>
    /// <remarks>
    /// This does not provide all the feature to write YAMl, only what we need for the scope of World Yaml serialization
    /// </remarks>
    internal class YamlWriter
    {
        #region Construction

        public YamlWriter(StreamWriter writer)
        {
            m_CurrentIndent = 0;
            m_IndentText = "";
            m_Writer = writer;
        }

        #endregion

        #region Yaml low level line writting

        public YamlWriter WriteLine(string line)
        {
            m_Writer.Write(m_IndentText);
            m_Writer.WriteLine(line);

            return this;
        }

        #endregion

        #region Yaml high level writing

        public YamlWriter WriteCommentLine<TC>(TC comment)
        {
            var commentAsText = comment.ToString();
            ThrowMultiline(commentAsText);
            m_Writer.WriteLine($"# {commentAsText}");
            return this;
        }

        public YamlWriter WriteKeyValue<TK, TV>(TK key, TV value)
        {
            var valueAsText = FormatValue(value);
            ThrowMultiline(valueAsText);
            WriteLine($"{key.ToString()}: {valueAsText}");
            return this;
        }

        public Collection WriteCollection<TC>(TC collectionName)
        {
            return new Collection(this, collectionName.ToString());
        }

        public YamlWriter WriteInlineSequence<TS>(TS sequenceName, IEnumerable<object> values)
        {
            var sb = new StringBuilder();
            sb.Append('[');
            bool first = true;
            foreach (var value in values)
            {
                if (first == false)
                {
                    sb.Append(", ");
                }

                var valueText = FormatValue(value);
                ThrowMultiline(valueText);

                sb.Append(valueText);
                first = false;
            }

            sb.Append(']');

            WriteLine($"{sequenceName}: {sb}");
            return this;
        }

        public YamlWriter WriteInlineMap<TM>(TM mapName, IEnumerable<KeyValuePair<object, object>> map)
        {
            var sb = new StringBuilder();
            sb.Append('{');
            bool first = true;
            foreach (var kvp in map)
            {
                if (first == false)
                {
                    sb.Append(", ");
                }

                var valueAsText = FormatValue(kvp.Value);
                ThrowMultiline(valueAsText);
                sb.Append($"{kvp.Key}: {valueAsText}");

                first = false;
            }

            sb.Append('}');

            WriteLine($"{mapName}: {sb}");
            return this;
        }

        public YamlWriter WriteInlineMap(string mapName, Dictionary<string, string> map)
        {
            var sb = new StringBuilder();
            sb.Append('{');
            bool first = true;
            foreach (var kvp in map)
            {
                if (first == false)
                {
                    sb.Append(", ");
                }

                var valueAsText = FormatValue(kvp.Value);
                ThrowMultiline(valueAsText);
                sb.Append($"{kvp.Key}: {valueAsText}");

                first = false;
            }

            sb.Append('}');

            WriteLine($"{mapName}: {sb}");
            return this;
        }

        public unsafe YamlWriter WriteFormattedBinaryData(string name, void* buffer, int size, int displayOffset = 0)
        {
            const int intPerRow = 16;

            if (size % 4 != 0)
            {
                throw new ArgumentException($"Size must be at least integer aligned, but is {size}", nameof(size));
            }

            int* b = (int*)buffer;
            int rowOffset = 0;

            var sb = new StringBuilder();
            var intLeft = size / 4;

            using (WriteCollection(name))
            {
                while (intLeft > 0)
                {
                    var rowSize = math.min(intPerRow, intLeft);

                    sb.Clear();
                    sb.Append($"0x{rowOffset + displayOffset:X8}: [");

                    for (int i = 0; i < rowSize; i++)
                    {
                        if (i != 0)
                        {
                            sb.Append(", ");
                        }

                        var val = *b;
                        b++;
                        sb.Append($"0x{val:X8}");
                    }

                    sb.Append("]");
                    WriteLine(sb.ToString());

                    rowOffset += intPerRow * 4;
                    intLeft -= rowSize;
                }
            }

            return this;
        }

        #endregion

        #region Helpers

        static string FormatValue<T>(T value)
        {
            if (typeof(T) == typeof(string))
            {
                return $"\"{value}\"";
            }

            return value.ToString();
        }

        static bool IsMultiLineString(string text)
        {
            return text.Contains("\r") || text.Contains("\n");
        }

        static void ThrowMultiline(string text, [CallerMemberName] string caller = "")
        {
            if (IsMultiLineString(text))
            {
                throw new NotImplementedException($"Method {caller} does not currently support multi-lines text");
            }
        }

        #endregion

        #region Private data

        int m_CurrentIndent;
        string m_IndentText;
        StreamWriter m_Writer;

        #endregion

        #region Scope management

        public int CurrentIndent => m_CurrentIndent;

        void IncrementIndent()
        {
            ++m_CurrentIndent;
            m_IndentText += "  ";
        }

        void DecrementIndent()
        {
            if (--m_CurrentIndent < 0)
            {
                throw new InvalidOperationException("Indent can't be less than 0");
            }

            m_IndentText = m_IndentText.Substring(0, m_CurrentIndent * 2);
        }

        public class Scope : IDisposable
        {
            protected YamlWriter m_Owner;

            internal Scope(YamlWriter owner, bool deferIndentIncrement)
            {
                m_Owner = owner;
                if (!deferIndentIncrement)
                {
                    m_Owner.IncrementIndent();
                }
            }

            public void Dispose()
            {
                m_Owner.DecrementIndent();
            }
        }

        public class Collection : Scope
        {
            public Collection(YamlWriter owner, string collectionName) : base(owner, true)
            {
                m_Owner.WriteLine($"{collectionName}:");
                m_Owner.IncrementIndent();
                CollectionName = collectionName;
            }

            public string CollectionName { get; }
        }

        #endregion
    }
}
