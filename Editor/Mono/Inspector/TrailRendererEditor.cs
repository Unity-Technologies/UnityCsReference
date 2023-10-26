// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEditor.EditorTools;
using UnityEditor.IMGUI.Controls;
using UnityEditor.Overlays;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(TrailRenderer))]
    [CanEditMultipleObjects]
    internal class TrailRendererInspector : RendererEditorBase
    {
        private class Styles
        {
            public static readonly GUIContent colorGradient = EditorGUIUtility.TrTextContent("Color", "The gradient describing the color along the trail.");
            public static readonly GUIContent numCornerVertices = EditorGUIUtility.TrTextContent("Corner Vertices", "How many vertices to add for each corner.");
            public static readonly GUIContent numCapVertices = EditorGUIUtility.TrTextContent("End Cap Vertices", "How many vertices to add at each end.");
            public static readonly GUIContent alignment = EditorGUIUtility.TrTextContent("Alignment", "Trails can rotate to face their transform component or the camera. When using TransformZ mode, lines extrude along the XY plane of the Transform.");
            public static readonly GUIContent textureMode = EditorGUIUtility.TrTextContent("Texture Mode", "Should the U coordinate be stretched or tiled?");
            public static readonly GUIContent textureScale = EditorGUIUtility.TrTextContent("Texture Scale", "Scale the texture along the UV coordinates using this multiplier.");
            public static readonly GUIContent shadowBias = EditorGUIUtility.TrTextContent("Shadow Bias", "Apply a shadow bias to prevent self-shadowing artifacts. The specified value is the proportion of the trail width at each segment.");
            public static readonly GUIContent generateLightingData = EditorGUIUtility.TrTextContent("Generate Lighting Data", "Toggle generation of normal and tangent data, for use in lit shaders.");
            public static readonly GUIContent applyActiveColorSpace = EditorGUIUtility.TrTextContent("Apply Active Color Space", "When using Linear Rendering, colors will be converted appropriately before being passed to the GPU.");

            public static readonly GUIContent play = EditorGUIUtility.TrTextContent("Play");
            public static readonly GUIContent playDisabled = EditorGUIUtility.TrTextContent("Play", "Play is disabled, because the Time Scale in the Time Manager is set to 0.0.");
            public static readonly GUIContent stop = EditorGUIUtility.TrTextContent("Stop");
            public static readonly GUIContent pause = EditorGUIUtility.TrTextContent("Pause");
            public static readonly GUIContent restart = EditorGUIUtility.TrTextContent("Restart");
            public static readonly GUIContent movementSpeed = EditorGUIUtility.TrTextContent("Movement Speed", "Speed is also affected by the Time Scale setting in the Time Manager.");
            public static readonly GUIContent movementSpeedDisabled = EditorGUIUtility.TrTextContent("Movement Speed", "Speed is locked to 0.0, because the Time Scale in the Time Manager is set to 0.0.");
            public static readonly GUIContent timeScale = EditorGUIUtility.TrTextContent("Time Scale", "Speed up or slow down the preview of the trail.");
            public static readonly GUIContent timeScaleDisabled = EditorGUIUtility.TrTextContent("Time Scale", "Time Scale is locked to 0.0, because the Time Scale in the Time Manager is set to 0.0.");
            public static readonly GUIContent showBounds = EditorGUIUtility.TrTextContent("Show Bounds", "Show world space bounding boxes.");
            public static readonly GUIContent previewShape = EditorGUIUtility.TrTextContent("Shape", "The trail preview will follow the selected shape.");
            public static readonly GUIContent previewShapeSize = EditorGUIUtility.TrTextContent("Shape Size", "The size of the shape.");

            public static readonly GUIContent toolIcon = EditorGUIUtility.IconContent("ParticleShapeTool", "Shape gizmo editing mode.");

            public static readonly string secondsFloatFieldFormatString = "f2";
        }

        private enum PreviewShape
        {
            Circle,
            Square,
            Line,
            SineWave,
            Spring
        }

        private const string k_PreviewMovementSpeed = "TrailPreviewMovementSpeed";
        private const string k_PreviewTimeScale = "TrailPreviewTimeScale";
        private const string k_PreviewShape = "TrailPreviewShape";
        private const string k_PreviewShapeSize = "TrailPreviewShapeSize";

        private static LinkedList<TrailRendererInspector> s_Inspectors = new LinkedList<TrailRendererInspector>();
        private static bool s_PreviewIsPlaying;
        private static bool s_PreviewIsPaused;
        private static float s_PreviewMovementSpeed;
        private static float s_PreviewTimeScale;
        private static bool s_PreviewShowBounds;
        private static PreviewShape s_PreviewShape;
        private static float s_PreviewShapeSize;
        private Vector3? m_PreviewBackupPosition;
        private bool m_PreviewIsFirstMove;
        private float m_PreviewTimePercentage;

        private static readonly Matrix4x4 s_ArcHandleOffsetMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.AngleAxis(90f, Vector3.right) * Quaternion.AngleAxis(90f, Vector3.up), Vector3.one);
        private const float k_SineWaveRepeat = 2.0f;
        private const float k_SineWaveHeightMultiplier = 0.5f;
        private const float k_SpringRotations = 5.0f;
        private const float k_PreviewMinSize = 0.1f;
        private const float k_SceneViewOverlayLabelWidth = 110.0f;
        private ArcHandle m_PreviewArcHandle = new ArcHandle();

        private LineRendererCurveEditor m_CurveEditor = new LineRendererCurveEditor();
        private SerializedProperty m_Time;
        private SerializedProperty m_MinVertexDistance;
        private SerializedProperty m_Autodestruct;
        private SerializedProperty m_Emitting;
        private SerializedProperty m_ApplyActiveColorSpace;
        private SerializedProperty m_ColorGradient;
        private SerializedProperty m_NumCornerVertices;
        private SerializedProperty m_NumCapVertices;
        private SerializedProperty m_Alignment;
        private SerializedProperty m_TextureMode;
        private SerializedProperty m_TextureScale;
        private SerializedProperty m_ShadowBias;
        private SerializedProperty m_GenerateLightingData;
        private SerializedProperty m_MaskInteraction;

        public class ShortcutContext : IShortcutToolContext
        {
            public bool active { get; set; }
        }

        private ShortcutContext m_ShortcutContext = new ShortcutContext { active = true };

        private static Event CreateCommandEvent(string commandName)
        {
            return new Event { type = EventType.ExecuteCommand, commandName = "TrailRenderer/" + commandName };
        }

        private static Event s_PlayEvent;
        private static Event s_StopEvent;
        private static Event s_RestartEvent;
        private static Event s_ShowBoundsEvent;

        private static PrefColor s_BoundsColor = new PrefColor("Trail Renderer/Bounds", 1.0f, 235.0f / 255.0f, 4.0f / 255.0f, 1.0f);
        private static PrefColor s_GizmoColor = new PrefColor("Trail Renderer/Shape Gizmos", 148f / 255f, 229f / 255f, 1f, 0.9f);

        private static void DispatchShortcutEvent(Event evt)
        {
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                if (sceneView.SendEvent(evt))
                    return;
            }

            var inspectors = Resources.FindObjectsOfTypeAll<TrailRendererInspector>();
            foreach (var inspector in inspectors)
            {
                if (inspector != null)
                {
                    if (inspector.HandleShortcutEvent(evt))
                        return;
                }
            }
        }

        [Shortcut("TrailRenderer/Play", typeof(ShortcutContext), KeyCode.Comma)]
        private static void PlayPauseShortcut(ShortcutArguments args)
        {
            DispatchShortcutEvent(s_PlayEvent);
        }

        [Shortcut("TrailRenderer/Stop", typeof(ShortcutContext), KeyCode.Period)]
        private static void StopShortcut(ShortcutArguments args)
        {
            DispatchShortcutEvent(s_StopEvent);
        }

        [Shortcut("TrailRenderer/Restart", typeof(ShortcutContext), KeyCode.Slash)]
        private static void RestartShortcut(ShortcutArguments args)
        {
            DispatchShortcutEvent(s_RestartEvent);
        }

        [Shortcut("TrailRenderer/ShowBounds", typeof(ShortcutContext))]
        private static void ShowBoundsShortcut(ShortcutArguments args)
        {
            DispatchShortcutEvent(s_ShowBoundsEvent);
        }

        public override void OnEnable()
        {
            base.OnEnable();

            s_PreviewMovementSpeed = SessionState.GetFloat(k_PreviewMovementSpeed, 0.2f);
            s_PreviewTimeScale = SessionState.GetFloat(k_PreviewTimeScale, 1.0f);
            s_PreviewShape = (PreviewShape)SessionState.GetInt(k_PreviewShape, 0);
            s_PreviewShapeSize = SessionState.GetFloat(k_PreviewShapeSize, 5.0f);

            m_CurveEditor.OnEnable(serializedObject);
            s_Inspectors.AddLast(this);
            SceneView.duringSceneGui += OnSceneViewGUI;
            EditorApplication.update += RepaintSceneView;
            ShortcutIntegration.instance.contextManager.RegisterToolContext(m_ShortcutContext);

            s_PlayEvent = CreateCommandEvent("Play");
            s_StopEvent = CreateCommandEvent("Stop");
            s_RestartEvent = CreateCommandEvent("Restart");
            s_ShowBoundsEvent = CreateCommandEvent("ShowBounds");

            m_Time = serializedObject.FindProperty("m_Time");
            m_MinVertexDistance = serializedObject.FindProperty("m_MinVertexDistance");
            m_Autodestruct = serializedObject.FindProperty("m_Autodestruct");
            m_Emitting = serializedObject.FindProperty("m_Emitting");
            m_ApplyActiveColorSpace = serializedObject.FindProperty("m_ApplyActiveColorSpace");
            m_ColorGradient = serializedObject.FindProperty("m_Parameters.colorGradient");
            m_NumCornerVertices = serializedObject.FindProperty("m_Parameters.numCornerVertices");
            m_NumCapVertices = serializedObject.FindProperty("m_Parameters.numCapVertices");
            m_Alignment = serializedObject.FindProperty("m_Parameters.alignment");
            m_TextureMode = serializedObject.FindProperty("m_Parameters.textureMode");
            m_TextureScale = serializedObject.FindProperty("m_Parameters.textureScale");
            m_ShadowBias = serializedObject.FindProperty("m_Parameters.shadowBias");
            m_GenerateLightingData = serializedObject.FindProperty("m_Parameters.generateLightingData");
            m_MaskInteraction = serializedObject.FindProperty("m_MaskInteraction");
        }

        public void OnDisable()
        {
            Stop();

            m_CurveEditor.OnDisable();
            s_Inspectors.Remove(this);
            SceneView.duringSceneGui -= OnSceneViewGUI;
            EditorApplication.update -= RepaintSceneView;
            ShortcutIntegration.instance.contextManager.DeregisterToolContext(m_ShortcutContext);

            SessionState.SetFloat(k_PreviewMovementSpeed, s_PreviewMovementSpeed);
            SessionState.SetFloat(k_PreviewTimeScale, s_PreviewTimeScale);
            SessionState.SetInt(k_PreviewShape, (int)s_PreviewShape);
            SessionState.SetFloat(k_PreviewShapeSize, s_PreviewShapeSize);
        }

        private static float ClampPreviewSize(TrailRenderer tr, float size)
        {
            float minSize = Mathf.Max(k_PreviewMinSize, tr.widthMultiplier * 0.5f);
            return Mathf.Max(size, minSize);
        }

        private void SavePositionForPreview()
        {
            if (!m_PreviewBackupPosition.HasValue)
            {
                if (target is TrailRenderer tr)
                {
                    m_PreviewBackupPosition = tr.transform.localPosition;
                    m_PreviewIsFirstMove = true;

                    s_PreviewShapeSize = ClampPreviewSize(tr, s_PreviewShapeSize);

                    DrivenPropertyManager.TryRegisterProperty(this, tr.transform, "m_LocalPosition");
                    DrivenPropertyManager.TryRegisterProperty(this, tr, "s_PreviewTimeScale");
                    EditorGUIUtility.beginProperty += BeginDrivenPropertyCheck;
                }

                m_PreviewTimePercentage = 0.0f;
                Tools.hidden = true;
            }
        }

        private void RestorePositionAfterPreview()
        {
            DrivenPropertyManager.UnregisterProperties(this);
            EditorGUIUtility.beginProperty -= BeginDrivenPropertyCheck;

            if (m_PreviewBackupPosition.HasValue)
            {
                if (target is TrailRenderer tr)
                    tr.Clear();
                m_PreviewBackupPosition = null;
            }

            Tools.hidden = false;
        }

        private void BeginDrivenPropertyCheck(Rect rect, SerializedProperty property)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (DrivenPropertyManagerInternal.IsDriving(this, property.serializedObject.targetObject, property.propertyPath))
            {
                // Properties driven by a UnityEvent are disabled as changes to them would be ignored.
                GUI.enabled = false;

                Color animatedColor = AnimationMode.animatedPropertyColor;
                animatedColor.a *= GUI.backgroundColor.a;
                GUI.backgroundColor = AnimationMode.animatedPropertyColor;
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            m_CurveEditor.CheckCurveChangedExternally();
            m_CurveEditor.OnInspectorGUI();

            EditorGUILayout.PropertyField(m_Time);
            EditorGUILayout.PropertyField(m_MinVertexDistance);
            EditorGUILayout.PropertyField(m_Autodestruct);
            EditorGUILayout.PropertyField(m_Emitting);
            EditorGUILayout.PropertyField(m_ApplyActiveColorSpace, Styles.applyActiveColorSpace);
            EditorGUILayout.PropertyField(m_ColorGradient, Styles.colorGradient);
            EditorGUILayout.PropertyField(m_NumCornerVertices, Styles.numCornerVertices);
            EditorGUILayout.PropertyField(m_NumCapVertices, Styles.numCapVertices);
            EditorGUILayout.PropertyField(m_Alignment, Styles.alignment);
            EditorGUILayout.PropertyField(m_TextureMode, Styles.textureMode);
            EditorGUILayout.PropertyField(m_TextureScale, Styles.textureScale);
            EditorGUILayout.PropertyField(m_GenerateLightingData, Styles.generateLightingData);
            EditorGUILayout.PropertyField(m_ShadowBias, Styles.shadowBias);
            EditorGUILayout.PropertyField(m_MaskInteraction);

            DrawMaterials();
            LightingSettingsGUI(false);
            OtherSettingsGUI(true, false, true);

            serializedObject.ApplyModifiedProperties();
        }

        private void OnSceneViewGUI(SceneView sceneView)
        {
            // Bounds
            if (s_PreviewShowBounds)
            {
                var oldCol = Handles.color;
                Handles.color = s_BoundsColor;

                foreach (var obj in targets)
                {
                    if (obj is TrailRenderer trail)
                    {
                        var worldBounds = trail.bounds;
                        Handles.DrawWireCube(worldBounds.center, worldBounds.size);
                    }
                }

                Handles.color = oldCol;
            }

            // Move trail using shape
            if (target is TrailRenderer tr)
            {
                if (m_PreviewBackupPosition.HasValue)
                {
                    if (s_PreviewIsPlaying && !s_PreviewIsPaused)
                    {
                        Tools.hidden = !(EditorToolManager.activeTool is ShapeGizmoTool);

                        float previousPreviewTimePercentage = m_PreviewTimePercentage;
                        float timeStep = Mathf.Min(Time.deltaTime, 0.1f);

                        float moveSpeed = s_PreviewMovementSpeed;
                        if (s_PreviewShape == PreviewShape.Spring)
                            moveSpeed *= 0.5f;

                        m_PreviewTimePercentage += (timeStep * moveSpeed * s_PreviewTimeScale);
                        m_PreviewTimePercentage %= 1.0f;

                        tr.previewTimeScale = s_PreviewTimeScale;

                        var transform = tr.transform;
                        Matrix4x4 localTransform = Matrix4x4.TRS(Vector3.zero, transform.localRotation, transform.localScale);

                        switch (s_PreviewShape)
                        {
                            case PreviewShape.Circle:
                                transform.localPosition = m_PreviewBackupPosition.Value + localTransform.MultiplyPoint(new Vector3(Mathf.Sin(m_PreviewTimePercentage * Mathf.PI * 2.0f), Mathf.Cos(m_PreviewTimePercentage * Mathf.PI * 2.0f), 0.0f) * s_PreviewShapeSize);
                                break;
                            case PreviewShape.Square:
                                if (m_PreviewTimePercentage < 0.25f)
                                    transform.localPosition = m_PreviewBackupPosition.Value + localTransform.MultiplyPoint(new Vector3(Mathf.Lerp(-1.0f, 1.0f, m_PreviewTimePercentage * 4.0f), -1.0f, 0.0f) * s_PreviewShapeSize);
                                else if (m_PreviewTimePercentage < 0.5f)
                                    transform.localPosition = m_PreviewBackupPosition.Value + localTransform.MultiplyPoint(new Vector3(1.0f, Mathf.Lerp(-1.0f, 1.0f, (m_PreviewTimePercentage - 0.25f) * 4.0f), 0.0f) * s_PreviewShapeSize);
                                else if (m_PreviewTimePercentage < 0.75f)
                                    transform.localPosition = m_PreviewBackupPosition.Value + localTransform.MultiplyPoint(new Vector3(Mathf.Lerp(1.0f, -1.0f, (m_PreviewTimePercentage - 0.5f) * 4.0f), 1.0f, 0.0f) * s_PreviewShapeSize);
                                else
                                    transform.localPosition = m_PreviewBackupPosition.Value + localTransform.MultiplyPoint(new Vector3(-1.0f, Mathf.Lerp(1.0f, -1.0f, (m_PreviewTimePercentage - 0.75f) * 4.0f), 0.0f) * s_PreviewShapeSize);
                                break;
                            case PreviewShape.Line:
                                transform.localPosition = m_PreviewBackupPosition.Value + localTransform.MultiplyPoint(new Vector3(Mathf.Lerp(-1.0f, 1.0f, m_PreviewTimePercentage), 0.0f, 0.0f) * s_PreviewShapeSize);
                                break;
                            case PreviewShape.SineWave:
                                transform.localPosition = m_PreviewBackupPosition.Value + localTransform.MultiplyPoint(new Vector3(Mathf.Lerp(-1.0f, 1.0f, m_PreviewTimePercentage), Mathf.Sin(m_PreviewTimePercentage * Mathf.PI * 2.0f * k_SineWaveRepeat) * k_SineWaveHeightMultiplier, 0.0f) * s_PreviewShapeSize);
                                break;
                            case PreviewShape.Spring:
                                transform.localPosition = m_PreviewBackupPosition.Value + localTransform.MultiplyPoint(new Vector3(Mathf.Sin(m_PreviewTimePercentage * Mathf.PI * 2.0f * k_SpringRotations), Mathf.Cos(m_PreviewTimePercentage * Mathf.PI * 2.0f * k_SpringRotations), Mathf.Lerp(-0.5f, 0.5f, m_PreviewTimePercentage) * k_SpringRotations) * s_PreviewShapeSize);
                                break;
                        }

                        // Clear non-looping shapes when they repeat
                        if (previousPreviewTimePercentage > m_PreviewTimePercentage)
                        {
                            if (s_PreviewShape == PreviewShape.Line || s_PreviewShape == PreviewShape.SineWave || s_PreviewShape == PreviewShape.Spring)
                                tr.Clear();
                        }

                        // Clear the trail when a preview starts
                        if (m_PreviewIsFirstMove)
                        {
                            tr.Clear();
                            m_PreviewIsFirstMove = false;
                        }
                    }
                    else if (s_PreviewIsPaused)
                    {
                        tr.previewTimeScale = 0.0f;
                    }
                }
                else
                {
                    tr.previewTimeScale = s_PreviewTimeScale; // Not playing or paused, but might be dragging the Trail around the scene view using the Transform gizmo
                }
            }
        }

        private void RepaintSceneView()
        {
            if (s_PreviewIsPlaying && !s_PreviewIsPaused)
                SceneView.RepaintAll();
        }

        private void Play()
        {
            if (!s_PreviewIsPlaying && !s_PreviewIsPaused)
                SavePositionForPreview();

            s_PreviewIsPlaying = true;
            s_PreviewIsPaused = false;
        }

        private void Pause()
        {
            s_PreviewIsPlaying = false;
            s_PreviewIsPaused = true;
            Tools.hidden = false;
        }

        private void Stop()
        {
            s_PreviewIsPlaying = false;
            s_PreviewIsPaused = false;
            RestorePositionAfterPreview();
        }

        private void PlayStopGUI()
        {
            bool disablePlayButton = (Time.timeScale == 0.0f);
            GUIContent playText = disablePlayButton ? Styles.playDisabled : Styles.play;

            // Play/Stop buttons
            GUILayout.BeginHorizontal(GUILayout.Width(220.0f));
            {
                using (new EditorGUI.DisabledScope(disablePlayButton))
                {
                    bool isPlaying = s_PreviewIsPlaying && !s_PreviewIsPaused && !disablePlayButton;
                    if (GUILayout.Button(isPlaying ? Styles.pause : playText, "ButtonLeft"))
                    {
                        if (isPlaying)
                            Pause();
                        else
                            Play();
                    }
                }

                if (GUILayout.Button(Styles.restart, "ButtonMid"))
                {
                    Stop();
                    Play();
                }

                if (GUILayout.Button(Styles.stop, "ButtonRight"))
                {
                    Stop();
                }
            }
            GUILayout.EndHorizontal();

            // Playback info
            PlayBackInfoGUI();

            // Handle shortcut keys last so we do not activate them if inputfield has used the event
            HandleKeyboardShortcuts();
        }

        private void PlayBackInfoGUI()
        {
            EventType oldEventType = Event.current.type;
            int oldHotControl = GUIUtility.hotControl;
            string oldFormat = EditorGUI.kFloatFieldFormatString;

            EditorGUIUtility.labelWidth = k_SceneViewOverlayLabelWidth;

            EditorGUI.kFloatFieldFormatString = Styles.secondsFloatFieldFormatString;
            if (Time.timeScale == 0.0f)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.FloatField(Styles.movementSpeedDisabled, 0.0f);
                    EditorGUILayout.FloatField(Styles.timeScaleDisabled, 0.0f);
                }
            }
            else
            {
                EditorGUI.BeginChangeCheck();
                s_PreviewMovementSpeed = Mathf.Clamp(EditorGUILayout.FloatField(Styles.movementSpeed, s_PreviewMovementSpeed), 0.0f, 2.0f);
                s_PreviewTimeScale = Mathf.Clamp(EditorGUILayout.FloatField(Styles.timeScale, s_PreviewTimeScale), 0.1f, 1.0f);
                if (EditorGUI.EndChangeCheck())
                    m_PreviewIsFirstMove = true;
            }

            EditorGUI.BeginChangeCheck();
            s_PreviewShape = (PreviewShape)EditorGUILayout.EnumPopup(Styles.previewShape, s_PreviewShape);
            if (EditorGUI.EndChangeCheck())
                m_PreviewIsFirstMove = true;

            EditorGUI.BeginChangeCheck();
            s_PreviewShapeSize = EditorGUILayout.FloatField(Styles.previewShapeSize, s_PreviewShapeSize);
            if (EditorGUI.EndChangeCheck())
            {
                s_PreviewShapeSize = ClampPreviewSize(target as TrailRenderer, s_PreviewShapeSize);
                m_PreviewIsFirstMove = true;
            }

            EditorGUI.kFloatFieldFormatString = oldFormat;

            s_PreviewShowBounds = GUILayout.Toggle(s_PreviewShowBounds, Styles.showBounds, EditorStyles.toggle);

            EditorGUIUtility.labelWidth = 0.0f;
        }

        private void HandleKeyboardShortcuts()
        {
            var evt = Event.current;

            if (evt.type == EventType.ExecuteCommand)
            {
                if (HandleShortcutEvent(evt))
                    evt.Use();
            }
        }

        private bool HandleShortcutEvent(Event evt)
        {
            if (EditorApplication.isPlaying)
                return false;

            if (evt.commandName == s_PlayEvent.commandName)
            {
                if (!s_PreviewIsPlaying)
                    Play();
                else
                    Pause();

                return true;
            }
            else if (evt.commandName == s_StopEvent.commandName)
            {
                Stop();
                return true;
            }
            else if (evt.commandName == s_RestartEvent.commandName)
            {
                Stop();
                Play();
                return true;
            }
            else if (evt.commandName == s_ShowBoundsEvent.commandName)
            {
                s_PreviewShowBounds = !s_PreviewShowBounds;
                return true;
            }

            return false;
        }

        private static Matrix4x4 GetPreviewMatrix(Vector3? backupPosition, Transform transform)
        {
            Matrix4x4 parentMatrix = Matrix4x4.identity;
            if (transform.parent)
                parentMatrix = transform.parent.localToWorldMatrix;

            Vector3 localPosition = backupPosition.HasValue ? backupPosition.Value : transform.localPosition;
            Matrix4x4 localMatrix = Matrix4x4.TRS(localPosition, transform.localRotation, transform.localScale);
            return parentMatrix * localMatrix;
        }

        [Overlay(typeof(SceneView), k_OverlayId, k_DisplayName)]
        class SceneViewTrailRendererOverlay : TransientSceneViewOverlay
        {
            const string k_OverlayId = "Scene View/TrailRenderer";
            const string k_DisplayName = "Trail Renderer";

            public override bool visible
            {
                get { return s_Inspectors != null && s_Inspectors.Count > 0 && !EditorApplication.isPlaying && (s_Inspectors.Last().targets.Length == 1); }
            }

            public override void OnGUI()
            {
                if (!visible)
                    return;
                s_Inspectors.Last().PlayStopGUI();
            }
        }

        [EditorTool("Shape", typeof(TrailRenderer))]
        class ShapeGizmoTool : EditorTool, IDrawSelectedHandles
        {
            public override GUIContent toolbarIcon
            {
                get { return Styles.toolIcon; }
            }

            public override bool IsAvailable()
            {
                // Check for count != 1
                if (targets.Skip(1).Any())
                    return false;
                if (s_Inspectors == null || s_Inspectors.Count == 0)
                    return false;

                return !EditorApplication.isPlaying;
            }

            public override void OnToolGUI(EditorWindow window)
            {
                if (s_Inspectors == null || !s_Inspectors.Any())
                    return;

                DrawHandles(s_Inspectors.Last(), true);
            }

            public void OnDrawHandles()
            {
                if (!s_PreviewIsPlaying)
                    DrawHandles(s_Inspectors.Last(), false);
            }

            private void DrawHandles(TrailRendererInspector inspector, bool allowGizmoEditing)
            {
                Color gizmoEditingColor = new Color(1.0f, 1.0f, 1.0f, allowGizmoEditing && !Event.current.alt ? 1.0f : 0.0f);

                Color origCol = Handles.color;
                Handles.color = s_GizmoColor;

                Matrix4x4 orgMatrix = Handles.matrix;

                foreach (var tr in targets.OfType<TrailRenderer>())
                {
                    Handles.matrix = GetPreviewMatrix(inspector.m_PreviewBackupPosition, tr.transform);

                    switch (s_PreviewShape)
                    {
                        case PreviewShape.Circle:
                            {
                                EditorGUI.BeginChangeCheck();

                                inspector.m_PreviewArcHandle.angle = 360.0f;
                                inspector.m_PreviewArcHandle.radius = s_PreviewShapeSize;
                                inspector.m_PreviewArcHandle.SetColorWithRadiusHandle(Color.white, 0f);
                                inspector.m_PreviewArcHandle.radiusHandleColor *= gizmoEditingColor;
                                inspector.m_PreviewArcHandle.angleHandleColor = Color.clear;

                                using (new Handles.DrawingScope(Handles.matrix * s_ArcHandleOffsetMatrix))
                                    inspector.m_PreviewArcHandle.DrawHandle();

                                if (EditorGUI.EndChangeCheck())
                                    s_PreviewShapeSize = ClampPreviewSize(tr, inspector.m_PreviewArcHandle.radius);
                            }
                            break;
                        case PreviewShape.Square:
                            {
                                EditorGUI.BeginChangeCheck();
                                float size = DoSimpleSquareHandle(s_PreviewShapeSize, allowGizmoEditing);
                                if (EditorGUI.EndChangeCheck())
                                    s_PreviewShapeSize = ClampPreviewSize(tr, size);
                            }
                            break;
                        case PreviewShape.Line:
                            {
                                EditorGUI.BeginChangeCheck();
                                float radius = Handles.DoSimpleEdgeHandle(Quaternion.identity, Vector3.zero, s_PreviewShapeSize, allowGizmoEditing);
                                if (EditorGUI.EndChangeCheck())
                                    s_PreviewShapeSize = ClampPreviewSize(tr, radius);
                            }
                            break;
                        case PreviewShape.SineWave:
                            {
                                EditorGUI.BeginChangeCheck();
                                float radius = DoSimpleSineWaveHandle(s_PreviewShapeSize, allowGizmoEditing, k_SineWaveHeightMultiplier, k_SineWaveRepeat);
                                if (EditorGUI.EndChangeCheck())
                                    s_PreviewShapeSize = ClampPreviewSize(tr, radius);
                            }
                            break;
                        case PreviewShape.Spring:
                            {
                                EditorGUI.BeginChangeCheck();
                                float radius = DoSimpleSpringHandle(s_PreviewShapeSize, allowGizmoEditing, k_SpringRotations);
                                if (EditorGUI.EndChangeCheck())
                                    s_PreviewShapeSize = ClampPreviewSize(tr, radius);
                            }
                            break;
                    }
                }

                Handles.color = origCol;
                Handles.matrix = orgMatrix;
            }

            private static float DoSimpleSquareHandle(float size, bool editable)
            {
                if (Event.current.alt)
                    editable = false;

                Vector3 right = Vector3.right;
                Vector3 up = Vector3.up;

                if (editable)
                {
                    // Size handles at edges
                    EditorGUI.BeginChangeCheck();
                    size = Handles.SizeSlider(Vector3.zero, up, size);
                    size = Handles.SizeSlider(Vector3.zero, -up, size);
                    size = Handles.SizeSlider(Vector3.zero, right, size);
                    size = Handles.SizeSlider(Vector3.zero, -right, size);
                    if (EditorGUI.EndChangeCheck())
                        size = Mathf.Max(0.0f, size);
                }

                // Draw gizmo
                if (size > 0)
                {
                    Vector3[] points = new Vector3[5];

                    points[0] = up * size + right * size;
                    points[1] = -up * size + right * size;
                    points[2] = -up * size - right * size;
                    points[3] = up * size - right * size;
                    points[4] = points[0];

                    Handles.DrawPolyLine(points);
                }

                return size;
            }

            private static float DoSimpleSineWaveHandle(float radius, bool editable, float heightMultiplier, float repeatCount)
            {
                if (Event.current.alt)
                    editable = false;

                Vector3 right = Vector3.right;
                Vector3 up = Vector3.up;

                if (editable)
                {
                    // Radius handles at ends
                    EditorGUI.BeginChangeCheck();
                    radius = Handles.SizeSlider(Vector3.zero, right, radius);
                    radius = Handles.SizeSlider(Vector3.zero, -right, radius);
                    if (EditorGUI.EndChangeCheck())
                        radius = Mathf.Max(0.0f, radius);
                }

                // Draw gizmo
                if (radius > 0)
                {
                    Vector3 start = -right * radius;

                    Vector3[] points = new Vector3[128];
                    for (int i = 0; i < points.Length; i++)
                    {
                        float percent = ((float)i / (points.Length - 1));
                        Vector3 sine = start + right * (radius * percent) * 2.0f;
                        sine += up * Mathf.Sin(percent * Mathf.PI * 2.0f * repeatCount) * radius * heightMultiplier;
                        points[i] = sine;
                    }

                    Handles.DrawPolyLine(points);
                }

                return radius;
            }

            private static float DoSimpleSpringHandle(float radius, bool editable, float rotationCount)
            {
                if (Event.current.alt)
                    editable = false;

                Vector3 right = Vector3.right;
                Vector3 up = Vector3.up;

                if (editable)
                {
                    // Radius handles at ends
                    EditorGUI.BeginChangeCheck();

                    Vector3 start = new Vector3(0.0f, 0.0f, -0.5f * rotationCount) * radius;
                    radius = Handles.SizeSlider(start, up, radius);

                    Vector3 end = new Vector3(0.0f, 0.0f, 0.5f * rotationCount) * radius;
                    radius = Handles.SizeSlider(end, up, radius);

                    if (EditorGUI.EndChangeCheck())
                        radius = Mathf.Max(0.0f, radius);
                }

                // Draw gizmo
                if (radius > 0)
                {
                    Vector3[] points = new Vector3[128];
                    for (int i = 0; i < points.Length; i++)
                    {
                        float percent = ((float)i / (points.Length - 1));
                        float theta = percent * Mathf.PI * 2.0f * rotationCount;
                        Vector3 sine = new Vector3(Mathf.Sin(theta), Mathf.Cos(theta), Mathf.Lerp(-0.5f, 0.5f, percent) * rotationCount);
                        points[i] = sine * radius;
                    }

                    Handles.DrawPolyLine(points);
                }

                return radius;
            }
        }
    }
}
