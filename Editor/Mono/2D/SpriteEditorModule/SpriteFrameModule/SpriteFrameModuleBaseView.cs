// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditorInternal;
using UnityEngine.U2D.Interface;

namespace UnityEditor
{
    internal abstract partial class SpriteFrameModuleBase : ISpriteEditorModule
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

            public readonly GUIContent[] spriteAlignmentOptions =
            {
                EditorGUIUtility.TextContent("Center"),
                EditorGUIUtility.TextContent("Top Left"),
                EditorGUIUtility.TextContent("Top"),
                EditorGUIUtility.TextContent("Top Right"),
                EditorGUIUtility.TextContent("Left"),
                EditorGUIUtility.TextContent("Right"),
                EditorGUIUtility.TextContent("Bottom Left"),
                EditorGUIUtility.TextContent("Bottom"),
                EditorGUIUtility.TextContent("Bottom Right"),
                EditorGUIUtility.TextContent("Custom"),
            };

            public readonly GUIContent pivotLabel = EditorGUIUtility.TextContent("Pivot");

            public readonly GUIContent spriteLabel = EditorGUIUtility.TextContent("Sprite");
            public readonly GUIContent customPivotLabel = EditorGUIUtility.TextContent("Custom Pivot");

            public readonly GUIContent borderLabel = EditorGUIUtility.TextContent("Border");
            public readonly GUIContent lLabel = EditorGUIUtility.TextContent("L");
            public readonly GUIContent tLabel = EditorGUIUtility.TextContent("T");
            public readonly GUIContent rLabel = EditorGUIUtility.TextContent("R");
            public readonly GUIContent bLabel = EditorGUIUtility.TextContent("B");

            public readonly GUIContent positionLabel = EditorGUIUtility.TextContent("Position");
            public readonly GUIContent xLabel = EditorGUIUtility.TextContent("X");
            public readonly GUIContent yLabel = EditorGUIUtility.TextContent("Y");
            public readonly GUIContent wLabel = EditorGUIUtility.TextContent("W");
            public readonly GUIContent hLabel = EditorGUIUtility.TextContent("H");

            public readonly GUIContent nameLabel = EditorGUIUtility.TextContent("Name");

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

        private const float kScrollbarMargin = 16f;
        private const float kInspectorWindowMargin = 8f;
        private const float kInspectorWidth = 330f;
        private const float kInspectorHeight = 160f;

        private float m_Zoom = 1.0f;
        private GizmoMode m_GizmoMode;

        private bool ShouldShowRectScaling()
        {
            return hasSelected && m_GizmoMode == GizmoMode.RectEditing;
        }

        private void DoPivotFields()
        {
            EditorGUI.BeginChangeCheck();

            SpriteAlignment alignment = selectedSpriteAlignment;
            alignment = (SpriteAlignment)EditorGUILayout.Popup(styles.pivotLabel, (int)alignment, styles.spriteAlignmentOptions);

            Vector2 oldPivot = selectedSpritePivot;
            Vector2 newPivot = oldPivot;

            using (new EditorGUI.DisabledScope(alignment != SpriteAlignment.Custom))
            {
                Rect pivotRect = GUILayoutUtility.GetRect(kInspectorWidth - kInspectorWindowMargin, EditorGUI.GetPropertyHeight(SerializedPropertyType.Vector2, styles.customPivotLabel));
                GUI.SetNextControlName("PivotField");
                newPivot = EditorGUI.Vector2Field(pivotRect, styles.customPivotLabel, oldPivot);
            }

            if (EditorGUI.EndChangeCheck())
                SetSpritePivotAndAlignment(newPivot, alignment);
        }

        private void DoBorderFields()
        {
            EditorGUI.BeginChangeCheck();

            Vector4 oldBorder = selectedSpriteBorder;
            int x = Mathf.RoundToInt(oldBorder.x);
            int y = Mathf.RoundToInt(oldBorder.y);
            int z = Mathf.RoundToInt(oldBorder.z);
            int w = Mathf.RoundToInt(oldBorder.w);

            SpriteEditorUtility.FourIntFields(new Vector2(kInspectorWidth - kInspectorWindowMargin, EditorGUI.kSingleLineHeight * 2f + EditorGUI.kVerticalSpacingMultiField),
                styles.borderLabel,
                styles.lLabel,
                styles.tLabel,
                styles.rLabel,
                styles.bLabel,
                ref x, ref w, ref z, ref y);

            Vector4 newBorder = new Vector4(x, y, z, w);

            if (EditorGUI.EndChangeCheck())
                selectedSpriteBorder = newBorder;
        }

        private void DoPositionField()
        {
            EditorGUI.BeginChangeCheck();

            Rect oldRect = selectedSpriteRect;
            int x = Mathf.RoundToInt(oldRect.x);
            int y = Mathf.RoundToInt(oldRect.y);
            int w = Mathf.RoundToInt(oldRect.width);
            int h = Mathf.RoundToInt(oldRect.height);

            SpriteEditorUtility.FourIntFields(new Vector2(kInspectorWidth - kInspectorWindowMargin, EditorGUI.kSingleLineHeight * 2f + EditorGUI.kVerticalSpacingMultiField),
                styles.positionLabel,
                styles.xLabel,
                styles.yLabel,
                styles.wLabel,
                styles.hLabel,
                ref x, ref y, ref w, ref h);

            if (EditorGUI.EndChangeCheck())
                selectedSpriteRect = new Rect(x, y, w, h);
        }

        private void DoNameField()
        {
            EditorGUI.BeginChangeCheck();

            string oldName = selectedSpriteName;
            GUI.SetNextControlName("SpriteName");
            string newName = EditorGUILayout.TextField(styles.nameLabel, oldName);

            if (EditorGUI.EndChangeCheck())
                selectedSpriteName = newName;
        }

        private Rect inspectorRect
        {
            get
            {
                Rect position = spriteEditor.windowDimension;
                return new Rect(
                    position.width - kInspectorWidth - kInspectorWindowMargin - kScrollbarMargin,
                    position.height - kInspectorHeight - kInspectorWindowMargin - kScrollbarMargin,
                    kInspectorWidth,
                    kInspectorHeight);
            }
        }

        private void DoSelectedFrameInspector()
        {
            if (!hasSelected)
                return;

            EditorGUIUtility.wideMode = true;
            float oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 135f;

            GUILayout.BeginArea(inspectorRect);
            GUILayout.BeginVertical(styles.spriteLabel, GUI.skin.window);

            // Name and Position is set by importer in Single import mode
            using (new EditorGUI.DisabledScope(!containsMultipleSprites))
            {
                DoNameField();
                DoPositionField();
            }

            DoBorderFields();
            DoPivotFields();

            GUILayout.EndVertical();
            GUILayout.EndArea();

            EditorGUIUtility.labelWidth = oldLabelWidth;
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
                    SnapPivot(pivotHandlePosition, out pivot, out alignment);
                else
                {
                    pivot = pivotHandlePosition;
                    alignment = SpriteAlignment.Custom;
                }
                SetSpritePivotAndAlignment(pivot, alignment);
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

        public virtual void DoTextureGUI()
        {
            m_Zoom = Handles.matrix.GetColumn(0).magnitude;
        }

        public virtual void OnPostGUI()
        {
            DoSelectedFrameInspector();
        }

        public abstract void DrawToolbarGUI(Rect drawArea);
    }
}
