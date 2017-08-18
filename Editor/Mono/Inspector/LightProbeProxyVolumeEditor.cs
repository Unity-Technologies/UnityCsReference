// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEditor.AnimatedValues;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor
{
    [CustomEditor(typeof(LightProbeProxyVolume))]
    [CanEditMultipleObjects]
    internal class LightProbeProxyVolumeEditor : Editor
    {
        static LightProbeProxyVolumeEditor s_LastInteractedEditor;

        private SerializedProperty m_ResolutionX;
        private SerializedProperty m_ResolutionY;
        private SerializedProperty m_ResolutionZ;
        private SerializedProperty m_BoundingBoxSize;
        private SerializedProperty m_BoundingBoxOrigin;
        private SerializedProperty m_BoundingBoxMode;
        private SerializedProperty m_ResolutionMode;
        private SerializedProperty m_ResolutionProbesPerUnit;
        private SerializedProperty m_ProbePositionMode;
        private SerializedProperty m_RefreshMode;

        // Should match gizmo color in GizmoDrawers.cpp!
        internal static Color kGizmoLightProbeProxyVolumeColor = new Color(0xFF / 255f, 0xE5 / 255f, 0x94 / 255f, 0x80 / 255f);
        internal static Color kGizmoLightProbeProxyVolumeHandleColor = new Color(0xFF / 255f, 0xE5 / 255f, 0xAA / 255f, 0xFF / 255f);

        private BoxBoundsHandle m_BoundsHandle = new BoxBoundsHandle();

        private static class Styles
        {
            static Styles()
            {
                richTextMiniLabel.richText = true;
            }

            public static GUIStyle richTextMiniLabel = new GUIStyle(EditorStyles.miniLabel);
            public static GUIContent volumeResolutionText = EditorGUIUtility.TextContent("Proxy Volume Resolution|Specifies the resolution of the 3D grid of interpolated light probes. Higher resolution/density means better lighting but the CPU cost will increase.");
            public static GUIContent resolutionXText = new GUIContent("X");
            public static GUIContent resolutionYText = new GUIContent("Y");
            public static GUIContent resolutionZText = new GUIContent("Z");
            public static GUIContent sizeText = EditorGUIUtility.TextContent("Size");
            public static GUIContent bbSettingsText = EditorGUIUtility.TextContent("Bounding Box Settings");
            public static GUIContent originText = EditorGUIUtility.TextContent("Origin");
            public static GUIContent bbModeText = EditorGUIUtility.TextContent("Bounding Box Mode|The mode in which the bounding box is computed. A 3D grid of interpolated light probes will be generated inside this bounding box.\n\nAutomatic Local - the local-space bounding box of the Renderer is used.\n\nAutomatic Global - a bounding box is computed which encloses the current Renderer and all the Renderers down the hierarchy that have the Light Probes property set to Use Proxy Volume. The bounding box will be world-space aligned.\n\nCustom - a custom bounding box is used. The bounding box is specified in the local-space of the game object.");
            public static GUIContent resModeText = EditorGUIUtility.TextContent("Resolution Mode|The mode in which the resolution of the 3D grid of interpolated light probes is specified:\n\nAutomatic - the resolution on each axis is computed using a user-specified number of interpolated light probes per unit area(Density).\n\nCustom - the user can specify a different resolution on each axis.");
            public static GUIContent probePositionText = EditorGUIUtility.TextContent("Probe Position Mode|The mode in which the interpolated probe positions are generated.\n\nCellCorner - divide the volume in cells and generate interpolated probe positions in the corner/edge of the cells.\n\nCellCenter - divide the volume in cells and generate interpolated probe positions in the center of the cells.");
            public static GUIContent refreshModeText = EditorGUIUtility.TextContent("Refresh Mode");
            public static GUIContent[] bbMode = (Enum.GetNames(typeof(LightProbeProxyVolume.BoundingBoxMode)).Select(x => ObjectNames.NicifyVariableName(x)).ToArray()).Select(x => new GUIContent(x)).ToArray();
            public static GUIContent[] resMode = (Enum.GetNames(typeof(LightProbeProxyVolume.ResolutionMode)).Select(x => ObjectNames.NicifyVariableName(x)).ToArray()).Select(x => new GUIContent(x)).ToArray();
            public static GUIContent[] probePositionMode = (Enum.GetNames(typeof(LightProbeProxyVolume.ProbePositionMode)).Select(x => ObjectNames.NicifyVariableName(x)).ToArray()).Select(x => new GUIContent(x)).ToArray();
            public static GUIContent[] refreshMode = (Enum.GetNames(typeof(LightProbeProxyVolume.RefreshMode)).Select(x => ObjectNames.NicifyVariableName(x)).ToArray()).Select(x => new GUIContent(x)).ToArray();
            public static GUIContent resProbesPerUnit = EditorGUIUtility.TextContent("Density|Density in probes per world unit.");
            public static GUIContent componentUnusedNote = EditorGUIUtility.TextContent("In order to use the component on this game object, the Light Probes property should be set to 'Use Proxy Volume' in Renderer.");
            public static GUIContent noRendererNode = EditorGUIUtility.TextContent("The component is unused by this game object because there is no Renderer component attached.");
            public static GUIContent noLightProbes = EditorGUIUtility.TextContent("The scene doesn't contain any light probes. Add light probes using Light Probe Group components (menu: Component->Rendering->Light Probe Group).");
            public static GUIContent componentUnsuportedOnTreesNote = EditorGUIUtility.TextContent("Tree rendering doesn't support Light Probe Proxy Volume components.");

            public static int[] volTextureSizesValues = { 1, 2, 4, 8, 16, 32 };
            public static GUIContent[] volTextureSizes = volTextureSizesValues.Select(n => new GUIContent(n.ToString())).ToArray();

            public static GUIContent[] toolContents =
            {
                PrimitiveBoundsHandle.editModeButton,
                EditorGUIUtility.IconContent("MoveTool", "|Move the selected objects.")
            };
            public static EditMode.SceneViewEditMode[] sceneViewEditModes = new[]
            {
                EditMode.SceneViewEditMode.LightProbeProxyVolumeBox,
                EditMode.SceneViewEditMode.LightProbeProxyVolumeOrigin
            };

            public static string baseSceneEditingToolText = "<color=grey>Light Probe Proxy Volume Scene Editing Mode:</color> ";
            public static GUIContent[] toolNames =
            {
                new GUIContent(baseSceneEditingToolText + "Box Bounds", ""),
                new GUIContent(baseSceneEditingToolText + "Box Origin", "")
            };
        }

        private bool IsLightProbeVolumeProxyEditMode(EditMode.SceneViewEditMode editMode)
        {
            return editMode == EditMode.SceneViewEditMode.LightProbeProxyVolumeBox ||
                editMode == EditMode.SceneViewEditMode.LightProbeProxyVolumeOrigin;
        }

        private bool sceneViewEditing
        {
            get { return IsLightProbeVolumeProxyEditMode(EditMode.editMode) && EditMode.IsOwner(this); }
        }

        private AnimBool m_ShowBoundingBoxOptions = new AnimBool();
        private AnimBool m_ShowComponentUnusedWarning = new AnimBool();
        private AnimBool m_ShowResolutionXYZOptions = new AnimBool();
        private AnimBool m_ShowResolutionProbesOption = new AnimBool();
        private AnimBool m_ShowNoRendererWarning = new AnimBool();
        private AnimBool m_ShowNoLightProbesWarning = new AnimBool();

        private bool boundingBoxOptionsValue        { get { return (!m_BoundingBoxMode.hasMultipleDifferentValues) && (m_BoundingBoxMode.intValue == (int)LightProbeProxyVolume.BoundingBoxMode.Custom); } }
        private bool resolutionXYZOptionValue       { get { return (!m_ResolutionMode.hasMultipleDifferentValues) && (m_ResolutionMode.intValue == (int)LightProbeProxyVolume.ResolutionMode.Custom); } }
        private bool resolutionProbesOptionValue    { get { return (!m_ResolutionMode.hasMultipleDifferentValues) && (m_ResolutionMode.intValue == (int)LightProbeProxyVolume.ResolutionMode.Automatic); } }
        private bool noLightProbesWarningValue      { get { return (LightmapSettings.lightProbes == null) || (LightmapSettings.lightProbes.count == 0); } }
        private bool componentUnusedWarningValue
        {
            get
            {
                Renderer renderer = ((LightProbeProxyVolume)target).GetComponent(typeof(Renderer)) as Renderer;
                bool useLightProbes = (renderer != null) && LightProbes.AreLightProbesAllowed(renderer);
                return (renderer != null) && (targets.Length == 1) && ((renderer.lightProbeUsage != LightProbeUsage.UseProxyVolume) || !useLightProbes);
            }
        }

        private bool noRendererWarningValue
        {
            get
            {
                Renderer renderer = ((LightProbeProxyVolume)target).GetComponent(typeof(Renderer)) as Renderer;
                return (renderer == null) && (targets.Length == 1);
            }
        }

        private void SetOptions(AnimBool animBool, bool initialize, bool targetValue)
        {
            if (initialize)
            {
                animBool.value = targetValue;
                animBool.valueChanged.AddListener(Repaint);
            }
            else
            {
                animBool.target = targetValue;
            }
        }

        private void UpdateShowOptions(bool initialize)
        {
            SetOptions(m_ShowBoundingBoxOptions, initialize, boundingBoxOptionsValue);
            SetOptions(m_ShowComponentUnusedWarning, initialize, componentUnusedWarningValue);
            SetOptions(m_ShowResolutionXYZOptions, initialize, resolutionXYZOptionValue);
            SetOptions(m_ShowResolutionProbesOption, initialize, resolutionProbesOptionValue);
            SetOptions(m_ShowNoRendererWarning, initialize, noRendererWarningValue);
            SetOptions(m_ShowNoLightProbesWarning, initialize, noLightProbesWarningValue);
        }

        public void OnEnable()
        {
            m_ResolutionX = serializedObject.FindProperty("m_ResolutionX");
            m_ResolutionY = serializedObject.FindProperty("m_ResolutionY");
            m_ResolutionZ = serializedObject.FindProperty("m_ResolutionZ");
            m_BoundingBoxSize = serializedObject.FindProperty("m_BoundingBoxSize");
            m_BoundingBoxOrigin = serializedObject.FindProperty("m_BoundingBoxOrigin");
            m_BoundingBoxMode = serializedObject.FindProperty("m_BoundingBoxMode");
            m_ResolutionMode = serializedObject.FindProperty("m_ResolutionMode");
            m_ResolutionProbesPerUnit = serializedObject.FindProperty("m_ResolutionProbesPerUnit");
            m_ProbePositionMode = serializedObject.FindProperty("m_ProbePositionMode");
            m_RefreshMode = serializedObject.FindProperty("m_RefreshMode");

            m_BoundsHandle.handleColor = kGizmoLightProbeProxyVolumeHandleColor;
            m_BoundsHandle.wireframeColor = Color.clear;

            UpdateShowOptions(true);
        }

        internal override Bounds GetWorldBoundsOfTarget(UnityEngine.Object targetObject)
        {
            return ((LightProbeProxyVolume)target).boundsGlobal;
        }

        void DoToolbar()
        {
            using (new EditorGUI.DisabledScope(m_BoundingBoxMode.intValue != (int)LightProbeProxyVolume.BoundingBoxMode.Custom))
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                var oldEditMode = EditMode.editMode;

                EditorGUI.BeginChangeCheck();
                EditMode.DoInspectorToolbar(Styles.sceneViewEditModes, Styles.toolContents, this);
                if (EditorGUI.EndChangeCheck())
                    s_LastInteractedEditor = this;

                if (oldEditMode != EditMode.editMode)
                {
                    if (Toolbar.get != null)
                        Toolbar.get.Repaint();
                }

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                // Info box for tools
                GUILayout.BeginVertical(EditorStyles.helpBox);
                string helpText = Styles.baseSceneEditingToolText;
                if (sceneViewEditing)
                {
                    int index = ArrayUtility.IndexOf(Styles.sceneViewEditModes, EditMode.editMode);
                    if (index >= 0)
                        helpText = Styles.toolNames[index].text;
                }
                GUILayout.Label(helpText, Styles.richTextMiniLabel);
                GUILayout.EndVertical();
                EditorGUILayout.Space();
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            UpdateShowOptions(false);

            var tree = ((LightProbeProxyVolume)target).GetComponent<Tree>();
            if (tree != null)
            {
                EditorGUILayout.HelpBox(Styles.componentUnsuportedOnTreesNote.text, MessageType.Info);
                return;
            }

            EditorGUILayout.Space();

            EditorGUILayout.Popup(m_RefreshMode, Styles.refreshMode,  Styles.refreshModeText);

            EditorGUILayout.Popup(m_BoundingBoxMode, Styles.bbMode, Styles.bbModeText);

            if (EditorGUILayout.BeginFadeGroup(m_ShowBoundingBoxOptions.faded))
            {
                if (targets.Length == 1)
                    DoToolbar();

                GUILayout.Label(Styles.bbSettingsText);

                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(m_BoundingBoxSize, Styles.sizeText);
                EditorGUILayout.PropertyField(m_BoundingBoxOrigin, Styles.originText);

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFadeGroup();

            EditorGUILayout.Space();

            GUILayout.Label(Styles.volumeResolutionText);

            EditorGUI.indentLevel++;
            {
                EditorGUILayout.Popup(m_ResolutionMode, Styles.resMode, Styles.resModeText);

                if (EditorGUILayout.BeginFadeGroup(m_ShowResolutionXYZOptions.faded))
                {
                    EditorGUILayout.IntPopup(m_ResolutionX, Styles.volTextureSizes, Styles.volTextureSizesValues, Styles.resolutionXText, GUILayout.MinWidth(40));
                    EditorGUILayout.IntPopup(m_ResolutionY, Styles.volTextureSizes, Styles.volTextureSizesValues, Styles.resolutionYText, GUILayout.MinWidth(40));
                    EditorGUILayout.IntPopup(m_ResolutionZ, Styles.volTextureSizes, Styles.volTextureSizesValues, Styles.resolutionZText, GUILayout.MinWidth(40));
                }
                EditorGUILayout.EndFadeGroup();

                if (EditorGUILayout.BeginFadeGroup(m_ShowResolutionProbesOption.faded))
                {
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(m_ResolutionProbesPerUnit, Styles.resProbesPerUnit);
                    GUILayout.Label(" probes per unit", EditorStyles.wordWrappedMiniLabel);
                    GUILayout.EndHorizontal();
                }
                EditorGUILayout.EndFadeGroup();
            }

            EditorGUI.indentLevel--;

            EditorGUILayout.Space();

            EditorGUILayout.Popup(m_ProbePositionMode, Styles.probePositionMode, Styles.probePositionText);

            if (EditorGUILayout.BeginFadeGroup(m_ShowComponentUnusedWarning.faded) && LightProbeProxyVolume.isFeatureSupported)
            {
                EditorGUILayout.HelpBox(Styles.componentUnusedNote.text, MessageType.Warning);
            }
            EditorGUILayout.EndFadeGroup();

            if (EditorGUILayout.BeginFadeGroup(m_ShowNoRendererWarning.faded))
            {
                EditorGUILayout.HelpBox(Styles.noRendererNode.text, MessageType.Info);
            }
            EditorGUILayout.EndFadeGroup();

            if (EditorGUILayout.BeginFadeGroup(m_ShowNoLightProbesWarning.faded))
            {
                EditorGUILayout.HelpBox(Styles.noLightProbes.text, MessageType.Info);
            }
            EditorGUILayout.EndFadeGroup();

            serializedObject.ApplyModifiedProperties();
        }

        [DrawGizmo(GizmoType.Active)]
        static void RenderBoxGizmo(LightProbeProxyVolume probeProxyVolume, GizmoType gizmoType)
        {
            if (s_LastInteractedEditor == null)
                return;

            if (s_LastInteractedEditor.sceneViewEditing && EditMode.editMode == EditMode.SceneViewEditMode.LightProbeProxyVolumeBox)
            {
                Color oldColor = Gizmos.color;
                Gizmos.color = kGizmoLightProbeProxyVolumeColor;
                Vector3 position = probeProxyVolume.originCustom;
                Matrix4x4 oldMatrix = Gizmos.matrix;

                Gizmos.matrix = probeProxyVolume.transform.localToWorldMatrix;
                Gizmos.DrawCube(position, -1f * probeProxyVolume.sizeCustom);

                Gizmos.matrix = oldMatrix;
                Gizmos.color = oldColor;
            }
        }

        public void OnSceneGUI()
        {
            if (!sceneViewEditing)
                return;

            if (m_BoundingBoxMode.intValue != (int)LightProbeProxyVolume.BoundingBoxMode.Custom)
                EditMode.QuitEditMode();

            switch (EditMode.editMode)
            {
                case EditMode.SceneViewEditMode.LightProbeProxyVolumeBox:
                    DoBoxEditing();
                    break;
                case EditMode.SceneViewEditMode.LightProbeProxyVolumeOrigin:
                    DoOriginEditing();
                    break;
            }
        }

        void DoOriginEditing()
        {
            LightProbeProxyVolume proxyVolume = (LightProbeProxyVolume)target;

            Vector3 handlePosition = proxyVolume.transform.TransformPoint(proxyVolume.originCustom);

            EditorGUI.BeginChangeCheck();

            Vector3 newPostion = Handles.PositionHandle(handlePosition, proxyVolume.transform.rotation);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(proxyVolume, "Modified Light Probe Proxy Volume Box Origin");
                proxyVolume.originCustom = proxyVolume.transform.InverseTransformPoint(newPostion);
                EditorUtility.SetDirty(target);
            }
        }

        void DoBoxEditing()
        {
            // Drawing of the probe box is done from GizmoDrawers.cpp,
            // here we only want to show the box editing handles when needed.
            LightProbeProxyVolume proxyVolume = (LightProbeProxyVolume)target;

            using (new Handles.DrawingScope(proxyVolume.transform.localToWorldMatrix))
            {
                m_BoundsHandle.center = proxyVolume.originCustom;
                m_BoundsHandle.size = proxyVolume.sizeCustom;

                EditorGUI.BeginChangeCheck();
                m_BoundsHandle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(proxyVolume, "Modified Light Probe Proxy Volume AABB");
                    proxyVolume.originCustom = m_BoundsHandle.center;
                    proxyVolume.sizeCustom = m_BoundsHandle.size;
                    EditorUtility.SetDirty(target);
                }
            }
        }
    }
}
