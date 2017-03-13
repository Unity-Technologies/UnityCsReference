// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditorInternal;

namespace UnityEditor
{
    internal class SpriteUtilityWindow : EditorWindow
    {
        protected class Styles
        {
            public readonly GUIStyle dragdot = "U2D.dragDot";
            public readonly GUIStyle dragdotDimmed = "U2D.dragDotDimmed";
            public readonly GUIStyle dragdotactive = "U2D.dragDotActive";
            public readonly GUIStyle createRect = "U2D.createRect";
            public readonly GUIStyle preToolbar = "preToolbar";
            public readonly GUIStyle preButton = "preButton";
            public readonly GUIStyle preLabel = "preLabel";
            public readonly GUIStyle preSlider = "preSlider";
            public readonly GUIStyle preSliderThumb = "preSliderThumb";
            public readonly GUIStyle preBackground = "preBackground";
            public readonly GUIStyle pivotdotactive = "U2D.pivotDotActive";
            public readonly GUIStyle pivotdot = "U2D.pivotDot";

            public readonly GUIStyle dragBorderdot = new GUIStyle();
            public readonly GUIStyle dragBorderDotActive = new GUIStyle();

            public readonly GUIStyle toolbar;
            public readonly GUIContent alphaIcon;
            public readonly GUIContent RGBIcon;
            public readonly GUIStyle notice;

            public readonly GUIContent smallMip;
            public readonly GUIContent largeMip;

            public Styles()
            {
                toolbar = new GUIStyle(EditorStyles.inspectorBig);
                toolbar.margin.top = 0;
                toolbar.margin.bottom = 0;
                alphaIcon = EditorGUIUtility.IconContent("PreTextureAlpha");
                RGBIcon = EditorGUIUtility.IconContent("PreTextureRGB");
                preToolbar.border.top = 0;
                createRect.border = new RectOffset(3, 3, 3, 3);

                notice = new GUIStyle(GUI.skin.label);
                notice.alignment = TextAnchor.MiddleCenter;
                notice.normal.textColor = Color.yellow;

                dragBorderdot.fixedHeight = 5f;
                dragBorderdot.fixedWidth = 5f;
                dragBorderdot.normal.background = EditorGUIUtility.whiteTexture;

                dragBorderDotActive.fixedHeight = dragBorderdot.fixedHeight;
                dragBorderDotActive.fixedWidth = dragBorderdot.fixedWidth;
                dragBorderDotActive.normal.background = EditorGUIUtility.whiteTexture;

                smallMip = EditorGUIUtility.IconContent("PreTextureMipMapLow");
                largeMip = EditorGUIUtility.IconContent("PreTextureMipMapHigh");
            }
        }

        protected void InitStyles()
        {
            if (m_Styles == null)
                m_Styles = new Styles();
        }

        protected Styles m_Styles;

        protected const float k_BorderMargin = 10f;
        protected const float k_ScrollbarMargin = 16f;
        protected const float k_InspectorWindowMargin = 8f;
        protected const float k_InspectorWidth = 330f;
        protected const float k_MinZoomPercentage = 0.9f;
        protected const float k_MaxZoom = 50f;
        protected const float k_WheelZoomSpeed = 0.03f;
        protected const float k_MouseZoomSpeed = 0.005f;
        protected const float k_ToolbarHeight = 17f;

        protected UnityEngine.U2D.Interface.ITexture2D m_Texture;
        protected UnityEngine.U2D.Interface.ITexture2D m_TextureAlphaOverride;
        protected Rect m_TextureViewRect;
        protected Rect m_TextureRect;

        protected bool m_ShowAlpha = false;
        protected float m_Zoom = -1f;
        protected float m_MipLevel = 0;
        protected Vector2 m_ScrollPosition = new Vector2();

        protected float GetMinZoom()
        {
            if (m_Texture == null)
                return 1.0f;
            // Case 654327: Add k_MaxZoom size to min check to ensure that min zoom is smaller than max zoom
            return Mathf.Min(m_TextureViewRect.width / m_Texture.width, m_TextureViewRect.height / m_Texture.height, k_MaxZoom) * k_MinZoomPercentage;
        }

        protected void HandleZoom()
        {
            bool zoomMode = Event.current.alt && Event.current.button == 1;
            if (zoomMode)
            {
                EditorGUIUtility.AddCursorRect(m_TextureViewRect, MouseCursor.Zoom);
            }

            if (
                ((Event.current.type == EventType.MouseUp || Event.current.type == EventType.MouseDown) && zoomMode) ||
                ((Event.current.type == EventType.KeyUp || Event.current.type == EventType.KeyDown) && Event.current.keyCode == KeyCode.LeftAlt)
                )
            {
                Repaint();
            }

            if (Event.current.type == EventType.ScrollWheel || (Event.current.type == EventType.MouseDrag && Event.current.alt && Event.current.button == 1))
            {
                float zoomMultiplier = 1f - Event.current.delta.y * (Event.current.type == EventType.ScrollWheel ? k_WheelZoomSpeed : -k_MouseZoomSpeed);

                // Clamp zoom
                float wantedZoom = m_Zoom * zoomMultiplier;

                float currentZoom = Mathf.Clamp(wantedZoom, GetMinZoom(), k_MaxZoom);

                if (currentZoom != m_Zoom)
                {
                    m_Zoom = currentZoom;

                    // We need to fix zoomMultiplier if we clamped wantedZoom != currentZoom
                    if (wantedZoom != currentZoom)
                        zoomMultiplier /= wantedZoom / currentZoom;

                    m_ScrollPosition *= zoomMultiplier;

                    // Zooming towards mouse cursor
                    float xRatio = Event.current.mousePosition.x / m_TextureViewRect.width - 0.5f;
                    float yRatio = Event.current.mousePosition.y / m_TextureViewRect.height - 0.5f;
                    float diffX = xRatio * (zoomMultiplier - 1);
                    float diffY = yRatio * (zoomMultiplier - 1);

                    Rect scrollRect = maxScrollRect;
                    m_ScrollPosition.x += (diffX * (scrollRect.width / 2.0f));
                    m_ScrollPosition.y += (diffY * (scrollRect.height / 2.0f));

                    Event.current.Use();
                }
            }
        }

        protected void HandlePanning()
        {
            // You can pan by holding ALT and using left button or NOT holding ALT and using right button. ALT + right is reserved for zooming.
            bool panMode = (!Event.current.alt && Event.current.button > 0 || Event.current.alt && Event.current.button <= 0);
            if (panMode && GUIUtility.hotControl == 0)
            {
                EditorGUIUtility.AddCursorRect(m_TextureViewRect, MouseCursor.Pan);

                if (Event.current.type == EventType.MouseDrag)
                {
                    m_ScrollPosition -= Event.current.delta;
                    Event.current.Use();
                }
            }

            //We need to repaint when entering or exiting the pan mode, so the mouse cursor gets refreshed.
            if (
                ((Event.current.type == EventType.MouseUp || Event.current.type == EventType.MouseDown) && panMode) ||
                (Event.current.type == EventType.KeyUp || Event.current.type == EventType.KeyDown)  && Event.current.keyCode == KeyCode.LeftAlt
                )
            {
                Repaint();
            }
        }

        // Bounding values for scrollbars. Changes with zoom, because we want min/max scroll to stop at texture edges.
        protected Rect maxScrollRect
        {
            get
            {
                float halfWidth = m_Texture.width * .5f * m_Zoom;
                float halfHeight = m_Texture.height * .5f * m_Zoom;
                return new Rect(-halfWidth, -halfHeight, m_TextureViewRect.width + halfWidth * 2f, m_TextureViewRect.height + halfHeight * 2f);
            }
        }

        // Max rect in texture space that can ever be visible
        protected Rect maxRect
        {
            get
            {
                float marginW = m_TextureViewRect.width * .5f / GetMinZoom();
                float marginH = m_TextureViewRect.height * .5f / GetMinZoom();
                float left = -marginW;
                float top = -marginH;
                float width = m_Texture.width + marginW * 2f;
                float height = m_Texture.height + marginH * 2f;
                return new Rect(left, top, width, height);
            }
        }

        protected void DrawTexturespaceBackground()
        {
            float size = Mathf.Max(maxRect.width, maxRect.height);
            Vector2 offset = new Vector2(maxRect.xMin, maxRect.yMin);

            float halfSize = size * .5f;
            float alpha = EditorGUIUtility.isProSkin ? 0.15f : 0.08f;
            float gridSize = 8f;

            SpriteEditorUtility.BeginLines(new Color(0f, 0f, 0f, alpha));
            for (float v = 0; v <= size; v += gridSize)
                SpriteEditorUtility.DrawLine(new Vector2(-halfSize + v, halfSize + v) + offset, new Vector2(halfSize + v, -halfSize + v) + offset);
            SpriteEditorUtility.EndLines();
        }

        private float Log2(float x)
        {
            return (float)(System.Math.Log(x) / System.Math.Log(2));
        }

        protected void DrawTexture()
        {
            int texWidth = Mathf.Max(m_Texture.width, 1);
            float mipLevel = Mathf.Min(m_MipLevel, TextureUtil.GetMipmapCount(m_Texture) - 1);

            float oldBias = m_Texture.mipMapBias;
            TextureUtil.SetMipMapBiasNoDirty(m_Texture, mipLevel - Log2(texWidth / m_TextureRect.width));
            FilterMode oldFilter = m_Texture.filterMode;
            TextureUtil.SetFilterModeNoDirty(m_Texture, FilterMode.Point);

            if (m_ShowAlpha)
            {
                // check if we have a valid alpha texture
                if (m_TextureAlphaOverride != null)
                    EditorGUI.DrawTextureTransparent(m_TextureRect, m_TextureAlphaOverride);
                // else use the original texture and display its alpha
                else
                    EditorGUI.DrawTextureAlpha(m_TextureRect, m_Texture);
            }
            else
                EditorGUI.DrawTextureTransparent(m_TextureRect, m_Texture);

            TextureUtil.SetMipMapBiasNoDirty(m_Texture, oldBias);
            TextureUtil.SetFilterModeNoDirty(m_Texture, oldFilter);
        }

        protected void DrawScreenspaceBackground()
        {
            if (Event.current.type == EventType.Repaint)
                m_Styles.preBackground.Draw(m_TextureViewRect, false, false, false, false);
        }

        protected void HandleScrollbars()
        {
            Rect horizontalScrollBarPosition = new Rect(m_TextureViewRect.xMin, m_TextureViewRect.yMax, m_TextureViewRect.width, k_ScrollbarMargin);
            m_ScrollPosition.x = GUI.HorizontalScrollbar(horizontalScrollBarPosition, m_ScrollPosition.x, m_TextureViewRect.width, maxScrollRect.xMin, maxScrollRect.xMax);

            Rect verticalScrollBarPosition = new Rect(m_TextureViewRect.xMax, m_TextureViewRect.yMin, k_ScrollbarMargin, m_TextureViewRect.height);
            m_ScrollPosition.y = GUI.VerticalScrollbar(verticalScrollBarPosition, m_ScrollPosition.y, m_TextureViewRect.height, maxScrollRect.yMin, maxScrollRect.yMax);
        }

        protected void SetupHandlesMatrix()
        {
            // Offset from top left to center in view space
            Vector3 handlesPos = new Vector3(m_TextureRect.x, m_TextureRect.yMax, 0f);
            // We flip Y-scale because Unity texture space is bottom-up
            Vector3 handlesScale = new Vector3(m_Zoom, -m_Zoom, 1f);

            // Handle matrix is for converting between view and texture space coordinates, without taking account the scroll position.
            // Scroll position is added separately so we can use it with GUIClip.
            Handles.matrix = Matrix4x4.TRS(handlesPos, Quaternion.identity, handlesScale);
        }

        protected Rect DoAlphaZoomToolbarGUI(Rect area)
        {
            int mipCount = 1;
            if (m_Texture != null)
                mipCount = Mathf.Max(mipCount, TextureUtil.GetMipmapCount(m_Texture));

            Rect drawArea = new Rect(area.width, 0, 0, area.height);
            using (new EditorGUI.DisabledScope(mipCount == 1))
            {
                drawArea.width = m_Styles.largeMip.image.width;
                drawArea.x -= drawArea.width;
                GUI.Box(drawArea, m_Styles.largeMip, m_Styles.preLabel);

                drawArea.width = EditorGUI.kSliderMinW;
                drawArea.x -= drawArea.width;
                m_MipLevel = Mathf.Round(GUI.HorizontalSlider(drawArea, m_MipLevel, mipCount - 1, 0, m_Styles.preSlider, m_Styles.preSliderThumb));

                drawArea.width = m_Styles.smallMip.image.width;
                drawArea.x -= drawArea.width;
                GUI.Box(drawArea, m_Styles.smallMip, m_Styles.preLabel);
            }

            drawArea.width = EditorGUI.kSliderMinW;
            drawArea.x -= drawArea.width;
            m_Zoom = GUI.HorizontalSlider(drawArea, m_Zoom, GetMinZoom(), k_MaxZoom, m_Styles.preSlider, m_Styles.preSliderThumb);

            drawArea.width = EditorGUI.kObjectFieldMiniThumbnailWidth;
            drawArea.x -= drawArea.width + EditorGUI.kSpacing;
            m_ShowAlpha = GUI.Toggle(drawArea, m_ShowAlpha, m_ShowAlpha ? m_Styles.alphaIcon : m_Styles.RGBIcon, "toolbarButton");

            // Returns the area that is not used
            return new Rect(area.x, area.y, drawArea.x, area.height);
        }

        protected void DoTextureGUI()
        {
            if (m_Texture == null)
                return;

            // zoom startup init
            if (m_Zoom < 0f)
                m_Zoom = GetMinZoom();

            // Texture rect in view space
            m_TextureRect = new Rect(
                    m_TextureViewRect.width / 2f - (m_Texture.width * m_Zoom / 2f),
                    m_TextureViewRect.height / 2f - (m_Texture.height * m_Zoom / 2f),
                    (m_Texture.width * m_Zoom),
                    (m_Texture.height * m_Zoom)
                    );

            HandleScrollbars();
            SetupHandlesMatrix();
            HandleZoom();
            HandlePanning();
            DrawScreenspaceBackground();

            GUIClip.Push(m_TextureViewRect, -m_ScrollPosition, Vector2.zero, false);

            if (Event.current.type == EventType.Repaint)
            {
                DrawTexturespaceBackground();
                DrawTexture();
                DrawGizmos();
            }

            DoTextureGUIExtras();

            GUIClip.Pop();
        }

        protected virtual void DoTextureGUIExtras()
        {
        }

        protected virtual void DrawGizmos()
        {
        }

        protected void SetNewTexture(Texture2D texture)
        {
            if (texture != m_Texture)
            {
                m_Texture = new UnityEngine.U2D.Interface.Texture2D(texture);
                m_Zoom = -1;
                m_TextureAlphaOverride = null;
            }
        }

        protected void SetAlphaTextureOverride(Texture2D alphaTexture)
        {
            if (alphaTexture != m_TextureAlphaOverride)
            {
                m_TextureAlphaOverride = new UnityEngine.U2D.Interface.Texture2D(alphaTexture);
                m_Zoom = -1;
            }
        }

        internal override void OnResized()
        {
            if (m_Texture != null && Event.current != null)
                HandleZoom();
        }

        internal static void DrawToolBarWidget(ref Rect drawRect, ref Rect toolbarRect, Action<Rect> drawAction)
        {
            toolbarRect.width -= drawRect.width;
            if (toolbarRect.width < 0)
                drawRect.width += toolbarRect.width;

            if (drawRect.width > 0)
                drawAction(drawRect);
        }
    } // class
}
