// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityEditor.Search
{
    [CustomEditor(typeof(SearchDatabase))]
    class SearchDatabaseEditor : Editor
    {
        private SearchDatabase m_DB;
        private SerializedProperty m_Settings;
        private GUIContent m_IndexTitleLabel;

        [SerializeField] private bool m_KeywordsFoldout;
        [SerializeField] private bool m_DocumentsFoldout;
        [SerializeField] private bool m_DependenciesFoldout;

        private GUIContent title
        {
            get
            {
                if (m_IndexTitleLabel == null)
                    m_IndexTitleLabel = new GUIContent();

                var time = System.DateTime.FromBinary(m_DB.index?.timestamp ?? 0);
                if (m_DB.index == null || !m_DB.index.IsReady())
                    m_IndexTitleLabel.text = $"Building {m_DB.index?.name ?? m_DB.name}...";
                else
                    m_IndexTitleLabel.text = $"{m_DB.index?.name ?? m_DB.name} ({Utils.FormatBytes(m_DB.bytes?.Length ?? 0)}, {m_DB.index?.indexCount ?? 0} indexes, {time})";

                return m_IndexTitleLabel;
            }
        }

        internal void OnEnable()
        {
            m_DB = (SearchDatabase)target;
            m_Settings = serializedObject.FindProperty("settings");
            m_Settings.isExpanded = true;
        }

        protected override void OnHeaderGUI()
        {
            // Do not draw any header
        }

        public override void OnInspectorGUI()
        {
            if (!m_DB || m_DB.index == null)
                return;

            EditorGUILayout.PropertyField(m_Settings, title, true);

            var documentTitle = "Documents";
            if (m_DB.index is SceneIndexer objectIndexer)
            {
                var dependencies = objectIndexer.GetDependencies();
                m_DependenciesFoldout = EditorGUILayout.Foldout(m_DependenciesFoldout, $"Documents (Count={dependencies.Count})", true);
                if (m_DependenciesFoldout)
                {
                    foreach (var d in dependencies)
                        EditorGUILayout.LabelField(d);
                }

                documentTitle = "Objects";
            }

            m_DocumentsFoldout = EditorGUILayout.Foldout(m_DocumentsFoldout, $"{documentTitle} (Count={m_DB.index.documentCount})", true);
            if (m_DocumentsFoldout)
            {
                foreach (var documentEntry in m_DB.index.GetDocuments(true).OrderBy(p => p.id))
                {
                    m_DB.index.TryGetHash(documentEntry.id, out var hash);
                    EditorGUILayout.LabelField($"{documentEntry.id} ({hash})");
                }
            }

            m_KeywordsFoldout = EditorGUILayout.Foldout(m_KeywordsFoldout, $"Keywords (Count={m_DB.index.keywordCount})", true);
            if (m_KeywordsFoldout)
            {
                foreach (var t in m_DB.index.GetKeywords().OrderBy(p => p))
                    EditorGUILayout.LabelField(t);
            }
        }

        protected override bool ShouldHideOpenButton()
        {
            return true;
        }

        public override bool HasPreviewGUI()
        {
            return false;
        }

        public override bool RequiresConstantRepaint()
        {
            return false;
        }

        public override bool UseDefaultMargins()
        {
            return true;
        }
    }
}
