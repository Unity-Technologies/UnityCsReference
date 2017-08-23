// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using System.IO;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    [EditorWindowTitle(title = "Occlusion", icon = "Occlusion")]
    internal class OcclusionCullingWindow : EditorWindow
    {
        static bool s_IsVisible = false;

        private bool m_PreVis;
        private string m_Warning;

        static OcclusionCullingWindow ms_OcclusionCullingWindow;
        Vector2 m_ScrollPosition = Vector2.zero;
        Mode m_Mode = Mode.AreaSettings;

        static Styles s_Styles;

        class Styles
        {
            public GUIContent[] ModeToggles =
            {
                new GUIContent("Object"),
                new GUIContent("Bake"),
                new GUIContent("Visualization")
            };
            public GUIStyle labelStyle = EditorStyles.wordWrappedMiniLabel;
            public GUIContent emptyAreaSelection = new GUIContent("Select a Mesh Renderer or an Occlusion Area from the scene.");
            public GUIContent emptyCameraSelection = new GUIContent("Select a Camera from the scene.");
            public GUIContent visualizationNote = new GUIContent("The visualization may not correspond to current bake settings and Occlusion Area placements if they have been changed since last bake.");
            public GUIContent seeVisualizationInScene = new GUIContent("See the occlusion culling visualization in the Scene View based on the selected Camera.");
            public GUIContent noOcclusionData = new GUIContent("No occlusion data has been baked.");
            public GUIContent smallestHole = EditorGUIUtility.TextContent("Smallest Hole|Smallest hole in the geometry through which the camera is supposed to see. The single float value of the parameter represents the diameter of the imaginary smallest hole, i.e. the maximum extent of a 3D object that fits through the hole.");
            public GUIContent backfaceThreshold = EditorGUIUtility.TextContent("Backface Threshold|The backface threshold is a size optimization that reduces unnecessary details by testing backfaces. A value of 100 is robust and never removes any backfaces. A value of 5 aggressively reduces the data based on locations with visible backfaces. The idea is that typically valid camera positions cannot see many backfaces.  For example geometry under terrain and inside solid objects can be removed.");
            public GUIContent farClipPlane = EditorGUIUtility.TextContent("Far Clip Plane|Far Clip Plane used during baking. This should match the largest far clip plane used by any camera in the scene. A value of 0.0 sets the far plane to Infinity.");
            public GUIContent smallestOccluder = EditorGUIUtility.TextContent("Smallest Occluder|The size of the smallest object that will be used to hide other objects when doing occlusion culling. For example, if a value of 4 is chosen, then all the objects that are higher or wider than 4 meters will block visibility and the objects that are smaller than that will not. This value is a tradeoff between occlusion accuracy and storage size.");
            public GUIContent defaultParameterText = EditorGUIUtility.TextContent("Default Parameters|The default parameters guarantee that any given scene computes fast and the occlusion culling results are good. As the parameters are always scene specific, better results will be achieved when fine tuning the parameters on a scene to scene basis. All the parameters are dependent on the unit scale of the scene and it is imperative that the unit scale parameter is set correctly before setting the default values.");
        };

        enum Mode
        {
            AreaSettings = 0,
            BakeSettings = 1,
            Visualization = 2
        }

        void OnBecameVisible()
        {
            if (s_IsVisible == true) return;

            s_IsVisible = true;

            SceneView.onSceneGUIDelegate += OnSceneViewGUI;
            StaticOcclusionCullingVisualization.showOcclusionCulling = true;
            SceneView.RepaintAll();
        }

        void OnBecameInvisible()
        {
            s_IsVisible = false;

            SceneView.onSceneGUIDelegate -= OnSceneViewGUI;
            StaticOcclusionCullingVisualization.showOcclusionCulling = false;
            SceneView.RepaintAll();
        }

        void OnSelectionChange()
        {
            if (m_Mode == Mode.AreaSettings || m_Mode == Mode.Visualization)
                Repaint();
        }

        void OnEnable()
        {
            titleContent = GetLocalizedTitleContent();
            ms_OcclusionCullingWindow = this;
            autoRepaintOnSceneChange = true;
            EditorApplication.searchChanged += Repaint;
            Repaint();
        }

        void OnDisable()
        {
            ms_OcclusionCullingWindow = null;
            EditorApplication.searchChanged -= Repaint;
        }

        static void BackgroundTaskStatusChanged()
        {
            if (ms_OcclusionCullingWindow)
                ms_OcclusionCullingWindow.Repaint();
        }

        [MenuItem("Window/Occlusion Culling", false, 2099)]
        static void GenerateWindow()
        {
            var window = GetWindow<OcclusionCullingWindow>(typeof(InspectorWindow));
            window.minSize = new Vector2(300, 250);
        }

        void SummaryGUI()
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);

            if (StaticOcclusionCulling.umbraDataSize == 0)
            {
                GUILayout.Label(s_Styles.noOcclusionData, s_Styles.labelStyle);
            }
            else
            {
                GUILayout.Label("Last bake:", s_Styles.labelStyle);

                GUILayout.BeginHorizontal();

                GUILayout.BeginVertical();
                GUILayout.Label("Occlusion data size ", s_Styles.labelStyle);
                GUILayout.EndVertical();

                GUILayout.BeginVertical();
                GUILayout.Label(EditorUtility.FormatBytes(StaticOcclusionCulling.umbraDataSize), s_Styles.labelStyle);
                GUILayout.EndVertical();

                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
        }

        OcclusionArea CreateNewArea()
        {
            GameObject go = new GameObject("Occlusion Area");
            OcclusionArea oa = go.AddComponent<OcclusionArea>();
            Selection.activeGameObject = go;
            return oa;
        }

        void AreaSelectionGUI()
        {
            bool emptySelection = true;
            GameObject[] gos;
            Type focusType = SceneModeUtility.SearchBar(typeof(Renderer), typeof(OcclusionArea));
            EditorGUILayout.Space();

            // Occlusion Areas
            OcclusionArea[] oas = SceneModeUtility.GetSelectedObjectsOfType<OcclusionArea>(out gos);
            if (gos.Length > 0)
            {
                emptySelection = false;
                EditorGUILayout.MultiSelectionObjectTitleBar(oas);
                SerializedObject so = new SerializedObject(oas);
                EditorGUILayout.PropertyField(so.FindProperty("m_IsViewVolume"));
                so.ApplyModifiedProperties();
            }

            // Renderers
            Renderer[] renderers = SceneModeUtility.GetSelectedObjectsOfType<Renderer>(out gos, typeof(MeshRenderer), typeof(SkinnedMeshRenderer));
            if (gos.Length > 0)
            {
                emptySelection = false;
                EditorGUILayout.MultiSelectionObjectTitleBar(renderers);
                SerializedObject goso = new SerializedObject(gos);
                SceneModeUtility.StaticFlagField("Occluder Static", goso.FindProperty("m_StaticEditorFlags"), (int)StaticEditorFlags.OccluderStatic);
                SceneModeUtility.StaticFlagField("Occludee Static", goso.FindProperty("m_StaticEditorFlags"), (int)StaticEditorFlags.OccludeeStatic);
                goso.ApplyModifiedProperties();
            }

            if (emptySelection)
            {
                GUILayout.Label(s_Styles.emptyAreaSelection, EditorStyles.helpBox);
                if (focusType == typeof(OcclusionArea))
                {
                    EditorGUIUtility.labelWidth = 80;
                    EditorGUILayout.Space();
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel("Create New");
                    if (GUILayout.Button("Occlusion Area", EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                        CreateNewArea();
                    EditorGUILayout.EndHorizontal();
                }
            }
        }

        void CameraSelectionGUI()
        {
            SceneModeUtility.SearchBar(typeof(Camera));
            EditorGUILayout.Space();

            Camera cam = null;
            if (Selection.activeGameObject)
                cam = Selection.activeGameObject.GetComponent<Camera>();

            // Camera
            if (cam)
            {
                Camera[] cameras = new Camera[] { cam };
                EditorGUILayout.MultiSelectionObjectTitleBar(cameras);
                EditorGUILayout.HelpBox(s_Styles.seeVisualizationInScene.text, MessageType.Info);
            }
            else
            {
                GUILayout.Label(s_Styles.emptyCameraSelection, EditorStyles.helpBox);
            }
        }

        void BakeSettings()
        {
            // Button for setting default values
            float buttonWidth = 150;
            if (GUILayout.Button("Set default parameters", GUILayout.Width(buttonWidth)))
            {
                GUIUtility.keyboardControl = 0; // Force focus out from potentially selected field for default parameters setting
                StaticOcclusionCulling.SetDefaultOcclusionBakeSettings();
            }

            // Label for default parameter setting
            GUILayout.Label(s_Styles.defaultParameterText.tooltip, EditorStyles.helpBox);

            // Edit Smallest Occluder
            EditorGUI.BeginChangeCheck();
            float smallestOccluder = EditorGUILayout.FloatField(s_Styles.smallestOccluder, StaticOcclusionCulling.smallestOccluder);
            if (EditorGUI.EndChangeCheck())
            {
                StaticOcclusionCulling.smallestOccluder = smallestOccluder;
            }

            // Edit smallest hole
            EditorGUI.BeginChangeCheck();
            float smallestHole = EditorGUILayout.FloatField(s_Styles.smallestHole, StaticOcclusionCulling.smallestHole);
            if (EditorGUI.EndChangeCheck())
            {
                StaticOcclusionCulling.smallestHole = smallestHole;
            }

            // Edit backface threshold
            EditorGUI.BeginChangeCheck();
            float backfaceThreshold = EditorGUILayout.Slider(s_Styles.backfaceThreshold, StaticOcclusionCulling.backfaceThreshold, 5.0F, 100.0F);
            if (EditorGUI.EndChangeCheck())
            {
                StaticOcclusionCulling.backfaceThreshold = backfaceThreshold;
            }
        }

        void BakeButtons()
        {
            float buttonWidth = 95;
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            bool allowBaking = !EditorApplication.isPlayingOrWillChangePlaymode;

            // Clear Tome button
            GUI.enabled = StaticOcclusionCulling.umbraDataSize != 0 && allowBaking;
            if (GUILayout.Button("Clear", GUILayout.Width(buttonWidth)))
                StaticOcclusionCulling.Clear();
            GUI.enabled = allowBaking;

            // Is occlusion culling running
            if (StaticOcclusionCulling.isRunning)
            {
                if (GUILayout.Button("Cancel", GUILayout.Width(buttonWidth)))
                    StaticOcclusionCulling.Cancel();
            }
            else
            {
                if (GUILayout.Button("Bake", GUILayout.Width(buttonWidth)))
                {
                    StaticOcclusionCulling.GenerateInBackground();
                }
            }

            GUILayout.EndHorizontal();

            GUI.enabled = true;
        }

        void ModeToggle()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            using (var check = new EditorGUI.ChangeCheckScope())
            {
                m_Mode = (Mode)GUILayout.Toolbar((int)m_Mode, s_Styles.ModeToggles, "LargeButton", GUI.ToolbarButtonSize.FitToContents);
                if (check.changed)
                {
                    if (m_Mode == Mode.Visualization && StaticOcclusionCulling.umbraDataSize > 0)
                        StaticOcclusionCullingVisualization.showPreVisualization = false;
                    else
                        StaticOcclusionCullingVisualization.showPreVisualization = true;
                    SceneView.RepaintAll();
                }
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        void OnGUI()
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            // Make sure the tab jumps to visualization if we're in visualize mode.
            // (Don't do the reverse. Since tabs can't be marked disabled, the user
            // will be confused if the visualization tab can't be clicked, and that
            // would be the result if we changed the tab away from visualization
            // whenever showPreVisualization is false.)
            if (m_Mode != Mode.Visualization && StaticOcclusionCullingVisualization.showPreVisualization == false)
                m_Mode = Mode.Visualization;

            EditorGUILayout.Space();
            ModeToggle();
            EditorGUILayout.Space();

            m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);
            switch (m_Mode)
            {
                case Mode.AreaSettings:
                    AreaSelectionGUI();
                    break;
                case Mode.BakeSettings:
                    BakeSettings();
                    break;
                case Mode.Visualization:
                    if (StaticOcclusionCulling.umbraDataSize > 0)
                    {
                        CameraSelectionGUI();
                        GUILayout.FlexibleSpace();
                        GUILayout.Label(s_Styles.visualizationNote, EditorStyles.helpBox);
                    }
                    else
                    {
                        GUILayout.Label(s_Styles.noOcclusionData, EditorStyles.helpBox);
                    }
                    break;
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            BakeButtons();

            EditorGUILayout.Space();

            // Info GUI
            SummaryGUI();
        }

        public void OnSceneViewGUI(SceneView sceneView)
        {
            if (!s_IsVisible)
                return;

            SceneViewOverlay.Window(new GUIContent("Occlusion Culling"), DisplayControls, (int)SceneViewOverlay.Ordering.OcclusionCulling, SceneViewOverlay.WindowDisplayOption.OneWindowPerTarget);
        }

        void OnDidOpenScene()
        {
            StaticOcclusionCulling.InvalidatePrevisualisationData();
            Repaint();
        }

        void SetShowVolumePreVis()
        {
            StaticOcclusionCullingVisualization.showPreVisualization = true;
            if (m_Mode == Mode.Visualization)
                m_Mode = Mode.AreaSettings;
            if (ms_OcclusionCullingWindow)
                ms_OcclusionCullingWindow.Repaint();
            SceneView.RepaintAll();
        }

        void SetShowVolumeCulling()
        {
            StaticOcclusionCullingVisualization.showPreVisualization = false;
            m_Mode = Mode.Visualization;
            if (ms_OcclusionCullingWindow)
                ms_OcclusionCullingWindow.Repaint();
            SceneView.RepaintAll();
        }

        bool ShowModePopup(Rect popupRect)
        {
            // Visualization mode popup
            int tomeSize = StaticOcclusionCulling.umbraDataSize;

            // We can only change the preVis state during layout mode. However, the Tome data could be emptied at anytime, which will immediately disable preVis.
            // We need to detect this and force a repaint, so we can change the state.
            if (m_PreVis != StaticOcclusionCullingVisualization.showPreVisualization)
                SceneView.RepaintAll();

            if (Event.current.type == EventType.Layout)
                m_PreVis = StaticOcclusionCullingVisualization.showPreVisualization;

            string[] options = new string[] { "Edit", "Visualize" };
            int selected = m_PreVis ? 0 : 1;
            if (EditorGUI.DropdownButton(popupRect, new GUIContent(options[selected]), FocusType.Passive, EditorStyles.popup))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent(options[0]), selected == 0, SetShowVolumePreVis);
                if (tomeSize > 0)
                    menu.AddItem(new GUIContent(options[1]), selected == 1, SetShowVolumeCulling);
                else
                    menu.AddDisabledItem(new GUIContent(options[1]));

                menu.Popup(popupRect, selected);
            }
            return m_PreVis;
        }

        void DisplayControls(Object target, SceneView sceneView)
        {
            if (!sceneView)
                return;

            if (!s_IsVisible)
                return;

            bool temp;

            // See if pre-vis is set to true.
            // If we don't have any Tome data, act as if pvs is true, but don't actually set it to true
            // - that way it will act like it switches to false (the default value) by itself as soon as pvs data has been build,
            // which better leads to discovery of this feature.

            bool preVis = ShowModePopup(GUILayoutUtility.GetRect(170, EditorGUIUtility.singleLineHeight));

            if (Event.current.type == EventType.Layout)
            {
                m_Warning = "";

                if (!preVis)
                {
                    if (StaticOcclusionCullingVisualization.previewOcclucionCamera == null)
                        m_Warning = "No camera selected for occlusion preview.";
                    else if (!StaticOcclusionCullingVisualization.isPreviewOcclusionCullingCameraInPVS)
                        m_Warning = "Camera is not inside an Occlusion View Area.";
                }
            }

            int legendHeight = 12;

            if (!string.IsNullOrEmpty(m_Warning))
            {
                Rect warningRect = GUILayoutUtility.GetRect(100, legendHeight + 19);
                warningRect.x += EditorGUI.indent;
                warningRect.width -= EditorGUI.indent;
                GUI.Label(warningRect, m_Warning, EditorStyles.helpBox);
            }
            else
            {
                // Show legend / volume toggles
                Rect legendRect = GUILayoutUtility.GetRect(200, legendHeight);
                legendRect.x += EditorGUI.indent;
                legendRect.width -= EditorGUI.indent;
                Rect viewLegendRect = new Rect(legendRect.x, legendRect.y, legendRect.width, legendRect.height);

                if (preVis)
                    EditorGUI.DrawLegend(viewLegendRect, Color.white, "View Volumes", StaticOcclusionCullingVisualization.showViewVolumes);
                else
                    EditorGUI.DrawLegend(viewLegendRect, Color.white, "Camera Volumes", StaticOcclusionCullingVisualization.showViewVolumes);

                temp = GUI.Toggle(viewLegendRect, StaticOcclusionCullingVisualization.showViewVolumes, "", GUIStyle.none);
                if (temp != StaticOcclusionCullingVisualization.showViewVolumes)
                {
                    StaticOcclusionCullingVisualization.showViewVolumes = temp;
                    SceneView.RepaintAll();
                }

                if (!preVis)
                {
                    // TODO: FORE REALS cleanup this bad code BUG: 496650
                    legendRect = GUILayoutUtility.GetRect(100, legendHeight);
                    legendRect.x += EditorGUI.indent;
                    legendRect.width -= EditorGUI.indent;
                    viewLegendRect = new Rect(legendRect.x, legendRect.y, legendRect.width, legendRect.height);
                    EditorGUI.DrawLegend(viewLegendRect, Color.green, "Visibility Lines", StaticOcclusionCullingVisualization.showVisibilityLines);

                    temp = GUI.Toggle(viewLegendRect, StaticOcclusionCullingVisualization.showVisibilityLines, "", GUIStyle.none);
                    if (temp != StaticOcclusionCullingVisualization.showVisibilityLines)
                    {
                        StaticOcclusionCullingVisualization.showVisibilityLines = temp;
                        SceneView.RepaintAll();
                    }

                    legendRect = GUILayoutUtility.GetRect(100, legendHeight);
                    legendRect.x += EditorGUI.indent;
                    legendRect.width -= EditorGUI.indent;
                    viewLegendRect = new Rect(legendRect.x, legendRect.y, legendRect.width, legendRect.height);
                    EditorGUI.DrawLegend(viewLegendRect, Color.grey, "Portals", StaticOcclusionCullingVisualization.showPortals);

                    temp = GUI.Toggle(viewLegendRect, StaticOcclusionCullingVisualization.showPortals, "", GUIStyle.none);
                    if (temp != StaticOcclusionCullingVisualization.showPortals)
                    {
                        StaticOcclusionCullingVisualization.showPortals = temp;
                        SceneView.RepaintAll();
                    }
                }

                // Geometry culling toggle
                if (!preVis)
                {
                    temp = GUILayout.Toggle(StaticOcclusionCullingVisualization.showGeometryCulling, "Occlusion culling");
                    if (temp != StaticOcclusionCullingVisualization.showGeometryCulling)
                    {
                        StaticOcclusionCullingVisualization.showGeometryCulling = temp;
                        SceneView.RepaintAll();
                    }
                }
            }
        }
    }
}
