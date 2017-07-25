// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor
{
    internal class DopeSheetEditorRectangleTool : RectangleTool
    {
        const float kDefaultFrameRate = 60f;

        const int kScaleLeftWidth = 17;
        const int kScaleLeftMarginHorizontal = 0;
        const float kScaleLeftMarginVertical = 4;

        const int kScaleRightWidth = 17;
        const int kScaleRightMarginHorizontal = 0;
        const float kScaleRightMarginVertical = 4;

        const int kHLabelMarginHorizontal = 8;
        const int kHLabelMarginVertical = 1;

        static Rect g_EmptyRect = new Rect(0f, 0f, 0f, 0f);

        struct ToolLayout
        {
            public Rect summaryRect;
            public Rect selectionRect;

            public Rect scaleLeftRect;
            public Rect scaleRightRect;

            public Vector2 leftLabelAnchor;
            public Vector2 rightLabelAnchor;
        }

        private DopeSheetEditor m_DopeSheetEditor;
        private AnimationWindowState m_State;

        private ToolLayout m_Layout;

        private Vector2 m_Pivot;
        private Vector2 m_Previous;
        private Vector2 m_MouseOffset;

        private bool m_IsDragging;
        private bool m_RippleTime;
        private float m_RippleTimeStart;
        private float m_RippleTimeEnd;

        private AreaManipulator[] m_SelectionBoxes;

        private AreaManipulator m_SelectionScaleLeft;
        private AreaManipulator m_SelectionScaleRight;

        private bool hasSelection { get { return (m_State.selectedKeys.Count > 0); } }
        private Bounds selectionBounds { get { return m_State.selectionBounds; } }
        private float frameRate { get { return m_State.frameRate; } }

        private bool isDragging { get { return m_IsDragging || m_DopeSheetEditor.isDragging; } }

        public override void Initialize(TimeArea timeArea)
        {
            base.Initialize(timeArea);
            m_DopeSheetEditor = timeArea as DopeSheetEditor;
            m_State = m_DopeSheetEditor.state;

            if (m_SelectionBoxes == null)
            {
                m_SelectionBoxes = new AreaManipulator[2];

                for (int i = 0; i < 2; ++i)
                {
                    m_SelectionBoxes[i] = new AreaManipulator(styles.rectangleToolSelection, MouseCursor.MoveArrow);

                    m_SelectionBoxes[i].onStartDrag += (AnimationWindowManipulator manipulator, Event evt) =>
                        {
                            bool curveEditorOverride = evt.shift || EditorGUI.actionKey;
                            if (!curveEditorOverride && hasSelection && manipulator.rect.Contains(evt.mousePosition))
                            {
                                OnStartMove(new Vector2(PixelToTime(evt.mousePosition.x, frameRate), 0.0f), rippleTimeClutch);
                                return true;
                            }

                            return false;
                        };
                    m_SelectionBoxes[i].onDrag += (AnimationWindowManipulator manipulator, Event evt) =>
                        {
                            OnMove(new Vector2(PixelToTime(evt.mousePosition.x, frameRate), 0.0f));
                            return true;
                        };
                    m_SelectionBoxes[i].onEndDrag += (AnimationWindowManipulator manipulator, Event evt) =>
                        {
                            OnEndMove();
                            return true;
                        };
                }
            }

            if (m_SelectionScaleLeft == null)
            {
                m_SelectionScaleLeft = new AreaManipulator(styles.dopesheetScaleLeft, MouseCursor.ResizeHorizontal);

                m_SelectionScaleLeft.onStartDrag += (AnimationWindowManipulator manipulator, Event evt) =>
                    {
                        if (hasSelection && manipulator.rect.Contains(evt.mousePosition))
                        {
                            OnStartScale(ToolCoord.Right, ToolCoord.Left, new Vector2(PixelToTime(evt.mousePosition.x, frameRate), 0f), rippleTimeClutch);
                            return true;
                        }

                        return false;
                    };
                m_SelectionScaleLeft.onDrag += (AnimationWindowManipulator manipulator, Event evt) =>
                    {
                        OnScaleTime(PixelToTime(evt.mousePosition.x, frameRate));
                        return true;
                    };
                m_SelectionScaleLeft.onEndDrag += (AnimationWindowManipulator manipulator, Event evt) =>
                    {
                        OnEndScale();
                        return true;
                    };
            }

            if (m_SelectionScaleRight == null)
            {
                m_SelectionScaleRight = new AreaManipulator(styles.dopesheetScaleRight, MouseCursor.ResizeHorizontal);

                m_SelectionScaleRight.onStartDrag += (AnimationWindowManipulator manipulator, Event evt) =>
                    {
                        if (hasSelection && manipulator.rect.Contains(evt.mousePosition))
                        {
                            OnStartScale(ToolCoord.Left, ToolCoord.Right, new Vector2(PixelToTime(evt.mousePosition.x, frameRate), 0f), rippleTimeClutch);
                            return true;
                        }

                        return false;
                    };
                m_SelectionScaleRight.onDrag += (AnimationWindowManipulator manipulator, Event evt) =>
                    {
                        OnScaleTime(PixelToTime(evt.mousePosition.x, frameRate));
                        return true;
                    };
                m_SelectionScaleRight.onEndDrag += (AnimationWindowManipulator manipulator, Event evt) =>
                    {
                        OnEndScale();
                        return true;
                    };
            }
        }

        public void OnGUI()
        {
            if (!hasSelection)
                return;

            if (Event.current.type != EventType.Repaint)
                return;

            m_Layout = CalculateLayout();

            m_SelectionBoxes[0].OnGUI(m_Layout.summaryRect);
            m_SelectionBoxes[1].OnGUI(m_Layout.selectionRect);

            m_SelectionScaleLeft.OnGUI(m_Layout.scaleLeftRect);
            m_SelectionScaleRight.OnGUI(m_Layout.scaleRightRect);

            DrawLabels();
        }

        public void HandleEvents()
        {
            HandleClutchKeys();

            m_SelectionScaleLeft.HandleEvents();
            m_SelectionScaleRight.HandleEvents();

            m_SelectionBoxes[0].HandleEvents();
            m_SelectionBoxes[1].HandleEvents();
        }

        private ToolLayout CalculateLayout()
        {
            ToolLayout layout = new ToolLayout();

            Bounds bounds = selectionBounds;

            bool canScaleX = !Mathf.Approximately(bounds.size.x, 0f);

            float xMin = TimeToPixel(bounds.min.x);
            float xMax = TimeToPixel(bounds.max.x);

            float yMin = 0f, yMax = 0f;
            bool firstKey = true;

            float heightCumul = 0f;

            List<DopeLine> dopelines = m_State.dopelines;
            for (int i = 0; i < dopelines.Count; ++i)
            {
                DopeLine dopeline = dopelines[i];

                float dopelineHeight = (dopeline.tallMode ? AnimationWindowHierarchyGUI.k_DopeSheetRowHeightTall : AnimationWindowHierarchyGUI.k_DopeSheetRowHeight);

                if (!dopeline.isMasterDopeline)
                {
                    int length = dopeline.keys.Count;
                    for (int j = 0; j < length; j++)
                    {
                        AnimationWindowKeyframe keyframe = dopeline.keys[j];
                        if (m_State.KeyIsSelected(keyframe))
                        {
                            if (firstKey)
                            {
                                yMin = heightCumul;
                                firstKey = false;
                            }

                            yMax = heightCumul + dopelineHeight;
                            break;
                        }
                    }
                }

                heightCumul += dopelineHeight;
            }

            layout.summaryRect = new Rect(xMin, 0f, xMax - xMin, AnimationWindowHierarchyGUI.k_DopeSheetRowHeight);
            layout.selectionRect = new Rect(xMin, yMin, xMax - xMin, yMax - yMin);

            // Scale handles.
            if (canScaleX)
            {
                layout.scaleLeftRect = new Rect(layout.selectionRect.xMin - kScaleLeftMarginHorizontal - kScaleLeftWidth, layout.selectionRect.yMin + kScaleLeftMarginVertical, kScaleLeftWidth, layout.selectionRect.height - kScaleLeftMarginVertical * 2);
                layout.scaleRightRect = new Rect(layout.selectionRect.xMax + kScaleRightMarginHorizontal, layout.selectionRect.yMin + kScaleRightMarginVertical, kScaleRightWidth, layout.selectionRect.height - kScaleRightMarginVertical * 2);
            }
            else
            {
                layout.scaleLeftRect = g_EmptyRect;
                layout.scaleRightRect = g_EmptyRect;
            }

            if (canScaleX)
            {
                layout.leftLabelAnchor = new Vector2(layout.summaryRect.xMin - kHLabelMarginHorizontal, contentRect.yMin + kHLabelMarginVertical);
                layout.rightLabelAnchor = new Vector2(layout.summaryRect.xMax + kHLabelMarginHorizontal, contentRect.yMin + kHLabelMarginVertical);
            }
            else
            {
                layout.leftLabelAnchor = layout.rightLabelAnchor = new Vector2(layout.summaryRect.center.x + kHLabelMarginHorizontal, contentRect.yMin + kHLabelMarginVertical);
            }

            return layout;
        }

        private void DrawLabels()
        {
            if (isDragging == false)
                return;

            bool canScaleX = !Mathf.Approximately(selectionBounds.size.x, 0f);

            if (canScaleX)
            {
                GUIContent leftLabelContent = new GUIContent(string.Format("{0}", m_DopeSheetEditor.FormatTime(selectionBounds.min.x, m_State.frameRate, m_State.timeFormat)));
                GUIContent rightLabelContent = new GUIContent(string.Format("{0}", m_DopeSheetEditor.FormatTime(selectionBounds.max.x, m_State.frameRate, m_State.timeFormat)));

                Vector2 leftLabelSize = styles.dragLabel.CalcSize(leftLabelContent);
                Vector2 rightLabelSize = styles.dragLabel.CalcSize(rightLabelContent);

                EditorGUI.DoDropShadowLabel(new Rect(m_Layout.leftLabelAnchor.x - leftLabelSize.x, m_Layout.leftLabelAnchor.y, leftLabelSize.x, leftLabelSize.y), leftLabelContent, styles.dragLabel, 0.3f);
                EditorGUI.DoDropShadowLabel(new Rect(m_Layout.rightLabelAnchor.x, m_Layout.rightLabelAnchor.y, rightLabelSize.x, rightLabelSize.y), rightLabelContent, styles.dragLabel, 0.3f);
            }
            else
            {
                GUIContent labelContent = new GUIContent(string.Format("{0}", m_DopeSheetEditor.FormatTime(selectionBounds.center.x, m_State.frameRate, m_State.timeFormat)));
                Vector2 labelSize = styles.dragLabel.CalcSize(labelContent);

                EditorGUI.DoDropShadowLabel(new Rect(m_Layout.leftLabelAnchor.x, m_Layout.leftLabelAnchor.y, labelSize.x, labelSize.y), labelContent, styles.dragLabel, 0.3f);
            }
        }

        private void OnStartScale(ToolCoord pivotCoord, ToolCoord pickedCoord, Vector2 mousePos, bool rippleTime)
        {
            Bounds bounds = selectionBounds;

            m_IsDragging = true;
            m_Pivot = ToolCoordToPosition(pivotCoord, bounds);
            m_Previous = ToolCoordToPosition(pickedCoord, bounds);
            m_MouseOffset = mousePos - m_Previous;
            m_RippleTime = rippleTime;
            m_RippleTimeStart = bounds.min.x;
            m_RippleTimeEnd = bounds.max.x;

            m_State.StartLiveEdit();
        }

        private void OnScaleTime(float time)
        {
            Matrix4x4 transform;
            bool flipX;
            if (CalculateScaleTimeMatrix(m_Previous.x, time, m_MouseOffset.x, m_Pivot.x, frameRate, out transform, out flipX))
                TransformKeys(transform, flipX, false);
        }

        private void OnScaleValue(float val)
        {
            Matrix4x4 transform;
            bool flipY;
            if (CalculateScaleValueMatrix(m_Previous.y, val, m_MouseOffset.y, m_Pivot.y, out transform, out flipY))
                TransformKeys(transform, false, flipY);
        }

        private void OnEndScale()
        {
            m_State.EndLiveEdit();
            m_IsDragging = false;
        }

        internal void OnStartMove(Vector2 position, bool rippleTime)
        {
            Bounds bounds = selectionBounds;

            m_IsDragging = true;
            m_Previous = position;
            m_RippleTime = rippleTime;
            m_RippleTimeStart = bounds.min.x;
            m_RippleTimeEnd = bounds.max.x;

            m_State.StartLiveEdit();
        }

        internal void OnMove(Vector2 position)
        {
            Vector2 dv = position - m_Previous;

            Matrix4x4 transform = Matrix4x4.identity;
            transform.SetTRS(new Vector3(dv.x, dv.y, 0f), Quaternion.identity, Vector3.one);

            TransformKeys(transform, false, false);
        }

        internal void OnEndMove()
        {
            m_State.EndLiveEdit();
            m_IsDragging = false;
        }

        private void TransformKeys(Matrix4x4 matrix, bool flipX, bool flipY)
        {
            if (m_RippleTime)
                m_State.TransformRippleKeys(matrix, m_RippleTimeStart, m_RippleTimeEnd, flipX, true);
            else
                m_State.TransformSelectedKeys(matrix, flipX, flipY, true);
        }
    }
}
