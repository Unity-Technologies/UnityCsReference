// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Experimental.U2D;
using UnityEditor.Experimental.UIElements;
using UnityEngine.Experimental.UIElements;
using UnityEngine;
using UnityEditorInternal;
using UnityEngine.U2D.Interface;

namespace UnityEditor
{
    internal abstract partial class SpriteFrameModuleBase : SpriteEditorModuleBase
    {
        protected enum GizmoMode
        {
            BorderEditing,
            RectEditing
        }

        protected class Styles
        {
            public readonly GUIStyle dragdot = "U2D.dragDot";
            public readonly GUIStyle dragdotactive = "U2D.dragDotActive";
            public readonly GUIStyle createRect = "U2D.createRect";
            public readonly GUIStyle pivotdotactive = "U2D.pivotDotActive";
            public readonly GUIStyle pivotdot = "U2D.pivotDot";

            public readonly GUIStyle dragBorderdot = new GUIStyle();
            public readonly GUIStyle dragBorderDotActive = new GUIStyle();

            public readonly GUIStyle toolbar;

            public Styles()
            {
                toolbar = new GUIStyle(EditorStyles.inspectorBig);
                toolbar.margin.top = 0;
                toolbar.margin.bottom = 0;
                createRect.border = new RectOffset(3, 3, 3, 3);

                dragBorderdot.fixedHeight = 5f;
                dragBorderdot.fixedWidth = 5f;
                dragBorderdot.normal.background = EditorGUIUtility.whiteTexture;

                dragBorderDotActive.fixedHeight = dragBorderdot.fixedHeight;
                dragBorderDotActive.fixedWidth = dragBorderdot.fixedWidth;
                dragBorderDotActive.normal.background = EditorGUIUtility.whiteTexture;
            }
        }

        private static Styles s_Styles;

        protected static Styles styles
        {
            get
            {
                if (s_Styles == null)
                    s_Styles = new Styles();
                return s_Styles;
            }
        }

        private const float kInspectorWidth = 330f;
        private const float kInspectorHeight = 170;
        private const float kPivotFieldPrecision = 0.0001f;
        private float m_Zoom = 1.0f;
        private GizmoMode m_GizmoMode;

        private VisualElement m_NameElement;
        private PropertyControl<string> m_NameField;
        private VisualElement m_PositionElement;
        private PropertyControl<long> m_PositionFieldX;
        private PropertyControl<long> m_PositionFieldY;
        private PropertyControl<long> m_PositionFieldW;
        private PropertyControl<long> m_PositionFieldH;
        private PropertyControl<long> m_BorderFieldL;
        private PropertyControl<long> m_BorderFieldT;
        private PropertyControl<long> m_BorderFieldR;
        private PropertyControl<long> m_BorderFieldB;
        private EnumField m_PivotField;
        private EnumField m_PivotUnitModeField;
        private VisualElement m_CustomPivotElement;
        private PropertyControl<double> m_CustomPivotFieldX;
        private PropertyControl<double> m_CustomPivotFieldY;
        private VisualElement m_SelectedFrameInspector;

        private bool ShouldShowRectScaling()
        {
            return hasSelected && m_GizmoMode == GizmoMode.RectEditing;
        }

        private static Rect inspectorRect
        {
            get
            {
                return new Rect(
                    0, 0,
                    kInspectorWidth,
                    kInspectorHeight);
            }
        }

        private void RemoveMainUI(VisualElement mainView)
        {
            if (mainView.Contains(m_SelectedFrameInspector))
                mainView.Remove(m_SelectedFrameInspector);
            mainView.UnregisterCallback<SpriteSelectionChangeEvent>(SelectionChange);
        }

        private void UpdatePositionField(Rect rect)
        {
            m_PositionFieldX.SetValueWithoutNotify((long)selectedSpriteRect.x);
            m_PositionFieldY.SetValueWithoutNotify((long)selectedSpriteRect.y);
            m_PositionFieldW.SetValueWithoutNotify((long)selectedSpriteRect.width);
            m_PositionFieldH.SetValueWithoutNotify((long)selectedSpriteRect.height);
        }

        private void AddMainUI(VisualElement mainView)
        {
            var visualTree = EditorGUIUtility.Load("UXML/SpriteEditor/SpriteFrameModuleInspector.uxml") as VisualTreeAsset;
            m_SelectedFrameInspector = visualTree.CloneTree(null).Q("spriteFrameModuleInspector");

            m_NameElement = m_SelectedFrameInspector.Q("name");
            m_NameField = m_SelectedFrameInspector.Q<PropertyControl<string>>("spriteName");
            m_NameField.OnValueChanged((evt) =>
            {
                if (hasSelected)
                {
                    selectedSpriteName = evt.newValue;
                }
            });

            m_NameField.RegisterCallback<FocusOutEvent>((focus) =>
            {
                if (hasSelected)
                {
                    m_NameField.SetValueWithoutNotify(selectedSpriteName);
                }
            });


            m_PositionElement = m_SelectedFrameInspector.Q("position");
            m_PositionFieldX = m_PositionElement.Q<PropertyControl<long>>("positionX");
            m_PositionFieldX.OnValueChanged((evt) =>
            {
                if (hasSelected)
                {
                    var rect = selectedSpriteRect;
                    rect.x = evt.newValue;
                    selectedSpriteRect = rect;
                    UpdatePositionField(selectedSpriteRect);
                }
            });

            m_PositionFieldY = m_PositionElement.Q<PropertyControl<long>>("positionY");
            m_PositionFieldY.OnValueChanged((evt) =>
            {
                if (hasSelected)
                {
                    var rect = selectedSpriteRect;
                    rect.y = evt.newValue;
                    selectedSpriteRect = rect;
                    UpdatePositionField(selectedSpriteRect);
                }
            });

            m_PositionFieldW = m_PositionElement.Q<PropertyControl<long>>("positionW");
            m_PositionFieldW.OnValueChanged((evt) =>
            {
                if (hasSelected)
                {
                    var rect = selectedSpriteRect;
                    rect.width = evt.newValue;
                    selectedSpriteRect = rect;
                    UpdatePositionField(selectedSpriteRect);
                }
            });

            m_PositionFieldH = m_PositionElement.Q<PropertyControl<long>>("positionH");
            m_PositionFieldH.OnValueChanged((evt) =>
            {
                if (hasSelected)
                {
                    var rect = selectedSpriteRect;
                    rect.height = evt.newValue;
                    selectedSpriteRect = rect;
                    UpdatePositionField(selectedSpriteRect);
                }
            });

            var borderElement = m_SelectedFrameInspector.Q("border");
            m_BorderFieldL = borderElement.Q<PropertyControl<long>>("borderL");
            m_BorderFieldL.OnValueChanged((evt) =>
            {
                if (hasSelected)
                {
                    var border = selectedSpriteBorder;
                    border.x = evt.newValue;
                    selectedSpriteBorder = border;
                    m_BorderFieldL.SetValueWithoutNotify((long)selectedSpriteBorder.x);
                }
            });

            m_BorderFieldT = borderElement.Q<PropertyControl<long>>("borderT");
            m_BorderFieldT.OnValueChanged((evt) =>
            {
                if (hasSelected)
                {
                    var border = selectedSpriteBorder;
                    border.w = evt.newValue;
                    selectedSpriteBorder = border;
                    m_BorderFieldT.SetValueWithoutNotify((long)selectedSpriteBorder.w);
                    evt.StopPropagation();
                }
            });

            m_BorderFieldR = borderElement.Q<PropertyControl<long>>("borderR");
            m_BorderFieldR.OnValueChanged((evt) =>
            {
                if (hasSelected)
                {
                    var border = selectedSpriteBorder;
                    border.z = evt.newValue;
                    selectedSpriteBorder = border;
                    m_BorderFieldR.SetValueWithoutNotify((long)selectedSpriteBorder.z);
                }
            });

            m_BorderFieldB = borderElement.Q<PropertyControl<long>>("borderB");
            m_BorderFieldB.OnValueChanged((evt) =>
            {
                if (hasSelected)
                {
                    var border = selectedSpriteBorder;
                    border.y = evt.newValue;
                    selectedSpriteBorder = border;
                    m_BorderFieldB.SetValueWithoutNotify((long)selectedSpriteBorder.y);
                }
            });

            m_PivotField = m_SelectedFrameInspector.Q<EnumField>("pivotField");
            m_PivotField.Init(SpriteAlignment.Center);
            m_PivotField.OnValueChanged((evt) =>
            {
                if (hasSelected)
                {
                    SpriteAlignment alignment = (SpriteAlignment)evt.newValue;
                    SetSpritePivotAndAlignment(selectedSpritePivot, alignment);
                    m_CustomPivotElement.SetEnabled(selectedSpriteAlignment == SpriteAlignment.Custom);
                    Vector2 pivot = selectedSpritePivotInCurUnitMode;
                    m_CustomPivotFieldX.SetValueWithoutNotify(pivot.x);
                    m_CustomPivotFieldY.SetValueWithoutNotify(pivot.y);
                }
            });


            m_PivotUnitModeField = m_SelectedFrameInspector.Q<EnumField>("pivotUnitModeField");
            m_PivotUnitModeField.Init(PivotUnitMode.Normalized);
            m_PivotUnitModeField.OnValueChanged((evt) =>
            {
                if (hasSelected)
                {
                    m_PivotUnitMode = (PivotUnitMode)evt.newValue;

                    Vector2 pivot = selectedSpritePivotInCurUnitMode;
                    m_CustomPivotFieldX.SetValueWithoutNotify(pivot.x);
                    m_CustomPivotFieldY.SetValueWithoutNotify(pivot.y);
                }
            });


            m_CustomPivotElement = m_SelectedFrameInspector.Q("customPivot");
            m_CustomPivotFieldX = m_CustomPivotElement.Q<PropertyControl<double>>("customPivotX");
            m_CustomPivotFieldX.OnValueChanged((evt) =>
            {
                if (hasSelected)
                {
                    float newValue = (float)evt.newValue;
                    float pivotX = m_PivotUnitMode == PivotUnitMode.Pixels
                        ? ConvertFromRectToNormalizedSpace(new Vector2(newValue, 0.0f), selectedSpriteRect).x
                        : newValue;

                    var pivot = selectedSpritePivot;
                    pivot.x = pivotX;
                    SetSpritePivotAndAlignment(pivot, selectedSpriteAlignment);
                }
            });

            m_CustomPivotFieldY = m_CustomPivotElement.Q<PropertyControl<double>>("customPivotY");
            m_CustomPivotFieldY.OnValueChanged((evt) =>
            {
                if (hasSelected)
                {
                    float newValue = (float)evt.newValue;
                    float pivotY = m_PivotUnitMode == PivotUnitMode.Pixels
                        ? ConvertFromRectToNormalizedSpace(new Vector2(0.0f, newValue), selectedSpriteRect).y
                        : newValue;

                    var pivot = selectedSpritePivot;
                    pivot.y = pivotY;
                    SetSpritePivotAndAlignment(pivot, selectedSpriteAlignment);
                }
            });

            //// Force an update of all the fields.
            PopulateSpriteFrameInspectorField();

            mainView.RegisterCallback<SpriteSelectionChangeEvent>(SelectionChange);

            // Stop mouse events from reaching the main view.
            m_SelectedFrameInspector.pickingMode = PickingMode.Ignore;
            m_SelectedFrameInspector.RegisterCallback<MouseDownEvent>((e) => { e.StopPropagation(); });
            m_SelectedFrameInspector.RegisterCallback<MouseUpEvent>((e) => { e.StopPropagation(); });
            m_SelectedFrameInspector.AddToClassList("moduleWindow");
            m_SelectedFrameInspector.AddToClassList("bottomRightFloating");
            mainView.Add(m_SelectedFrameInspector);
        }

        private void SelectionChange(SpriteSelectionChangeEvent evt)
        {
            m_SelectedFrameInspector.visible = hasSelected;
            PopulateSpriteFrameInspectorField();
        }

        private void UIUndoCallback()
        {
            PopulateSpriteFrameInspectorField();
        }

        protected void PopulateSpriteFrameInspectorField()
        {
            m_SelectedFrameInspector.visible = hasSelected;
            if (!hasSelected)
                return;
            m_NameElement.SetEnabled(containsMultipleSprites);
            m_NameField.SetValueWithoutNotify(selectedSpriteName);
            m_PositionElement.SetEnabled(containsMultipleSprites);
            var spriteRect = selectedSpriteRect;
            m_PositionFieldX.SetValueWithoutNotify(Mathf.RoundToInt(spriteRect.x));
            m_PositionFieldY.SetValueWithoutNotify(Mathf.RoundToInt(spriteRect.y));
            m_PositionFieldW.SetValueWithoutNotify(Mathf.RoundToInt(spriteRect.width));
            m_PositionFieldH.SetValueWithoutNotify(Mathf.RoundToInt(spriteRect.height));
            var spriteBorder = selectedSpriteBorder;
            m_BorderFieldL.SetValueWithoutNotify(Mathf.RoundToInt(spriteBorder.x));
            m_BorderFieldT.SetValueWithoutNotify(Mathf.RoundToInt(spriteBorder.w));
            m_BorderFieldR.SetValueWithoutNotify(Mathf.RoundToInt(spriteBorder.z));
            m_BorderFieldB.SetValueWithoutNotify(Mathf.RoundToInt(spriteBorder.y));
            m_PivotField.SetValueWithoutNotify(selectedSpriteAlignment);
            m_PivotUnitModeField.SetValueWithoutNotify(m_PivotUnitMode);
            Vector2 pivot = selectedSpritePivotInCurUnitMode;
            m_CustomPivotFieldX.SetValueWithoutNotify(pivot.x);
            m_CustomPivotFieldY.SetValueWithoutNotify(pivot.y);

            m_CustomPivotElement.SetEnabled(hasSelected && selectedSpriteAlignment == SpriteAlignment.Custom);
        }

        private static Vector2 ApplySpriteAlignmentToPivot(Vector2 pivot, Rect rect, SpriteAlignment alignment)
        {
            if (alignment != SpriteAlignment.Custom)
            {
                Vector2[] snapPoints = GetSnapPointsArray(rect);
                Vector2 texturePos = snapPoints[(int)alignment];
                return ConvertFromTextureToNormalizedSpace(texturePos, rect);
            }
            return pivot;
        }

        private static Vector2 ConvertFromTextureToNormalizedSpace(Vector2 texturePos, Rect rect)
        {
            return new Vector2((texturePos.x - rect.xMin) / rect.width, (texturePos.y - rect.yMin) / rect.height);
        }

        private static Vector2 ConvertFromNormalizedToRectSpace(Vector2 normalizedPos, Rect rect)
        {
            Vector2 rectPos = new Vector2(rect.width * normalizedPos.x, rect.height * normalizedPos.y);

            // This is to combat the lack of precision formating on the UI controls.
            rectPos.x = Mathf.Round(rectPos.x / kPivotFieldPrecision) * kPivotFieldPrecision;
            rectPos.y = Mathf.Round(rectPos.y / kPivotFieldPrecision) * kPivotFieldPrecision;

            return rectPos;
        }

        private static Vector2 ConvertFromRectToNormalizedSpace(Vector2 rectPos, Rect rect)
        {
            return new Vector2(rectPos.x / rect.width, rectPos.y / rect.height);
        }

        private static Vector2[] GetSnapPointsArray(Rect rect)
        {
            Vector2[] snapPoints = new Vector2[9];
            snapPoints[(int)SpriteAlignment.TopLeft] = new Vector2(rect.xMin, rect.yMax);
            snapPoints[(int)SpriteAlignment.TopCenter] = new Vector2(rect.center.x, rect.yMax);
            snapPoints[(int)SpriteAlignment.TopRight] = new Vector2(rect.xMax, rect.yMax);
            snapPoints[(int)SpriteAlignment.LeftCenter] = new Vector2(rect.xMin, rect.center.y);
            snapPoints[(int)SpriteAlignment.Center] = new Vector2(rect.center.x, rect.center.y);
            snapPoints[(int)SpriteAlignment.RightCenter] = new Vector2(rect.xMax, rect.center.y);
            snapPoints[(int)SpriteAlignment.BottomLeft] = new Vector2(rect.xMin, rect.yMin);
            snapPoints[(int)SpriteAlignment.BottomCenter] = new Vector2(rect.center.x, rect.yMin);
            snapPoints[(int)SpriteAlignment.BottomRight] = new Vector2(rect.xMax, rect.yMin);
            return snapPoints;
        }

        protected void Repaint()
        {
            spriteEditor.RequestRepaint();
        }

        protected void HandleGizmoMode()
        {
            GizmoMode oldGizmoMode = m_GizmoMode;
            IEvent evt = eventSystem.current;
            if (evt.control)
                m_GizmoMode = GizmoMode.BorderEditing;
            else
                m_GizmoMode = GizmoMode.RectEditing;

            if (oldGizmoMode != m_GizmoMode && (evt.type == EventType.KeyDown || evt.type == EventType.KeyUp) && (evt.keyCode == KeyCode.LeftControl || evt.keyCode == KeyCode.RightControl || evt.keyCode == KeyCode.LeftAlt || evt.keyCode == KeyCode.RightAlt))
                Repaint();
        }

        protected bool MouseOnTopOfInspector()
        {
            if (hasSelected == false)
                return false;

            // GUIClip.Unclip sets the mouse position to include the windows tab.
            Vector2 mousePosition = GUIClip.Unclip(eventSystem.current.mousePosition) - (GUIClip.topmostRect.position - GUIClip.GetTopRect().position);
            return inspectorRect.Contains(mousePosition);
        }

        protected void HandlePivotHandle()
        {
            if (!hasSelected)
                return;

            EditorGUI.BeginChangeCheck();

            SpriteAlignment alignment = selectedSpriteAlignment;
            Vector2 pivot = selectedSpritePivot;
            Rect rect = selectedSpriteRect;
            pivot = ApplySpriteAlignmentToPivot(pivot, rect, alignment);
            Vector2 pivotHandlePosition = SpriteEditorHandles.PivotSlider(rect, pivot, styles.pivotdot, styles.pivotdotactive);

            if (EditorGUI.EndChangeCheck())
            {
                // Pivot snapping only happen when ctrl is press. Same as scene view snapping move
                if (eventSystem.current.control)
                    SnapPivotToSnapPoints(pivotHandlePosition, out pivot, out alignment);
                else if (m_PivotUnitMode == PivotUnitMode.Pixels)
                    SnapPivotToPixels(pivotHandlePosition, out pivot, out alignment);
                else
                {
                    pivot = pivotHandlePosition;
                    alignment = SpriteAlignment.Custom;
                }
                SetSpritePivotAndAlignment(pivot, alignment);
                PopulateSpriteFrameInspectorField();
            }
        }

        protected void HandleBorderSidePointScalingSliders()
        {
            if (!hasSelected)
                return;

            GUIStyle dragDot = styles.dragBorderdot;
            GUIStyle dragDotActive = styles.dragBorderDotActive;
            var color = new Color(0f, 1f, 0f);

            Rect rect = selectedSpriteRect;
            Vector4 border = selectedSpriteBorder;

            float left = rect.xMin + border.x;
            float right = rect.xMax - border.z;
            float top = rect.yMax - border.w;
            float bottom = rect.yMin + border.y;

            EditorGUI.BeginChangeCheck();

            float horizontal = bottom - (bottom - top) / 2;
            float vertical = left - (left - right) / 2;

            float center = horizontal;
            HandleBorderPointSlider(ref left, ref center, MouseCursor.ResizeHorizontal, false, dragDot, dragDotActive, color);

            center = horizontal;
            HandleBorderPointSlider(ref right, ref center, MouseCursor.ResizeHorizontal, false, dragDot, dragDotActive, color);

            center = vertical;
            HandleBorderPointSlider(ref center, ref top, MouseCursor.ResizeVertical, false, dragDot, dragDotActive, color);

            center = vertical;
            HandleBorderPointSlider(ref center, ref bottom, MouseCursor.ResizeVertical, false, dragDot, dragDotActive, color);

            if (EditorGUI.EndChangeCheck())
            {
                border.x = left - rect.xMin;
                border.z = rect.xMax - right;
                border.w = rect.yMax - top;
                border.y = bottom - rect.yMin;
                selectedSpriteBorder = border;
                PopulateSpriteFrameInspectorField();
            }
        }

        protected void HandleBorderCornerScalingHandles()
        {
            if (!hasSelected)
                return;

            GUIStyle dragDot = styles.dragBorderdot;
            GUIStyle dragDotActive = styles.dragBorderDotActive;
            var color = new Color(0f, 1f, 0f);

            Rect rect = selectedSpriteRect;
            Vector4 border = selectedSpriteBorder;

            float left = rect.xMin + border.x;
            float right = rect.xMax - border.z;
            float top = rect.yMax - border.w;
            float bottom = rect.yMin + border.y;

            EditorGUI.BeginChangeCheck();

            // Handle corner points, but hide them if border values are below 1
            HandleBorderPointSlider(ref left, ref top, MouseCursor.ResizeUpLeft, border.x < 1 && border.w < 1, dragDot, dragDotActive, color);
            HandleBorderPointSlider(ref right, ref top, MouseCursor.ResizeUpRight, border.z < 1 && border.w < 1, dragDot, dragDotActive, color);
            HandleBorderPointSlider(ref left, ref bottom, MouseCursor.ResizeUpRight, border.x < 1 && border.y < 1, dragDot, dragDotActive, color);
            HandleBorderPointSlider(ref right, ref bottom, MouseCursor.ResizeUpLeft, border.z < 1 && border.y < 1, dragDot, dragDotActive, color);

            if (EditorGUI.EndChangeCheck())
            {
                border.x = left - rect.xMin;
                border.z = rect.xMax - right;
                border.w = rect.yMax - top;
                border.y = bottom - rect.yMin;
                selectedSpriteBorder = border;
                PopulateSpriteFrameInspectorField();
            }
        }

        protected void HandleBorderSideScalingHandles()
        {
            if (hasSelected == false)
                return;

            Rect rect = new Rect(selectedSpriteRect);
            Vector4 border = selectedSpriteBorder;

            float left = rect.xMin + border.x;
            float right = rect.xMax - border.z;
            float top = rect.yMax - border.w;
            float bottom = rect.yMin + border.y;

            Vector2 screenRectTopLeft = Handles.matrix.MultiplyPoint(new Vector3(rect.xMin, rect.yMin));
            Vector2 screenRectBottomRight = Handles.matrix.MultiplyPoint(new Vector3(rect.xMax, rect.yMax));

            float screenRectWidth = Mathf.Abs(screenRectBottomRight.x - screenRectTopLeft.x);
            float screenRectHeight = Mathf.Abs(screenRectBottomRight.y - screenRectTopLeft.y);

            EditorGUI.BeginChangeCheck();

            left = HandleBorderScaleSlider(left, rect.yMax, screenRectWidth, screenRectHeight, true);
            right = HandleBorderScaleSlider(right, rect.yMax, screenRectWidth, screenRectHeight, true);

            top = HandleBorderScaleSlider(rect.xMin, top, screenRectWidth, screenRectHeight, false);
            bottom = HandleBorderScaleSlider(rect.xMin, bottom, screenRectWidth, screenRectHeight, false);

            if (EditorGUI.EndChangeCheck())
            {
                border.x = left - rect.xMin;
                border.z = rect.xMax - right;
                border.w = rect.yMax - top;
                border.y = bottom - rect.yMin;

                selectedSpriteBorder = border;
                PopulateSpriteFrameInspectorField();
            }
        }

        protected void HandleBorderPointSlider(ref float x, ref float y, MouseCursor mouseCursor, bool isHidden, GUIStyle dragDot, GUIStyle dragDotActive, Color color)
        {
            var originalColor = GUI.color;

            if (isHidden)
                GUI.color = new Color(0, 0, 0, 0);
            else
                GUI.color = color;

            Vector2 point = SpriteEditorHandles.PointSlider(new Vector2(x, y), mouseCursor, dragDot, dragDotActive);
            x = point.x;
            y = point.y;

            GUI.color = originalColor;
        }

        protected float HandleBorderScaleSlider(float x, float y, float width, float height, bool isHorizontal)
        {
            float handleSize = styles.dragBorderdot.fixedWidth;
            Vector2 point = Handles.matrix.MultiplyPoint(new Vector2(x, y));
            float result;

            EditorGUI.BeginChangeCheck();

            if (isHorizontal)
            {
                Rect newRect = new Rect(point.x - handleSize * .5f, point.y, handleSize, height);
                result = SpriteEditorHandles.ScaleSlider(point, MouseCursor.ResizeHorizontal, newRect).x;
            }
            else
            {
                Rect newRect = new Rect(point.x, point.y - handleSize * .5f, width, handleSize);
                result = SpriteEditorHandles.ScaleSlider(point, MouseCursor.ResizeVertical, newRect).y;
            }

            if (EditorGUI.EndChangeCheck())
                return result;

            return isHorizontal ? x : y;
        }

        protected void DrawSpriteRectGizmos()
        {
            if (eventSystem.current.type != EventType.Repaint)
                return;

            SpriteEditorUtility.BeginLines(new Color(0f, 1f, 0f, 0.7f));
            int currentSelectedSpriteIndex = CurrentSelectedSpriteIndex();
            for (int i = 0; i < spriteCount; i++)
            {
                Vector4 border = GetSpriteBorderAt(i);
                if (currentSelectedSpriteIndex != i && m_GizmoMode != GizmoMode.BorderEditing)
                {
                    if (Mathf.Approximately(border.sqrMagnitude, 0))
                        continue;
                }


                var rect = GetSpriteRectAt(i);
                SpriteEditorUtility.DrawLine(new Vector3(rect.xMin + border.x, rect.yMin), new Vector3(rect.xMin + border.x, rect.yMax));
                SpriteEditorUtility.DrawLine(new Vector3(rect.xMax - border.z, rect.yMin), new Vector3(rect.xMax - border.z, rect.yMax));

                SpriteEditorUtility.DrawLine(new Vector3(rect.xMin, rect.yMin + border.y), new Vector3(rect.xMax, rect.yMin + border.y));
                SpriteEditorUtility.DrawLine(new Vector3(rect.xMin, rect.yMax - border.w), new Vector3(rect.xMax, rect.yMax - border.w));
            }
            SpriteEditorUtility.EndLines();

            if (ShouldShowRectScaling())
            {
                Rect r = selectedSpriteRect;
                SpriteEditorUtility.BeginLines(new Color(0f, 0.1f, 0.3f, 0.25f));
                SpriteEditorUtility.DrawBox(new Rect(r.xMin + 1f / m_Zoom, r.yMin + 1f / m_Zoom, r.width, r.height));
                SpriteEditorUtility.EndLines();
                SpriteEditorUtility.BeginLines(new Color(0.25f, 0.5f, 1f, 0.75f));
                SpriteEditorUtility.DrawBox(r);
                SpriteEditorUtility.EndLines();
            }
        }

        // implements ISpriteEditorModule

        public override void DoMainGUI()
        {
            m_Zoom = Handles.matrix.GetColumn(0).magnitude;
        }

        public override void DoPostGUI()
        {
        }
    }
}
