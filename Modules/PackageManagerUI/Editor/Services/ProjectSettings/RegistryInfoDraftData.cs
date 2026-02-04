// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class RegistryInfoDraftData : ScriptableObject
    {
        [SerializeField]
        protected string m_RegistryName;
        public new string name
        {
            get => m_RegistryName?.Trim() ?? string.Empty;
            set
            {
                if (m_RegistryName == value)
                    return;
                m_RegistryName = value ?? string.Empty;
            }
        }

        [SerializeField]
        protected string m_Url;
        public string url
        {
            get => m_Url?.Trim() ?? string.Empty;
            set
            {
                if (m_Url == value)
                    return;
                m_Url = value ?? string.Empty;
            }
        }

        [SerializeField]
        protected List<string> m_Scopes;
        public IReadOnlyCollection<string> scopes => m_Scopes;
        [SerializeField]
        private int m_SelectedScopeIndex;
        public int selectedScopeIndex
        {
            get => m_SelectedScopeIndex;
            set => m_SelectedScopeIndex = Math.Min(value, m_Scopes.Count - 1);
        }

        public RegistryInfoDraftData()
        {
            m_RegistryName = string.Empty;
            m_Url = string.Empty;
            m_Scopes = new List<string> { string.Empty };
            m_SelectedScopeIndex = 0;
        }

        public void RevertChanges(RegistryInfoOriginalData original)
        {
            m_RegistryName = original?.name ?? string.Empty;
            m_Url = original?.url ?? string.Empty;
            m_Scopes.Clear();
            if (original?.scopes?.Length > 0)
                m_Scopes.AddRange(original.scopes);
            if (m_Scopes.Count == 0)
                m_Scopes.Add(string.Empty);
            m_SelectedScopeIndex = 0;
        }

        public string[] GetSanitizedScopes()
        {
            return m_Scopes.SelectNonEmpty(s => s.Trim()).ToNewArray(m_Scopes.Count);
        }

        public void SetScopes(IEnumerable<string> newScopes)
        {
            m_Scopes.Clear();
            foreach (var scope in newScopes ?? Array.Empty<string>())
                m_Scopes.Add(scope ?? string.Empty);
            if (m_Scopes.Count == 0)
                m_Scopes.Add(string.Empty);
            m_SelectedScopeIndex = Math.Min(m_SelectedScopeIndex, m_Scopes.Count - 1);
        }
    }
}
