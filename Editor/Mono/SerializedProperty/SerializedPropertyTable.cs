// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor.IMGUI.Controls;
using UnityEngine.Profiling;

namespace UnityEditor
{
    internal class SerializedPropertyTable
    {
        static class Styles
        {
            public static readonly GUIStyle DragHandle = "RL DragHandle";
        }

        internal delegate SerializedPropertyTreeView.Column[] HeaderDelegate(out string[] propNames);

        SerializedPropertyDataStore.GatherDelegate  m_GatherDelegate;
        HeaderDelegate                              m_HeaderDelegate;
        bool                                        m_Initialized;
        TreeViewState                               m_TreeViewState;
        MultiColumnHeaderState                      m_MultiColumnHeaderState;
        SerializedPropertyTreeView                  m_TreeView;
        SerializedPropertyDataStore                 m_DataStore;
        float                                       m_ColumnHeaderHeight;
        float                                       m_TableHeight = 200;
        string                                      m_SerializationUID;
        static readonly string                      s_TableHeight = "_TableHeight";
        bool                                        m_DragHandleEnabled = false;
        readonly float                              m_FilterHeight = 20;
        readonly float                              m_DragHeight = 20;
        readonly float                              m_DragWidth = 32;

        public bool dragHandleEnabled { get { return m_DragHandleEnabled; } set { m_DragHandleEnabled = value; } }

        public SerializedPropertyTable(string serializationUID, SerializedPropertyDataStore.GatherDelegate gatherDelegate, HeaderDelegate headerDelegate)
        {
            m_SerializationUID = serializationUID;
            m_GatherDelegate = gatherDelegate;
            m_HeaderDelegate = headerDelegate;

            OnEnable();
        }

        void InitIfNeeded()
        {
            if (m_Initialized)
                return;

            if (m_TreeViewState == null)
                m_TreeViewState = new TreeViewState();


            if (m_MultiColumnHeaderState == null)
            {
                string[] propNames;

                m_MultiColumnHeaderState = new MultiColumnHeaderState(m_HeaderDelegate(out propNames));
                m_DataStore = new SerializedPropertyDataStore(propNames, m_GatherDelegate);
            }

            var header = new MultiColumnHeader(m_MultiColumnHeaderState);
            m_ColumnHeaderHeight = header.height;
            m_TreeView = new SerializedPropertyTreeView(m_TreeViewState, header, m_DataStore);

            m_TreeView.DeserializeState(m_SerializationUID);
            m_TreeView.Reload();

            m_Initialized = true;
        }

        float GetMinHeight()
        {
            float rowHeight = EditorGUIUtility.singleLineHeight;
            float fixedHeight = m_FilterHeight + m_ColumnHeaderHeight + rowHeight + m_DragHeight;

            return fixedHeight + rowHeight * 3; // three rows
        }

        public void OnInspectorUpdate()
        {
            if (m_DataStore != null && m_DataStore.Repopulate() && m_TreeView != null)
            {
                m_TreeView.FullReload();
            }
            else if (m_TreeView != null && m_TreeView.Update())
            {
                m_TreeView.Repaint();
            }
        }

        public void OnHierarchyChange()
        {
            if (m_DataStore != null && m_DataStore.Repopulate() && m_TreeView != null)
                m_TreeView.FullReload();
        }

        public void OnSelectionChange()
        {
            OnSelectionChange(Selection.instanceIDs);
        }

        public void OnSelectionChange(int[] instanceIDs)
        {
            if (m_TreeView != null)
            {
                m_TreeView.SetSelection(instanceIDs);
            }
        }

        public void OnGUI()
        {
            Profiler.BeginSample("SerializedPropertyTable.OnGUI");
            InitIfNeeded();

            Rect r;

            if (dragHandleEnabled)
                r = GUILayoutUtility.GetRect(0, 10000, m_TableHeight, m_TableHeight);
            else
                r = GUILayoutUtility.GetRect(0, float.MaxValue, 0, float.MaxValue);

            if (Event.current.type == EventType.Layout)
                return;

            float windowWidth = r.width;
            float tableHeight = r.height - m_FilterHeight - (dragHandleEnabled ? m_DragHeight : 0);
            // filter rect
            float h = r.height;
            r.height = m_FilterHeight;
            Rect filterRect = r;
            // table rect
            r.height = tableHeight;
            r.y += m_FilterHeight;
            Rect tableRect = r;

            // table
            Profiler.BeginSample("TreeView.OnGUI");
            m_TreeView.OnGUI(tableRect);
            Profiler.EndSample();

            if (dragHandleEnabled)
            {
                // separator rect
                r.y += tableHeight + 1;
                r.height = 1;
                Rect sepRect = r;
                // drag rect
                r.height = 10;
                r.y += 10;
                r.x += (r.width - m_DragWidth) * 0.5f;
                r.width = m_DragWidth;

                m_TableHeight = EditorGUI.HeightResizer(r, m_TableHeight, GetMinHeight(), float.MaxValue);

                // separator (TODO: this doesn't quite work in the case where the vertical bar is visible and the last column overlaps it)
                // once trunk is merged again the treeview will provide a routine to draw the separator for us
                if (m_MultiColumnHeaderState.widthOfAllVisibleColumns <= windowWidth)
                {
                    Rect uv = new Rect(0, 1f, 1, 1f - 1f / EditorStyles.inspectorTitlebar.normal.background.height);
                    GUI.DrawTextureWithTexCoords(sepRect, EditorStyles.inspectorTitlebar.normal.background, uv);
                }

                if (Event.current.type == EventType.Repaint)
                    Styles.DragHandle.Draw(r, false, false, false, false);
            }

            m_TreeView.OnFilterGUI(filterRect);

            if (m_TreeView.IsFilteredDirty())
                m_TreeView.Reload();

            Profiler.EndSample();
        }

        public void OnEnable()
        {
            m_TableHeight = SessionState.GetFloat(m_SerializationUID + s_TableHeight, 200);
        }

        public void OnDisable()
        {
            if (m_TreeView != null)
                m_TreeView.SerializeState(m_SerializationUID);

            SessionState.SetFloat(m_SerializationUID + s_TableHeight, m_TableHeight);
        }
    }
}
