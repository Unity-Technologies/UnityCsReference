// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class ImportedSampleCollection : ISerializationCallbackReceiver
    {
        [SerializeField]
        private string m_SanitizedPackageDisplayName;
        public string sanitizedPackageDisplayName => m_SanitizedPackageDisplayName;

        [SerializeField]
        private ImportedSample[] m_SerializedSamples;
        private Dictionary<string, ImportedSample> m_Samples;
        public IReadOnlyCollection<ImportedSample> samples => m_Samples.Values;

        public ImportedSample GetImportedSample(string sanitizedDisplayName) => m_Samples.GetValueOrDefault(sanitizedDisplayName ?? string.Empty);

        public ImportedSampleCollection(string packageDisplayName, Dictionary<string, ImportedSample> samples)
        {
            m_SanitizedPackageDisplayName = packageDisplayName;
            m_Samples = samples;
        }

        public bool IsEquivalent(ImportedSampleCollection other)
        {
            if (m_Samples.Count != other.m_Samples.Count || sanitizedPackageDisplayName != other.sanitizedPackageDisplayName)
                return false;

            foreach (var sample in m_Samples.Values)
            {
                var otherSample = other.GetImportedSample(sample.sanitizedDisplayName);
                if (otherSample == null || !sample.IsEquivalent(otherSample))
                    return false;
            }
            return true;
        }

        public void OnBeforeSerialize()
        {
            m_SerializedSamples = new ImportedSample[m_Samples.Count];
            m_Samples.Values.CopyTo(m_SerializedSamples, 0);
        }

        public void OnAfterDeserialize()
        {
            m_Samples = new Dictionary<string, ImportedSample>();
            foreach (var value in m_SerializedSamples)
                m_Samples[value.sanitizedDisplayName] = value;
        }
    }
}
