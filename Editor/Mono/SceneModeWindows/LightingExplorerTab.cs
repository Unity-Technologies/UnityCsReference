// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System;
using System.Linq;

namespace UnityEditor
{
    public class LightingExplorerTab
    {
        SerializedPropertyTable m_LightTable;
        GUIContent m_Title;

        internal GUIContent title { get { return m_Title; } }

        public LightingExplorerTab(string title, Func<UnityEngine.Object[]> objects, Func<LightingExplorerTableColumn[]> columns)
        {
            if (objects() == null)
                throw new ArgumentException("Objects are not allowed to be null", "objects");

            if (columns() == null)
                throw new ArgumentException("Columns are not allowed to be null", "columns");

            m_LightTable = new SerializedPropertyTable(title.Replace(" ", string.Empty), new SerializedPropertyDataStore.GatherDelegate(objects), () => {
                return columns().Select(item => item.internalColumn).ToArray();
            });
            m_Title = EditorGUIUtility.TrTextContent(title);
        }

        internal void OnEnable()
        {
            if (m_LightTable != null)
                m_LightTable.OnEnable();
        }

        internal void OnDisable()
        {
            if (m_LightTable != null)
                m_LightTable.OnDisable();
        }

        internal void OnInspectorUpdate()
        {
            if (m_LightTable != null)
                m_LightTable.OnInspectorUpdate();
        }

        internal void OnSelectionChange(int[] instanceIDs)
        {
            if (m_LightTable != null)
                m_LightTable.OnSelectionChange(instanceIDs);
        }

        internal void OnSelectionChange()
        {
            if (m_LightTable != null)
                m_LightTable.OnSelectionChange();
        }

        internal void OnHierarchyChange()
        {
            if (m_LightTable != null)
                m_LightTable.OnHierarchyChange();
        }

        internal void OnGUI()
        {
            EditorGUI.indentLevel += 1;

            int cur_indent = EditorGUI.indentLevel;
            float cur_indent_px = EditorGUI.indent;
            EditorGUI.indentLevel = 0;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(cur_indent_px);

            using (new EditorGUILayout.VerticalScope())
            {
                if (m_LightTable != null)
                    m_LightTable.OnGUI();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            EditorGUI.indentLevel = cur_indent;

            EditorGUI.indentLevel -= 1;
        }
    }

    public sealed class LightingExplorerTableColumn
    {
        public enum DataType
        {
            Name = 0,
            Checkbox = 1,
            Enum = 2,
            Int = 3,
            Float = 4,
            Color = 5,
            // ..
            Custom = 20
        }

        SerializedPropertyTreeView.Column m_Column;

        internal SerializedPropertyTreeView.Column internalColumn { get { return m_Column; } }

        public delegate void OnGUIDelegate(Rect r, SerializedProperty prop, SerializedProperty[] dependencies);
        public delegate int ComparePropertiesDelegate(SerializedProperty lhs, SerializedProperty rhs);
        public delegate void CopyPropertiesDelegate(SerializedProperty target, SerializedProperty source);

        public LightingExplorerTableColumn(DataType type, GUIContent headerContent, string propertyName = null, int width = 100, OnGUIDelegate onGUIDelegate = null, ComparePropertiesDelegate compareDelegate = null, CopyPropertiesDelegate copyDelegate = null, int[] dependencyIndices = null)
        {
            m_Column = new SerializedPropertyTreeView.Column();

            m_Column.headerContent          = headerContent;
            m_Column.width                  = width;
            m_Column.minWidth               = width / 2;
            m_Column.propertyName           = propertyName;
            m_Column.dependencyIndices      = dependencyIndices;

            m_Column.sortedAscending        = true;
            m_Column.sortingArrowAlignment  = TextAlignment.Center;
            m_Column.autoResize             = false;
            m_Column.allowToggleVisibility  = true;
            m_Column.headerTextAlignment    = type == DataType.Checkbox ? TextAlignment.Center : TextAlignment.Left;

            switch (type)
            {
                case DataType.Name:
                    m_Column.compareDelegate = SerializedPropertyTreeView.DefaultDelegates.CompareName;
                    m_Column.drawDelegate    = SerializedPropertyTreeView.DefaultDelegates.DrawName;
                    m_Column.filter          = new SerializedPropertyFilters.Name();
                    break;

                case DataType.Checkbox:
                    m_Column.compareDelegate = SerializedPropertyTreeView.DefaultDelegates.CompareCheckbox;
                    m_Column.drawDelegate    = SerializedPropertyTreeView.DefaultDelegates.DrawCheckbox;
                    break;

                case DataType.Enum:
                    m_Column.compareDelegate = SerializedPropertyTreeView.DefaultDelegates.CompareEnum;
                    m_Column.drawDelegate    = SerializedPropertyTreeView.DefaultDelegates.DrawDefault;
                    break;

                case DataType.Int:
                    m_Column.compareDelegate = SerializedPropertyTreeView.DefaultDelegates.CompareInt;
                    m_Column.drawDelegate    = SerializedPropertyTreeView.DefaultDelegates.DrawDefault;
                    break;

                case DataType.Float:
                    m_Column.compareDelegate = SerializedPropertyTreeView.DefaultDelegates.CompareFloat;
                    m_Column.drawDelegate    = SerializedPropertyTreeView.DefaultDelegates.DrawDefault;
                    break;

                case DataType.Color:
                    m_Column.compareDelegate = SerializedPropertyTreeView.DefaultDelegates.CompareColor;
                    m_Column.drawDelegate    = SerializedPropertyTreeView.DefaultDelegates.DrawDefault;
                    break;

                default:
                    break;
            }

            if (onGUIDelegate != null)
            {
                // when allowing the user to draw checkboxes, we will make sure that the rect is in the center
                if (type == DataType.Checkbox)
                {
                    m_Column.drawDelegate = (r, prop, dep) => {
                        float off = System.Math.Max(0.0f, ((r.width / 2) - 8));
                        r.x += off;
                        r.width -= off;
                        onGUIDelegate(r, prop, dep);
                    };
                }
                else
                    m_Column.drawDelegate = new SerializedPropertyTreeView.Column.DrawEntry(onGUIDelegate);
            }

            if (compareDelegate != null)
                m_Column.compareDelegate = new SerializedPropertyTreeView.Column.CompareEntry(compareDelegate);

            if (copyDelegate != null)
                m_Column.copyDelegate = new SerializedPropertyTreeView.Column.CopyDelegate(copyDelegate);
        }
    }
}
