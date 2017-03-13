// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SelectionType = UnityEditor.ShapeEditor.SelectionType;

namespace UnityEditor
{
    internal class ShapeEditorRectSelectionTool
    {
        Vector2 m_SelectStartPoint;
        Vector2 m_SelectMousePoint;
        bool m_RectSelecting;
        int m_RectSelectionID;
        const float k_MinSelectionSize = 6f;

        public event Action<Rect, SelectionType> RectSelect = (i, p) => {};
        public event Action ClearSelection = () => {};

        public ShapeEditorRectSelectionTool(IGUIUtility gu)
        {
            guiUtility = gu;
            m_RectSelectionID = guiUtility.GetPermanentControlID();
        }

        public void OnGUI()
        {
            Event evt = Event.current;

            Handles.BeginGUI();

            Vector2 mousePos = evt.mousePosition;
            int id = m_RectSelectionID;

            switch (evt.GetTypeForControl(id))
            {
                case EventType.Layout:
                    if (!Tools.viewToolActive)
                        HandleUtility.AddDefaultControl(id);
                    break;

                case EventType.MouseDown:
                    if (HandleUtility.nearestControl == id && evt.button == 0)
                    {
                        guiUtility.hotControl = id;
                        m_SelectStartPoint = mousePos;
                    }
                    break;
                case EventType.MouseDrag:
                    if (guiUtility.hotControl == id)
                    {
                        if (!m_RectSelecting && (mousePos - m_SelectStartPoint).magnitude > k_MinSelectionSize)
                        {
                            m_RectSelecting = true;
                        }
                        if (m_RectSelecting)
                        {
                            m_SelectMousePoint = mousePos;

                            SelectionType type = SelectionType.Normal;
                            if (Event.current.control)
                                type = SelectionType.Subtractive;
                            else if (Event.current.shift)
                                type = SelectionType.Additive;
                            RectSelect(EditorGUIExt.FromToRect(m_SelectStartPoint, m_SelectMousePoint), type);
                        }
                        evt.Use();
                    }
                    break;

                case EventType.Repaint:
                    if (guiUtility.hotControl == id && m_RectSelecting)
                    {
                        EditorStyles.selectionRect.Draw(EditorGUIExt.FromToRect(m_SelectStartPoint, m_SelectMousePoint), GUIContent.none,
                            false, false, false, false);
                    }
                    break;

                case EventType.MouseUp:
                    if (guiUtility.hotControl == id && evt.button == 0)
                    {
                        guiUtility.hotControl = 0;
                        guiUtility.keyboardControl = 0;
                        if (m_RectSelecting)
                        {
                            m_SelectMousePoint = new Vector2(mousePos.x, mousePos.y);

                            SelectionType type = SelectionType.Normal;
                            if (Event.current.control)
                                type = SelectionType.Subtractive;
                            else if (Event.current.shift)
                                type = SelectionType.Additive;

                            RectSelect(EditorGUIExt.FromToRect(m_SelectStartPoint, m_SelectMousePoint), type);

                            m_RectSelecting = false;
                        }
                        else
                        {
                            ClearSelection();
                        }
                        evt.Use();
                    }
                    break;
            }

            Handles.EndGUI();
        }

        public bool isSelecting
        {
            get { return guiUtility.hotControl == m_RectSelectionID; }
        }

        IGUIUtility guiUtility
        {
            get; set;
        }
    }

    // TODO: For now we copy-paste from RectSelection. Refactor to avoid duplicate codes.
    internal class ShapeEditorSelection : IEnumerable<int>
    {
        HashSet<int> m_SelectedPoints = new HashSet<int>();
        ShapeEditor m_ShapeEditor;

        public ShapeEditorSelection(ShapeEditor owner)
        {
            m_ShapeEditor = owner;
        }

        public bool Contains(int i)
        {
            return m_SelectedPoints.Contains(i);
        }

        public int Count
        {
            get { return m_SelectedPoints.Count; }
        }

        public void DeleteSelection()
        {
            var sorted = m_SelectedPoints.OrderByDescending(x => x);
            foreach (int selectedIndex in sorted)
            {
                m_ShapeEditor.RemovePointAt(selectedIndex);
            }
            if (m_ShapeEditor.activePoint >= m_ShapeEditor.GetPointsCount())
                m_ShapeEditor.activePoint = m_ShapeEditor.GetPointsCount() - 1;
            m_SelectedPoints.Clear();
        }

        public void MoveSelection(Vector3 delta)
        {
            if (delta.sqrMagnitude < float.Epsilon)
                return;

            foreach (int selectedIndex in m_SelectedPoints)
            {
                m_ShapeEditor.SetPointPosition(selectedIndex, m_ShapeEditor.GetPointPosition(selectedIndex) + delta);
            }
        }

        public void Clear()
        {
            m_SelectedPoints.Clear();
            if (m_ShapeEditor != null)
                m_ShapeEditor.activePoint = -1;
        }

        public void SelectPoint(int i, SelectionType type)
        {
            switch (type)
            {
                case SelectionType.Additive:
                    m_ShapeEditor.activePoint = i;
                    m_SelectedPoints.Add(i);
                    break;
                case SelectionType.Subtractive:
                    m_ShapeEditor.activePoint = i > 0 ? i - 1 : 0;
                    m_SelectedPoints.Remove(i);
                    break;
                case SelectionType.Normal:
                    m_SelectedPoints.Clear();
                    m_ShapeEditor.activePoint = i;
                    m_SelectedPoints.Add(i);
                    break;
                default:
                    m_ShapeEditor.activePoint = i; break;
            }
            m_ShapeEditor.Repaint();
        }

        public void RectSelect(Rect rect, SelectionType type)
        {
            if (type == SelectionType.Normal)
            {
                m_SelectedPoints.Clear();
                m_ShapeEditor.activePoint = -1;
                type = SelectionType.Additive;
            }

            for (int i = 0; i < m_ShapeEditor.GetPointsCount(); i++)
            {
                var p0 = m_ShapeEditor.GetPointPosition(i);
                if (rect.Contains(p0))
                {
                    SelectPoint(i, type);
                }
            }
            m_ShapeEditor.Repaint();
        }

        public HashSet<int> indices { get { return m_SelectedPoints; } }

        public IEnumerator<int> GetEnumerator()
        {
            return m_SelectedPoints.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
} // namespace
