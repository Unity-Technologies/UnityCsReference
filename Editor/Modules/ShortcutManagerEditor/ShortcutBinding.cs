// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor.ShortcutManagement
{
    public struct ShortcutBinding : IEquatable<ShortcutBinding>
    {
        [NoAutoStaticsCleanup] // immutable default/empty value-type sentinel; safe to persist across reload
        public static ShortcutBinding empty { get; } = new ShortcutBinding();

        readonly KeyCombination[] m_KeyCombinationSequence;

        public IEnumerable<KeyCombination> keyCombinationSequence => m_KeyCombinationSequence ?? Array.Empty<KeyCombination>();

        internal ShortcutBinding(IEnumerable<KeyCombination> keyCombinationSequence)
        {
            if (keyCombinationSequence == null)
                throw new ArgumentNullException(nameof(keyCombinationSequence));

#pragma warning disable UAC2001 // Avoid Linq
#pragma warning disable UAC2002 // Avoid Linq
            m_KeyCombinationSequence = keyCombinationSequence.Any() ? keyCombinationSequence.ToArray() : null;
#pragma warning restore UAC2001
#pragma warning restore UAC2002
        }

        public ShortcutBinding(KeyCombination keyCombination)
            : this(new[] { keyCombination })
        {
        }

        public override string ToString() => KeyCombination.SequenceToString(keyCombinationSequence);

        public bool Equals(ShortcutBinding other)
        {
            #pragma warning disable UAC2014 // Avoid Linq
            return keyCombinationSequence.SequenceEqual(other.keyCombinationSequence);
#pragma warning restore UAC2014
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;

            return obj is ShortcutBinding && Equals((ShortcutBinding)obj);
        }

        public override int GetHashCode()
        {
            if (m_KeyCombinationSequence == null || m_KeyCombinationSequence.Length == 0)
                return 0;

            var hashCode = m_KeyCombinationSequence[0].GetHashCode();
            for (var index = 1; index < m_KeyCombinationSequence.Length; index++)
            {
                hashCode = Tuple.CombineHashCodes(hashCode, m_KeyCombinationSequence[index].GetHashCode());
            }
            return hashCode;
        }
    }
}
