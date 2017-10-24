// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.AnimatedValues;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace UnityEditor
{
    [CustomEditor(typeof(ReflectionProbe))]
    [CanEditMultipleObjects]
    internal class ReflectionProbeEditor : Editor
    {
        static ReflectionProbeEditor s_LastInteractedEditor;

        SerializedProperty m_Mode;
        SerializedProperty m_RefreshMode;
        SerializedProperty m_TimeSlicingMode;
        SerializedProperty m_Resolution;
        SerializedProperty m_ShadowDistance;
        SerializedProperty m_Importance;
        SerializedProperty m_BoxSize;
        SerializedProperty m_BoxOffset;
        SerializedProperty m_CullingMask;
        SerializedProperty m_ClearFlags;
        SerializedProperty m_BackgroundColor;
        SerializedProperty m_HDR;
        SerializedProperty m_BoxProjection;
        SerializedProperty m_IntensityMultiplier;
        SerializedProperty m_BlendDistance;
        SerializedProperty m_CustomBakedTexture;
        SerializedProperty m_RenderDynamicObjects;
        SerializedProperty m_UseOcclusionCulling;

        SerializedProperty[] m_NearAndFarProperties;

        private static Mesh s_SphereMesh;
        private Material m_ReflectiveMaterial;
        private Matrix4x4 m_OldLocalSpace = Matrix4x4.identity;
        private float m_MipLevelPreview = 0.0F;

        private BoxBoundsHandle m_BoundsHandle = new BoxBoundsHandle();

        private Hashtable m_CachedGizmoMaterials = new Hashtable();

        public static void GetResolutionArray(ref int[] resolutionList, ref GUIContent[] resolutionStringList)
        {
            if (Styles.reflectionResolutionValuesArray == null && Styles.reflectionResolutionTextArray == null)
            {
                int cubemapResolution = Mathf.Max(1, ReflectionProbe.minBakedCubemapResolution);

                List<int> envReflectionResolutionValues = new List<int>();
                List<GUIContent> envReflectionResolutionText = new List<GUIContent>();

                do
                {
                    envReflectionResolutionValues.Add(cubemapResolution);
                    envReflectionResolutionText.Add(new GUIContent(cubemapResolution.ToString()));
                    cubemapResolution *= 2;
                }
                while (cubemapResolution <= ReflectionProbe.maxBakedCubemapResolution);

                Styles.reflectionResolutionValuesArray = envReflectionResolutionValues.ToArray();
                Styles.reflectionResolutionTextArray = envReflectionResolutionText.ToArray();
            }

            resolutionList = Styles.reflectionResolutionValuesArray;
            resolutionStringList = Styles.reflectionResolutionTextArray;
        }

        static internal class Styles
        {
            static Styles()
            {
                richTextMiniLabel.richText = true;
            }

            public static GUIStyle richTextMiniLabel = new GUIStyle(EditorStyles.miniLabel);

            public static string bakeButtonText = "Bake";
            public static string[] bakeCustomOptionText = {"Bake as new Cubemap..."};
            public static string[] bakeButtonsText = {"Bake All Reflection Probes"};

            public static GUIContent bakeCustomButtonText = EditorGUIUtility.TextContent("Bake|Bakes Reflection Probe's cubemap, overwriting the existing cubemap texture asset (if any).");
            public static GUIContent runtimeSettingsHeader = new GUIContent("Runtime settings", "These settings are used by objects when they render with the cubemap of this probe");
            public static GUIContent backgroundColorText = new GUIContent("Background", "Camera clears the screen to this color before rendering.");
            public static GUIContent clearFlagsText = new GUIContent("Clear Flags");
            public static GUIContent intensityText = new GUIContent("Intensity");
            public static GUIContent resolutionText = new GUIContent("Resolution");
            public static GUIContent captureCubemapHeaderText = new GUIContent("Cubemap capture settings");
            public static GUIContent boxProjectionText = new GUIContent("Box Projection", "Box projection causes reflections to appear to change based on the object's position within the probe's box, while still using a single probe as the source of the reflection. This works well for reflections on objects that are moving through enclosed spaces such as corridors and rooms. Setting box projection to False and the cubemap reflection will be treated as coming from infinitely far away. Note that this feature can be globally disabled from Graphics Settings -> Tier Settings");
            public static GUIContent blendDistanceText = new GUIContent("Blend Distance", "Area around the probe where it is blended with other probes. Only used in deferred probes.");
            public static GUIContent sizeText = EditorGUIUtility.TextContent("Box Size|The size of the box in which the reflections will be applied to objects. The value is not affected by the Transform of the Game Object.");
            public static GUIContent centerText = EditorGUIUtility.TextContent("Box Offset|The center of the box in which the reflections will be applied to objects. The value is relative to the position of the Game Object.");
            public static GUIContent customCubemapText = new GUIContent("Cubemap");
            public static GUIContent importanceText = new GUIContent("Importance");
            public static GUIContent renderDynamicObjects = new GUIContent("Dynamic Objects", "If enabled dynamic objects are also rendered into the cubemap");
            public static GUIContent timeSlicing = new GUIContent("Time Slicing", "If enabled this probe will update over several frames, to help reduce the impact on the frame rate");
            public static GUIContent refreshMode = new GUIContent("Refresh Mode", "Controls how this probe refreshes in the Player");

            public static  GUIContent typeText = new GUIContent("Type", "'Baked Cubemap' uses the 'Auto Baking' mode from the Lighting window. If it is enabled then baking is automatic otherwise manual bake is needed (use the bake button below). \n'Custom' can be used if a custom cubemap is wanted. \n'Realtime' can be used to dynamically re-render the cubemap during runtime (via scripting).");
            public static GUIContent[] reflectionProbeMode = { new GUIContent("Baked"), new GUIContent("Custom"), new GUIContent("Realtime") };
            public static int[] reflectionProbeModeValues = { (int)ReflectionProbeMode.Baked, (int)ReflectionProbeMode.Custom, (int)ReflectionProbeMode.Realtime };

            public static int[] reflectionResolutionValuesArray = null;
            public static GUIContent[] reflectionResolutionTextArray = null;

            public static GUIContent[] clearFlags =
            {
                new GUIContent("Skybox"),
                new GUIContent("Solid Color")
            };
            public static int[] clearFlagsValues = { 1, 2 }; // taken from Camera.h

            public static GUIContent[] toolContents =
            {
                PrimitiveBoundsHandle.editModeButton,
                EditorGUIUtility.IconContent("MoveTool", "|Move the selected objects.")
            };
            public static EditMode.SceneViewEditMode[] sceneViewEditModes = new[]
            {
                EditMode.SceneViewEditMode.ReflectionProbeBox,
                EditMode.SceneViewEditMode.ReflectionProbeOrigin
            };

            public static string baseSceneEditingToolText = "<color=grey>Probe Scene Editing Mode:</color> ";
            public static GUIContent[] toolNames =
            {
                new GUIContent(baseSceneEditingToolText + "Box Projection Bounds", ""),
                new GUIContent(baseSceneEditingToolText + "Probe Origin", "")
            };
        } // end of class Styles

        // Should match reflection probe gizmo color in GizmoDrawers.cpp!
        internal static Color kGizmoReflectionProbe = new Color(0xFF / 255f, 0xE5 / 255f, 0x94 / 255f, 0x80 / 255f);
        internal static Color kGizmoReflectionProbeDisabled = new Color(0x99 / 255f, 0x89 / 255f, 0x59 / 255f, 0x60 / 255f);
        internal static Color kGizmoHandleReflectionProbe = new Color(0xFF / 255f, 0xE5 / 255f, 0xAA / 255f, 0xFF / 255f);


        readonly AnimBool m_ShowProbeModeRealtimeOptions = new AnimBool(); // p.mode == ReflectionProbeMode.Realtime; Will be brought back in 5.1
        readonly AnimBool m_ShowProbeModeCustomOptions = new AnimBool();
        readonly AnimBool m_ShowBoxOptions = new AnimBool();

        private TextureInspector m_CubemapEditor = null;

        bool IsReflectionProbeEditMode(EditMode.SceneViewEditMode editMode)
        {
            return editMode == EditMode.SceneViewEditMode.ReflectionProbeBox ||
                editMode == EditMode.SceneViewEditMode.ReflectionProbeOrigin;
        }

        bool sceneViewEditing
        {
            get { return IsReflectionProbeEditMode(EditMode.editMode) && EditMode.IsOwner(this); }
        }

        public void OnEnable()
        {
            m_Mode = serializedObject.FindProperty("m_Mode");
            m_RefreshMode = serializedObject.FindProperty("m_RefreshMode");
            m_TimeSlicingMode = serializedObject.FindProperty("m_TimeSlicingMode");

            m_Resolution = serializedObject.FindProperty("m_Resolution");
            m_NearAndFarProperties = new[] { serializedObject.FindProperty("m_NearClip"), serializedObject.FindProperty("m_FarClip") };
            m_ShadowDistance = serializedObject.FindProperty("m_ShadowDistance");
            m_Importance = serializedObject.FindProperty("m_Importance");
            m_BoxSize = serializedObject.FindProperty("m_BoxSize");
            m_BoxOffset = serializedObject.FindProperty("m_BoxOffset");
            m_CullingMask = serializedObject.FindProperty("m_CullingMask");
            m_ClearFlags = serializedObject.FindProperty("m_ClearFlags");
            m_BackgroundColor = serializedObject.FindProperty("m_BackGroundColor");
            m_HDR = serializedObject.FindProperty("m_HDR");
            m_BoxProjection = serializedObject.FindProperty("m_BoxProjection");
            m_IntensityMultiplier = serializedObject.FindProperty("m_IntensityMultiplier");
            m_BlendDistance = serializedObject.FindProperty("m_BlendDistance");
            m_CustomBakedTexture = serializedObject.FindProperty("m_CustomBakedTexture");
            m_RenderDynamicObjects = serializedObject.FindProperty("m_RenderDynamicObjects");
            m_UseOcclusionCulling = serializedObject.FindProperty("m_UseOcclusionCulling");

            ReflectionProbe p = target as ReflectionProbe;
            m_ShowProbeModeRealtimeOptions.valueChanged.AddListener(Repaint);
            m_ShowProbeModeCustomOptions.valueChanged.AddListener(Repaint);
            m_ShowBoxOptions.valueChanged.AddListener(Repaint);
            m_ShowProbeModeRealtimeOptions.value = p.mode == ReflectionProbeMode.Realtime;
            m_ShowProbeModeCustomOptions.value = p.mode == ReflectionProbeMode.Custom;
            m_ShowBoxOptions.value = true;

            m_BoundsHandle.handleColor = kGizmoHandleReflectionProbe;
            m_BoundsHandle.wireframeColor = Color.clear;

            UpdateOldLocalSpace();
            SceneView.onPreSceneGUIDelegate += OnPreSceneGUICallback;
        }

        public void OnDisable()
        {
            SceneView.onPreSceneGUIDelegate -= OnPreSceneGUICallback;

            DestroyImmediate(m_ReflectiveMaterial);
            DestroyImmediate(m_CubemapEditor);

            foreach (Material mat in m_CachedGizmoMaterials.Values)
                DestroyImmediate(mat);
            m_CachedGizmoMaterials.Clear();
        }

        private bool IsCollidingWithOtherProbes(string targetPath, ReflectionProbe targetProbe, out ReflectionProbe collidingProbe)
        {
            ReflectionProbe[] probes = FindObjectsOfType<ReflectionProbe>().ToArray();
            collidingProbe = null;
            foreach (var probe in probes)
            {
                if (probe == targetProbe || probe.customBakedTexture == null)
                    continue;
                string path = AssetDatabase.GetAssetPath(probe.customBakedTexture);
                if (path == targetPath)
                {
                    collidingProbe = probe;
                    return true;
                }
            }
            return false;
        }

        private void BakeCustomReflectionProbe(ReflectionProbe probe, bool usePreviousAssetPath)
        {
            string path = "";
            if (usePreviousAssetPath)
                path = AssetDatabase.GetAssetPath(probe.customBakedTexture);

            string targetExtension = probe.hdr ? "exr" : "png";
            if (string.IsNullOrEmpty(path) || Path.GetExtension(path) != "." + targetExtension)
            {
                // We use the path of the active scene as the target path
                string targetPath = FileUtil.GetPathWithoutExtension(SceneManager.GetActiveScene().path);
                if (string.IsNullOrEmpty(targetPath))
                    targetPath = "Assets";
                else if (Directory.Exists(targetPath) == false)
                    Directory.CreateDirectory(targetPath);

                string fileName = probe.name + (probe.hdr ? "-reflectionHDR" : "-reflection") + "." + targetExtension;
                fileName = Path.GetFileNameWithoutExtension(AssetDatabase.GenerateUniqueAssetPath(Path.Combine(targetPath, fileName)));

                path = EditorUtility.SaveFilePanelInProject("Save reflection probe's cubemap.", fileName, targetExtension, "", targetPath);
                if (string.IsNullOrEmpty(path))
                    return;

                ReflectionProbe collidingProbe;
                if (IsCollidingWithOtherProbes(path, probe, out collidingProbe))
                {
                    if (!EditorUtility.DisplayDialog("Cubemap is used by other reflection probe",
                            string.Format("'{0}' path is used by the game object '{1}', do you really want to overwrite it?",
                                path, collidingProbe.name), "Yes", "No"))
                    {
                        return;
                    }
                }
            }

            EditorUtility.DisplayProgressBar("Reflection Probes", "Baking " + path, 0.5f);
            if (!Lightmapping.BakeReflectionProbe(probe, path))
                Debug.LogError("Failed to bake reflection probe to " + path);
            EditorUtility.ClearProgressBar();
        }

        private void OnBakeCustomButton(object data)
        {
            int mode = (int)data;

            ReflectionProbe p = target as ReflectionProbe;
            if (mode == 0)
                BakeCustomReflectionProbe(p, false);
        }

        private void OnBakeButton(object data)
        {
            int mode = (int)data;
            if (mode == 0)
                Lightmapping.BakeAllReflectionProbesSnapshots();
        }

        ReflectionProbe reflectionProbeTarget
        {
            get { return (ReflectionProbe)target; }
        }

        void DoBakeButton()
        {
            if (reflectionProbeTarget.mode == ReflectionProbeMode.Realtime)
            {
                EditorGUILayout.HelpBox("Baking of this reflection probe should be initiated from the scripting API because the type is 'Realtime'", MessageType.Info);

                if (!QualitySettings.realtimeReflectionProbes)
                    EditorGUILayout.HelpBox("Realtime reflection probes are disabled in Quality Settings", MessageType.Warning);
                return;
            }

            if (reflectionProbeTarget.mode == ReflectionProbeMode.Baked && Lightmapping.giWorkflowMode != Lightmapping.GIWorkflowMode.OnDemand)
            {
                EditorGUILayout.HelpBox("Baking of this reflection probe is automatic because this probe's type is 'Baked' and the Lighting window is using 'Auto Baking'. The cubemap created is stored in the GI cache.", MessageType.Info);
                return;
            }

            GUILayout.BeginHorizontal();

            GUILayout.Space(EditorGUIUtility.labelWidth);
            switch (reflectionProbeMode)
            {
                case ReflectionProbeMode.Custom:
                    if (EditorGUI.ButtonWithDropdownList(Styles.bakeCustomButtonText, Styles.bakeCustomOptionText, OnBakeCustomButton))
                    {
                        BakeCustomReflectionProbe(reflectionProbeTarget, true);
                        GUIUtility.ExitGUI();
                    }
                    break;

                case ReflectionProbeMode.Baked:
                    using (new EditorGUI.DisabledScope(!reflectionProbeTarget.enabled))
                    {
                        // Bake button in non-continous mode
                        if (EditorGUI.ButtonWithDropdownList(Styles.bakeButtonText, Styles.bakeButtonsText, OnBakeButton))
                        {
                            Lightmapping.BakeReflectionProbeSnapshot(reflectionProbeTarget);
                            GUIUtility.ExitGUI();
                        }
                    }

                    break;

                case ReflectionProbeMode.Realtime:
                    // Not showing bake button in realtime
                    break;
            }

            GUILayout.EndHorizontal();
        }

        void DoToolbar()
        {
            // Show the master tool selector
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.changed = false;
            var oldEditMode = EditMode.editMode;

            EditorGUI.BeginChangeCheck();
            EditMode.DoInspectorToolbar(Styles.sceneViewEditModes, Styles.toolContents, this);
            if (EditorGUI.EndChangeCheck())
                s_LastInteractedEditor = this;

            if (oldEditMode != EditMode.editMode)
            {
                switch (EditMode.editMode)
                {
                    case EditMode.SceneViewEditMode.ReflectionProbeOrigin:
                        UpdateOldLocalSpace();
                        break;
                }
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

        ReflectionProbeMode reflectionProbeMode
        {
            get { return reflectionProbeTarget.mode; }
        }


        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (targets.Length == 1)
                DoToolbar();

            m_ShowProbeModeRealtimeOptions.target = reflectionProbeMode == ReflectionProbeMode.Realtime;
            m_ShowProbeModeCustomOptions.target = reflectionProbeMode == ReflectionProbeMode.Custom;

            // Bake/custom/realtime type
            EditorGUILayout.IntPopup(m_Mode, Styles.reflectionProbeMode, Styles.reflectionProbeModeValues, Styles.typeText);

            // We cannot show multiple different type controls
            if (!m_Mode.hasMultipleDifferentValues)
            {
                EditorGUI.indentLevel++;
                {
                    // Custom cubemap UI (Bake button and manual cubemap assignment)
                    if (EditorGUILayout.BeginFadeGroup(m_ShowProbeModeCustomOptions.faded))
                    {
                        EditorGUILayout.PropertyField(m_RenderDynamicObjects, Styles.renderDynamicObjects);

                        EditorGUI.BeginChangeCheck();
                        EditorGUI.showMixedValue = m_CustomBakedTexture.hasMultipleDifferentValues;
                        var newCubemap = EditorGUILayout.ObjectField(Styles.customCubemapText, m_CustomBakedTexture.objectReferenceValue, typeof(Cubemap), false);
                        EditorGUI.showMixedValue = false;
                        if (EditorGUI.EndChangeCheck())
                            m_CustomBakedTexture.objectReferenceValue = newCubemap;
                    }
                    EditorGUILayout.EndFadeGroup();

                    // Realtime UI
                    if (EditorGUILayout.BeginFadeGroup(m_ShowProbeModeRealtimeOptions.faded))
                    {
                        EditorGUILayout.PropertyField(m_RefreshMode, Styles.refreshMode);
                        EditorGUILayout.PropertyField(m_TimeSlicingMode, Styles.timeSlicing);

                        EditorGUILayout.Space();
                    }
                    EditorGUILayout.EndFadeGroup();
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            GUILayout.Label(Styles.runtimeSettingsHeader);

            EditorGUI.indentLevel++;
            {
                EditorGUILayout.PropertyField(m_Importance, Styles.importanceText);
                EditorGUILayout.PropertyField(m_IntensityMultiplier, Styles.intensityText);

                if (Rendering.EditorGraphicsSettings.GetCurrentTierSettings().reflectionProbeBoxProjection == false)
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.Toggle(Styles.boxProjectionText, false);
                    }
                }
                else
                {
                    EditorGUILayout.PropertyField(m_BoxProjection, Styles.boxProjectionText);
                }

                bool isDeferredRenderingPath = SceneView.IsUsingDeferredRenderingPath();
                bool isDeferredReflections = isDeferredRenderingPath && (UnityEngine.Rendering.GraphicsSettings.GetShaderMode(BuiltinShaderType.DeferredReflections) != BuiltinShaderMode.Disabled);
                using (new EditorGUI.DisabledScope(!isDeferredReflections))
                {
                    EditorGUILayout.PropertyField(m_BlendDistance, Styles.blendDistanceText);
                }

                // Bounds editing (box projection bounds + the bounds that objects use to check if they should be affected by this reflection probe)
                if (EditorGUILayout.BeginFadeGroup(m_ShowBoxOptions.faded))
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(m_BoxSize, Styles.sizeText);
                    EditorGUILayout.PropertyField(m_BoxOffset, Styles.centerText);

                    if (EditorGUI.EndChangeCheck())
                    {
                        Vector3 center = m_BoxOffset.vector3Value;
                        Vector3 size = m_BoxSize.vector3Value;
                        if (ValidateAABB(ref center, ref size))
                        {
                            m_BoxOffset.vector3Value = center;
                            m_BoxSize.vector3Value = size;
                        }
                    }
                }
                EditorGUILayout.EndFadeGroup();
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();
            GUILayout.Label(Styles.captureCubemapHeaderText);

            EditorGUI.indentLevel++;
            {
                int[] reflectionResolutionValuesArray = null;
                GUIContent[] reflectionResolutionTextArray = null;
                GetResolutionArray(ref reflectionResolutionValuesArray, ref reflectionResolutionTextArray);

                EditorGUILayout.IntPopup(m_Resolution, reflectionResolutionTextArray, reflectionResolutionValuesArray, Styles.resolutionText, GUILayout.MinWidth(40));
                EditorGUILayout.PropertyField(m_HDR);
                EditorGUILayout.PropertyField(m_ShadowDistance);
                EditorGUILayout.IntPopup(m_ClearFlags, Styles.clearFlags, Styles.clearFlagsValues, Styles.clearFlagsText);
                EditorGUILayout.PropertyField(m_BackgroundColor, Styles.backgroundColorText);
                EditorGUILayout.PropertyField(m_CullingMask);
                EditorGUILayout.PropertyField(m_UseOcclusionCulling);
                EditorGUILayout.PropertiesField(EditorGUI.s_ClipingPlanesLabel, m_NearAndFarProperties, EditorGUI.s_NearAndFarLabels, EditorGUI.kNearFarLabelsWidth);
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();

            if (targets.Length == 1)
            {
                ReflectionProbe probe = (ReflectionProbe)target;
                if (probe.mode == ReflectionProbeMode.Custom && probe.customBakedTexture != null)
                {
                    Cubemap cubemap = probe.customBakedTexture as Cubemap;
                    if (cubemap && cubemap.mipmapCount == 1)
                        EditorGUILayout.HelpBox("No mipmaps in the cubemap, Smoothness value in Standard shader will be ignored.", MessageType.Warning);
                }
            }

            DoBakeButton();
            EditorGUILayout.Space();
            serializedObject.ApplyModifiedProperties();
        }

        internal override Bounds GetWorldBoundsOfTarget(Object targetObject)
        {
            return ((ReflectionProbe)targetObject).bounds;
        }

        bool ValidPreviewSetup()
        {
            ReflectionProbe p = (ReflectionProbe)target;
            return (p != null && p.texture != null);
        }

        public override bool HasPreviewGUI()
        {
            if (targets.Length > 1)
                return false;  // We only handle one preview for reflection probes

            // Ensure valid cube map editor (if possible)
            if (ValidPreviewSetup())
            {
                Editor editor = m_CubemapEditor;
                Editor.CreateCachedEditor(((ReflectionProbe)target).texture, null, ref editor);
                m_CubemapEditor = editor as TextureInspector;
            }

            // If having one probe selected we always want preview (to prevent preview window from popping)
            return true;
        }

        public override void OnPreviewSettings()
        {
            if (!ValidPreviewSetup())
                return;

            m_CubemapEditor.mipLevel = m_MipLevelPreview;

            EditorGUI.BeginChangeCheck();
            m_CubemapEditor.OnPreviewSettings();
            // Need to repaint, because mipmap value changes affect reflection probe preview in the scene
            if (EditorGUI.EndChangeCheck())
            {
                EditorApplication.SetSceneRepaintDirty();
                m_MipLevelPreview = m_CubemapEditor.mipLevel;
            }
        }

        public override void OnPreviewGUI(Rect position, GUIStyle style)
        {
            // Fix for case 939947 where we didn't get the Layout event if the texture was null when changing color
            if (!ValidPreviewSetup() && Event.current.type != EventType.ExecuteCommand)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                Color prevColor = GUI.color;
                GUI.color = new Color(1, 1, 1, 0.5f);
                GUILayout.Label("Reflection Probe not baked/ready yet");
                GUI.color = prevColor;
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                return;
            }

            ReflectionProbe p = target as ReflectionProbe;
            if (p != null && p.texture != null && targets.Length == 1)
            {
                Editor editor = m_CubemapEditor;
                Editor.CreateCachedEditor(p.texture, null, ref editor);
                m_CubemapEditor = editor as TextureInspector;
            }

            if (m_CubemapEditor != null)
            {
                m_CubemapEditor.SetCubemapIntensity(GetProbeIntensity((ReflectionProbe)target));
                m_CubemapEditor.OnPreviewGUI(position, style);
            }
        }

        private static Mesh sphereMesh
        {
            get { return s_SphereMesh ?? (s_SphereMesh = Resources.GetBuiltinResource(typeof(Mesh), "New-Sphere.fbx") as Mesh); }
        }

        private Material reflectiveMaterial
        {
            get
            {
                if (m_ReflectiveMaterial == null)
                {
                    m_ReflectiveMaterial = (Material)Instantiate(EditorGUIUtility.Load("Previews/PreviewCubemapMaterial.mat"));
                    m_ReflectiveMaterial.hideFlags = HideFlags.HideAndDontSave;
                }
                return m_ReflectiveMaterial;
            }
        }

        private float GetProbeIntensity(ReflectionProbe p)
        {
            if (p == null || p.texture == null)
                return 1.0f;

            float intensity = p.intensity;
            if (TextureUtil.GetTextureColorSpaceString(p.texture) == "Linear")
                intensity = Mathf.LinearToGammaSpace(intensity);
            return intensity;
        }

        // Draw Reflection probe preview sphere
        private void OnPreSceneGUICallback(SceneView sceneView)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            foreach (var t in targets)
            {
                ReflectionProbe p = (ReflectionProbe)t;
                if (!reflectiveMaterial)
                    return;

                Matrix4x4 m = new Matrix4x4();

                // @TODO: use MaterialPropertyBlock instead - once it actually works!
                // Tried to use MaterialPropertyBlock in 5.4.0b2, but would get incorrectly set parameters when using with Graphics.DrawMesh
                if (!m_CachedGizmoMaterials.ContainsKey(p))
                    m_CachedGizmoMaterials.Add(p, Instantiate(reflectiveMaterial));
                Material mat = m_CachedGizmoMaterials[p] as Material;
                if (!mat)
                    return;
                {
                    // Get mip level
                    float mipLevel = 0.0F;
                    TextureInspector cubemapEditor = m_CubemapEditor as TextureInspector;
                    if (cubemapEditor)
                        mipLevel = cubemapEditor.GetMipLevelForRendering();

                    mat.SetTexture("_MainTex", p.texture);
                    mat.SetMatrix("_CubemapRotation", Matrix4x4.identity);
                    mat.SetFloat("_Mip", mipLevel);
                    mat.SetFloat("_Alpha", 0.0f);
                    mat.SetFloat("_Intensity", GetProbeIntensity(p));

                    // draw a preview sphere that scales with overall GO scale, but always uniformly
                    var scale = p.transform.lossyScale.magnitude * 0.5f;
                    m.SetTRS(p.transform.position, Quaternion.identity, new Vector3(scale, scale, scale));
                    Graphics.DrawMesh(sphereMesh, m, mat, 0, SceneView.currentDrawingSceneView.camera, 0);
                }
            }
        }

        // Ensures that probe's AABB encapsulates probe's position
        // Returns true, if center or size was modified
        private bool ValidateAABB(ref Vector3 center, ref Vector3 size)
        {
            ReflectionProbe p = (ReflectionProbe)target;

            Matrix4x4 localSpace = GetLocalSpace(p);
            Vector3 localTransformPosition = localSpace.inverse.MultiplyPoint3x4(p.transform.position);

            Bounds b = new Bounds(center, size);

            if (b.Contains(localTransformPosition)) return false;

            b.Encapsulate(localTransformPosition);

            center =  b.center;
            size = b.size;
            return true;
        }

        [DrawGizmo(GizmoType.Active)]
        static void RenderBoxGizmo(ReflectionProbe reflectionProbe, GizmoType gizmoType)
        {
            if (s_LastInteractedEditor == null)
                return;

            if (s_LastInteractedEditor.sceneViewEditing && EditMode.editMode == EditMode.SceneViewEditMode.ReflectionProbeBox)
            {
                Color oldColor = Gizmos.color;
                Gizmos.color = kGizmoReflectionProbe;

                Gizmos.matrix = GetLocalSpace(reflectionProbe);
                Gizmos.DrawCube(reflectionProbe.center, -1f * reflectionProbe.size);
                Gizmos.matrix = Matrix4x4.identity;
                Gizmos.color = oldColor;
            }
        }

        [DrawGizmo(GizmoType.Selected)]
        static void RenderBoxOutline(ReflectionProbe reflectionProbe, GizmoType gizmoType)
        {
            Color oldColor = Gizmos.color;
            Gizmos.color = reflectionProbe.isActiveAndEnabled ? kGizmoReflectionProbe : kGizmoReflectionProbeDisabled;

            Gizmos.matrix = GetLocalSpace(reflectionProbe);
            Gizmos.DrawWireCube(reflectionProbe.center, reflectionProbe.size);
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = oldColor;
        }

        public void OnSceneGUI()
        {
            if (!sceneViewEditing)
                return;

            switch (EditMode.editMode)
            {
                case EditMode.SceneViewEditMode.ReflectionProbeBox:
                    DoBoxEditing();
                    break;
                case EditMode.SceneViewEditMode.ReflectionProbeOrigin:
                    DoOriginEditing();
                    break;
            }
        }

        void UpdateOldLocalSpace()
        {
            m_OldLocalSpace = GetLocalSpace((ReflectionProbe)target);
        }

        void DoOriginEditing()
        {
            ReflectionProbe p = (ReflectionProbe)target;
            Vector3 transformPosition = p.transform.position;
            Vector3 size = p.size;

            EditorGUI.BeginChangeCheck();
            Vector3 newPostion = Handles.PositionHandle(transformPosition, GetLocalSpaceRotation(p));

            bool changed = EditorGUI.EndChangeCheck();

            if (changed || m_OldLocalSpace != GetLocalSpace((ReflectionProbe)target))
            {
                Vector3 localNewPosition = m_OldLocalSpace.inverse.MultiplyPoint3x4(newPostion);

                Bounds b = new Bounds(p.center, size);
                localNewPosition = b.ClosestPoint(localNewPosition);

                Undo.RecordObject(p.transform, "Modified Reflection Probe Origin");
                p.transform.position = m_OldLocalSpace.MultiplyPoint3x4(localNewPosition);

                Undo.RecordObject(p, "Modified Reflection Probe Origin");
                p.center = GetLocalSpace(p).inverse.MultiplyPoint3x4(m_OldLocalSpace.MultiplyPoint3x4(p.center));

                EditorUtility.SetDirty(target);

                UpdateOldLocalSpace();
            }
        }

        static Matrix4x4 GetLocalSpace(ReflectionProbe probe)
        {
            Vector3 t = probe.transform.position;
            return Matrix4x4.TRS(t, GetLocalSpaceRotation(probe), Vector3.one);
        }

        static Quaternion GetLocalSpaceRotation(ReflectionProbe probe)
        {
            bool supportsRotation = (SupportedRenderingFeatures.active.reflectionProbe & SupportedRenderingFeatures.ReflectionProbe.Rotation) != 0;
            if (supportsRotation)
                return probe.transform.rotation;
            else
                return Quaternion.identity;
        }

        void DoBoxEditing()
        {
            // Drawing of the probe box is done from GizmoDrawers.cpp,
            // here we only want to show the box editing handles when needed.
            ReflectionProbe p = (ReflectionProbe)target;

            using (new Handles.DrawingScope(GetLocalSpace(p)))
            {
                m_BoundsHandle.center = p.center;
                m_BoundsHandle.size = p.size;

                EditorGUI.BeginChangeCheck();
                m_BoundsHandle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(p, "Modified Reflection Probe AABB");
                    Vector3 center = m_BoundsHandle.center;
                    Vector3 size = m_BoundsHandle.size;
                    ValidateAABB(ref center, ref size);
                    p.center = center;
                    p.size = size;
                    EditorUtility.SetDirty(target);
                }
            }
        }
    }
}
