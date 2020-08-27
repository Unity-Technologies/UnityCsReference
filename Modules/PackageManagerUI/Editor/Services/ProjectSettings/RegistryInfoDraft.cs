// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal class RegistryInfoDraft
    {
        [SerializeField]
        private string m_ErrorMessage;
        public string errorMessage
        {
            get => m_ErrorMessage;
            set => m_ErrorMessage = value;
        }

        [SerializeField]
        private RegistryInfo m_Original;
        public RegistryInfo original => string.IsNullOrEmpty(m_Original?.id) ? null : m_Original;

        public void SetOriginalRegistryInfo(RegistryInfo registryInfo)
        {
            m_Original = registryInfo;
            RevertChanges();
        }

        [SerializeField]
        private bool m_Modified;
        [SerializeField]
        private string m_Name;
        public string name
        {
            get => m_Name;
            set
            {
                if (m_Name == value)
                    return;
                m_Name = value ?? string.Empty;
                m_Modified = true;
            }
        }

        [SerializeField]
        private string m_Url;
        public string url
        {
            get => m_Url;
            set
            {
                if (m_Url == value)
                    return;
                m_Url = value ?? string.Empty;
                m_Modified = true;
            }
        }

        [SerializeField]
        private List<string> m_Scopes;
        public ReadOnlyCollection<string> scopes => m_Scopes.AsReadOnly();
        public ReadOnlyCollection<string> sanitizedScopes => m_Scopes.Where(scope => !string.IsNullOrEmpty(scope)).ToList().AsReadOnly();
        [SerializeField]
        private int m_SelectedScopeIndex;
        public int selectedScopeIndex
        {
            get => m_SelectedScopeIndex;
            set => m_SelectedScopeIndex = Math.Min(value, m_Scopes.Count - 1);
        }

        public void SetScopes(IEnumerable<string> scopes)
        {
            m_Scopes = scopes?.Select(s => s ?? string.Empty).ToList() ?? new List<string>();
            if (m_Scopes.Count == 0)
                m_Scopes.Add(string.Empty);
            m_SelectedScopeIndex = Math.Min(m_SelectedScopeIndex, m_Scopes.Count - 1);
            m_Modified = true;
        }

        public bool hasUnsavedChanges
        {
            get
            {
                if (!m_Modified)
                    return false;
                if (original == null)
                    return !string.IsNullOrEmpty(m_Name) || !string.IsNullOrEmpty(m_Url) || !(m_Scopes.Count == 0 || m_Scopes.All(string.IsNullOrEmpty));

                var comparer = new UpmRegistryClient.RegistryInfoComparer();

                return !comparer.Equals(original, new RegistryInfo(original.id, m_Name, m_Url, m_Scopes.ToArray(), original.isDefault, original.capabilities));
            }
        }

        public bool isUrlOrScopesUpdated
        {
            get => m_Modified && original != null &&
            !new UpmRegistryClient.RegistryInfoComparer().Equals(original, new RegistryInfo(original.id, original.name, m_Url, m_Scopes.ToArray(), original.isDefault, original.capabilities));
        }

        public RegistryInfoDraft()
        {
            m_Modified = false;
            m_Name = string.Empty;
            m_Url = string.Empty;
            m_Scopes = new List<string>() { "" };
            m_SelectedScopeIndex = 0;
        }

        public void RevertChanges()
        {
            var original = this.original;
            m_Modified = false;
            m_Name = original?.name ?? string.Empty;
            m_Url = original?.url ?? string.Empty;
            m_Scopes.Clear();
            if (original?.scopes?.Length > 0)
                m_Scopes.AddRange(original.scopes);
            if (m_Scopes.Count == 0)
                m_Scopes.Add(string.Empty);
            m_SelectedScopeIndex = 0;
            m_ErrorMessage = string.Empty;
        }

        public bool Validate()
        {
            m_ErrorMessage = string.Empty;
            if (string.IsNullOrEmpty(m_Name?.Trim()))
                AddErrorMessage(L10n.Tr("Registry name cannot be null, empty or whitespace"));
            if (sanitizedScopes.Count == 0)
                AddErrorMessage(L10n.Tr("Scope(s) cannot be empty"));
            if (!(Uri.TryCreate(m_Url, UriKind.Absolute, out var uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps)))
                AddErrorMessage(L10n.Tr("\"URL\" must be a valid uri with a scheme matching the http|https pattern"));

            return string.IsNullOrEmpty(m_ErrorMessage);
        }

        private void AddErrorMessage(string message)
        {
            if (string.IsNullOrEmpty(m_ErrorMessage))
                m_ErrorMessage = message;
            else
                m_ErrorMessage += "\n" + message;
        }
    }
}
