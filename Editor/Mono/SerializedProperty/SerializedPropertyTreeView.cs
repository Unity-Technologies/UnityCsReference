// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.IMGUI.Controls;
using UnityEngine.Profiling;

namespace UnityEditor
{
    internal class SerializedPropertyDataStore
    {
        internal class Data
        {
            UnityEngine.Object m_Object;
            SerializedObject m_SerializedObject;
            SerializedProperty[] m_Properties;

            public Data(UnityEngine.Object obj, string[] props)
            {
                m_Object = obj;
                m_SerializedObject = new SerializedObject(obj);
                m_Properties = new SerializedProperty[props.Length];

                for (int i = 0; i < props.Length; i++)
                    m_Properties[i] = m_SerializedObject.FindProperty(props[i]);
            }

            public void Dispose()
            {
                foreach (var prop in m_Properties)
                {
                    if (prop != null)
                        prop.Dispose();
                }

                m_SerializedObject.Dispose();

                m_Object            = null;
                m_SerializedObject  = null;
                m_Properties        = null;
            }

            public string name { get { return m_Object != null ? m_Object.name : string.Empty; } }
            public SerializedObject serializedObject { get { return m_SerializedObject; } }
            public SerializedProperty[] properties { get { return m_Properties; } }

            public int objectId
            {
                get
                {
                    if (!m_Object)
                        return 0;

                    Component comp = m_Object as Component;
                    return comp != null ? comp.gameObject.GetInstanceID() : m_Object.GetInstanceID();
                }
            }

            public bool activeInHierarchy
            {
                get
                {
                    if (m_Object != null && m_Object is Component)
                    {
                        return ((Component)m_Object).gameObject.activeInHierarchy;
                    }

                    return false;
                }
            }

            public bool Update() { return m_Object != null ? m_SerializedObject.UpdateIfRequiredOrScript() : false; }
            public void Store() { if (m_Object != null) {  m_SerializedObject.ApplyModifiedProperties(); } }
        }

        // LightTreeDataStore members
        internal delegate UnityEngine.Object[] GatherDelegate();
        UnityEngine.Object[] m_Objects; // only used to keep track of changes
        bool[] m_ActiveObjects;
        Data[] m_Elements;
        string[] m_PropNames;
        GatherDelegate m_GatherDel;

        public Data[] GetElements() { return m_Elements; }

        public SerializedPropertyDataStore(string[] propNames, GatherDelegate gatherDel)
        {
            m_PropNames = propNames;
            m_GatherDel = gatherDel;

            bool needsReload;
            Repopulate(out needsReload);
        }

        ~SerializedPropertyDataStore()
        {
            Clear();
        }

        public bool Repopulate(out bool needsReload)
        {
            // this is done due to optimization reasons. we want to know if any major changes has happened to the data
            // without having to refetch it in another place and loop through it there
            needsReload = false;

            Profiler.BeginSample("SerializedPropertyDataStore.Repopulate.GatherDelegate");
            UnityEngine.Object[] objs = m_GatherDel();
            Profiler.EndSample();

            if (m_Objects != null)
            {
                // If nothing's changed -> bail
                if (objs.Length == m_Objects.Length && ArrayUtility.ArrayReferenceEquals(objs, m_Objects))
                {
                    // The list of objects is the same, but since some should now potentially be hidden, we need to request a reload
                    if (!ActiveObjectsEquals(objs, m_ActiveObjects))
                        needsReload = true;

                    return false;
                }

                Clear();
            }

            // Recreate data store
            m_Objects = objs;
            m_Elements = new Data[objs.Length];
            m_ActiveObjects = new bool[objs.Length];

            int elementIndex = 0;
            for (int i = 0; i < objs.Length; i++)
            {
                // we don't want to list hidden objects
                if (objs[i] == null || objs[i].hideFlags == HideFlags.HideAndDontSave || objs[i].hideFlags == HideFlags.HideInHierarchy)
                    continue;

                if (objs[i] is Component)
                {
                    m_ActiveObjects[i] = ((Component)objs[i]).gameObject.activeInHierarchy;
                }

                m_Elements[elementIndex] = new Data(objs[i], m_PropNames);

                elementIndex++;
            }

            if (elementIndex < objs.Length)
                System.Array.Resize(ref m_Elements, elementIndex);

            needsReload = true;
            return true;
        }

        private void Clear()
        {
            // Clear current
            for (int i = 0; i < m_Elements.Length; i++)
                m_Elements[i].Dispose();

            m_Objects  = null;
            m_Elements = null;
            m_ActiveObjects = null;
        }

        private static bool ActiveObjectsEquals(Object[] objects, bool[] activeObjects)
        {
            if (activeObjects != null && objects.Length != activeObjects.Length)
                return true;

            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] != null && objects[i] is Component)
                {
                    if (((Component)objects[i]).gameObject.activeInHierarchy != activeObjects[i])
                        return false;
                }
            }
            return true;
        }
    }

    internal class SerializedPropertyTreeView : TreeView
    {
        internal static class Styles
        {
            public static readonly GUIStyle entryEven = "OL EntryBackEven";
            public static readonly GUIStyle entryOdd  = "OL EntryBackOdd";
            public static readonly string focusHelper = "SerializedPropertyTreeViewFocusHelper";
            public static readonly string serializeFilterSelection = "_FilterSelection";
            public static readonly string serializeFilterDisable   = "_FilterDisable";
            public static readonly string serializeFilterInvert    = "_FilterInvert";
            public static readonly string serializeTreeViewState    = "_TreeViewState";
            public static readonly string serializeColumnHeaderState = "_ColumnHeaderState";
            public static readonly string serializeFilter = "_Filter_";
            public static readonly GUIContent filterSelection = EditorGUIUtility.TrTextContent("Isolate Selection", "Limits the table contents to the active selection.");
            public static readonly GUIContent showInactiveObjects = EditorGUIUtility.TrTextContent("Show Inactive Objects", "Show objects that are not active in the hierarchy but contains the component.");
        }

        // this gets stuffed into the view and displayed on screen. It is a visible subset of the actual data
        internal class SerializedPropertyItem : TreeViewItem
        {
            SerializedPropertyDataStore.Data m_Data;

            public SerializedPropertyItem(int id, int depth, SerializedPropertyDataStore.Data ltd) : base(id, depth, ltd != null ? ltd.name : "root")
            {
                m_Data = ltd;
            }

            public SerializedPropertyDataStore.Data GetData() { return m_Data; }
        }

        internal class Column : MultiColumnHeaderState.Column
        {
            public delegate void DrawEntry(Rect r, SerializedProperty prop, SerializedProperty[] dependencies);
            public delegate int CompareEntry(SerializedProperty lhs, SerializedProperty rhs);
            public delegate void CopyDelegate(SerializedProperty target, SerializedProperty source);

            public string                               propertyName;
            public int[]                                dependencyIndices;
            public DrawEntry                            drawDelegate;
            public CompareEntry                         compareDelegate;
            public CopyDelegate                         copyDelegate;
            public SerializedPropertyFilters.IFilter    filter;

            public static void OverwriteSerializedFields(MultiColumnHeaderState source, MultiColumnHeaderState destination)
            {
                destination.visibleColumns = source.visibleColumns;
                destination.sortedColumns = source.sortedColumns;

                for (int i = 0; i < destination.columns.Length; ++i)
                {
                    destination.columns[i].sortedAscending = source.columns[i].sortedAscending;

                    var sourceColumn = source.columns[i] as Column;
                    var destColumn = destination.columns[i] as Column;

                    if ((sourceColumn == null || destColumn == null) || string.Equals(sourceColumn.propertyName, destColumn.propertyName))
                    {
                        // Only makes sense if the property is the same property name
                        destination.columns[i].width = source.columns[i].width;
                    }
                }
            }
        }

        private struct ColumnInternal
        {
            public SerializedProperty[] dependencyProps;    // helper array, must be the same size as dependencyIndices
        }

        internal class DefaultDelegates
        {
            public static readonly Column.DrawEntry DrawDefault = (Rect r, SerializedProperty prop, SerializedProperty[] dependencies) =>
            {
                Profiler.BeginSample("PropDrawDefault");
                EditorGUI.PropertyField(r, prop, GUIContent.none);
                Profiler.EndSample();
            };
            public static readonly Column.DrawEntry DrawCheckbox = (Rect r, SerializedProperty prop, SerializedProperty[] dependencies) =>
            {
                Profiler.BeginSample("PropDrawCheckbox");
                float off = System.Math.Max(0.0f, ((r.width / 2) - 8));
                r.x += off;
                r.width -= off;
                EditorGUI.PropertyField(r, prop, GUIContent.none);
                Profiler.EndSample();
            };
            public static readonly Column.DrawEntry DrawName = (Rect r, SerializedProperty prop, SerializedProperty[] dependencies) => {};

            public static readonly Column.CompareEntry CompareFloat = (SerializedProperty lhs, SerializedProperty rhs) =>
            {
                return lhs.floatValue.CompareTo(rhs.floatValue);
            };
            public static readonly Column.CompareEntry CompareCheckbox = (SerializedProperty lhs, SerializedProperty rhs) =>
            {
                return lhs.boolValue.CompareTo(rhs.boolValue);
            };
            public static readonly Column.CompareEntry CompareEnum = (SerializedProperty lhs, SerializedProperty rhs) =>
            {
                return lhs.enumValueIndex.CompareTo(rhs.enumValueIndex);
            };
            public static readonly Column.CompareEntry CompareInt = (SerializedProperty lhs, SerializedProperty rhs) =>
            {
                return lhs.intValue.CompareTo(rhs.intValue);
            };
            public static readonly Column.CompareEntry CompareColor = (SerializedProperty lhs, SerializedProperty rhs) =>
            {
                float lh, ls, lv, rh, rs, rv;
                Color.RGBToHSV(lhs.colorValue, out lh, out ls, out lv);
                Color.RGBToHSV(rhs.colorValue, out rh, out rs, out rv);
                return lh.CompareTo(rh);
            };
            public static readonly Column.CompareEntry CompareName = (SerializedProperty lhs, SerializedProperty rhs) =>
            {
                return 0;
            };
            public static readonly Column.CopyDelegate CopyDefault = (SerializedProperty target, SerializedProperty source) =>
            {
                target.serializedObject.CopyFromSerializedProperty(source);
            };
        }
        // reference to the data store (not owned by this class)
        SerializedPropertyDataStore m_DataStore;
        ColumnInternal[]            m_ColumnsInternal;
        List<TreeViewItem>          m_Items;
        int                         m_ChangedId;
        bool                        m_bFilterSelection;
        bool                        m_ShowInactiveObjects;
        int[]                       m_SelectionFilter;

        public SerializedPropertyTreeView(TreeViewState state, MultiColumnHeader multicolumnHeader, SerializedPropertyDataStore dataStore) : base(state, multicolumnHeader)
        {
            m_DataStore = dataStore;
            // initialize internal data for the columns
            int colcnt = multiColumnHeader.state.columns.Length;
            m_ColumnsInternal = new ColumnInternal[colcnt];
            for (int i = 0; i < colcnt; i++)
            {
                Column c = Col(i);
                if (c.propertyName != null)
                    m_ColumnsInternal[i].dependencyProps = new SerializedProperty[c.propertyName.Length];
            }

            multiColumnHeader.sortingChanged += OnSortingChanged;
            multiColumnHeader.visibleColumnsChanged += OnVisibleColumnChanged;

            showAlternatingRowBackgrounds = true;
            showBorder = true;
            rowHeight = EditorGUIUtility.singleLineHeight;
        }

        public void SerializeState(string uid)
        {
            SessionState.SetBool(uid + Styles.serializeFilterSelection, m_bFilterSelection);
            SessionState.SetBool(uid + Styles.showInactiveObjects, m_ShowInactiveObjects);

            for (int i = 0; i < multiColumnHeader.state.columns.Length; i++)
            {
                var filter = Col(i).filter;

                if (filter == null)
                    continue;

                string filterState = filter.SerializeState();

                if (string.IsNullOrEmpty(filterState))
                    continue;

                SessionState.SetString(uid + Styles.serializeFilter + i, filterState);
            }

            SessionState.SetString(uid + Styles.serializeTreeViewState, JsonUtility.ToJson(state));
            EditorPrefs.SetString(uid + Styles.serializeColumnHeaderState, JsonUtility.ToJson(multiColumnHeader.state));
        }

        public void DeserializeState(string uid)
        {
            m_bFilterSelection = SessionState.GetBool(uid + Styles.serializeFilterSelection, false);
            m_ShowInactiveObjects = SessionState.GetBool(uid + Styles.showInactiveObjects, false);

            MultiColumnHeaderState headerState = new MultiColumnHeaderState(multiColumnHeader.state.columns);
            string columnHeaderState = EditorPrefs.GetString(uid + Styles.serializeColumnHeaderState, "");

            if (!string.IsNullOrEmpty(columnHeaderState))
                JsonUtility.FromJsonOverwrite(columnHeaderState, headerState);

            if (MultiColumnHeaderState.CanOverwriteSerializedFields(headerState, multiColumnHeader.state))
                Column.OverwriteSerializedFields(headerState, multiColumnHeader.state);

            for (int i = 0; i < multiColumnHeader.state.columns.Length; i++)
            {
                var filter = Col(i).filter;
                if (filter == null)
                    continue;

                string filterState = SessionState.GetString(uid + Styles.serializeFilter + i, null);
                if (string.IsNullOrEmpty(filterState))
                    continue;

                filter.DeserializeState(filterState);
            }

            string treeViewState = SessionState.GetString(uid + Styles.serializeTreeViewState, "");

            if (!string.IsNullOrEmpty(treeViewState))
                JsonUtility.FromJsonOverwrite(treeViewState, state);
        }

        public bool IsFilteredDirty()
        {
            return m_ChangedId != 0 && (m_ChangedId != GUIUtility.keyboardControl || !EditorGUIUtility.editingTextField);
        }

        public void Update()
        {
            bool needsReload = false;

            if (m_DataStore != null)
            {
                // If we re-populated all data, we need to clear the cached items
                // The data can also be fine, but needs a reload without having to rebuild it all (inactive/active objects changed)
                if (m_DataStore.Repopulate(out needsReload))
                    m_Items = null;

                if (needsReload)
                {
                    Reload();
                    return;
                }
            }

            var rows = GetRows();
            int first, last;

            GetFirstAndLastVisibleRows(out first, out last);

            bool changed = false;

            if (last != -1)
            {
                for (int i = first; i <= last; i++)
                {
                    changed = changed || ((SerializedPropertyItem)rows[i]).GetData().Update();
                }
            }

            if (changed)
                Repaint();
        }

        protected override TreeViewItem BuildRoot()
        {
            return new SerializedPropertyItem(-1, -1, null);
        }

        // this gets called whenever someone issues a reload() call to the parent
        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            if (m_Items == null)
            {
                var rawData = m_DataStore.GetElements();
                m_Items = new List<TreeViewItem>(rawData.Length);

                for (int i = 0; i < rawData.Length; i++)
                {
                    var item = new SerializedPropertyItem(rawData[i].objectId, 0, rawData[i]);
                    m_Items.Add(item);
                }
            }

            // filtering
            IEnumerable<TreeViewItem> tmprows = m_Items;

            if (!m_ShowInactiveObjects)
                tmprows = m_Items.Where((TreeViewItem item) => { return ((SerializedPropertyItem)item).GetData().activeInHierarchy; });

            if (m_bFilterSelection)
            {
                if (m_SelectionFilter == null)
                    m_SelectionFilter = Selection.instanceIDs;
                tmprows = m_Items.Where((TreeViewItem item) => { return m_SelectionFilter.Contains(item.id); });
            }
            else
                m_SelectionFilter = null;

            tmprows = Filter(tmprows);

            var rows = tmprows.ToList();

            if (multiColumnHeader.sortedColumnIndex >= 0)
                Sort(rows, multiColumnHeader.sortedColumnIndex);

            m_ChangedId = 0;

            // We still need to setup the child parent information for the rows since this
            // information is used by the TreeView internal logic (navigation, dragging etc)
            TreeViewUtility.SetParentAndChildrenForItems(rows, root);

            return rows;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = (SerializedPropertyItem)args.item;

            for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                CellGUI(args.GetCellRect(i), item, args.GetColumn(i), ref args);
            }
        }

        void CellGUI(Rect cellRect, SerializedPropertyItem item, int columnIndex, ref RowGUIArgs args)
        {
            Profiler.BeginSample("SerializedPropertyTreeView.CellGUI");
            CenterRectUsingSingleLineHeight(ref cellRect);
            var ltd = item.GetData();
            Column column = (Column)this.multiColumnHeader.GetColumn(columnIndex);

            if (column.drawDelegate == DefaultDelegates.DrawName)
            {
                // default drawing
                Profiler.BeginSample("SerializedPropertyTreeView.OnItemGUI.LabelField");
                using (new EditorGUI.DisabledScope(!ltd.activeInHierarchy))
                {
                    DefaultGUI.Label(cellRect, ltd.name, IsSelected(args.item.id), false);
                }
                Profiler.EndSample();
            }
            else if (column.drawDelegate != null)
            {
                SerializedProperty[] props = ltd.properties;
                int depcnt = column.dependencyIndices != null ? column.dependencyIndices.Length : 0;

                for (int i = 0; i < depcnt; i++)
                    m_ColumnsInternal[columnIndex].dependencyProps[i] = props[column.dependencyIndices[i]];

                // allow to capture tabs
                if (args.item.id == state.lastClickedID && HasFocus() && columnIndex == multiColumnHeader.state.visibleColumns[multiColumnHeader.state.visibleColumns[0] == 0 ? 1 : 0])
                    GUI.SetNextControlName(Styles.focusHelper);

                SerializedProperty prop = ltd.properties[columnIndex];

                EditorGUI.BeginChangeCheck();

                Profiler.BeginSample("SerializedPropertyTreeView.OnItemGUI.drawDelegate");

                column.drawDelegate(cellRect, prop, m_ColumnsInternal[columnIndex].dependencyProps);

                Profiler.EndSample();
                if (EditorGUI.EndChangeCheck())
                {
                    // if we changed a value in a filtered column we'll have to reload the table
                    m_ChangedId = ((column.filter != null) && column.filter.Active()) ? GUIUtility.keyboardControl : m_ChangedId;
                    // update all selected items if the current row was part of the selection list
                    ltd.Store();

                    var selIds = GetSelection();

                    if (selIds.Contains(ltd.objectId))
                    {
                        IList<TreeViewItem> rows = FindRows(selIds);

                        Undo.RecordObjects(rows.Select(r => ((SerializedPropertyItem)r).GetData().serializedObject.targetObject).ToArray(), "Modify Multiple Properties");

                        foreach (var r in rows)
                        {
                            if (r.id == args.item.id)
                                continue;

                            var data = ((SerializedPropertyItem)r).GetData();

                            if (!IsEditable(data.serializedObject.targetObject))
                                continue;

                            if (column.copyDelegate != null)
                                column.copyDelegate(data.properties[columnIndex], prop);
                            else
                                DefaultDelegates.CopyDefault(data.properties[columnIndex], prop);

                            data.Store();
                        }
                    }
                }
            }
            Profiler.EndSample();
        }

        private static bool IsEditable(Object target)
        {
            return ((target.hideFlags & HideFlags.NotEditable) == 0);
        }

        protected override void BeforeRowsGUI()
        {
            var rows = GetRows();
            int first, last;
            GetFirstAndLastVisibleRows(out first, out last);

            if (last != -1)
            {
                for (int i = first; i <= last; i++)
                    ((SerializedPropertyItem)rows[i]).GetData().Update();
            }

            // don't forget to refresh selected objects that got shoved off screen
            var selected = FindRows(GetSelection());
            foreach (SerializedPropertyItem sel in selected)
                sel.GetData().Update();

            base.BeforeRowsGUI();
        }

        public void OnFilterGUI(Rect r)
        {
            EditorGUI.BeginChangeCheck();

            float fullWidth = r.width;
            float windowPadding = r.x;
            float toggleWidth = 16;

            r.width = toggleWidth;
            m_bFilterSelection = EditorGUI.Toggle(r, m_bFilterSelection);
            r.x += toggleWidth;
            r.width = GUI.skin.label.CalcSize(SerializedPropertyTreeView.Styles.filterSelection).x;
            EditorGUI.LabelField(r, SerializedPropertyTreeView.Styles.filterSelection);

            r.x += r.width + 10;
            r.width = toggleWidth;
            m_ShowInactiveObjects = EditorGUI.Toggle(r, m_ShowInactiveObjects);
            r.x += toggleWidth;
            r.width = GUI.skin.label.CalcSize(SerializedPropertyTreeView.Styles.showInactiveObjects).x;
            EditorGUI.LabelField(r, SerializedPropertyTreeView.Styles.showInactiveObjects);

            r.width = Mathf.Min(fullWidth - (r.x + r.width), 300);
            r.x = fullWidth - r.width + windowPadding;

            for (int i = 0; i < multiColumnHeader.state.columns.Length; i++)
            {
                if (!IsColumnVisible(i))
                    continue;

                Column c = Col(i);

                if (c.filter != null && c.filter.GetType().Equals(typeof(SerializedPropertyFilters.Name)))
                {
                    c.filter.OnGUI(r);
                }
            }

            if (EditorGUI.EndChangeCheck())
                Reload();
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            Selection.instanceIDs = selectedIds.ToArray();
        }

        protected override void KeyEvent()
        {
            if (Event.current.type != EventType.KeyDown)
                return;

            // We always focus as this callback only fires when the name column is selected.
            // Tabbing through the property columns never goes through here.
            // Also note that we're not capturing the KeyCode Tab. There are 2 tab events,
            // one for the keycode, and one for the character code. The keycode comes first,
            // so if we change the focus based on the keycode, we never get the second character code
            // event as the treeview loses focus and doesn't fire the callback. The second tab event
            // then gets handled as an ordinary tab, leading to a double tab.
            if (Event.current.character == '\t')
            {
                GUI.FocusControl(Styles.focusHelper);
                Event.current.Use();
            }
        }

        void OnVisibleColumnChanged(MultiColumnHeader header)
        {
            Reload();
        }

        void OnSortingChanged(MultiColumnHeader multiColumnHeader)
        {
            var rows = GetRows();
            Sort(rows, multiColumnHeader.sortedColumnIndex);
        }

        void Sort(IList<TreeViewItem> rows, int sortIdx)
        {
            Debug.Assert(sortIdx >= 0);

            bool ascend = multiColumnHeader.IsSortedAscending(sortIdx);
            var comp = Col(sortIdx).compareDelegate;
            var myRows = rows as List<TreeViewItem>;

            System.Comparison<TreeViewItem> sortAscend, sortDescend;
            if (comp == null)
            {
                return;
            }
            else if (comp == DefaultDelegates.CompareName) // special case for sorting by the object name
            {
                sortAscend = (TreeViewItem lhs, TreeViewItem rhs) =>
                {
                    return EditorUtility.NaturalCompare(((SerializedPropertyItem)lhs).GetData().name, ((SerializedPropertyItem)rhs).GetData().name);
                };
                sortDescend = (TreeViewItem lhs, TreeViewItem rhs) =>
                {
                    return -EditorUtility.NaturalCompare(((SerializedPropertyItem)lhs).GetData().name, ((SerializedPropertyItem)rhs).GetData().name);
                };
            }
            else
            {
                sortAscend = (TreeViewItem lhs, TreeViewItem rhs) =>
                {
                    return comp(((SerializedPropertyItem)lhs).GetData().properties[sortIdx], ((SerializedPropertyItem)rhs).GetData().properties[sortIdx]);
                };
                sortDescend = (TreeViewItem lhs, TreeViewItem rhs) =>
                {
                    return -comp(((SerializedPropertyItem)lhs).GetData().properties[sortIdx], ((SerializedPropertyItem)rhs).GetData().properties[sortIdx]);
                };
            }

            myRows.Sort(ascend ? sortAscend : sortDescend);
        }

        IEnumerable<TreeViewItem> Filter(IEnumerable<TreeViewItem> rows)
        {
            var tmp = rows;
            int cnt = m_ColumnsInternal.Length;
            for (int i = 0; i < cnt; i++)
            {
                if (!IsColumnVisible(i))
                    continue;

                Column c = Col(i);
                int idx = i;
                if (c.filter != null)
                {
                    if (!c.filter.Active())
                        continue;

                    if (c.filter.GetType().Equals(typeof(SerializedPropertyFilters.Name)))
                    {
                        var f = (SerializedPropertyFilters.Name)c.filter;
                        tmp = tmp.Where((TreeViewItem item) => { return f.Filter(((SerializedPropertyItem)item).GetData().name); });
                    }
                    else
                    {
                        tmp = tmp.Where((TreeViewItem item) => { return c.filter.Filter(((SerializedPropertyItem)item).GetData().properties[idx]); });
                    }
                }
            }

            return tmp;
        }

        private bool IsColumnVisible(int idx)
        {
            for (int i = 0; i < multiColumnHeader.state.visibleColumns.Length; i++)
                if (multiColumnHeader.state.visibleColumns[i] == idx)
                    return true;

            return false;
        }

        // casting helper
        private Column Col(int idx) { return (Column)multiColumnHeader.state.columns[idx]; }
    }
}
