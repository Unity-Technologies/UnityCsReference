// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Scripting;

namespace UnityEngine
{
    [Serializable]
    [RequiredByNativeCode]
    public sealed class AnimatableFloatChannels : IEnumerable<KeyValuePair<string, float>>
    {
        // The animation binding writes m_Values by position; see MonoBehaviourAnimationBinding.cpp.
        [SerializeField] string[] m_Names = Array.Empty<string>();
        [SerializeField] float[] m_Values = Array.Empty<float>();

        public int channelCount => AnimatableChannelsDispatcher.CountOf(m_Names, m_Values);

        public string GetChannelName(int index) => AnimatableChannelsDispatcher.IsValidIndex(m_Names, m_Values, index) ? m_Names[index] : string.Empty;

        public int IndexOf(string name) => AnimatableChannelsDispatcher.IndexOf(m_Names, m_Values, name);

        // Hashes of the channel names extended from a field's prefix, which the binding matches curves
        // against; see AnimatableChannelsDispatcher.FindChannel.
        [NonSerialized] uint[] m_NameHashes;
        [NonSerialized] int m_NameHashesPrefix;

        public void SetChannels(params string[] names)
        {
            m_Names = AnimatableChannelsDispatcher.CopyChannelNames(names);
            m_Values = new float[m_Names.Length];
            m_NameHashes = null;
        }

        // Native code finds these by method name alone, so each container names them differently.
        [RequiredByNativeCode]
        internal int FindFloatChannel(int prefixHash, int attributeHash) => AnimatableChannelsDispatcher.FindChannel(m_Names, channelCount, ref m_NameHashes, ref m_NameHashesPrefix, prefixHash, attributeHash);

        [RequiredByNativeCode]
        internal string[] GetFloatChannelNames() => AnimatableChannelsDispatcher.GetBindableNames(m_Names, channelCount);

        public float this[int index]
        {
            get => AnimatableChannelsDispatcher.IsValidIndex(m_Names, m_Values, index) ? m_Values[index] : 0f;
            set
            {
                if (AnimatableChannelsDispatcher.IsValidIndex(m_Names, m_Values, index))
                    m_Values[index] = value;
            }
        }

        public Enumerator GetEnumerator() => new Enumerator(m_Names, m_Values);

        IEnumerator<KeyValuePair<string, float>> IEnumerable<KeyValuePair<string, float>>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // A struct, so that foreach does not allocate.
        public struct Enumerator : IEnumerator<KeyValuePair<string, float>>
        {
            readonly string[] m_Names;
            readonly float[] m_Values;
            readonly int m_Count;
            int m_Index;

            internal Enumerator(string[] names, float[] values)
            {
                m_Names = names;
                m_Values = values;
                m_Count = AnimatableChannelsDispatcher.CountOf(names, values);
                m_Index = -1;
            }

            public KeyValuePair<string, float> Current => new KeyValuePair<string, float>(m_Names[m_Index], m_Values[m_Index]);

            object IEnumerator.Current => Current;

            public bool MoveNext() => ++m_Index < m_Count;

            void IEnumerator.Reset() => m_Index = -1;

            void IDisposable.Dispose()
            {
            }
        }
    }

    [Serializable]
    [RequiredByNativeCode]
    public sealed class AnimatableIntChannels : IEnumerable<KeyValuePair<string, int>>
    {
        // The animation binding writes m_Values by position; see MonoBehaviourAnimationBinding.cpp.
        [SerializeField] string[] m_Names = Array.Empty<string>();
        [SerializeField] int[] m_Values = Array.Empty<int>();

        public int channelCount => AnimatableChannelsDispatcher.CountOf(m_Names, m_Values);

        public string GetChannelName(int index) => AnimatableChannelsDispatcher.IsValidIndex(m_Names, m_Values, index) ? m_Names[index] : string.Empty;

        public int IndexOf(string name) => AnimatableChannelsDispatcher.IndexOf(m_Names, m_Values, name);

        // Hashes of the channel names extended from a field's prefix, which the binding matches curves
        // against; see AnimatableChannelsDispatcher.FindChannel.
        [NonSerialized] uint[] m_NameHashes;
        [NonSerialized] int m_NameHashesPrefix;

        public void SetChannels(params string[] names)
        {
            m_Names = AnimatableChannelsDispatcher.CopyChannelNames(names);
            m_Values = new int[m_Names.Length];
            m_NameHashes = null;
        }

        // Native code finds these by method name alone, so each container names them differently.
        [RequiredByNativeCode]
        internal int FindIntChannel(int prefixHash, int attributeHash) => AnimatableChannelsDispatcher.FindChannel(m_Names, channelCount, ref m_NameHashes, ref m_NameHashesPrefix, prefixHash, attributeHash);

        [RequiredByNativeCode]
        internal string[] GetIntChannelNames() => AnimatableChannelsDispatcher.GetBindableNames(m_Names, channelCount);

        public int this[int index]
        {
            get => AnimatableChannelsDispatcher.IsValidIndex(m_Names, m_Values, index) ? m_Values[index] : 0;
            set
            {
                if (AnimatableChannelsDispatcher.IsValidIndex(m_Names, m_Values, index))
                    m_Values[index] = value;
            }
        }

        public Enumerator GetEnumerator() => new Enumerator(m_Names, m_Values);

        IEnumerator<KeyValuePair<string, int>> IEnumerable<KeyValuePair<string, int>>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // A struct, so that foreach does not allocate.
        public struct Enumerator : IEnumerator<KeyValuePair<string, int>>
        {
            readonly string[] m_Names;
            readonly int[] m_Values;
            readonly int m_Count;
            int m_Index;

            internal Enumerator(string[] names, int[] values)
            {
                m_Names = names;
                m_Values = values;
                m_Count = AnimatableChannelsDispatcher.CountOf(names, values);
                m_Index = -1;
            }

            public KeyValuePair<string, int> Current => new KeyValuePair<string, int>(m_Names[m_Index], m_Values[m_Index]);

            object IEnumerator.Current => Current;

            public bool MoveNext() => ++m_Index < m_Count;

            void IEnumerator.Reset() => m_Index = -1;

            void IDisposable.Dispose()
            {
            }
        }
    }

    [Serializable]
    [RequiredByNativeCode]
    public sealed class AnimatableObjectChannels : IEnumerable<KeyValuePair<string, Object>>
    {
        // The animation binding writes m_Values by position; see MonoBehaviourAnimationBinding.cpp.
        [SerializeField] string[] m_Names = Array.Empty<string>();
        [SerializeField] Object[] m_Values = Array.Empty<Object>();

        public int channelCount => AnimatableChannelsDispatcher.CountOf(m_Names, m_Values);

        public string GetChannelName(int index) => AnimatableChannelsDispatcher.IsValidIndex(m_Names, m_Values, index) ? m_Names[index] : string.Empty;

        public int IndexOf(string name) => AnimatableChannelsDispatcher.IndexOf(m_Names, m_Values, name);

        // Hashes of the channel names extended from a field's prefix, which the binding matches curves
        // against; see AnimatableChannelsDispatcher.FindChannel.
        [NonSerialized] uint[] m_NameHashes;
        [NonSerialized] int m_NameHashesPrefix;

        public void SetChannels(params string[] names)
        {
            m_Names = AnimatableChannelsDispatcher.CopyChannelNames(names);
            m_Values = new Object[m_Names.Length];
            m_NameHashes = null;
        }

        // Native code finds these by method name alone, so each container names them differently.
        [RequiredByNativeCode]
        internal int FindObjectChannel(int prefixHash, int attributeHash) => AnimatableChannelsDispatcher.FindChannel(m_Names, channelCount, ref m_NameHashes, ref m_NameHashesPrefix, prefixHash, attributeHash);

        [RequiredByNativeCode]
        internal string[] GetObjectChannelNames() => AnimatableChannelsDispatcher.GetBindableNames(m_Names, channelCount);

        public Object this[int index]
        {
            get => AnimatableChannelsDispatcher.IsValidIndex(m_Names, m_Values, index) ? m_Values[index] : null;
            set
            {
                if (AnimatableChannelsDispatcher.IsValidIndex(m_Names, m_Values, index))
                    m_Values[index] = value;
            }
        }

        public Enumerator GetEnumerator() => new Enumerator(m_Names, m_Values);

        IEnumerator<KeyValuePair<string, Object>> IEnumerable<KeyValuePair<string, Object>>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // A struct, so that foreach does not allocate.
        public struct Enumerator : IEnumerator<KeyValuePair<string, Object>>
        {
            readonly string[] m_Names;
            readonly Object[] m_Values;
            readonly int m_Count;
            int m_Index;

            internal Enumerator(string[] names, Object[] values)
            {
                m_Names = names;
                m_Values = values;
                m_Count = AnimatableChannelsDispatcher.CountOf(names, values);
                m_Index = -1;
            }

            public KeyValuePair<string, Object> Current => new KeyValuePair<string, Object>(m_Names[m_Index], m_Values[m_Index]);

            object IEnumerator.Current => Current;

            public bool MoveNext() => ++m_Index < m_Count;

            void IEnumerator.Reset() => m_Index = -1;

            void IDisposable.Dispose()
            {
            }
        }
    }

    // Shared by the three containers. The binding finds the containers on a component from its
    // serialized layout in native code and calls the containers directly; see
    // MonoBehaviourAnimationBinding.cpp.
    internal static class AnimatableChannelsDispatcher
    {
        // A bound curve carries the channel's position in 14 bits (see MonoBehaviourAnimationBinding.cpp).
        internal const int k_MaxChannelCount = 1 << 14;

        // Deserialized data does not go through SetChannels, so the arrays can disagree in length or be null.
        internal static int CountOf(string[] names, Array values)
        {
            return names != null && values != null ? Math.Min(names.Length, values.Length) : 0;
        }

        internal static bool IsValidIndex(string[] names, Array values, int index)
        {
            return index >= 0 && index < CountOf(names, values);
        }

        internal static int IndexOf(string[] names, Array values, string name)
        {
            var count = CountOf(names, values);
            for (int i = 0; i < count; i++)
            {
                if (string.Equals(names[i], name, StringComparison.Ordinal))
                    return i;
            }
            return -1;
        }

        // Curves reach a channel by name, so a repeated name would leave all but the first unreachable.
        internal static string[] CopyChannelNames(string[] names)
        {
            if (names == null)
                return Array.Empty<string>();

            ValidateChannelNames(names);
            return (string[])names.Clone();
        }

        static void ValidateChannelNames(string[] names)
        {
            if (names.Length > k_MaxChannelCount)
                throw new ArgumentException($"A channel set holds at most {k_MaxChannelCount} channels; {names.Length} were given.", nameof(names));

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var name in names)
            {
                if (name == null)
                    throw new ArgumentException("Channel names must not be null.", nameof(names));
                if (!seen.Add(name))
                    throw new ArgumentException($"The channel name '{name}' appears more than once.", nameof(names));
            }
        }

        // The names a curve can reach. Deserialized data does not go through SetChannels, so it can
        // hold more than a bound curve can address.
        internal static string[] GetBindableNames(string[] names, int count)
        {
            var result = new string[Math.Min(count, k_MaxChannelCount)];
            for (int i = 0; i < result.Length; i++)
                result[i] = names[i] ?? string.Empty;
            return result;
        }

        // The position of the channel whose "<prefix><name>" hashes to attributeHash, or -1. prefixHash is
        // the finished CRC of the prefix, which is how native code passes the prefix without a string.
        // The hashes are kept between calls, so binding a container's curves costs one pass over its
        // names rather than one per curve.
        internal static int FindChannel(string[] names, int count, ref uint[] cachedHashes, ref int cachedPrefixHash, int prefixHash, int attributeHash)
        {
            var prefixState = (uint)prefixHash ^ k_Crc32InitialState;
            var hashes = cachedHashes;
            var built = false;
            if (hashes == null || hashes.Length != count || cachedPrefixHash != prefixHash)
            {
                hashes = HashNames(names, count, prefixState);
                built = true;
            }

            var index = Array.IndexOf(hashes, (uint)attributeHash);

            // The serializer refills m_Names in place when its length doesn't change, so cached hashes can
            // be stale: confirm a hit, and look again with fresh hashes after a miss.
            if (!built && (index < 0 || HashName(prefixState, names[index]) != (uint)attributeHash))
            {
                hashes = HashNames(names, count, prefixState);
                index = Array.IndexOf(hashes, (uint)attributeHash);
            }

            // Assigned whole, so a reader never sees a partly built array.
            cachedHashes = hashes;
            cachedPrefixHash = prefixHash;
            return index;
        }

        static uint[] HashNames(string[] names, int count, uint prefixState)
        {
            var hashes = new uint[count];
            for (int i = 0; i < count; i++)
                hashes[i] = HashName(prefixState, names[i]);
            return hashes;
        }

        static uint HashName(uint prefixState, string name)
        {
            return UpdateCrc32(prefixState, name ?? string.Empty) ^ k_Crc32InitialState;
        }

        // Matches crc32 in Runtime/Utilities/CRC32.h, which hashes curve attributes as UTF-8.
        const uint k_Crc32InitialState = 0xFFFFFFFF;
        static readonly uint[] k_Crc32Table = CreateCrc32Table();

        static uint[] CreateCrc32Table()
        {
            var table = new uint[256];
            for (uint i = 0; i < table.Length; i++)
            {
                var entry = i;
                for (int bit = 0; bit < 8; bit++)
                    entry = (entry & 1) != 0 ? (entry >> 1) ^ 0xEDB88320 : entry >> 1;
                table[i] = entry;
            }
            return table;
        }

        static uint UpdateCrc32(uint state, byte value)
        {
            return k_Crc32Table[(state ^ value) & 0xFF] ^ (state >> 8);
        }

        // Hashes the UTF-8 encoding of text without allocating it. Unpaired surrogates encode as
        // U+FFFD, as they do when the string is converted for native code.
        static uint UpdateCrc32(uint state, string text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                int codePoint = text[i];
                if (char.IsSurrogate(text[i]))
                {
                    if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                    {
                        codePoint = char.ConvertToUtf32(text[i], text[i + 1]);
                        i++;
                    }
                    else
                        codePoint = 0xFFFD;
                }

                if (codePoint < 0x80)
                    state = UpdateCrc32(state, (byte)codePoint);
                else if (codePoint < 0x800)
                {
                    state = UpdateCrc32(state, (byte)(0xC0 | (codePoint >> 6)));
                    state = UpdateCrc32(state, (byte)(0x80 | (codePoint & 0x3F)));
                }
                else if (codePoint < 0x10000)
                {
                    state = UpdateCrc32(state, (byte)(0xE0 | (codePoint >> 12)));
                    state = UpdateCrc32(state, (byte)(0x80 | ((codePoint >> 6) & 0x3F)));
                    state = UpdateCrc32(state, (byte)(0x80 | (codePoint & 0x3F)));
                }
                else
                {
                    state = UpdateCrc32(state, (byte)(0xF0 | (codePoint >> 18)));
                    state = UpdateCrc32(state, (byte)(0x80 | ((codePoint >> 12) & 0x3F)));
                    state = UpdateCrc32(state, (byte)(0x80 | ((codePoint >> 6) & 0x3F)));
                    state = UpdateCrc32(state, (byte)(0x80 | (codePoint & 0x3F)));
                }
            }
            return state;
        }
    }
}
