// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.Search
{
    [Flags]
    enum AssetModification
    {
        Updated = 1,
        Removed = 1 << 1,
        Moved = 1 << 2
    }

    struct Transaction
    {
        public long timestamp;
        public Hash128 guid;
        public int state;

        public Transaction(string guid, int state)
            : this(guid, state, DateTime.Now.ToBinary())
        {
        }

        public Transaction(string guid, int state, long timestamp)
        {
            this.timestamp = timestamp;
            this.guid = Hash128.Parse(guid);
            this.state = state;
        }

        public Transaction(string guid, AssetModification state)
            : this(guid, (int)state)
        {
        }

        public Transaction(string guid, AssetModification state, long timestamp)
            : this(guid, (int)state, timestamp)
        {
        }

        public override string ToString()
        {
            return $"[{DateTime.FromBinary(timestamp):u}] ({state}) {guid}";
        }

        public AssetModification GetState()
        {
            return (AssetModification)state;
        }
    }
}
