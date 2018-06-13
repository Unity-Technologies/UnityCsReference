// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace UnityEditor
{
    internal class LineRendererEditor : IEditablePoint, ICreatablePoint
    {
        public enum InputMode
        {
            MousePosition,
            PhysicsRaycast
        }

        private LineRenderer m_LineRenderer;
        private LineRendererInspector m_Inspector;
        public List<int> m_Selection = new List<int>();

        public static float createPointSeparation
        {
            get { return EditorPrefs.GetFloat("LineRendererEditorCreatePointSeparation", 1.0f); }
            set { EditorPrefs.SetFloat("LineRendererEditorCreatePointSeparation", value < 0 ? 0 : value); }
        }

        public static float creationOffset
        {
            get { return EditorPrefs.GetFloat("LineRendererEditorCreationOffset", 5.0f); }
            set { EditorPrefs.SetFloat("LineRendererEditorCreationOffset", value); }
        }

        public static InputMode inputMode
        {
            get { return (InputMode)EditorPrefs.GetInt("LineRendererEditorInputMode", 0); }
            set { EditorPrefs.SetInt("LineRendererEditorInputMode", (int)value); }
        }

        public static LayerMask raycastMask
        {
            get { return EditorPrefs.GetInt("LineRendererEditorRaycastMask", -1); }
            set { EditorPrefs.SetInt("LineRendererEditorRaycastMask", value); }
        }

        public static bool showWireframe
        {
            get { return EditorPrefs.GetBool("LineRendererEditorShowWireFrame", false); }
            set { EditorPrefs.SetBool("LineRendererEditorShowWireFrame", value); }
        }

        private static readonly Color kCloudColor = new Color(200f / 255f, 200f / 255f, 20f / 255f, 0.85f);
        private static readonly Color kSelectedCloudColor = new Color(.3f, .6f, 1, 1);

        public LineRendererEditor(LineRenderer lineRenderer, LineRendererInspector inspector)
        {
            m_LineRenderer = lineRenderer;
            m_Inspector = inspector;
        }

        public void Deselect()
        {
            m_Selection.Clear();
        }

        public void HandleEditMenuHotKeyCommands()
        {
            //Handle other events!
            if (Event.current.type == EventType.ValidateCommand
                || Event.current.type == EventType.ExecuteCommand)
            {
                bool execute = Event.current.type == EventType.ExecuteCommand;
                switch (Event.current.commandName)
                {
                    case "SoftDelete":
                    case "Delete":
                        if (execute)
                            DestroySelected();
                        Event.current.Use();
                        break;
                    case "Duplicate":
                        if (execute)
                            DuplicateSelected();
                        Event.current.Use();
                        break;
                    case "SelectAll":
                        if (execute)
                            SelectAllPoints();
                        Event.current.Use();
                        break;
                    case "Cut":
                    case "Copy":
                    case "Paste":
                        // We need to capture these events to prevent being taken out of Edit mode by accident.
                        // The copy paste would trigger the Inspector and duplicate the GameObject instead.
                        Event.current.Use();
                        break;
                }
            }
        }

        public void RemoveInvalidSelections()
        {
            // Remove any selections that do not exist.
            int points = m_LineRenderer.positionCount;
            m_Selection = m_Selection.Where(o => o < points).ToList();
        }

        public void SelectAllPoints()
        {
            m_Selection.Clear();
            for (int i = 0; i < m_LineRenderer.positionCount; ++i)
                m_Selection.Add(i);
            m_Inspector.Repaint();
        }

        public void DestroySelected()
        {
            if (m_Selection.Count == 0)
                return;

            Undo.RecordObject(m_LineRenderer, "Delete Selected");
            var positions = new Vector3[m_LineRenderer.positionCount];
            var newPositions = new Vector3[m_LineRenderer.positionCount - m_Selection.Count];
            m_LineRenderer.GetPositions(positions);

            int selectionIndex = 0;
            int insertIndex = 0;
            for (int i = 0; i < positions.Length; ++i)
            {
                if (i == m_Selection[selectionIndex])
                {
                    if (selectionIndex < m_Selection.Count - 1)
                        ++selectionIndex;
                }
                else
                    newPositions[insertIndex++] = positions[i];
            }

            m_LineRenderer.positionCount = newPositions.Length;
            m_LineRenderer.SetPositions(newPositions);
            m_Selection.Clear();
        }

        public void DuplicateSelected()
        {
            if (m_Selection.Count == 0)
                return;

            Undo.RecordObject(m_LineRenderer, "Duplicate Selected");
            var positions = new Vector3[m_LineRenderer.positionCount];
            var newPositions = new Vector3[m_LineRenderer.positionCount + m_Selection.Count];
            m_LineRenderer.GetPositions(positions);

            int selectionIndex = 0;
            int insertIndex = 0;
            var insertedIndexes = new List<int>(m_Selection.Count);
            for (int i = 0; i < positions.Length; ++i)
            {
                if (i == m_Selection[selectionIndex])
                {
                    if (selectionIndex < m_Selection.Count - 1)
                        ++selectionIndex;

                    newPositions[insertIndex++] = positions[i];
                    insertedIndexes.Add(insertIndex); // Select the duplicate
                    newPositions[insertIndex++] = positions[i];
                }
                else
                    newPositions[insertIndex++] = positions[i];
            }

            m_LineRenderer.positionCount = newPositions.Length;
            m_LineRenderer.SetPositions(newPositions);
            m_Selection = insertedIndexes;
        }

        void DrawWireframe()
        {
            Handles.color = Color.white;

            Vector3[] positions = new Vector3[m_LineRenderer.loop ? m_LineRenderer.positionCount + 1 : m_LineRenderer.positionCount];
            m_LineRenderer.GetPositions(positions);
            if (m_LineRenderer.loop)
                positions[m_LineRenderer.positionCount] = positions[0];

            var oldMatrix = Handles.matrix;
            if (!m_LineRenderer.useWorldSpace)
                Handles.matrix = m_LineRenderer.transform.localToWorldMatrix;

            Handles.DrawAAPolyLine(3, positions.Length, positions);
            Handles.matrix = oldMatrix;
        }

        public void CreateSceneGUI()
        {
            PointCreator.CreatePoints(this, true, inputMode == InputMode.PhysicsRaycast, raycastMask, creationOffset, createPointSeparation);
            PointCreator.Draw();
        }

        public void EditSceneGUI()
        {
            Transform transform = m_LineRenderer.useWorldSpace ? null : m_LineRenderer.transform;

            if (m_LineRenderer.positionCount == 0)
                return;

            PointEditor.SelectPoints(this, transform, ref m_Selection, false);
            PointEditor.Draw(this, transform, m_Selection, true);
            PointEditor.MovePoints(this, transform, m_Selection);
            if (showWireframe)
                DrawWireframe();

            HandleEditMenuHotKeyCommands();
        }

        public int Count { get { return m_LineRenderer.positionCount; } }

        public Color GetDefaultColor()
        {
            return kCloudColor;
        }

        public Color GetSelectedColor()
        {
            return kSelectedCloudColor;
        }

        public float GetPointScale()
        {
            return 5.0f * AnnotationUtility.iconSize;
        }

        public Vector3 GetPosition(int idx)
        {
            return m_LineRenderer.GetPosition(idx);
        }

        public IEnumerable<Vector3> GetPositions()
        {
            Vector3[] positions = new Vector3[m_LineRenderer.positionCount];
            m_LineRenderer.GetPositions(positions);

            return positions;
        }

        public Vector3[] GetSelectedPositions()
        {
            Vector3[] positions = new Vector3[m_LineRenderer.positionCount];
            m_LineRenderer.GetPositions(positions);

            var result = new Vector3[m_Selection.Count];
            for (int i = 0; i < m_Selection.Count; i++)
            {
                result[i] = positions[m_Selection[i]];
            }

            return result;
        }

        public Vector3[] GetUnselectedPositions()
        {
            if (m_Selection.Count == m_LineRenderer.positionCount)
            {
                return new Vector3[0];
            }

            Vector3[] positions = new Vector3[m_LineRenderer.positionCount];
            m_LineRenderer.GetPositions(positions);
            if (m_Selection.Count == 0)
            {
                return positions;
            }

            var selectionList = new bool[positions.Length];

            // Mark everything unselected
            for (int i = 0; i < positions.Length; i++)
            {
                selectionList[i] = false;
            }

            // Mark selected
            for (int i = 0; i < m_Selection.Count; i++)
            {
                selectionList[m_Selection[i]] = true;
            }

            // Get remaining unselected
            var result = new Vector3[positions.Length - m_Selection.Count];
            var unselectedCount = 0;
            for (int i = 0; i < positions.Length; i++)
            {
                if (selectionList[i] == false)
                {
                    result[unselectedCount++] = positions[i];
                }
            }

            return result;
        }

        public void SetPosition(int idx, Vector3 position)
        {
            Undo.RecordObject(m_LineRenderer, "Move Position");
            m_LineRenderer.SetPosition(idx, position);
        }

        private Bounds GetBounds(List<Vector3> positions)
        {
            if (positions.Count == 0)
                return new Bounds();

            Transform transform = m_LineRenderer.useWorldSpace ? null : m_LineRenderer.transform;
            if (positions.Count == 1)
            {
                return new Bounds(transform != null ? m_LineRenderer.transform.TransformPoint(positions[0]) : positions[0], new Vector3(1f, 1f, 1f));
            }

            return GeometryUtility.CalculateBounds(positions.ToArray(), transform != null ? transform.localToWorldMatrix : Matrix4x4.identity);
        }

        public void AddPositions(List<Vector3> newPositions)
        {
            Undo.RecordObject(m_LineRenderer, "Add Positions");
            var positions = new Vector3[m_LineRenderer.positionCount + newPositions.Count];

            m_LineRenderer.GetPositions(positions);
            int readPos = 0;
            int writePos = m_LineRenderer.positionCount;
            Matrix4x4 worldToLocal = m_LineRenderer.transform.worldToLocalMatrix;
            for (; writePos < positions.Length; ++writePos, ++readPos)
            {
                var transformedPos = m_LineRenderer.useWorldSpace ? newPositions[readPos] : worldToLocal.MultiplyPoint(newPositions[readPos]);
                positions[writePos] = transformedPos;
            }

            m_LineRenderer.positionCount = positions.Length;
            m_LineRenderer.SetPositions(positions);
        }

        public Bounds selectedPositionsBounds
        {
            get
            {
                List<Vector3> selectedPoints = new List<Vector3>();
                foreach (var idx in m_Selection)
                    selectedPoints.Add(GetPosition(idx));
                return GetBounds(selectedPoints);
            }
        }
    }
}
