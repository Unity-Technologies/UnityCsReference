// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Threading;

namespace UnityEngine
{
    /// <summary>
    /// Intern table for managed objects (delegates, Types, MemberInfos, ...) that
    /// native serialization commands need to reference. Commands store a small
    /// integer index into this table instead of holding a GCHandle directly,
    /// which keeps command storage compact and centralizes the GC-root lifecycle.
    ///
    /// The table grows monotonically and is cleared together with the
    /// SerializationCache (typically on a domain reload). Calls to <see cref="Intern"/>
    /// dedupe by default equality so two equivalent delegates / two references to
    /// the same Type collapse onto the same index.
    /// </summary>
    /// <remarks>
    /// Threading: writes are serialized by <c>s_GrowLock</c>. Reads via
    /// <see cref="Get"/> are lock-free — the backing array reference is published
    /// with <see cref="Volatile.Write"/> and observed with <see cref="Volatile.Read"/>,
    /// so a reader sees either the pre-resize array (still valid for the index
    /// requested, since the old array remains alive while any reader holds it)
    /// or the post-resize array (also valid).
    /// </remarks>
    internal static class SerializationCommandObjectTable
    {
        private const int InitialCapacity = 64;

        private static object[] s_Objects = new object[InitialCapacity];
        private static int s_Count;
        private static readonly Dictionary<object, int> s_Dedup = new Dictionary<object, int>(InitialCapacity);
        private static readonly object s_GrowLock = new object();

        /// <summary>
        /// Returns the existing index for <paramref name="obj"/> if it's already
        /// in the table, otherwise appends it and returns the new index.
        /// </summary>
        internal static int Intern(object obj)
        {
            lock (s_GrowLock)
            {
                if (s_Dedup.TryGetValue(obj, out int existing))
                    return existing;

                if (s_Count == s_Objects.Length)
                {
                    var grown = new object[s_Objects.Length * 2];
                    System.Array.Copy(s_Objects, grown, s_Count);
                    // Atomic publish so concurrent readers see either the
                    // pre-resize or fully-populated post-resize array.
                    Volatile.Write(ref s_Objects, grown);
                }

                int idx = s_Count;
                s_Objects[idx] = obj;
                s_Dedup[obj] = idx;
                s_Count++;
                return idx;
            }
        }

        /// <summary>
        /// Lock-free index lookup. Caller must pass an index previously returned
        /// by <see cref="Intern"/> on a still-valid table generation.
        /// </summary>
        internal static object Get(int index)
        {
            return Volatile.Read(ref s_Objects)[index];
        }

        /// <summary>
        /// Empties the table and the dedup map. Called from the same site that
        /// invalidates the SerializationCache — typically on domain reload.
        /// </summary>
        internal static void Clear()
        {
            lock (s_GrowLock)
            {
                if (s_Count > 0)
                    System.Array.Clear(s_Objects, 0, s_Count);
                s_Count = 0;
                s_Dedup.Clear();
            }
        }
    }
}
