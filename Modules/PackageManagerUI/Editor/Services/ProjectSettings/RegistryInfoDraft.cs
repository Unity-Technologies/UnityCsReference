// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class RegistryInfoDraft : ISerializationCallbackReceiver
    {
        [SerializeField]
        private bool m_Modified;

        public string name
        {
            get => m_UserModifications.name;
            set
            {
                m_UserModifications.name = value;
                m_Modified = true;
            }
        }

        public string url
        {
            get => m_UserModifications.url;
            set
            {
                m_UserModifications.url = value;
                m_Modified = true;
            }
        }

        public int selectedScopeIndex
        {
            get => m_UserModifications.selectedScopeIndex;
            set => m_UserModifications.selectedScopeIndex = value;
        }

        public ReadOnlyCollection<string> scopes => m_UserModifications.scopes;
        public ReadOnlyCollection<string> sanitizedScopes => m_UserModifications.sanitizedScopes;

        [SerializeField]
        private string m_ErrorMessage;
        public string errorMessage
        {
            get => m_ErrorMessage;
            set => m_ErrorMessage = value;
        }

        [SerializeField]
        private int m_UserModificationsInstanceId;
        [NonSerialized]
        private RegistryInfoDraftData m_UserModifications;

        [SerializeField]
        private int m_OriginalInstanceId;
        [NonSerialized]
        private RegistryInfoOriginalData m_Original;
        public RegistryInfoOriginalData original => string.IsNullOrEmpty(m_Original?.id) ? null : m_Original;

        public void OnBeforeSerialize()
        {
            if (m_UserModifications != null)
                m_UserModificationsInstanceId = m_UserModifications.GetInstanceID();

            if (m_Original != null)
                m_OriginalInstanceId = m_Original.GetInstanceID();
        }

        // do nothing
        public void OnAfterDeserialize() {}

        public bool IsReady()
        {
            // check if the scriptableObject holding user input data is ready
            return m_UserModifications;
        }

        public void OnEnable()
        {
            m_UserModifications = ScriptableObject.FindObjectFromInstanceID(m_UserModificationsInstanceId) as RegistryInfoDraftData ??
                ScriptableObject.CreateInstance<RegistryInfoDraftData>();
            m_UserModifications.hideFlags = HideFlags.DontSave;

            m_Original = ScriptableObject.FindObjectFromInstanceID(m_OriginalInstanceId) as RegistryInfoOriginalData ??
                ScriptableObject.CreateInstance<RegistryInfoOriginalData>();
            m_Original.hideFlags = HideFlags.DontSave;
        }

        public void SetModifiedAfterUndo()
        {
            m_Modified = true;
        }

        public void RegisterWithOriginalOnUndo(string undoEvent)
        {
            Undo.BeginAtomicUndoGroup();
            Undo.RegisterCompleteObjectUndo(m_UserModifications, undoEvent);
            Undo.RegisterCompleteObjectUndo(m_Original, undoEvent);
            Undo.EndAtomicUndoGroup();
        }

        public void RegisterOnUndo(string undoEvent)
        {
            Undo.RegisterCompleteObjectUndo(m_UserModifications, undoEvent);
        }

        public void SetOriginalRegistryInfo(RegistryInfo registryInfo, bool isUndo = false)
        {
            if (registryInfo != null)
            {
                m_Original = ScriptableObject.FindObjectFromInstanceID(m_OriginalInstanceId) as RegistryInfoOriginalData ??
                    ScriptableObject.CreateInstance<RegistryInfoOriginalData>();
                m_Original.hideFlags = HideFlags.DontSave;
                m_Original.SetRegistryInfo(registryInfo);
            }
            else
            {
                m_Original.SetRegistryInfo(null);
            }

            if (!isUndo)
                RevertChanges();
        }

        public void SetScopes(IEnumerable<string> scopes)
        {
            m_UserModifications.SetScopes(scopes);
            m_Modified = true;
        }

        public bool hasUnsavedChanges
        {
            get
            {
                if (!m_Modified)
                    return false;
                if (original == null)
                    return !string.IsNullOrEmpty(m_UserModifications.name) || !string.IsNullOrEmpty(m_UserModifications.url) || !(m_UserModifications.scopes.Count == 0 || m_UserModifications.scopes.All(string.IsNullOrEmpty));

                return !original.IsEqualTo(new RegistryInfo(original.id, m_UserModifications.name, m_UserModifications.url, m_UserModifications.scopes.ToArray(), original.isDefault, original.capabilities, original.configSource));
            }
        }

        public bool isUrlOrScopesUpdated
        {
            get => m_Modified && original != null &&
            !original.IsEqualTo(new RegistryInfo(original.id, original.name, m_UserModifications.url, m_UserModifications.scopes.ToArray(), original.isDefault, original.capabilities, original.configSource));
        }

        public void RevertChanges()
        {
            m_UserModifications.RevertChanges(original);
            m_Modified = false;
            m_ErrorMessage = string.Empty;
        }

        public bool Validate()
        {
            m_ErrorMessage = string.Empty;
            if (string.IsNullOrEmpty(m_UserModifications.name?.Trim()))
                AddErrorMessage(L10n.Tr("Registry name cannot be null, empty or whitespace"));
            if (m_UserModifications.sanitizedScopes.Count == 0)
                AddErrorMessage(L10n.Tr("Scope(s) cannot be empty"));
            if (!(Uri.TryCreate(m_UserModifications.url, UriKind.Absolute, out var uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps)))
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
