// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace Unity.Properties
{
    /// <summary>
    /// A <see cref="PropertyPathPartKind"/> specifies a type for a <see cref="PropertyPathPart"/>.
    /// </summary>
    public enum PropertyPathPartKind
    {
        /// <summary>
        /// Represents a named part of the path.
        /// </summary>
        Name,

        /// <summary>
        /// Represents an indexed part of the path.
        /// </summary>
        Index,

        /// <summary>
        /// Represents a keyed part of the path.
        /// </summary>
        Key
    }

    /// <summary>
    /// A <see cref="PropertyPathPart"/> represents a single element of the path.
    /// </summary>
    /// <remarks>
    /// <see cref="PropertyPathPartKind.Name"/>  -> ".{name}"
    /// <see cref="PropertyPathPartKind.Index"/> -> "[{index}]"
    /// <see cref="PropertyPathPartKind.Key"/>   -> "[{key}]"
    /// </remarks>
    public readonly struct PropertyPathPart : IEquatable<PropertyPathPart>
    {
        readonly PropertyPathPartKind m_Kind;
        readonly string m_Name;
        readonly int m_Index;
        readonly object m_Key;

        /// <summary>
        /// Returns true if the part is <see cref="PropertyPathPartKind.Name"/>.
        /// </summary>
        public bool IsName => Kind == PropertyPathPartKind.Name;

        /// <summary>
        /// Returns true if the part is <see cref="PropertyPathPartKind.Index"/>.
        /// </summary>
        public bool IsIndex => Kind == PropertyPathPartKind.Index;

        /// <summary>
        /// Returns true if the part is <see cref="PropertyPathPartKind.Key"/>.
        /// </summary>
        public bool IsKey => Kind == PropertyPathPartKind.Key;

        /// <summary>
        /// The <see cref="PropertyPathPartKind"/> for this path. This determines how algorithms will resolve the path.
        /// </summary>
        public PropertyPathPartKind Kind => m_Kind;

        /// <summary>
        /// The Name of the part. This will only be set when using <see cref="PropertyPathPartKind.Name"/>
        /// </summary>
        public string Name
        {
            get
            {
                CheckKind(PropertyPathPartKind.Name);
                return m_Name;
            }
        }

        /// <summary>
        /// The Index of the part. This will only be set when using <see cref="PropertyPathPartKind.Index"/>
        /// </summary>
        public int Index
        {
            get
            {
                CheckKind(PropertyPathPartKind.Index);
                return m_Index;
            }
        }

        /// <summary>
        /// The Key of the part. This will only be set when using <see cref="PropertyPathPartKind.Key"/>
        /// </summary>
        public object Key
        {
            get
            {
                CheckKind(PropertyPathPartKind.Key);
                return m_Key;
            }
        }

        /// <summary>
        /// Initializes a new <see cref="PropertyPathPart"/> with the specified name.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        public PropertyPathPart(string name)
        {
            m_Kind = PropertyPathPartKind.Name;
            m_Name = name;
            m_Index = -1;
            m_Key = null;
        }

        /// <summary>
        /// Initializes a new <see cref="PropertyPathPart"/> with the specified index.
        /// </summary>
        /// <param name="index">The index of the part.</param>
        public PropertyPathPart(int index)
        {
            m_Kind = PropertyPathPartKind.Index;
            m_Name = string.Empty;
            m_Index = index;
            m_Key = null;
        }

        /// <summary>
        /// Initializes a new <see cref="PropertyPathPart"/> with the specified key.
        /// </summary>
        /// <param name="key">The key of the part.</param>
        public PropertyPathPart(object key)
        {
            m_Kind = PropertyPathPartKind.Key;
            m_Name = string.Empty;
            m_Index = -1;
            m_Key = key;
        }

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void CheckKind(PropertyPathPartKind type)
        {
            if (type != Kind) throw new InvalidOperationException();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Kind switch
            {
                PropertyPathPartKind.Name => m_Name,
                PropertyPathPartKind.Index => "[" + m_Index + "]",
                PropertyPathPartKind.Key => "[\"" + m_Key + "\"]",
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        /// <summary>
        /// Indicates whether this instance and a specified object are equal.
        /// </summary>
        /// <param name="other">The object to compare with the current instance.</param>
        /// <returns><see langword="true"/> if obj and this instance are the same type and represent the same value; otherwise, <see langword="false"/>.</returns>
        public bool Equals(PropertyPathPart other)
        {
            return m_Kind == other.m_Kind && m_Name == other.m_Name && m_Index == other.m_Index && Equals(m_Key, other.m_Key);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is PropertyPathPart other && Equals(other);
        }

        /// <inheritdoc/>
        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int) m_Kind;

                hashCode = m_Kind switch
                {
                    PropertyPathPartKind.Name => (hashCode * 397) ^ (m_Name != null ? m_Name.GetHashCode() : 0),
                    PropertyPathPartKind.Index => (hashCode * 397) ^ m_Index,
                    PropertyPathPartKind.Key => (hashCode * 397) ^ (m_Key != null ? m_Key.GetHashCode() : 0),
                    _ => throw new ArgumentOutOfRangeException()
                };
                return hashCode;
            }
        }
    }

    /// <summary>
    /// A <see cref="PropertyPath"/> is used to store a reference to a single property within a tree.
    /// </summary>
    /// <remarks>
    /// The path is stored as an array of parts and can be easily queried for algorithms.
    /// </remarks>
    public readonly struct PropertyPath : IEquatable<PropertyPath>
    {
        internal const int k_InlineCount = 4;

        readonly PropertyPathPart m_Part0;
        readonly PropertyPathPart m_Part1;
        readonly PropertyPathPart m_Part2;
        readonly PropertyPathPart m_Part3;
        readonly PropertyPathPart[] m_AdditionalParts;

        /// <summary>
        /// Gets the number of parts contained in the <see cref="PropertyPath"/>.
        /// </summary>
        public int Length { get; }

        /// <summary>
        /// Gets if there is any part contained in the <see cref="PropertyPath"/>.
        /// </summary>
        public bool IsEmpty => Length == 0;

        /// <summary>
        /// Gets the <see cref="PropertyPathPart"/> at the given index.
        /// </summary>
        public PropertyPathPart this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                    {
                        if (Length < 1)
                            throw new IndexOutOfRangeException();
                        return m_Part0;
                    }
                    case 1:
                    {
                        if (Length < 2)
                            throw new IndexOutOfRangeException();
                        return m_Part1;
                    }
                    case 2:
                    {
                        if (Length < 3)
                            throw new IndexOutOfRangeException();
                        return m_Part2;
                    }
                    case 3:
                    {
                        if (Length < 4)
                            throw new IndexOutOfRangeException();
                        return m_Part3;
                    }
                    default: return m_AdditionalParts[index - k_InlineCount];
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyPath"/> based on the given property string.
        /// </summary>
        /// <param name="path">The string path to initialize this instance with.</param>
        public PropertyPath(string path)
        {
            var p = ConstructFromPath(path);
            m_Part0 = p.m_Part0;
            m_Part1 = p.m_Part1;
            m_Part2 = p.m_Part2;
            m_Part3 = p.m_Part3;
            m_AdditionalParts = p.m_AdditionalParts;
            Length = p.Length;
        }

        PropertyPath(in PropertyPathPart part)
        {
            m_Part0 = part;
            m_Part1 = default;
            m_Part2 = default;
            m_Part3 = default;
            m_AdditionalParts = default;
            Length = 1;
        }

        PropertyPath(in PropertyPathPart part0, in PropertyPathPart part1)
        {
            m_Part0 = part0;
            m_Part1 = part1;
            m_Part2 = default;
            m_Part3 = default;
            m_AdditionalParts = default;
            Length = 2;
        }

        PropertyPath(in PropertyPathPart part0, in PropertyPathPart part1, in PropertyPathPart part2)
        {
            m_Part0 = part0;
            m_Part1 = part1;
            m_Part2 = part2;
            m_Part3 = default;
            m_AdditionalParts = default;
            Length = 3;
        }

        PropertyPath(in PropertyPathPart part0, in PropertyPathPart part1, in PropertyPathPart part2, in PropertyPathPart part3)
        {
            m_Part0 = part0;
            m_Part1 = part1;
            m_Part2 = part2;
            m_Part3 = part3;
            m_AdditionalParts = default;
            Length = 4;
        }

        internal PropertyPath(List<PropertyPathPart> parts)
        {
            m_Part0 = default;
            m_Part1 = default;
            m_Part2 = default;
            m_Part3 = default;
            m_AdditionalParts = parts.Count > k_InlineCount
                ? new PropertyPathPart[parts.Count - k_InlineCount]
                : default;

            for (var i = 0; i < parts.Count; ++i)
            {
                switch (i)
                {
                    case 0:
                        m_Part0 = parts[i];
                        break;
                    case 1:
                        m_Part1 = parts[i];
                        break;
                    case 2:
                        m_Part2 = parts[i];
                        break;
                    case 3:
                        m_Part3 = parts[i];
                        break;
                    default:
                        // ReSharper disable once PossibleNullReferenceException
                        m_AdditionalParts[i - k_InlineCount] = parts[i];
                        break;
                }
            }
            Length = parts.Count;
        }

        /// <summary>
        /// Returns a new <see cref="PropertyPath"/> from the provided <see cref="PropertyPathPart"/>.
        /// </summary>
        /// <param name="part">The <see cref="PropertyPathPart"/></param>
        /// <returns>A new <see cref="PropertyPath"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PropertyPath FromPart(in PropertyPathPart part)
            => new PropertyPath(part);

        /// <summary>
        /// Returns a new <see cref="PropertyPath"/> from the provided name.
        /// </summary>
        /// <param name="name">The name of the <see cref="PropertyPathPart"/>.</param>
        /// <returns>A new <see cref="PropertyPath"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PropertyPath FromName(string name)
            => new PropertyPath(new PropertyPathPart(name));

        /// <summary>
        /// Returns a new <see cref="PropertyPath"/> from the provided index.
        /// </summary>
        /// <param name="index">The index of the <see cref="PropertyPathPart"/>.</param>
        /// <returns>A new <see cref="PropertyPath"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PropertyPath FromIndex(int index)
            => new PropertyPath(new PropertyPathPart(index));

        /// <summary>
        /// Returns a new <see cref="PropertyPath"/> from the provided key.
        /// </summary>
        /// <param name="key">The key of the <see cref="PropertyPathPart"/>.</param>
        /// <returns>A new <see cref="PropertyPath"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PropertyPath FromKey(object key)
            => new PropertyPath(new PropertyPathPart(key));

        /// <summary>
        /// Returns a new <see cref="PropertyPath"/> combining the parts of the two given <see cref="PropertyPath"/>.
        /// </summary>
        /// <param name="path">The <see cref="PropertyPath"/></param>
        /// <param name="pathToAppend">The <see cref="PropertyPath"/> to append.</param>
        /// <returns>A new <see cref="PropertyPath"/></returns>
        public static PropertyPath Combine(in PropertyPath path, in PropertyPath pathToAppend)
        {
            if (path.IsEmpty)
                return pathToAppend;

            if (pathToAppend.IsEmpty)
                return path;

            var firstPathLength = path.Length;
            var secondPathLength = pathToAppend.Length;
            var totalCount = firstPathLength + secondPathLength;
            if (totalCount <= k_InlineCount)
            {
                var secondIndex = 0;
                var part0 = path.m_Part0;
                var part1 = firstPathLength > 1 ? path.m_Part1 : pathToAppend[secondIndex++];
                var part2 = totalCount > 2 ? (firstPathLength > 2 ? path.m_Part2 : pathToAppend[secondIndex++]) : default;
                var part3 = totalCount > 3 ? (firstPathLength > 3 ? path.m_Part3 : pathToAppend[secondIndex]) : default;

                switch (totalCount)
                {
                    case 2: return new PropertyPath(part0, part1);
                    case 3: return new PropertyPath(part0, part1, part2);
                    case 4: return new PropertyPath(part0, part1, part2, part3);
                }
            }

            var parts = UnityEngine.Pool.ListPool<PropertyPathPart>.Get();
            try
            {
                GetParts(path, parts);
                GetParts(pathToAppend, parts);
                return new PropertyPath(parts);
            }
            finally
            {
                UnityEngine.Pool.ListPool<PropertyPathPart>.Release(parts);
            }
        }

        /// <summary>
        /// Returns a new <see cref="PropertyPath"/> combining the parts of the two given <see cref="PropertyPath"/>.
        /// </summary>
        /// <param name="path">The <see cref="PropertyPath"/></param>
        /// <param name="pathToAppend">The string path to append.</param>
        /// <returns>A new <see cref="PropertyPath"/></returns>
        public static PropertyPath Combine(in PropertyPath path, string pathToAppend)
        {
            if (string.IsNullOrEmpty(pathToAppend))
                return path;

            var other = new PropertyPath(pathToAppend);
            return Combine(path, other);
        }

        /// <summary>
        /// Returns a new <see cref="PropertyPath"/> combining the given <see cref="PropertyPath"/> and <see cref="PropertyPathPart"/>.
        /// </summary>
        /// <param name="path">The <see cref="PropertyPath"/></param>
        /// <param name="part">The part to add.</param>
        /// <returns>A new <see cref="PropertyPath"/></returns>
        public static PropertyPath AppendPart(in PropertyPath path, in PropertyPathPart part)
        {
            if (path.IsEmpty)
                return new PropertyPath(part);

            switch (path.Length + 1)
            {
                case 2:
                    return new PropertyPath(path.m_Part0, part);
                case 3:
                    return new PropertyPath(path.m_Part0, path.m_Part1, part);
                case 4:
                    return new PropertyPath(path.m_Part0, path.m_Part1, path.m_Part2, part);
                default:
                    var parts = UnityEngine.Pool.ListPool<PropertyPathPart>.Get();
                    try
                    {
                        GetParts(path, parts);
                        parts.Add(part);
                        return new PropertyPath(parts);
                    }
                    finally
                    {
                        UnityEngine.Pool.ListPool<PropertyPathPart>.Release(parts);
                    }
            }
        }

        /// <summary>
        /// Returns a new <see cref="PropertyPath"/> combining the given <see cref="PropertyPath"/> and an name-type
        /// <see cref="PropertyPathPart"/>.
        /// </summary>
        /// <param name="path">The <see cref="PropertyPath"/></param>
        /// <param name="name">The part name to add.</param>
        /// <returns>A new <see cref="PropertyPath"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PropertyPath AppendName(in PropertyPath path, string name)
            => AppendPart(path, new PropertyPathPart(name));

        /// <summary>
        /// Returns a new <see cref="PropertyPath"/> combining the given <see cref="PropertyPath"/> and an index-type
        /// <see cref="PropertyPathPart"/>.
        /// </summary>
        /// <param name="path">The <see cref="PropertyPath"/></param>
        /// <param name="index">The index to add.</param>
        /// <returns>A new <see cref="PropertyPath"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PropertyPath AppendIndex(in PropertyPath path, int index)
            => AppendPart(path, new PropertyPathPart(index));

        /// <summary>
        /// Returns a new <see cref="PropertyPath"/> combining the given <see cref="PropertyPath"/> and an key-type
        /// <see cref="PropertyPathPart"/>.
        /// </summary>
        /// <param name="path">The <see cref="PropertyPath"/></param>
        /// <param name="key">The key to add.</param>
        /// <returns>A new <see cref="PropertyPath"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PropertyPath AppendKey(in PropertyPath path, object key)
            => AppendPart(path, new PropertyPathPart(key));

        /// <summary>
        /// Returns a new <see cref="PropertyPath"/> combining the given <see cref="PropertyPath"/> and a <see cref="PropertyPathPart"/>
        /// whose type will be based on the property interfaces.
        /// </summary>
        /// <param name="path">The <see cref="PropertyPath"/></param>
        /// <param name="property">The property to add.</param>
        /// <returns>A new <see cref="PropertyPath"/></returns>
        public static PropertyPath AppendProperty(in PropertyPath path, IProperty property)
        {
            return property switch
            {
                IListElementProperty listElementProperty => AppendPart(path, new PropertyPathPart(listElementProperty.Index)),
                ISetElementProperty setElementProperty => AppendPart(path, new PropertyPathPart(setElementProperty.ObjectKey)),
                IDictionaryElementProperty dictionaryElementProperty => AppendPart(path, new PropertyPathPart(dictionaryElementProperty.ObjectKey)),
                _ => AppendPart(path, new PropertyPathPart(property.Name))
            };
        }

        /// <summary>
        /// Returns a new <see cref="PropertyPath"/> that will not include the last <see cref="PropertyPathPart"/>.
        /// </summary>
        /// <param name="path">The <see cref="PropertyPath"/></param>
        /// <returns>A new <see cref="PropertyPath"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PropertyPath Pop(in PropertyPath path)
            => SubPath(path, 0, path.Length - 1);

        /// <summary>
        /// Returns a new <see cref="PropertyPath"/> containing the <see cref="PropertyPathPart"/> starting at the given
        /// start index.
        /// </summary>
        /// <param name="path">The <see cref="PropertyPath"/></param>
        /// <param name="startIndex">The zero-based index where the sub path should start.</param>
        /// <returns>A new <see cref="PropertyPath"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PropertyPath SubPath(in PropertyPath path, int startIndex)
            => SubPath(path, startIndex, path.Length - startIndex);

        /// <summary>
        /// Returns a new <see cref="PropertyPath"/> containing the given number of <see cref="PropertyPathPart"/>
        /// starting at the given start index.
        /// </summary>
        /// <param name="path">The <see cref="PropertyPath"/></param>
        /// <param name="startIndex">The zero-based index where the sub path should start.</param>
        /// <param name="length">The number of parts to include.</param>
        /// <returns>A new <see cref="PropertyPath"/></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static PropertyPath SubPath(in PropertyPath path, int startIndex, int length)
        {
            var count = path.Length;
            if (startIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            if (startIndex > count)
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));
            if (startIndex > count - length)
                throw new ArgumentOutOfRangeException(nameof(length));
            if (length == 0)
                return default;

            if (startIndex == 0 && length == count)
                return path;

            switch (length)
            {
                case 1: return new PropertyPath(path[startIndex]);
                case 2: return new PropertyPath(path[startIndex], path[startIndex + 1]);
                case 3: return new PropertyPath(path[startIndex], path[startIndex + 1], path[startIndex + 2]);
                case 4: return new PropertyPath(path[startIndex], path[startIndex + 1], path[startIndex + 2], path[startIndex + 3]);
            }

            var parts = UnityEngine.Pool.ListPool<PropertyPathPart>.Get();

            try
            {
                for (var i = startIndex; i < startIndex + length; ++i)
                    parts.Add(path[i]);
                return new PropertyPath(parts);
            }
            finally
            {
                UnityEngine.Pool.ListPool<PropertyPathPart>.Release(parts);
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (Length == 0)
                return string.Empty;

            if (Length == 1 && m_Part0.IsName)
                return m_Part0.Name;

            var builder = new StringBuilder(32);

            if (Length > 0)
                AppendToBuilder(m_Part0, builder);
            if (Length > 1)
                AppendToBuilder(m_Part1, builder);
            if (Length > 2)
                AppendToBuilder(m_Part2, builder);
            if (Length > 3)
                AppendToBuilder(m_Part3, builder);

            if (Length > k_InlineCount)
            {
                foreach (var part in m_AdditionalParts)
                {
                    AppendToBuilder(part, builder);
                }
            }

            return builder.ToString();
        }

        static void AppendToBuilder(in PropertyPathPart part, StringBuilder builder)
        {
            switch (part.Kind)
            {
                case PropertyPathPartKind.Name:
                    if (builder.Length > 0)
                        builder.Append('.');
                    builder.Append(part.ToString());
                    break;

                case PropertyPathPartKind.Index:
                case PropertyPathPartKind.Key:
                    builder.Append(part.ToString());
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        static void GetParts(in PropertyPath path, List<PropertyPathPart> parts)
        {
            var count = path.Length;
            for (var i = 0; i < count; ++i)
                parts.Add(path[i]);
        }

        static PropertyPath ConstructFromPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return default;

            const int matchAny = 0;
            const int matchName = 1;
            const int matchIndexOrKey = 2;
            const int matchIndex = 3;
            const int matchKey = 4;

            var index = matchAny;
            var length = path.Length;

            var state = 0;

            void TrimStart()
            {
                while (index < length && path[index] == ' ')
                    ++index;
            }

            void ReadNext()
            {
                if (index == length)
                {
                    state = matchAny;
                    return;
                }

                switch (path[index])
                {
                    case '.':
                        ++index;
                        state = matchAny;
                        return;
                    case '[':
                        state = matchIndexOrKey;
                        return;
                    default:
                        throw new ArgumentException($"{nameof(PropertyPath)}: Invalid '{path[index]}' character encountered at index '{index}'.");
                }
            }

            var parts = UnityEngine.Pool.ListPool<PropertyPathPart>.Get();

            try
            {
                parts.Clear();
                while (index < length)
                {
                    switch (state)
                    {
                        case matchAny:
                            TrimStart();

                            if (index == length)
                                break;

                            if (path[index] == '.')
                                throw new ArgumentException($"{nameof(PropertyPath)}: Invalid '{path[index]}' character encountered at index '{index}'.");

                            if (path[index] == '[')
                            {
                                state = matchIndexOrKey;
                                continue;
                            }

                            if (path[index] == '"')
                                throw new ArgumentException($"{nameof(PropertyPath)}: Invalid '{path[index]}' character encountered at index '{index}'.");

                            state = matchName;
                            continue;
                        case matchName:
                        {
                            var startIndex = index;
                            while (index < length)
                            {
                                if (path[index] == '.' || path[index] == '[')
                                    break;
                                ++index;
                            }

                            if (startIndex == index)
                                throw new ArgumentException($"Invalid {nameof(PropertyPath)}: Name is empty.");

                            if (index == length)
                            {
                                parts.Add(new PropertyPathPart(path.Substring(startIndex)));
                                state = matchAny;
                                continue;
                            }

                            parts.Add(new PropertyPathPart(path.Substring(startIndex, index-startIndex)));

                            ReadNext();
                            continue;
                        }
                        case matchIndexOrKey:
                            if (path[index] != '[')
                                throw new ArgumentException($"{nameof(PropertyPath)}: Invalid '{path[index]}' character encountered at index '{index}'.");

                            if (index + 1 < length && path[index + 1] == '"')
                            {
                                state = matchKey;
                                continue;
                            }

                            state = matchIndex;
                            continue;
                        case matchIndex:
                        {
                            if (path[index] != '[')
                                throw new ArgumentException($"{nameof(PropertyPath)}: Invalid '{path[index]}' character encountered at index '{index}'.");
                            ++index;

                            var startIndex = index;

                            while (index < length)
                            {
                                var ci = path[index];
                                if (ci == ']')
                                    break;
                                ++index;
                            }

                            if (path[index] != ']')
                                throw new ArgumentException($"{nameof(PropertyPath)}: Invalid '{path[index]}' character encountered at index '{index}'.");

                            var indexStr = path.Substring(startIndex, index-startIndex);
                            if (!int.TryParse(indexStr, out var partIndex))
                                throw new ArgumentException($"Indices in {nameof(PropertyPath)} must be a numeric value.");

                            if (partIndex < 0)
                                throw new ArgumentException($"Invalid {nameof(PropertyPath)}: Negative indices are not supported.");

                            parts.Add(new PropertyPathPart(partIndex));
                            ++index;

                            if (index == length)
                                break;

                            ReadNext();
                            continue;
                        }

                        case matchKey:
                        {
                            if (path[index] != '[')
                                throw new ArgumentException($"{nameof(PropertyPath)}: Invalid '{path[index]}' character encountered at index '{index}'.");
                            ++index;

                            if (path[index] != '"')
                                throw new ArgumentException($"{nameof(PropertyPath)}: Invalid '{path[index]}' character encountered at index '{index}'.");
                            ++index;

                            var startIndex = index;

                            while (index < length)
                            {
                                var ci = path[index];
                                if (ci == '"')
                                    break;
                                ++index;
                            }

                            if (path[index] != '"')
                                throw new ArgumentException($"{nameof(PropertyPath)}: Invalid '{path[index]}' character encountered at index '{index}'.");

                            if (index + 1 < length && path[index + 1] == ']')
                            {
                                var keyStr = path.Substring(startIndex, index - startIndex);
                                parts.Add(new PropertyPathPart((object) keyStr));
                                index += 2; // "]

                                ReadNext();
                                continue;
                            }

                            throw new ArgumentException($"Invalid {nameof(PropertyPath)}: No matching end quote for key.");
                        }
                    }
                }

                return new PropertyPath(parts);
            }
            finally
            {
                UnityEngine.Pool.ListPool<PropertyPathPart>.Release(parts);
            }
        }

        /// <undoc/>
        public static bool operator==(PropertyPath lhs, PropertyPath rhs)
        {
            return lhs.Equals(rhs);
        }

        /// <undoc/>
        public static bool operator !=(PropertyPath lhs, PropertyPath rhs)
        {
            return !(lhs == rhs);
        }

        /// <summary>
        /// Indicates whether this instance and a specified object are equal.
        /// </summary>
        /// <param name="other">The object to compare with the current instance.</param>
        /// <returns><see langword="true"/> if obj and this instance are the same type and represent the same value; otherwise, <see langword="false"/>.</returns>
        public bool Equals(PropertyPath other)
        {
            if (Length != other.Length)
                return false;

            for (var i = 0; i < Length; ++i)
            {
                if (!this[i].Equals(other[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is PropertyPath path)
                return Equals(path);
            return false;
        }

        /// <inheritdoc/>
        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode()
        {
            var hashcode = 19;
            var count = Length;

            if (count == 0)
                return hashcode;

            if (count > 0)
                hashcode = hashcode * 31 + m_Part0.GetHashCode();
            if (count > 1)
                hashcode = hashcode * 31 + m_Part1.GetHashCode();
            if (count > 2)
                hashcode = hashcode * 31 + m_Part2.GetHashCode();
            if (count > 3)
                hashcode = hashcode * 31 + m_Part3.GetHashCode();

            if (count <= k_InlineCount)
                return hashcode;

            foreach (var part in m_AdditionalParts)
            {
                hashcode = hashcode * 31 + part.GetHashCode();
            }

            return hashcode;
        }
    }
}
