// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace UnityEditorInternal
{
    [System.Serializable]
    internal class AnimationWindowKeySelection : ScriptableObject, ISerializationCallbackReceiver
    {
        private HashSet<int> m_SelectedKeyHashes;
        [SerializeField] private List<int> m_SelectedKeyHashesSerialized;

        public HashSet<int> selectedKeyHashes
        {
            get { return m_SelectedKeyHashes ?? (m_SelectedKeyHashes = new HashSet<int>()); }
            set { m_SelectedKeyHashes = value; }
        }

        public void SaveSelection(string undoLabel)
        {
            Undo.RegisterCompleteObjectUndo(this, undoLabel);
        }

        public void OnBeforeSerialize()
        {
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            m_SelectedKeyHashesSerialized = m_SelectedKeyHashes.ToList();
#pragma warning restore UA2001
        }

        public void OnAfterDeserialize()
        {
            m_SelectedKeyHashes = new HashSet<int>(m_SelectedKeyHashesSerialized);
        }
    }
}
