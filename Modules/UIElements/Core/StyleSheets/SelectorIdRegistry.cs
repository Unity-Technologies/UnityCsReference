// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Scripting.LifecycleManagement;

namespace UnityEngine.UIElements
{
    // Grow-only set of the class and name ids used by any selector that was ever flattened into the
    // selector acceleration cache. Ids are never removed: a stale entry only costs a spurious restyle.
    internal static class SelectorIdRegistry
    {
        // Ids persist across code reload with UniqueStyleString's tables, so the registered bits must too.
        [NoAutoStaticsCleanup]
        static ulong[] s_ClassIds = Array.Empty<ulong>();
        [NoAutoStaticsCleanup]
        static ulong[] s_NameIds = Array.Empty<ulong>();

        public static void Register(StyleSelectorType type, int id)
        {
            if (id <= UniqueStyleString.Empty.id)
                return;

            if (type == StyleSelectorType.Class)
                Set(ref s_ClassIds, id);
            else if (type == StyleSelectorType.ID)
                Set(ref s_NameIds, id);
        }

        public static bool IsClassUsed(int id) => IsSet(s_ClassIds, id);

        public static bool IsNameUsed(int id) => IsSet(s_NameIds, id);

        public static bool IsAnyClassUsed(ReadOnlySpan<int> ids)
        {
            foreach (var id in ids)
            {
                if (IsSet(s_ClassIds, id))
                    return true;
            }
            return false;
        }

        public static bool IsAnyClassUsed(ReadOnlySpan<UniqueStyleString> classNames)
        {
            foreach (var className in classNames)
            {
                if (IsSet(s_ClassIds, className.id))
                    return true;
            }
            return false;
        }

        static void Set(ref ulong[] bits, int id)
        {
            var word = id >> 6;
            if (word >= bits.Length)
                Array.Resize(ref bits, Math.Max(word + 1, bits.Length * 2));
            bits[word] |= 1UL << (id & 63);
        }

        static bool IsSet(ulong[] bits, int id)
        {
            var word = (uint)id >> 6;
            return word < (uint)bits.Length && (bits[word] & (1UL << (id & 63))) != 0;
        }
    }
}
