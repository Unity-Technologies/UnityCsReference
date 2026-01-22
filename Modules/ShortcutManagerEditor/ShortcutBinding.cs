// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.ShortcutManagement
{
    public struct ShortcutBinding : IEquatable<ShortcutBinding>
    {
        public static ShortcutBinding empty { get; } = new ShortcutBinding();

        readonly KeyCombination[] m_KeyCombinationSequence;

        public IEnumerable<KeyCombination> keyCombinationSequence => m_KeyCombinationSequence ?? Array.Empty<KeyCombination>();

        internal ShortcutBinding(IEnumerable<KeyCombination> keyCombinationSequence)
        {
            if (keyCombinationSequence == null)
                throw new ArgumentNullException(nameof(keyCombinationSequence));

#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
#pragma warning disable RS0031 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            m_KeyCombinationSequence = keyCombinationSequence.Any() ? keyCombinationSequence.ToArray() : null;
#pragma warning restore RS0030
#pragma warning restore RS0031
        }

        public ShortcutBinding(KeyCombination keyCombination)
            : this(new[] { keyCombination })
        {
        }

        public override string ToString() => KeyCombination.SequenceToString(keyCombinationSequence);

        public bool Equals(ShortcutBinding other)
        {
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return keyCombinationSequence.SequenceEqual(other.keyCombinationSequence);
#pragma warning restore RS0030
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
