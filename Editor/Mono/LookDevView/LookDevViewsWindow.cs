// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    internal class LookDevViewsWindow
        : PopupWindowContent
    {
        public class Styles
        {
            public readonly GUIStyle sMenuItem      = "MenuItem";
            public readonly GUIStyle sHeaderStyle   = EditorStyles.miniLabel;
            public readonly GUIStyle sToolBarButton = "toolbarbutton";

            public readonly GUIContent sTitle       = EditorGUIUtility.TextContent("Views");
            public readonly GUIContent sExposure    = EditorGUIUtility.TextContent("EV|Exposure value: control the brightness of the environment.");
            public readonly GUIContent sEnvironment = EditorGUIUtility.TextContent("Environment|Select an environment from the list of currently available environments");
            public readonly GUIContent sRotation    = EditorGUIUtility.TextContent("Rotation|Change the rotation of the environment");
            public readonly GUIContent sZero        = EditorGUIUtility.TextContent("0");
            public readonly GUIContent sLoD         = EditorGUIUtility.TextContent("LoD|Choose displayed LoD");
            public readonly GUIContent sLoDAuto     = EditorGUIUtility.TextContent("LoD (auto)|Choose displayed LoD");
            public readonly GUIContent sShadingMode = EditorGUIUtility.TextContent("Shading|Select shading mode");

            public readonly GUIContent[] sViewTitle =
            {
                EditorGUIUtility.TextContent("Main View (1)"),
                EditorGUIUtility.TextContent("Second View (2)"),
            };

            public readonly GUIStyle[] sViewTitleStyles =
            {
                new GUIStyle(EditorStyles.miniLabel),
                new GUIStyle(EditorStyles.miniLabel)
            };

            public readonly string[] sShadingModeStrings = { "Shaded", "Shaded Wireframe", "Albedo", "Specular", "Smoothness", "Normal" };
            public readonly int[] sShadingModeValues = { (int)DrawCameraMode.Normal, (int)DrawCameraMode.TexturedWire, (int)DrawCameraMode.DeferredDiffuse, (int)DrawCameraMode.DeferredSpecular, (int)DrawCameraMode.DeferredSmoothness, (int)DrawCameraMode.DeferredNormal };

            public readonly GUIContent sLinkActive = EditorGUIUtility.IconContent("LookDevMirrorViewsActive", "Link|Links the property between the different views");
            public readonly GUIContent sLinkInactive = EditorGUIUtility.IconContent("LookDevMirrorViewsInactive", "Link|Links the property between the different views");


            public Styles()
            {
                sViewTitleStyles[0].normal.textColor = LookDevView.m_FirstViewGizmoColor;
                sViewTitleStyles[1].normal.textColor = LookDevView.m_SecondViewGizmoColor;
            }
        }

        GUIContent GetGUIContentLink(bool active)
        {
            return active ? styles.sLinkActive : styles.sLinkInactive;
        }

        static Styles s_Styles = new Styles();
        public static Styles styles { get { return s_Styles; } }

        static float kIconSize = 32;
        static float kLabelWidth = 120.0f;
        static float kSliderWidth = 100.0f;
        static float kSliderFieldWidth = 30.0f;
        static float kSliderFieldPadding = 5.0f;
        static float kLineHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        float m_WindowHeight = 5 * kLineHeight + EditorGUIUtility.standardVerticalSpacing;
        float m_WindowWidth = kLabelWidth + kSliderWidth + kSliderFieldWidth + kSliderFieldPadding + 5.0f;


        private readonly LookDevView m_LookDevView;

        public LookDevViewsWindow(LookDevView lookDevView)
        {
            m_LookDevView = lookDevView;
        }

        private bool NeedLoD()
        {
            return m_LookDevView.config.GetObjectLoDCount(LookDevEditionContext.Left) > 1 || m_LookDevView.config.GetObjectLoDCount(LookDevEditionContext.Right) > 1;
        }

        private float GetHeight()
        {
            float height = m_WindowHeight;
            if (NeedLoD())
            {
                height += kLineHeight;
            }

            return height;
        }

        public override Vector2 GetWindowSize()
        {
            float width = m_WindowWidth + ((m_LookDevView.config.lookDevMode == LookDevMode.Single1 || m_LookDevView.config.lookDevMode == LookDevMode.Single2) ? 0 : (m_WindowWidth + kIconSize));
            return new Vector2(width, GetHeight());
        }

        public override void OnGUI(Rect rect)
        {
            if (m_LookDevView.config == null)
                return;

            Rect drawPos = new Rect(0, 0, rect.width, GetHeight());

            DrawOneView(drawPos, (m_LookDevView.config.lookDevMode == LookDevMode.Single2) ? LookDevEditionContext.Right : LookDevEditionContext.Left);

            drawPos.x += m_WindowWidth;

            drawPos.x += kIconSize;
            DrawOneView(drawPos, LookDevEditionContext.Right);

            // Use mouse move so we get hover state correctly in the menu item rows
            if (Event.current.type == EventType.MouseMove)
                Event.current.Use();

            // Escape closes the window
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                editorWindow.Close();
                GUIUtility.ExitGUI();
            }
        }

        private void DrawOneView(Rect drawPos, LookDevEditionContext context)
        {
            int i = (int)context;
            bool drawLinks = ((m_LookDevView.config.lookDevMode != LookDevMode.Single1) && (context == LookDevEditionContext.Left)) || ((m_LookDevView.config.lookDevMode != LookDevMode.Single2) && (context == LookDevEditionContext.Right));

            GUILayout.BeginArea(drawPos);

            GUILayout.Label(styles.sViewTitle[i], styles.sViewTitleStyles[i]);

            GUILayout.BeginHorizontal();
            {
                GUILayout.BeginVertical(GUILayout.Width(m_WindowWidth));
                {
                    GUILayout.BeginHorizontal(GUILayout.Height(kLineHeight));
                    {
                        GUILayout.Label(styles.sExposure, styles.sMenuItem, GUILayout.Width(kLabelWidth));

                        float fExposureValue = m_LookDevView.config.GetFloatProperty(LookDevProperty.ExposureValue, context);
                        EditorGUI.BeginChangeCheck();
                        float roundedExposureRange = Mathf.Round(m_LookDevView.config.exposureRange);
                        fExposureValue = Mathf.Clamp(GUILayout.HorizontalSlider(fExposureValue, -roundedExposureRange, roundedExposureRange, GUILayout.Width(kSliderWidth)), -roundedExposureRange, roundedExposureRange); // Clamp is here to return value to the right range if the user decrease the exposure range
                        // Display in the float field is rounded for display. To 1 decimal in case of negative number to account for the '-' character.
                        fExposureValue = Mathf.Clamp(EditorGUILayout.FloatField((float)Math.Round(fExposureValue, fExposureValue < 0.0f ? 1 : 2), GUILayout.Width(kSliderFieldWidth)), -roundedExposureRange, roundedExposureRange);
                        if (EditorGUI.EndChangeCheck())
                        {
                            m_LookDevView.config.UpdateFocus(context);
                            m_LookDevView.config.UpdateFloatProperty(LookDevProperty.ExposureValue, fExposureValue);
                        }
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal(GUILayout.Height(kLineHeight));
                    {
                        int iHDRIIndex = -1;
                        int iHDRICount = m_LookDevView.envLibrary.hdriCount;

                        using (new EditorGUI.DisabledScope(iHDRICount <= 1))
                        {
                            GUILayout.Label(styles.sEnvironment, styles.sMenuItem, GUILayout.Width(kLabelWidth));

                            if (iHDRICount > 1)
                            {
                                int maxHDRIIndex = iHDRICount - 1;
                                iHDRIIndex = m_LookDevView.config.GetIntProperty(LookDevProperty.HDRI, context);
                                EditorGUI.BeginChangeCheck();
                                iHDRIIndex = (int)GUILayout.HorizontalSlider(iHDRIIndex, 0.0f, (float)maxHDRIIndex, GUILayout.Width(kSliderWidth));
                                iHDRIIndex = Mathf.Clamp(EditorGUILayout.IntField(iHDRIIndex, GUILayout.Width(kSliderFieldWidth)), 0, maxHDRIIndex);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    m_LookDevView.config.UpdateFocus(context);
                                    m_LookDevView.config.UpdateIntProperty(LookDevProperty.HDRI, iHDRIIndex);
                                }
                            }
                            else
                            {
                                GUILayout.HorizontalSlider(0.0f, 0.0f, 0.0f, GUILayout.Width(kSliderWidth));
                                GUILayout.Label(styles.sZero, styles.sMenuItem);
                            }
                        }
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal(GUILayout.Height(kLineHeight));
                    {
                        GUILayout.Label(styles.sShadingMode, styles.sMenuItem, GUILayout.Width(kLabelWidth));

                        int shadingMode = m_LookDevView.config.GetIntProperty(LookDevProperty.ShadingMode, context);
                        EditorGUI.BeginChangeCheck();
                        shadingMode = EditorGUILayout.IntPopup("", shadingMode, styles.sShadingModeStrings, styles.sShadingModeValues, GUILayout.Width(kSliderFieldWidth + kSliderWidth + 4.0f));
                        if (EditorGUI.EndChangeCheck())
                        {
                            m_LookDevView.config.UpdateFocus(context);
                            m_LookDevView.config.UpdateIntProperty(LookDevProperty.ShadingMode, shadingMode);
                        }
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal(GUILayout.Height(kLineHeight));
                    {
                        GUILayout.Label(styles.sRotation, styles.sMenuItem, GUILayout.Width(kLabelWidth));

                        float envRotation = m_LookDevView.config.GetFloatProperty(LookDevProperty.EnvRotation, context);
                        EditorGUI.BeginChangeCheck();
                        envRotation = GUILayout.HorizontalSlider(envRotation, 0.0f, 720.0f, GUILayout.Width(kSliderWidth));
                        envRotation = Mathf.Clamp(EditorGUILayout.FloatField((float)Math.Round(envRotation, 0), GUILayout.Width(kSliderFieldWidth)), 0.0f, 720.0f);
                        if (EditorGUI.EndChangeCheck())
                        {
                            m_LookDevView.config.UpdateFocus(context);
                            m_LookDevView.config.UpdateFloatProperty(LookDevProperty.EnvRotation, envRotation);
                        }
                    }
                    GUILayout.EndHorizontal();

                    if (NeedLoD())
                    {
                        GUILayout.BeginHorizontal(GUILayout.Height(kLineHeight));
                        {
                            if (m_LookDevView.config.GetObjectLoDCount(context) > 1)
                            {
                                int lodIndex = m_LookDevView.config.GetIntProperty(LookDevProperty.LoDIndex, context);

                                GUILayout.Label(lodIndex == -1 ? styles.sLoDAuto : styles.sLoD, styles.sMenuItem, GUILayout.Width(kLabelWidth));

                                EditorGUI.BeginChangeCheck();

                                int maxLoDIndex = m_LookDevView.config.GetObjectLoDCount(context) - 1;

                                // We need a specific handling here in case of linked property, because even if it is linked the two meshes can have different number of LOD
                                // We handle that by taking the min of the number of mesh if the property is link
                                if ((m_LookDevView.config.lookDevMode != LookDevMode.Single1 && m_LookDevView.config.lookDevMode != LookDevMode.Single2) && m_LookDevView.config.IsPropertyLinked(LookDevProperty.LoDIndex))
                                {
                                    maxLoDIndex = Math.Min(m_LookDevView.config.GetObjectLoDCount(LookDevEditionContext.Left), m_LookDevView.config.GetObjectLoDCount(LookDevEditionContext.Right)) - 1;
                                }

                                lodIndex = Mathf.Clamp(lodIndex, -1, maxLoDIndex);

                                lodIndex = (int)GUILayout.HorizontalSlider(lodIndex, -1, maxLoDIndex, GUILayout.Width(kSliderWidth));
                                lodIndex = EditorGUILayout.IntField(lodIndex, GUILayout.Width(kSliderFieldWidth));
                                if (EditorGUI.EndChangeCheck())
                                {
                                    m_LookDevView.config.UpdateFocus(context);
                                    m_LookDevView.config.UpdateIntProperty(LookDevProperty.LoDIndex, lodIndex);
                                }
                            }
                        }
                        GUILayout.EndHorizontal();
                    }
                }
                GUILayout.EndVertical();

                if (drawLinks)
                {
                    GUILayout.BeginVertical(GUILayout.Width(kIconSize));
                    {
                        LookDevProperty[] properties = { LookDevProperty.ExposureValue, LookDevProperty.HDRI, LookDevProperty.ShadingMode, LookDevProperty.EnvRotation, LookDevProperty.LoDIndex };
                        int propertyCount = 4 + (NeedLoD() ? 1 : 0);
                        for (int propertyIndex = 0; propertyIndex < propertyCount; ++propertyIndex)
                        {
                            bool linked = false;
                            EditorGUI.BeginChangeCheck();
                            bool isLink = m_LookDevView.config.IsPropertyLinked(properties[propertyIndex]);
                            linked = GUILayout.Toggle(isLink, GetGUIContentLink(isLink), styles.sToolBarButton, GUILayout.Height(kLineHeight));
                            if (EditorGUI.EndChangeCheck())
                            {
                                m_LookDevView.config.UpdatePropertyLink(properties[propertyIndex], linked);
                            }
                        }
                    }
                    GUILayout.EndVertical();
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
    }
}
