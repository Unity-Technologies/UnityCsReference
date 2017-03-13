// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using System.Text;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    internal class LookDevEnvironmentWindow
    {
        internal class EnvSettingsWindow
            : PopupWindowContent
        {
            public class Styles
            {
                public readonly GUIStyle    sMenuItem = "MenuItem";
                public readonly GUIStyle    sSeparator = "sv_iconselector_sep";
                public readonly GUIContent  sEnvironment = EditorGUIUtility.TextContent("Environment");
                public readonly GUIContent  sAngleOffset = EditorGUIUtility.TextContent("Angle offset|Rotate the environment");
                public readonly GUIContent  sResetEnv = EditorGUIUtility.TextContent("Reset Environment|Reset environment settings");
                public readonly GUIContent  sShadows = EditorGUIUtility.TextContent("Shadows");
                public readonly GUIContent  sShadowIntensity = EditorGUIUtility.TextContent("Shadow brightness|Shadow brightness");
                public readonly GUIContent  sShadowColor = EditorGUIUtility.TextContent("Color|Shadow color");
                public readonly GUIContent  sBrightest = EditorGUIUtility.TextContent("Set position to brightest point|Set the shadow direction to the brightest (higher value) point of the latLong map");
                public readonly GUIContent  sResetShadow = EditorGUIUtility.TextContent("Reset Shadows|Reset shadow properties");
            }

            static Styles s_Styles = null;
            public static Styles styles { get { if (s_Styles == null) s_Styles = new Styles(); return s_Styles; } }


            private CubemapInfo m_CubemapInfo;
            private LookDevView             m_LookDevView;
            private float                   kShadowSettingWidth = 200.0f;
            private float                   kShadowSettingHeight = EditorGUI.kSingleLineHeight * 10 - 3;

            public EnvSettingsWindow(LookDevView lookDevView, CubemapInfo infos)
            {
                m_LookDevView = lookDevView;
                m_CubemapInfo = infos;
            }

            public override Vector2 GetWindowSize()
            {
                return new Vector2(kShadowSettingWidth, kShadowSettingHeight);
            }

            private void DrawSeparator()
            {
                GUILayout.Space(3.0f);
                GUILayout.Label(GUIContent.none, styles.sSeparator);
            }

            public override void OnGUI(Rect rect)
            {
                GUILayout.BeginVertical();
                {
                    GUILayout.Label(styles.sEnvironment, EditorStyles.miniLabel);
                    EditorGUI.BeginChangeCheck();
                    float angleOffset = EditorGUILayout.Slider(styles.sAngleOffset, m_CubemapInfo.angleOffset % 360.0f, -360.0f, 360.0f);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(m_LookDevView.envLibrary, "Changed environment settings");
                        m_CubemapInfo.angleOffset = angleOffset;
                        m_LookDevView.envLibrary.dirty = true;
                        m_LookDevView.Repaint();
                    }

                    if (GUILayout.Button(styles.sResetEnv, EditorStyles.toolbarButton))
                    {
                        Undo.RecordObject(m_LookDevView.envLibrary, "Changed environment settings");
                        m_CubemapInfo.ResetEnvInfos();
                        m_LookDevView.envLibrary.dirty = true;
                        m_LookDevView.Repaint();
                    }

                    using (new EditorGUI.DisabledScope(!m_LookDevView.config.enableShadowCubemap))
                    {
                        DrawSeparator();
                        GUILayout.Label(styles.sShadows, EditorStyles.miniLabel);

                        EditorGUI.BeginChangeCheck();
                        float shadowIntensity = EditorGUILayout.Slider(styles.sShadowIntensity, m_CubemapInfo.shadowInfo.shadowIntensity, 0.0f, 5.0f);
                        Color shadowColor = EditorGUILayout.ColorField(styles.sShadowColor, m_CubemapInfo.shadowInfo.shadowColor);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(m_LookDevView.envLibrary, "Changed shadow settings");
                            m_CubemapInfo.shadowInfo.shadowIntensity = shadowIntensity;
                            m_CubemapInfo.shadowInfo.shadowColor = shadowColor;
                            m_LookDevView.envLibrary.dirty = true;
                            m_LookDevView.Repaint();
                        }

                        if (GUILayout.Button(styles.sBrightest, EditorStyles.toolbarButton))
                        {
                            Undo.RecordObject(m_LookDevView.envLibrary, "Changed shadow settings");
                            LookDevResources.UpdateShadowInfoWithBrightestSpot(m_CubemapInfo);
                            m_LookDevView.envLibrary.dirty = true;
                            m_LookDevView.Repaint();
                        }

                        if (GUILayout.Button(styles.sResetShadow, EditorStyles.toolbarButton))
                        {
                            Undo.RecordObject(m_LookDevView.envLibrary, "Changed shadow settings");
                            m_CubemapInfo.SetCubemapShadowInfo(m_CubemapInfo);
                            m_LookDevView.envLibrary.dirty = true;
                            m_LookDevView.Repaint();
                        }
                    }
                }
                GUILayout.EndVertical();
            }
        }

        public class Styles
        {
            public readonly GUIContent  sTitle = EditorGUIUtility.TextContent("HDRI View|Manage your list of HDRI environments.");
            public readonly GUIContent  sCloseIcon = new GUIContent(EditorGUIUtility.IconContent("LookDevClose"));
            public readonly GUIStyle    sSeparatorStyle = "sv_iconselector_sep";
            public readonly GUIStyle    sLabelStyleFirstContext = new GUIStyle(EditorStyles.miniLabel);
            public readonly GUIStyle    sLabelStyleSecondContext = new GUIStyle(EditorStyles.miniLabel);
            public readonly GUIStyle    sLabelStyleBothContext = new GUIStyle(EditorStyles.miniLabel);
            public readonly Texture     sLightTexture = EditorGUIUtility.FindTexture("LookDevLight");
            public readonly Texture     sLatlongFrameTexture = EditorGUIUtility.FindTexture("LookDevShadowFrame");
            public readonly GUIContent  sEnvControlIcon = new GUIContent(EditorGUIUtility.IconContent("LookDevPaneOption"));
            public readonly GUIContent  sDragAndDropHDRIText = EditorGUIUtility.TextContent("Drag and drop HDR panorama here.");

            public Styles()
            {
                sLabelStyleFirstContext.normal.textColor = LookDevView.m_FirstViewGizmoColor;
                sLabelStyleSecondContext.normal.textColor = LookDevView.m_SecondViewGizmoColor;
                sLabelStyleBothContext.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;

                sEnvControlIcon.tooltip = "Environment parameters";
            }
        }

        static Styles s_Styles = new Styles();
        public static Styles styles { get { if (s_Styles == null) s_Styles = new Styles(); return s_Styles; } }

        private LookDevView             m_LookDevView;

        private Vector2                 m_ScrollPosition = new Vector2(0.0f, 0.0f);
        private Rect                    m_PositionInLookDev;
        private Cubemap                 m_SelectedCubemap = null;
        private CubemapInfo             m_SelectedCubemapInfo = null;
        private CubemapInfo             m_SelectedShadowCubemapOwnerInfo = null;
        private int                     m_SelectedLightIconIndex = -1;
        private ShadowInfo              m_SelectedShadowInfo = null;
        private bool                    m_RenderOverlayThumbnailOnce = false;
        private int                     m_SelectedCubeMapOffsetIndex = -1;
        private int                     m_HoveringCubeMapIndex = -1;
        private float                   m_SelectedCubeMapOffsetValue = 0.0f;
        private Vector2                 m_SelectedPositionOffset = new Vector2(0.0f, 0.0f);
        private Rect                    m_GUIRect;
        private Rect                    m_displayRect;
        private bool                    m_DragBeingPerformed = false;

        public const float m_latLongHeight = 125.0f;
        public const float m_HDRIHeaderHeight = 18.0f; // Careful, labels/button are 16 but toggle is 18 so we put everyone at 18
        public const float m_HDRIHeight = m_HDRIHeaderHeight + m_latLongHeight + 3.0f; // Separator height
        public const float m_HDRIWidth = 250.0f;
        const float kButtonWidth = 16, kButtonHeight = 16;

        public LookDevEnvironmentWindow(LookDevView lookDevView)
        {
            m_LookDevView = lookDevView;
        }

        public void SetRects(Rect positionInLookDev, Rect GUIRect, Rect displayRect)
        {
            m_PositionInLookDev = positionInLookDev;
            m_GUIRect = GUIRect;
            m_displayRect = displayRect;
        }

        public Cubemap GetCurrentSelection()
        {
            return m_SelectedCubemap;
        }

        public Vector2 GetSelectedPositionOffset()
        {
            return m_SelectedPositionOffset;
        }

        public void CancelSelection()
        {
            m_SelectedCubemap = null;
            m_SelectedCubemapInfo = null;
            m_SelectedShadowCubemapOwnerInfo = null;
            m_SelectedLightIconIndex = -1;
            m_SelectedShadowInfo = null;
            m_HoveringCubeMapIndex = -1;
            m_DragBeingPerformed = false;
        }

        private float ComputeAngleOffsetFromMouseCoord(Vector2 mousePosition)
        {
            return mousePosition.x / m_HDRIWidth * 360.0f;
        }

        // Returns normalized coord [-1..1] from latitude/longitude ([-90..90]/[0..360])
        private Vector2 LatLongToPosition(float latitude, float longitude)
        {
            latitude = (latitude + 90.0f) % 180.0f - 90.0f;
            if (latitude < -90.0f) latitude = -90.0f;
            if (latitude > 89.0f) latitude = 89.0f;

            longitude = longitude % 360.0f;
            if (longitude < 0.0)
                longitude = 360.0f + longitude;

            Vector2 result = new Vector2(longitude * Mathf.Deg2Rad / (2.0f * Mathf.PI) * 2.0f - 1.0f, latitude * Mathf.Deg2Rad / (Mathf.PI * 0.5f));
            return result;
        }

        // Returns latitude/longtitude ([-90..90]/[0..360]) from normalized coord [-1..1]
        static public Vector2 PositionToLatLong(Vector2 position)
        {
            Vector2 result = new Vector2();
            result.x = position.y * Mathf.PI * 0.5f * Mathf.Rad2Deg;
            result.y = ((position.x * 0.5f + 0.5f) * 2.0f * Mathf.PI * Mathf.Rad2Deg);

            if (result.x < -90.0f) result.x = -90.0f;
            if (result.x > 89.0f) result.x = 89.0f;

            return result;
        }

        private Rect GetInsertionRect(int envIndex)
        {
            // Rect between each environment.
            // Used to know if user wants to insert an environment between two other.
            Rect insertionRect = m_GUIRect;
            insertionRect.height = m_HDRIHeight - m_latLongHeight;
            insertionRect.y = m_HDRIHeight * envIndex;
            return insertionRect;
        }

        private int IsPositionInInsertionArea(Vector2 pos)
        {
            // A HDRI can be insert in last position, so i goes up to count (<=)
            for (int i = 0; i <= m_LookDevView.envLibrary.hdriCount; ++i)
            {
                Rect insertionRect = GetInsertionRect(i);
                if (insertionRect.Contains(pos))
                {
                    return i;
                }
            }

            return -1;
        }

        private Rect GetThumbnailRect(int envIndex)
        {
            Rect thumbnailRect = m_GUIRect;

            thumbnailRect.height = m_latLongHeight;
            thumbnailRect.y = envIndex * m_HDRIHeight + m_HDRIHeaderHeight;
            return thumbnailRect;
        }

        private int IsPositionInThumbnailArea(Vector2 pos)
        {
            for (int i = 0; i < m_LookDevView.envLibrary.hdriCount; ++i)
            {
                Rect thumbnailRect = GetThumbnailRect(i);
                if (thumbnailRect.Contains(pos))
                {
                    return i;
                }
            }

            return -1;
        }

        private void RenderOverlayThumbnailIfNeeded()
        {
            // An environment was selected, stores the corresponding latlong texture for later overlay rendering
            if (m_RenderOverlayThumbnailOnce && Event.current.type == EventType.Repaint && m_SelectedCubemapInfo != null)
            {
                m_SelectedCubemap = m_SelectedCubemapInfo.cubemap;

                RenderTexture oldActive = RenderTexture.active;
                RenderTexture.active = LookDevResources.m_SelectionTexture;
                LookDevResources.m_LookDevCubeToLatlong.SetTexture("_MainTex", m_SelectedCubemap);
                LookDevResources.m_LookDevCubeToLatlong.SetVector("_WindowParams", new Vector4(m_displayRect.height, -1000.0f, 2, 1.0f)); // Doesn't matter but let's match DrawLatLongThumbnail settings,-1000.0f to be sure to not have clipping issue (we should not clip normally but don't want to create a new shader)
                LookDevResources.m_LookDevCubeToLatlong.SetVector("_CubeToLatLongParams", new Vector4(Mathf.Deg2Rad * m_SelectedCubemapInfo.angleOffset, 0.5f, 1.0f, 0.0f));
                LookDevResources.m_LookDevCubeToLatlong.SetPass(0);
                GL.sRGBWrite = (QualitySettings.activeColorSpace == ColorSpace.Linear);
                LookDevView.DrawFullScreenQuad(new Rect(0, 0, m_HDRIWidth, m_latLongHeight));
                GL.sRGBWrite = false;
                RenderTexture.active = oldActive;

                m_RenderOverlayThumbnailOnce = false;
            }
        }

        private void DrawLatLongThumbnail(CubemapInfo infos, float angleOffset, float intensity, float alpha, Rect textureRect)
        {
            // We need texture rect to clip correctly the Graphics.DrawTexture done for library GUI (otherwise it goes out the window)
            // But we can't use scissor rect here to clip the texture when outside the draw area because SetScissorRect in script only work if we are
            // in C++ code that have scissor enable, which is not the case here. We will clip in shader instead.
            // For this we will use the VPOS register and the y position of the widget to clip (see LookDevCubeToLatlong.shader).
            // To get current position y we need to take into account the tab panel + the menu panel.  Menu panel size is in m_PositionInLookDev.y
            // But tabsize panel is in DockAreas.cs: BeginOffsetArea (new Rect (r.x + 2, r.y + kTabHeight, r.width - 4, r.height - kTabHeight - 2), GUIContent.none, "TabWindowBackground");
            // This part exactly:  r.y + kTabHeight. r.y seems to always be 2 so use that for now (setup in z) and is store in ((GUIStyle)("dockarea"))overlay.margin.top
            GUIStyle overlay = "dockarea";
            LookDevResources.m_LookDevCubeToLatlong.SetVector("_WindowParams", new Vector4(m_displayRect.height, m_PositionInLookDev.y + DockArea.kTabHeight, overlay.margin.top, EditorGUIUtility.pixelsPerPoint));
            LookDevResources.m_LookDevCubeToLatlong.SetVector("_CubeToLatLongParams", new Vector4(Mathf.Deg2Rad * angleOffset, alpha, intensity, 0.0f));

            GL.sRGBWrite = (QualitySettings.activeColorSpace == ColorSpace.Linear);
            Graphics.DrawTexture(textureRect, infos.cubemap, LookDevResources.m_LookDevCubeToLatlong, 1);
            GL.sRGBWrite = false;
        }

        private void DrawSelectionFeedback(Rect textureRect, Color selectionColor1, Color selectionColor2)
        {
            // In case the environment is currently used in the look dev view
            // Draw a feedback rectangle around the image.
            // Two half rectangle : blue + blue / orange + orange / blue + orange depending on which side the environment is on.
            float xMin = 0.5f;
            float xMax = textureRect.width - 0.5f;
            float yMin = textureRect.y + 0.5f;
            float yMax = textureRect.y + textureRect.height - 1.0f;
            float xHalf = textureRect.width * 0.5f;

            Vector3[] points =
            {
                new Vector3(xHalf, yMin, 0.0f), new Vector3(xMin, yMin, 0.0f),
                new Vector3(xMin, yMin, 0.0f), new Vector3(xMin, yMax, 0.0f),
                new Vector3(xMin, yMax, 0.0f), new Vector3(xHalf, yMax, 0.0f)
            };

            Vector3[] points2 =
            {
                new Vector3(xHalf, yMin, 0.0f), new Vector3(xMax, yMin, 0.0f),
                new Vector3(xMax, yMin, 0.0f), new Vector3(xMax, yMax, 0.0f),
                new Vector3(xMax, yMax, 0.0f), new Vector3(xHalf, yMax, 0.0f)
            };

            Handles.color = selectionColor1;
            Handles.DrawLines(points);

            Handles.color = selectionColor2;
            Handles.DrawLines(points2);
        }

        // If there is a currently active selection drag and drop move that originate from a shadow cubemap this call will remove the reference
        // Public to be call by LookDevView
        // Must be call before any insertion or modification of HDRI list
        public void ResetShadowCubemap()
        {
            if (m_SelectedShadowCubemapOwnerInfo != null)
            {
                m_SelectedShadowCubemapOwnerInfo.SetCubemapShadowInfo(m_SelectedShadowCubemapOwnerInfo);
            }
        }

        private void HandleMouseInput()
        {
            List<CubemapInfo> cubemapList = m_LookDevView.envLibrary.hdriList;

            Vector2 scrollFixedPosition = new Vector2(Event.current.mousePosition.x, Event.current.mousePosition.y + m_ScrollPosition.y);

            Event evt = Event.current;
            switch (evt.GetTypeForControl(m_LookDevView.hotControl))
            {
                // Update overlay position for next repaint event
                case EventType.MouseDrag:
                {
                    if (m_SelectedCubeMapOffsetIndex != -1)
                    {
                        Undo.RecordObject(m_LookDevView.envLibrary, "");
                        CubemapInfo info = cubemapList[m_SelectedCubeMapOffsetIndex];
                        info.angleOffset = ComputeAngleOffsetFromMouseCoord(scrollFixedPosition) + m_SelectedCubeMapOffsetValue;
                        m_LookDevView.envLibrary.dirty = true;
                        Event.current.Use();
                    }

                    if (m_SelectedCubemapInfo != null)
                    {
                        if (IsPositionInInsertionArea(scrollFixedPosition) == -1)
                            m_HoveringCubeMapIndex = IsPositionInThumbnailArea(scrollFixedPosition);
                        else
                            m_HoveringCubeMapIndex = -1;
                    }

                    m_LookDevView.Repaint();
                    break;
                }

                // Handles environment drop
                case EventType.MouseUp:
                {
                    if (m_SelectedCubemap != null)
                    {
                        // The rect needs to include an extra slot when moving to last position
                        Rect extendedGUIRect = m_GUIRect;
                        extendedGUIRect.yMax += EditorGUI.kSingleLineHeight;

                        if (extendedGUIRect.Contains(Event.current.mousePosition))
                        {
                            int insertionRectIndex = IsPositionInInsertionArea(scrollFixedPosition);
                            if (insertionRectIndex != -1)
                            {
                                // Must be called before we do any modification to HDRI list
                                ResetShadowCubemap();

                                m_LookDevView.envLibrary.InsertHDRI(m_SelectedCubemap, insertionRectIndex);
                            }
                            else
                            {
                                int thumbnailRectIndex = IsPositionInThumbnailArea(scrollFixedPosition);
                                if (thumbnailRectIndex != -1 && m_LookDevView.config.enableShadowCubemap)
                                {
                                    Undo.RecordObject(m_LookDevView.envLibrary, "Update shadow cubemap");
                                    CubemapInfo cubemapInfo = m_LookDevView.envLibrary.hdriList[thumbnailRectIndex];

                                    // We don't want the user to drop a cubemap on itself and reset the shadows (it would happen all the time by mistake)
                                    if (cubemapInfo != m_SelectedCubemapInfo)
                                    {
                                        cubemapInfo.SetCubemapShadowInfo(m_SelectedCubemapInfo);
                                    }
                                    m_LookDevView.envLibrary.dirty = true;
                                }
                            }
                            CancelSelection();
                        }
                    }

                    m_LookDevView.Repaint();

                    if (m_SelectedCubeMapOffsetIndex != -1)
                    {
                        // Fall back to zero when near the center
                        if (Mathf.Abs(cubemapList[m_SelectedCubeMapOffsetIndex].angleOffset) <= 10.0f)
                        {
                            Undo.RecordObject(m_LookDevView.envLibrary, "");
                            cubemapList[m_SelectedCubeMapOffsetIndex].angleOffset = 0.0f;
                            m_LookDevView.envLibrary.dirty = true;
                        }
                    }
                    m_SelectedCubemapInfo = null;
                    m_SelectedShadowCubemapOwnerInfo = null;
                    m_SelectedLightIconIndex = -1;
                    m_SelectedShadowInfo = null;
                    m_SelectedCubeMapOffsetIndex = -1;
                    m_HoveringCubeMapIndex = -1;
                    m_SelectedCubeMapOffsetValue = 0.0f;

                    GUIUtility.hotControl = 0;

                    break;
                }

                // Escape closes the window
                case EventType.KeyDown:
                {
                    if (Event.current.keyCode == KeyCode.Escape)
                    {
                        CancelSelection();
                        m_LookDevView.Repaint();
                    }
                    break;
                }

                case EventType.DragPerform:
                {
                    int insertionIndex = IsPositionInInsertionArea(scrollFixedPosition);

                    foreach (UnityEngine.Object o in DragAndDrop.objectReferences)
                    {
                        Cubemap cubemap = o as Cubemap;
                        if (cubemap)
                        {
                            // When insertion outside the list the index is -1 which mean in InsertHDRI that it will be add at the end
                            m_LookDevView.envLibrary.InsertHDRI(cubemap, insertionIndex);
                        }
                    }

                    DragAndDrop.AcceptDrag();
                    m_DragBeingPerformed = false;
                    evt.Use();
                    break;
                }
                case EventType.DragUpdated:
                {
                    bool hasCubemap = false;
                    foreach (UnityEngine.Object o in DragAndDrop.objectReferences)
                    {
                        Cubemap cubemap = o as Cubemap;
                        if (cubemap)
                        {
                            hasCubemap = true;
                        }
                    }
                    DragAndDrop.visualMode = hasCubemap ? DragAndDropVisualMode.Link : DragAndDropVisualMode.Rejected;
                    if (hasCubemap)
                    {
                        m_DragBeingPerformed = true;
                    }
                    evt.Use();
                }
                break;
                case EventType.DragExited:
                    break;
                case EventType.Repaint:

                    if (m_SelectedCubeMapOffsetIndex != -1)
                    {
                        EditorGUIUtility.AddCursorRect(m_displayRect, MouseCursor.SlideArrow);
                    }
                    break;
            }
        }

        void GetFrameAndShadowTextureRect(Rect textureRect, out Rect frameTextureRect, out Rect shadowTextureRect)
        {
            frameTextureRect = textureRect;
            frameTextureRect.x += (textureRect.width - styles.sLatlongFrameTexture.width);
            frameTextureRect.y += (textureRect.height - styles.sLatlongFrameTexture.height * 1.05f);
            frameTextureRect.width = styles.sLatlongFrameTexture.width;
            frameTextureRect.height = styles.sLatlongFrameTexture.height;

            // These are hard coded values depending on the LatLongFrame texture and provided by the artist.
            shadowTextureRect = frameTextureRect;
            shadowTextureRect.x += 6.0f;
            shadowTextureRect.y += 4.0f;
            shadowTextureRect.width = 105.0f;
            shadowTextureRect.height = 52.0f;
        }

        public void OnGUI(int windowID)
        {
            if (m_LookDevView == null)
                return;

            List<CubemapInfo> cubemapList = m_LookDevView.envLibrary.hdriList;

            // Enable the ScrollView component only if there is enough HDRI (else HDRI will display on top of a scrollbar when moved)
            bool drawScrollBar = LookDevEnvironmentWindow.m_HDRIHeight * cubemapList.Count > m_PositionInLookDev.height;

            if (drawScrollBar)
            {
                m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);
            }
            else
            {
                m_ScrollPosition = new Vector2(0.0f, 0.0f);
            }

            if (cubemapList.Count == 1)
            {
                // Draw text
                Color oldColor = GUI.color;
                GUI.color = Color.gray;
                Vector2 textSize = GUI.skin.label.CalcSize(styles.sDragAndDropHDRIText);
                Rect labelRect = new Rect(m_PositionInLookDev.width * .5f - textSize.x * .5f, m_PositionInLookDev.height * .5f - textSize.y * .5f, textSize.x, textSize.y);
                GUI.Label(labelRect, styles.sDragAndDropHDRIText);
                GUI.color = oldColor;
            }

            {
                for (int i = 0; i < cubemapList.Count; ++i)
                {
                    CubemapInfo infos = cubemapList[i];
                    ShadowInfo shadowInfos = infos.shadowInfo;

                    int firstSelectionIndex = m_LookDevView.config.GetIntProperty(LookDevProperty.HDRI, LookDevEditionContext.Left);
                    int secondSelectionIndex = m_LookDevView.config.GetIntProperty(LookDevProperty.HDRI, LookDevEditionContext.Right);

                    // Disable the drawing of second selection if we are in single view
                    if (m_LookDevView.config.lookDevMode == LookDevMode.Single1 || m_LookDevView.config.lookDevMode == LookDevMode.Single2)
                    {
                        secondSelectionIndex = -1;
                    }

                    bool isSelection = (i == firstSelectionIndex || i == secondSelectionIndex);

                    Color selectionColor1 = Color.black;
                    Color selectionColor2 = Color.black;
                    GUIStyle selectionLabelStyle = EditorStyles.miniLabel;

                    if (isSelection)
                    {
                        if (i == firstSelectionIndex)
                        {
                            selectionColor1 = LookDevView.m_FirstViewGizmoColor;
                            selectionColor2 = LookDevView.m_FirstViewGizmoColor;
                            selectionLabelStyle = styles.sLabelStyleFirstContext;
                        }
                        else if (i == secondSelectionIndex)
                        {
                            selectionColor1 = LookDevView.m_SecondViewGizmoColor;
                            selectionColor2 = LookDevView.m_SecondViewGizmoColor;
                            selectionLabelStyle = styles.sLabelStyleSecondContext;
                        }
                        if (firstSelectionIndex == secondSelectionIndex)
                        {
                            selectionColor1 = LookDevView.m_FirstViewGizmoColor;
                            selectionColor2 = LookDevView.m_SecondViewGizmoColor;
                            selectionLabelStyle = styles.sLabelStyleBothContext;
                        }
                    }

                    Rect textureRect;
                    Rect lightIconRect;
                    Rect lightIconSelectionRect;
                    Rect frameTextureRect;
                    Rect shadowTextureRect;
                    GUILayout.BeginVertical(GUILayout.Width(m_HDRIWidth));
                    {
                        // Find index of current selection if it exist
                        int selectedCubeMapIndex = cubemapList.FindIndex(x => x == m_SelectedCubemapInfo);

                        // User is dragging another environment, we need to show the space for insertion
                        if ((m_SelectedCubemap != null || m_DragBeingPerformed) && GetInsertionRect(i).Contains(Event.current.mousePosition)
                            // Following test allow to not propose a slot that is neutral, i.e current position or next one
                            && (((selectedCubeMapIndex - i) != 0 && (selectedCubeMapIndex - i) != -1) || selectedCubeMapIndex == -1)
                            )
                        {
                            GUILayout.Label(GUIContent.none, styles.sSeparatorStyle);
                            GUILayoutUtility.GetRect(m_HDRIWidth, EditorGUI.kSingleLineHeight);
                        }

                        // Header for one HDRI: label + remove button
                        GUILayout.Label(GUIContent.none, styles.sSeparatorStyle);
                        GUILayout.BeginHorizontal(GUILayout.Width(m_HDRIWidth), GUILayout.Height(m_HDRIHeaderHeight));
                        {
                            StringBuilder sb = new StringBuilder();
                            sb.Append(i.ToString());
                            sb.Append(" - ");
                            sb.Append(infos.cubemap.name);

                            GUILayout.Label(sb.ToString(), selectionLabelStyle, GUILayout.Height(m_HDRIHeaderHeight), GUILayout.MaxWidth(m_HDRIWidth - 75));

                            GUILayout.FlexibleSpace();

                            if (GUILayout.Button(styles.sEnvControlIcon, LookDevView.styles.sToolBarButton))
                            {
                                Rect rect = GUILayoutUtility.topLevel.GetLast();
                                PopupWindow.Show(rect, new EnvSettingsWindow(m_LookDevView, infos));
                                GUIUtility.ExitGUI();
                            }

                            using (new EditorGUI.DisabledScope(infos.cubemap == LookDevResources.m_DefaultHDRI))
                            {
                                if (GUILayout.Button(styles.sCloseIcon, LookDevView.styles.sToolBarButton))
                                {
                                    m_LookDevView.envLibrary.RemoveHDRI(infos.cubemap);
                                }
                            }
                        }
                        GUILayout.EndHorizontal();

                        // We don't want to handle any control inside the label, following code disable mouseDown event in this case.
                        Rect lastRect = GUILayoutUtility.GetLastRect();
                        if (Event.current.type == EventType.MouseDown && lastRect.Contains(Event.current.mousePosition))
                        {
                            Event.current.Use(); // Avoid camera movement and focus change in main view
                        }

                        textureRect = GUILayoutUtility.GetRect(m_HDRIWidth, m_latLongHeight);
                        textureRect.width = m_HDRIWidth + 3; // Sometimes GUILayoutUtility.GetRect returns width as m_HDRIWidth, sometimes at m_HDRIWidth + 3 ...

                        float iconSize = 24.0f;
                        float iconHalfSize = iconSize * 0.5f;

                        // Convert from latlong to normalized coordinate to pixel coordinates.
                        float latitude = shadowInfos.latitude;
                        float longitude = shadowInfos.longitude;
                        Vector2 iconPosition = LatLongToPosition(latitude, longitude + infos.angleOffset) * 0.5f + new Vector2(0.5f, 0.5f);

                        lightIconRect = textureRect;
                        lightIconRect.x = lightIconRect.x + iconPosition.x * textureRect.width - iconHalfSize;
                        lightIconRect.y = lightIconRect.y + (1.0f - iconPosition.y) * textureRect.height - iconHalfSize; // Coordinates start from the bottom
                        lightIconRect.width = iconSize;
                        lightIconRect.height = iconSize;

                        // Selection rect of light is a bit smaller than the texture
                        lightIconSelectionRect = textureRect;
                        lightIconSelectionRect.x = lightIconSelectionRect.x + iconPosition.x * textureRect.width - iconHalfSize * 0.5f;
                        lightIconSelectionRect.y = lightIconSelectionRect.y + (1.0f - iconPosition.y) * textureRect.height - iconHalfSize * 0.5f; // Coordinates start from the bottom
                        lightIconSelectionRect.width = iconSize * 0.5f;
                        lightIconSelectionRect.height = iconSize * 0.5f;

                        GetFrameAndShadowTextureRect(textureRect, out frameTextureRect, out shadowTextureRect);

                        if (m_LookDevView.config.enableShadowCubemap)
                        {
                            EditorGUIUtility.AddCursorRect(lightIconSelectionRect, MouseCursor.Pan);
                        }

                        if (Event.current.type == EventType.MouseDown && textureRect.Contains(Event.current.mousePosition))
                        {
                            // Left Button
                            if ((!Event.current.control && Event.current.button == 0) && m_SelectedCubeMapOffsetIndex == -1)
                            {
                                // Light icon handling - it is higher priority then shadowTexture thumbnail selection
                                if (m_LookDevView.config.enableShadowCubemap && lightIconSelectionRect.Contains(Event.current.mousePosition))
                                {
                                    m_SelectedLightIconIndex = i;
                                    m_SelectedShadowInfo = shadowInfos;

                                    // We want to avoid to record object when draging the light icon, so save location here.
                                    // However the record need to be aware about what will change, so we perform an insignificant modification
                                    // to the lat/long position so it is record by the undo
                                    Undo.RecordObject(m_LookDevView.envLibrary, "Light Icon selection");
                                    m_SelectedShadowInfo.latitude = m_SelectedShadowInfo.latitude + 0.0001f;
                                    m_SelectedShadowInfo.longitude = m_SelectedShadowInfo.longitude + 0.0001f;
                                }

                                // Environment selection handling
                                if (m_SelectedShadowInfo == null)
                                {
                                    Rect resetShadowIconRect;
                                    // All values are taken from the LookDevShadowFrame.png (hard coded)
                                    resetShadowIconRect = frameTextureRect;
                                    resetShadowIconRect.x = resetShadowIconRect.x + 100.0f;
                                    resetShadowIconRect.y = resetShadowIconRect.y + 4.0f;
                                    resetShadowIconRect.width = 11;
                                    resetShadowIconRect.height = 11f;

                                    // Close icon handling
                                    if (m_LookDevView.config.enableShadowCubemap && resetShadowIconRect.Contains(Event.current.mousePosition))
                                    {
                                        // Reset Shadow Cubemap
                                        Undo.RecordObject(m_LookDevView.envLibrary, "Update shadow cubemap");
                                        cubemapList[i].SetCubemapShadowInfo(cubemapList[i]);
                                        m_LookDevView.envLibrary.dirty = true;
                                    }
                                    else
                                    {
                                        // If we have selected the shadowTexture, let's swap the index of the current selected map
                                        if (m_LookDevView.config.enableShadowCubemap && shadowTextureRect.Contains(Event.current.mousePosition))
                                        {
                                            m_SelectedShadowCubemapOwnerInfo = cubemapList[i];
                                            // Current selected cubemap is the one without sun
                                            m_SelectedCubemapInfo = m_SelectedShadowCubemapOwnerInfo.cubemapShadowInfo;
                                        }
                                        else
                                        {
                                            m_SelectedCubemapInfo = cubemapList[i];
                                        }
                                        m_SelectedPositionOffset = Event.current.mousePosition - new Vector2(textureRect.x, textureRect.y);
                                        m_RenderOverlayThumbnailOnce = true;
                                    }
                                }
                            }
                            // Left button with Ctrl - Rotate environment
                            else if ((Event.current.control && Event.current.button == 0) && m_SelectedCubemapInfo == null && m_SelectedShadowInfo == null)
                            {
                                m_SelectedCubeMapOffsetIndex = i;
                                m_SelectedCubeMapOffsetValue = infos.angleOffset - ComputeAngleOffsetFromMouseCoord(Event.current.mousePosition);
                            }

                            GUIUtility.hotControl = m_LookDevView.hotControl;

                            Event.current.Use(); // Avoid camera movement and focus change in main view
                        }


                        if (Event.current.GetTypeForControl(m_LookDevView.hotControl) == EventType.MouseDrag)
                        {
                            if (m_SelectedShadowInfo == shadowInfos && m_SelectedLightIconIndex == i)
                            {
                                Vector2 newLightPosition = Event.current.mousePosition;
                                newLightPosition.x = (newLightPosition.x - textureRect.x) / textureRect.width * 2.0f - 1.0f;
                                newLightPosition.y = (1.0f - (newLightPosition.y - textureRect.y) / textureRect.height) * 2.0f - 1.0f;

                                Vector2 newLatLongPos = PositionToLatLong(newLightPosition);
                                m_SelectedShadowInfo.latitude = newLatLongPos.x;
                                m_SelectedShadowInfo.longitude = newLatLongPos.y - infos.angleOffset;

                                m_LookDevView.envLibrary.dirty = true;
                            }
                        }

                        if (Event.current.type == EventType.Repaint)
                        {
                            // Draw the latlong thumbnail
                            DrawLatLongThumbnail(infos, infos.angleOffset, 1.0f, 1.0f, textureRect);

                            if (m_LookDevView.config.enableShadowCubemap)
                            {
                                // Draw the shadow cubemap thumbnail if either:
                                if ((infos.cubemapShadowInfo != infos) ||       // Shadows are enabled on this environment and shadow cubemap is not self
                                    (m_HoveringCubeMapIndex == i && m_SelectedCubemapInfo != infos)     // user is dragging over the environment that is not itself
                                    )
                                {
                                    // By default, we want to display the shadow cubemap associated with the current environment
                                    CubemapInfo cubemapShadowInfo = infos.cubemapShadowInfo;
                                    // If we are dragging another environment we want to display instead of the current one unless we are dragging a cubemap over itself
                                    if (m_HoveringCubeMapIndex == i && m_SelectedCubemapInfo != infos)
                                    {
                                        cubemapShadowInfo = m_SelectedCubemapInfo;
                                    }

                                    float alpha = 1.0f;
                                    if (m_SelectedShadowInfo == shadowInfos) // We need to fade the thumbnail almost completely to see where we move the light
                                    {
                                        alpha = 0.1f;
                                    }
                                    else if (m_HoveringCubeMapIndex == i && m_SelectedCubemapInfo != infos && infos.cubemapShadowInfo != m_SelectedCubemapInfo) // Visual transparent feedback to show where you are going to drop your cubemap
                                    {
                                        alpha = 0.5f;
                                    }

                                    DrawLatLongThumbnail(cubemapShadowInfo, infos.angleOffset, 0.3f, alpha, shadowTextureRect);

                                    GL.sRGBWrite = (QualitySettings.activeColorSpace == ColorSpace.Linear);
                                    GUI.DrawTexture(frameTextureRect, styles.sLatlongFrameTexture);
                                    GL.sRGBWrite = false;
                                }

                                GL.sRGBWrite = (QualitySettings.activeColorSpace == ColorSpace.Linear);
                                GUI.DrawTexture(lightIconRect, styles.sLightTexture);
                                GL.sRGBWrite = false;
                            }

                            if (isSelection)
                            {
                                DrawSelectionFeedback(textureRect, selectionColor1, selectionColor2);
                            }
                        }
                    }
                    GUILayout.EndVertical();
                }

                // Last vertical slot when drag and drop HDRI
                GUILayout.BeginVertical(GUILayout.Width(m_HDRIWidth));
                {
                    // User is dragging another environment, we need to show the space for insertion
                    if ((m_SelectedCubemap != null || m_DragBeingPerformed) && GetInsertionRect(cubemapList.Count).Contains(Event.current.mousePosition))
                    {
                        GUILayout.Label(GUIContent.none, styles.sSeparatorStyle);
                        GUILayoutUtility.GetRect(m_HDRIWidth, EditorGUI.kSingleLineHeight);
                        GUILayout.Label(GUIContent.none, styles.sSeparatorStyle);
                    }
                }
                GUILayout.EndVertical();
            }

            if (drawScrollBar)
            {
                EditorGUILayout.EndScrollView();
            }

            HandleMouseInput();
            RenderOverlayThumbnailIfNeeded();

            if (Event.current.type == EventType.Repaint)
            {
                if (m_SelectedCubemap != null)
                {
                    m_LookDevView.Repaint();
                }
            }
        }
    }
}
