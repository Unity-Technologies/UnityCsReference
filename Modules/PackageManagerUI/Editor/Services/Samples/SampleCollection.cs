// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class SampleCollection : IReadOnlyList<Sample>
    {
        public string packageUniqueId { get; private set; }

        [SerializeField]
        private Sample[] m_Samples;

        public SampleCollection(string packageUniqueId, Sample[] samples)
        {
            this.packageUniqueId = packageUniqueId;
            m_Samples = samples ?? Array.Empty<Sample>();
        }

        public bool IsEquivalent(SampleCollection other)
        {
            if (m_Samples.Length != other.m_Samples.Length || packageUniqueId != other.packageUniqueId)
                return false;

            for (var i = 0; i < m_Samples.Length; i++)
                if (!m_Samples[i].IsEquivalent(other.m_Samples[i]))
                    return false;

            return true;
        }

        public int Count => m_Samples.Length;

        public IEnumerator<Sample> GetEnumerator()
        {
            return ((IEnumerable<Sample>)m_Samples).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public Sample this[int index] => m_Samples[index];
    }
}
