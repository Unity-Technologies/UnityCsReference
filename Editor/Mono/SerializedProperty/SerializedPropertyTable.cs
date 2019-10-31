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
        internal delegate SerializedPropertyTreeView.Column[] HeaderDelegate();

        SerializedPropertyDataStore.GatherDelegate  m_GatherDelegate;
        HeaderDelegate                              m_HeaderDelegate;
        bool                                        m_Initialized;
        TreeViewState                               m_TreeViewState;
        MultiColumnHeaderState                      m_MultiColumnHeaderState;
        SerializedPropertyTreeView                  m_TreeView;
        SerializedPropertyDataStore                 m_DataStore;
        float                                       m_ColumnHeaderHeight;
        string                                      m_SerializationUID;
        readonly float                              m_FilterHeight = 20;

        public SerializedPropertyTable(string serializationUID, SerializedPropertyDataStore.GatherDelegate gatherDelegate, HeaderDelegate headerDelegate)
        {
            m_SerializationUID = serializationUID;
            m_GatherDelegate = gatherDelegate;
            m_HeaderDelegate = headerDelegate;
        }

        void InitIfNeeded()
        {
            if (m_Initialized)
                return;

            if (m_TreeViewState == null)
                m_TreeViewState = new TreeViewState();


            if (m_MultiColumnHeaderState == null)
            {
                SerializedPropertyTreeView.Column[] columns = m_HeaderDelegate();

                string[] propNames = GetPropertyNames(columns);

                m_MultiColumnHeaderState = new MultiColumnHeaderState(columns);
                m_DataStore = new SerializedPropertyDataStore(propNames, m_GatherDelegate);
            }

            var header = new MultiColumnHeader(m_MultiColumnHeaderState);
            m_ColumnHeaderHeight = header.height;
            m_TreeView = new SerializedPropertyTreeView(m_TreeViewState, header, m_DataStore);

            m_TreeView.DeserializeState(m_SerializationUID);
            m_TreeView.Reload();

            m_Initialized = true;
        }

        string[] GetPropertyNames(SerializedPropertyTreeView.Column[] columns)
        {
            string[] propNames = new string[columns.Length];

            for (int i = 0; i < columns.Length; i++)
                propNames[i] = columns[i].propertyName;

            return propNames;
        }

        public void OnInspectorUpdate()
        {
            if (m_TreeView != null)
            {
                m_TreeView.Update();
            }
        }

        public void OnHierarchyChange()
        {
            if (m_TreeView != null)
            {
                m_TreeView.Update();
            }
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

            Rect r = GUILayoutUtility.GetRect(0, float.MaxValue, 0, float.MaxValue);

            if (Event.current.type == EventType.Layout)
            {
                Profiler.EndSample();
                return;
            }

            float tableHeight = r.height - m_FilterHeight;
            // filter rect
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

            m_TreeView.OnFilterGUI(filterRect);

            if (m_TreeView.IsFilteredDirty())
                m_TreeView.Reload();

            Profiler.EndSample();
        }

        public void OnDisable()
        {
            if (m_TreeView != null)
                m_TreeView.SerializeState(m_SerializationUID);
        }
    }
}
