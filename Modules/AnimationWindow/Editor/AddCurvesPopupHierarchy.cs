// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;
using TreeViewController = UnityEditor.IMGUI.Controls.TreeViewController<int>;
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
using TreeViewState = UnityEditor.IMGUI.Controls.TreeViewState<int>;

namespace UnityEditorInternal
{
    internal class AddCurvesPopupHierarchy
    {
        private static readonly GUIContent s_AllPropertiesAddedContent = EditorGUIUtility.TrTextContent("All animatable properties have been added");
        private static readonly string s_NoResultsFoundString = L10n.Tr("No results found for \"{0}\"");

        private TreeViewController m_TreeView;
        private TreeViewState m_TreeViewState;
        private AddCurvesPopupHierarchyDataSource m_TreeViewDataSource;
        private GUIContent m_NoResultsFoundContent;

        private float m_ContentWidth = 0f;

        public string searchString
        {
            get => m_TreeView.searchString;
            set => m_TreeView.searchString = value;
        }

        public float GetContentWidth()
        {
            return m_ContentWidth;
        }

        public void OnGUI(Rect position, EditorWindow owner)
        {
            m_TreeView.SetTotalRect(position);
            m_TreeView.OnEvent();
            m_TreeView.OnGUI(position, GUIUtility.GetControlID(FocusType.Keyboard));

            if (m_TreeView.data.rowCount == 0)
            {
                GUIContent label;
                if (string.IsNullOrEmpty(searchString))
                {
                    label = s_AllPropertiesAddedContent;
                }
                else
                {
                    m_NoResultsFoundContent ??= new GUIContent(string.Format(s_NoResultsFoundString, searchString));
                    label = m_NoResultsFoundContent;
                }

                GUI.Label(position, label, EditorStyles.centeredGreyMiniLabel);
            }
        }

        public void InitIfNeeded(EditorWindow owner, Rect rect)
        {
            if (m_TreeViewState == null)
                m_TreeViewState = new TreeViewState();
            else
                return;

            m_TreeView = new TreeViewController(owner, m_TreeViewState);
            m_TreeView.searchChanged += _ => m_NoResultsFoundContent = null;

            m_TreeView.deselectOnUnhandledMouseDown = true;
            m_TreeView.showParentsInSearchResults = true;

            m_TreeViewDataSource = new AddCurvesPopupHierarchyDataSource(m_TreeView);
            AddCurvesPopupHierarchyGUI gui = new AddCurvesPopupHierarchyGUI(m_TreeView, owner);

            m_TreeView.Init(rect,
                m_TreeViewDataSource,
                gui,
                null
            );

            m_TreeViewDataSource.UpdateData();

            m_ContentWidth = gui.GetContentWidth();
        }

        internal virtual bool IsRenamingNodeAllowed(TreeViewItem node)
        {
            return false;
        }
    }
}
