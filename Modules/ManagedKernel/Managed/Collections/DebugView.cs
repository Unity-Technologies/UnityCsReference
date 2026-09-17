// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;

namespace Unity.Collections
{
    internal struct Pair<Key, Value>
    {
        public Key key;
        public Value value;
        public Pair(Key k, Value v)
        {
            key = k;
            value = v;
        }

        public override string ToString()
        {
            return $"{key} = {value}";
        }
    }

    // Tiny does not contains an IList definition (or even ICollection)
    internal struct ListPair<Key, Value> where Value : IList
    {
        public Key key;
        public Value value;

        public ListPair(Key k, Value v)
        {
            key = k;
            value = v;
        }

        public override string ToString()
        {
            String result = $"{key} = [";
            for (var v = 0; v < value.Count; ++v)
            {
                result += value[v];
                if (v < value.Count - 1)
                    result += ", ";
            }

            result += "]";
            return result;
        }
    }
}
