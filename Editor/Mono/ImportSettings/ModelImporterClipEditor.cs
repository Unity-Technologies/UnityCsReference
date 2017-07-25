// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditorInternal;
using System.Collections.Generic;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    internal class ModelImporterClipEditor : BaseAssetImporterTabUI
    {
        AnimationClipEditor m_AnimationClipEditor;
        ModelImporter singleImporter { get { return targets[0] as ModelImporter; } }

        public int m_SelectedClipIndexDoNotUseDirectly = -1;
        public int selectedClipIndex
        {
            get { return m_SelectedClipIndexDoNotUseDirectly; }
            set
            {
                m_SelectedClipIndexDoNotUseDirectly = value;
                if (m_ClipList != null)
                    m_ClipList.index = value;
            }
        }

        public string selectedClipName
        {
            get
            {
                var clipInfo = GetSelectedClipInfo();
                return clipInfo != null ? clipInfo.name : "";
            }
        }

        SerializedObject m_DefaultClipsSerializedObject = null;

        SerializedProperty m_AnimationType;

        SerializedProperty m_ImportAnimation;
        SerializedProperty m_ClipAnimations;
        SerializedProperty m_BakeSimulation;
        SerializedProperty m_ResampleCurves;
        SerializedProperty m_AnimationCompression;
        SerializedProperty m_AnimationRotationError;
        SerializedProperty m_AnimationPositionError;
        SerializedProperty m_AnimationScaleError;
        SerializedProperty m_AnimationWrapMode;
        SerializedProperty m_LegacyGenerateAnimations;
        SerializedProperty m_ImportAnimatedCustomProperties;

        SerializedProperty m_MotionNodeName;
        public int motionNodeIndex { get; set; }

        public int pivotNodeIndex { get; set; }

        private SerializedProperty m_RigImportErrors;
        private SerializedProperty m_RigImportWarnings;
        private SerializedProperty m_AnimationImportErrors;
        private SerializedProperty m_AnimationImportWarnings;
        private SerializedProperty m_AnimationRetargetingWarnings;
        private SerializedProperty m_AnimationDoRetargetingWarnings;

        GUIContent[] m_MotionNodeList;
        static private bool motionNodeFoldout = false;

        private static bool importMessageFoldout = false;

        ReorderableList m_ClipList;

        private string[] referenceTransformPaths
        {
            get { return singleImporter.transformPaths; }
        }

        private ModelImporterAnimationType animationType
        {
            get { return (ModelImporterAnimationType)m_AnimationType.intValue; }
            set { m_AnimationType.intValue = (int)value; }
        }

        private ModelImporterGenerateAnimations legacyGenerateAnimations
        {
            get { return (ModelImporterGenerateAnimations)m_LegacyGenerateAnimations.intValue; }
            set { m_LegacyGenerateAnimations.intValue = (int)value; }
        }

        private class Styles
        {
            public GUIContent ImportAnimations = EditorGUIUtility.TextContent("Import Animation|Controls if animations are imported.");

            public GUIStyle numberStyle = new GUIStyle(EditorStyles.label);

            public GUIContent AnimWrapModeLabel = EditorGUIUtility.TextContent("Wrap Mode|The default Wrap Mode for the animation in the mesh being imported.");

            public GUIContent[] AnimWrapModeOpt =
            {
                EditorGUIUtility.TextContent("Default|The animation plays as specified in the animation splitting options below."),
                EditorGUIUtility.TextContent("Once|The animation plays through to the end once and then stops."),
                EditorGUIUtility.TextContent("Loop|The animation plays through and then restarts when the end is reached."),
                EditorGUIUtility.TextContent("PingPong|The animation plays through and then plays in reverse from the end to the start, and so on."),
                EditorGUIUtility.TextContent("ClampForever|The animation plays through but the last frame is repeated indefinitely. This is not the same as Once mode because playback does not technically stop at the last frame (which is useful when blending animations).")
            };

            public GUIContent BakeIK = EditorGUIUtility.TextContent("Bake Animations|Enable this when using IK or simulation in your animation package. Unity will convert to forward kinematics on import. This option is available only for Maya, 3dsMax and Cinema4D files.");
            public GUIContent ResampleCurves = EditorGUIUtility.TextContent("Resample Curves | Curves will be resampled on every frame. Use this if you're having issues with the interpolation between keys in your original animation. Disable this to keep curves as close as possible to how they were originally authored.");
            public GUIContent AnimCompressionLabel = EditorGUIUtility.TextContent("Anim. Compression|The type of compression that will be applied to this mesh's animation(s).");
            public GUIContent[] AnimCompressionOptLegacy =
            {
                EditorGUIUtility.TextContent("Off|Disables animation compression. This means that Unity doesn't reduce keyframe count on import, which leads to the highest precision animations, but slower performance and bigger file and runtime memory size. It is generally not advisable to use this option - if you need higher precision animation, you should enable keyframe reduction and lower allowed Animation Compression Error values instead."),
                EditorGUIUtility.TextContent("Keyframe Reduction|Reduces keyframes on import. If selected, the Animation Compression Errors options are displayed."),
                EditorGUIUtility.TextContent("Keyframe Reduction and Compression|Reduces keyframes on import and compresses keyframes when storing animations in files. This affects only file size - the runtime memory size is the same as Keyframe Reduction. If selected, the Animation Compression Errors options are displayed.")
            };
            public GUIContent[] AnimCompressionOpt =
            {
                EditorGUIUtility.TextContent("Off|Disables animation compression. This means that Unity doesn't reduce keyframe count on import, which leads to the highest precision animations, but slower performance and bigger file and runtime memory size. It is generally not advisable to use this option - if you need higher precision animation, you should enable keyframe reduction and lower allowed Animation Compression Error values instead."),
                EditorGUIUtility.TextContent("Keyframe Reduction|Reduces keyframes on import. If selected, the Animation Compression Errors options are displayed."),
                EditorGUIUtility.TextContent("Optimal|Reduces keyframes on import and choose between different curve representations to reduce memory usage at runtime. This affects the runtime memory size and how curves are evaluated.")
            };

            public GUIContent AnimRotationErrorLabel = EditorGUIUtility.TextContent("Rotation Error|Defines how much rotation curves should be reduced. The smaller value you use - the higher precision you get.");
            public GUIContent AnimPositionErrorLabel = EditorGUIUtility.TextContent("Position Error|Defines how much position curves should be reduced. The smaller value you use - the higher precision you get.");
            public GUIContent AnimScaleErrorLabel = EditorGUIUtility.TextContent("Scale Error|Defines how much scale curves should be reduced. The smaller value you use - the higher precision you get.");
            public GUIContent AnimationCompressionHelp = EditorGUIUtility.TextContent("Rotation error is defined as maximum angle deviation allowed in degrees, for others it is defined as maximum distance/delta deviation allowed in percents");
            public GUIContent clipMultiEditInfo = new GUIContent("Multi-object editing of clips not supported.");

            public GUIContent updateMuscleDefinitionFromSource = EditorGUIUtility.TextContent("Update|Update the copy of the muscle definition from the source.");

            public GUIContent MotionSetting = EditorGUIUtility.TextContent("Motion|Advanced setting for root motion and blending pivot");
            public GUIContent MotionNode = EditorGUIUtility.TextContent("Root Motion Node|Define a transform node that will be used to create root motion curves");
            public GUIContent ImportMessages = EditorGUIUtility.TextContent("Import Messages");

            public GUIContent GenerateRetargetingWarnings = EditorGUIUtility.TextContent("Generate Retargeting Quality Report");

            public GUIContent Mask = EditorGUIUtility.TextContent("Mask|Configure the mask for this clip to remove unnecessary curves.");

            public GUIContent ImportAnimatedCustomProperties = EditorGUIUtility.TextContent("Animated Custom Properties|Controls if animated custom properties are imported.");

            public Styles()
            {
                numberStyle.alignment = TextAnchor.UpperRight;
            }
        }
        static Styles styles;

        public ModelImporterClipEditor(AssetImporterEditor panelContainer)
            : base(panelContainer)
        {}

        internal override void OnEnable()
        {
            m_ClipAnimations = serializedObject.FindProperty("m_ClipAnimations");

            m_AnimationType = serializedObject.FindProperty("m_AnimationType");
            m_LegacyGenerateAnimations = serializedObject.FindProperty("m_LegacyGenerateAnimations");

            // Animation
            m_ImportAnimation = serializedObject.FindProperty("m_ImportAnimation");
            m_BakeSimulation = serializedObject.FindProperty("m_BakeSimulation");
            m_ResampleCurves = serializedObject.FindProperty("m_ResampleCurves");
            m_AnimationCompression = serializedObject.FindProperty("m_AnimationCompression");
            m_AnimationRotationError = serializedObject.FindProperty("m_AnimationRotationError");
            m_AnimationPositionError = serializedObject.FindProperty("m_AnimationPositionError");
            m_AnimationScaleError = serializedObject.FindProperty("m_AnimationScaleError");
            m_AnimationWrapMode = serializedObject.FindProperty("m_AnimationWrapMode");
            m_ImportAnimatedCustomProperties = serializedObject.FindProperty("m_ImportAnimatedCustomProperties");

            m_RigImportErrors = serializedObject.FindProperty("m_RigImportErrors");
            m_RigImportWarnings = serializedObject.FindProperty("m_RigImportWarnings");
            m_AnimationImportErrors = serializedObject.FindProperty("m_AnimationImportErrors");
            m_AnimationImportWarnings = serializedObject.FindProperty("m_AnimationImportWarnings");
            m_AnimationRetargetingWarnings = serializedObject.FindProperty("m_AnimationRetargetingWarnings");
            m_AnimationDoRetargetingWarnings = serializedObject.FindProperty("m_AnimationDoRetargetingWarnings");

            if (serializedObject.isEditingMultipleObjects)
                return;

            // Find all serialized property before calling SetupDefaultClips
            if (m_ClipAnimations.arraySize == 0)
                SetupDefaultClips();

            selectedClipIndex = EditorPrefs.GetInt("ModelImporterClipEditor.ActiveClipIndex", 0);
            ValidateClipSelectionIndex();
            EditorPrefs.SetInt("ModelImporterClipEditor.ActiveClipIndex", selectedClipIndex);

            if (m_AnimationClipEditor != null && selectedClipIndex >= 0)
                SyncClipEditor();

            // Automatically select the first clip
            if (m_ClipAnimations.arraySize != 0)
                SelectClip(selectedClipIndex);

            string[] transformPaths = singleImporter.transformPaths;
            m_MotionNodeList = new GUIContent[transformPaths.Length + 1];

            m_MotionNodeList[0] = new GUIContent("<None>");

            for (int i = 0; i < transformPaths.Length; i++)
            {
                if (i == 0)
                {
                    m_MotionNodeList[1] = new GUIContent("<Root Transform>");
                }
                else
                {
                    m_MotionNodeList[i + 1] = new GUIContent(transformPaths[i]);
                }
            }

            m_MotionNodeName = serializedObject.FindProperty("m_MotionNodeName");
            motionNodeIndex = ArrayUtility.FindIndex(m_MotionNodeList, delegate(GUIContent content) { return content.text == m_MotionNodeName.stringValue; });
            motionNodeIndex = motionNodeIndex < 1 ? 0 : motionNodeIndex;
        }

        void SyncClipEditor()
        {
            if (m_AnimationClipEditor == null || m_MaskInspector == null)
                return;

            AnimationClipInfoProperties info = GetAnimationClipInfoAtIndex(selectedClipIndex);

            // It mandatory to set clip info into mask inspector first, this will update m_Mask.
            m_MaskInspector.clipInfo = info;

            m_AnimationClipEditor.ShowRange(info);
            m_AnimationClipEditor.mask = m_Mask;
        }

        private void SetupDefaultClips()
        {
            // Create dummy SerializedObject where we can add a clip for each
            // take without making any properties show up as changed.
            m_DefaultClipsSerializedObject = new SerializedObject(target);
            m_ClipAnimations = m_DefaultClipsSerializedObject.FindProperty("m_ClipAnimations");
            m_AnimationType = m_DefaultClipsSerializedObject.FindProperty("m_AnimationType");
            m_ClipAnimations.arraySize = 0;

            foreach (TakeInfo takeInfo in singleImporter.importedTakeInfos)
            {
                AddClip(takeInfo);
            }
        }

        // When switching to explicitly defined clips, we must fix up the recycleID's to not lose AnimationClip references.
        // When m_ClipAnimations is defined, the clips are identified by the clipName
        // When m_ClipAnimations is not defined, the clips are identified by the takeName
        void PatchDefaultClipTakeNamesToSplitClipNames()
        {
            foreach (TakeInfo takeInfo in singleImporter.importedTakeInfos)
            {
                PatchImportSettingRecycleID.Patch(serializedObject, 74, takeInfo.name, takeInfo.defaultClipName);
            }
        }

        // A dummy SerializedObject is created when there are no explicitly defined clips.
        // When the user modifies any settings these clips must be transferred to the model importer.
        private void TransferDefaultClipsToCustomClips()
        {
            if (m_DefaultClipsSerializedObject == null)
                return;

            bool wasEmpty = serializedObject.FindProperty("m_ClipAnimations").arraySize == 0;
            if (!wasEmpty)
                Debug.LogError("Transferring default clips failed, target already has clips");

            // Transfer data to main SerializedObject
            serializedObject.CopyFromSerializedProperty(m_ClipAnimations);
            m_ClipAnimations = serializedObject.FindProperty("m_ClipAnimations");

            m_DefaultClipsSerializedObject = null;

            PatchDefaultClipTakeNamesToSplitClipNames();

            SyncClipEditor();
        }

        private void ValidateClipSelectionIndex()
        {
            // selected clip index can be invalid if array was changed and then reverted.
            if (selectedClipIndex > m_ClipAnimations.arraySize - 1)
            {
                selectedClipIndex = 0;
            }
        }

        internal override void OnDestroy()
        {
            DestroyEditorsAndData();
        }

        internal override void OnDisable()
        {
            DestroyEditorsAndData();

            base.OnDisable();
        }

        internal override void ResetValues()
        {
            base.ResetValues();
            m_ClipAnimations = serializedObject.FindProperty("m_ClipAnimations");
            m_AnimationType = serializedObject.FindProperty("m_AnimationType");
            m_DefaultClipsSerializedObject = null;
            if (m_ClipAnimations.arraySize == 0)
                SetupDefaultClips();

            ValidateClipSelectionIndex();
            UpdateList();
            SelectClip(selectedClipIndex);
        }

        void AnimationClipGUI()
        {
            string errors = m_AnimationImportErrors.stringValue;
            string warnings = m_AnimationImportWarnings.stringValue;
            string rigWarnings = m_RigImportWarnings.stringValue;
            string retargetWarnings = m_AnimationRetargetingWarnings.stringValue;

            if (errors.Length > 0)
            {
                EditorGUILayout.HelpBox("Error(s) found while importing this animation file. Open \"Import Messages\" foldout below for more details.", MessageType.Error);
            }
            else
            {
                if (rigWarnings.Length > 0)
                {
                    EditorGUILayout.HelpBox("Warning(s) found while importing rig in this animation file. Open \"Rig\" tab for more details.", MessageType.Warning);
                }

                if (warnings.Length > 0)
                {
                    EditorGUILayout.HelpBox("Warning(s) found while importing this animation file. Open \"Import Messages\" foldout below for more details.", MessageType.Warning);
                }
            }

            // Show general animation import settings
            AnimationSettings();

            if (serializedObject.isEditingMultipleObjects)
                return;

            Profiler.BeginSample("Clip inspector");

            EditorGUILayout.Space();

            // Show list of animations and inspector for individual animation
            if (targets.Length == 1)
                AnimationSplitTable();
            else
                GUILayout.Label(styles.clipMultiEditInfo, EditorStyles.helpBox);

            Profiler.EndSample();

            RootMotionNodeSettings();

            importMessageFoldout = EditorGUILayout.Foldout(importMessageFoldout, styles.ImportMessages, true);

            if (importMessageFoldout)
            {
                if (errors.Length > 0)
                    EditorGUILayout.HelpBox(errors, MessageType.Error);
                if (warnings.Length > 0)
                    EditorGUILayout.HelpBox(warnings, MessageType.Warning);
                if (animationType == ModelImporterAnimationType.Human)
                {
                    EditorGUILayout.PropertyField(m_AnimationDoRetargetingWarnings, styles.GenerateRetargetingWarnings);

                    if (m_AnimationDoRetargetingWarnings.boolValue)
                    {
                        if (retargetWarnings.Length > 0)
                        {
                            EditorGUILayout.HelpBox(retargetWarnings, MessageType.Info);
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Retargeting Quality compares retargeted with original animation. It reports average and maximum position/orientation difference for body parts. It may slow down import time of this file.", MessageType.Info);
                    }
                }
            }
        }

        public override void OnInspectorGUI()
        {
            if (styles == null)
                styles = new Styles();

            EditorGUILayout.PropertyField(m_ImportAnimation, styles.ImportAnimations);

            if (m_ImportAnimation.boolValue && !m_ImportAnimation.hasMultipleDifferentValues)
            {
                bool hasNoValidAnimationData = targets.Length == 1 && singleImporter.importedTakeInfos.Length == 0 && singleImporter.animationType != ModelImporterAnimationType.None;

                if (IsDeprecatedMultiAnimationRootImport())
                    EditorGUILayout.HelpBox("Animation data was imported using a deprecated Generation option in the Rig tab. Please switch to a non-deprecated import mode in the Rig tab to be able to edit the animation import settings.", MessageType.Info);
                else if (hasNoValidAnimationData)
                {
                    if (serializedObject.hasModifiedProperties)
                    {
                        EditorGUILayout.HelpBox("The animations settings can be edited after clicking Apply.", MessageType.Info);
                    }
                    else
                    {
                        string errors = m_RigImportErrors.stringValue;

                        if (errors.Length > 0)
                        {
                            EditorGUILayout.HelpBox("Error(s) found while importing rig in this animation file. Open \"Rig\" tab for more details.", MessageType.Error);
                        }
                        else
                        {
                            EditorGUILayout.HelpBox("No animation data available in this model.", MessageType.Info);
                        }
                    }
                }
                else if (m_AnimationType.hasMultipleDifferentValues)
                    EditorGUILayout.HelpBox("The rigs of the selected models have different Animation Types.", MessageType.Info);
                else if (animationType == ModelImporterAnimationType.None)
                    EditorGUILayout.HelpBox("The rigs of the selected models are not setup to handle animation. Change the Animation Type in the Rig tab and click Apply.", MessageType.Info);
                else
                {
                    if (m_ImportAnimation.boolValue && !m_ImportAnimation.hasMultipleDifferentValues)
                        AnimationClipGUI();
                }
            }
        }

        void AnimationSettings()
        {
            EditorGUILayout.Space();

            // Bake IK
            bool isBakeIKSupported = true;
            foreach (ModelImporter importer in targets)
                if (!importer.isBakeIKSupported)
                    isBakeIKSupported = false;
            using (new EditorGUI.DisabledScope(!isBakeIKSupported))
            {
                EditorGUILayout.PropertyField(m_BakeSimulation, styles.BakeIK);
            }

            if (animationType == ModelImporterAnimationType.Generic)
            {
                EditorGUILayout.PropertyField(m_ResampleCurves, styles.ResampleCurves);
            }
            else
            {
                m_ResampleCurves.boolValue = true;
            }

            // Wrap mode
            if (animationType == ModelImporterAnimationType.Legacy)
            {
                EditorGUI.showMixedValue = m_AnimationWrapMode.hasMultipleDifferentValues;
                EditorGUILayout.Popup(m_AnimationWrapMode, styles.AnimWrapModeOpt, styles.AnimWrapModeLabel);
                EditorGUI.showMixedValue = false;

                // Compression
                int[] kCompressionValues = { (int)ModelImporterAnimationCompression.Off, (int)ModelImporterAnimationCompression.KeyframeReduction, (int)ModelImporterAnimationCompression.KeyframeReductionAndCompression };
                EditorGUILayout.IntPopup(m_AnimationCompression, styles.AnimCompressionOptLegacy, kCompressionValues, styles.AnimCompressionLabel);
            }
            else
            {
                // Compression
                int[] kCompressionValues = { (int)ModelImporterAnimationCompression.Off, (int)ModelImporterAnimationCompression.KeyframeReduction, (int)ModelImporterAnimationCompression.Optimal };
                EditorGUILayout.IntPopup(m_AnimationCompression, styles.AnimCompressionOpt, kCompressionValues, styles.AnimCompressionLabel);
            }

            if (m_AnimationCompression.intValue > (int)ModelImporterAnimationCompression.Off)
            {
                // keyframe reduction settings
                EditorGUILayout.PropertyField(m_AnimationRotationError, styles.AnimRotationErrorLabel);
                EditorGUILayout.PropertyField(m_AnimationPositionError, styles.AnimPositionErrorLabel);
                EditorGUILayout.PropertyField(m_AnimationScaleError, styles.AnimScaleErrorLabel);
                GUILayout.Label(styles.AnimationCompressionHelp, EditorStyles.helpBox);
            }

            EditorGUILayout.PropertyField(m_ImportAnimatedCustomProperties, styles.ImportAnimatedCustomProperties);
        }

        void RootMotionNodeSettings()
        {
            if (animationType == ModelImporterAnimationType.Human || animationType == ModelImporterAnimationType.Generic)
            {
                motionNodeFoldout = EditorGUILayout.Foldout(motionNodeFoldout, styles.MotionSetting, true);

                if (motionNodeFoldout)
                {
                    EditorGUI.BeginChangeCheck();
                    motionNodeIndex = EditorGUILayout.Popup(styles.MotionNode, motionNodeIndex, m_MotionNodeList);

                    if (EditorGUI.EndChangeCheck())
                    {
                        if (motionNodeIndex > 0 && motionNodeIndex < m_MotionNodeList.Length)
                        {
                            m_MotionNodeName.stringValue = m_MotionNodeList[motionNodeIndex].text;
                        }
                        else
                        {
                            m_MotionNodeName.stringValue = "";
                        }
                    }
                }
            }
        }

        void DestroyEditorsAndData()
        {
            if (m_AnimationClipEditor != null)
            {
                Object.DestroyImmediate(m_AnimationClipEditor);
                m_AnimationClipEditor = null;
            }

            if (m_MaskInspector)
            {
                DestroyImmediate(m_MaskInspector);
                m_MaskInspector = null;
            }
            if (m_Mask)
            {
                DestroyImmediate(m_Mask);
                m_Mask = null;
            }
        }

        void SelectClip(int selected)
        {
            // If you were editing Clip Name (delayed text field had focus) and then selected a new clip from the clip list,
            // the active string in the delayed text field would get applied to the new selected clip instead of the old.
            // HACK: Calling EndGUI here on the recycled delayed text editor seems to fix this issue.
            // Sometime we should reimplement delayed text field code to not be super confusing and then fix the issue more properly.
            if (EditorGUI.s_DelayedTextEditor != null && Event.current != null)
                EditorGUI.s_DelayedTextEditor.EndGUI(Event.current.type);

            DestroyEditorsAndData();

            selectedClipIndex = selected;
            if (selectedClipIndex < 0 || selectedClipIndex >= m_ClipAnimations.arraySize)
            {
                selectedClipIndex = -1;
                return;
            }

            AnimationClipInfoProperties info = GetAnimationClipInfoAtIndex(selected);
            AnimationClip clip = singleImporter.GetPreviewAnimationClipForTake(info.takeName);
            if (clip != null)
            {
                m_AnimationClipEditor = (AnimationClipEditor)Editor.CreateEditor(clip);
                InitMask(info);
                SyncClipEditor();
            }
        }

        void UpdateList()
        {
            if (m_ClipList == null)
                return;
            List<AnimationClipInfoProperties> clipInfos = new List<AnimationClipInfoProperties>();
            for (int i = 0; i < m_ClipAnimations.arraySize; i++)
                clipInfos.Add(GetAnimationClipInfoAtIndex(i));
            m_ClipList.list = clipInfos;
        }

        void AddClipInList(ReorderableList list)
        {
            if (m_DefaultClipsSerializedObject != null)
                TransferDefaultClipsToCustomClips();


            int takeIndex = 0;
            if (0 < selectedClipIndex && selectedClipIndex < m_ClipAnimations.arraySize)
            {
                AnimationClipInfoProperties info = GetAnimationClipInfoAtIndex(selectedClipIndex);
                for (int i = 0; i < singleImporter.importedTakeInfos.Length; i++)
                {
                    if (singleImporter.importedTakeInfos[i].name == info.takeName)
                    {
                        takeIndex = i;
                        break;
                    }
                }
            }

            AddClip(singleImporter.importedTakeInfos[takeIndex]);
            UpdateList();
            SelectClip(list.list.Count - 1);
        }

        void RemoveClipInList(ReorderableList list)
        {
            TransferDefaultClipsToCustomClips();

            RemoveClip(list.index);
            UpdateList();
            SelectClip(Mathf.Min(list.index, list.count - 1));
        }

        void SelectClipInList(ReorderableList list)
        {
            SelectClip(list.index);
        }

        const int kFrameColumnWidth = 45;

        private void DrawClipElement(Rect rect, int index, bool selected, bool focused)
        {
            AnimationClipInfoProperties info = m_ClipList.list[index] as AnimationClipInfoProperties;
            rect.xMax -= kFrameColumnWidth * 2;
            GUI.Label(rect, info.name, EditorStyles.label);
            rect.x = rect.xMax;
            rect.width = kFrameColumnWidth;
            GUI.Label(rect, info.firstFrame.ToString("0.0"), styles.numberStyle);
            rect.x = rect.xMax;
            GUI.Label(rect, info.lastFrame.ToString("0.0"), styles.numberStyle);
        }

        private void DrawClipHeader(Rect rect)
        {
            rect.xMax -= kFrameColumnWidth * 2;
            GUI.Label(rect, "Clips", EditorStyles.label);
            rect.x = rect.xMax;
            rect.width = kFrameColumnWidth;
            GUI.Label(rect, "Start", styles.numberStyle);
            rect.x = rect.xMax;
            GUI.Label(rect, "End", styles.numberStyle);
        }

        void AnimationSplitTable()
        {
            if (m_ClipList == null)
            {
                m_ClipList = new ReorderableList(new List<AnimationClipInfoProperties>(), typeof(string), false, true, true, true);
                m_ClipList.onAddCallback = AddClipInList;
                m_ClipList.onSelectCallback = SelectClipInList;
                m_ClipList.onRemoveCallback = RemoveClipInList;
                m_ClipList.drawElementCallback = DrawClipElement;
                m_ClipList.drawHeaderCallback = DrawClipHeader;
                m_ClipList.elementHeight = 16;
                UpdateList();
                m_ClipList.index = selectedClipIndex;
            }
            m_ClipList.DoLayoutList();

            EditorGUI.BeginChangeCheck();

            // Show selected clip info
            {
                AnimationClipInfoProperties clip = GetSelectedClipInfo();
                if (clip == null)
                    return;

                if (m_AnimationClipEditor != null && selectedClipIndex != -1)
                {
                    GUILayout.Space(5);

                    AnimationClip actualClip = m_AnimationClipEditor.target as AnimationClip;

                    if (!actualClip.legacy)
                        GetSelectedClipInfo().AssignToPreviewClip(actualClip);

                    TakeInfo[] importedTakeInfos = singleImporter.importedTakeInfos;
                    string[] takeNames = new string[importedTakeInfos.Length];
                    for (int i = 0; i < importedTakeInfos.Length; i++)
                        takeNames[i] = importedTakeInfos[i].name;

                    EditorGUI.BeginChangeCheck();
                    string currentName = clip.name;
                    int takeIndex = ArrayUtility.IndexOf(takeNames, clip.takeName);
                    m_AnimationClipEditor.takeNames = takeNames;
                    m_AnimationClipEditor.takeIndex = ArrayUtility.IndexOf(takeNames, clip.takeName);
                    m_AnimationClipEditor.DrawHeader();

                    if (EditorGUI.EndChangeCheck())
                    {
                        // We renamed the clip name, try to maintain the localIdentifierInFile so we don't lose any data.
                        if (clip.name != currentName)
                        {
                            TransferDefaultClipsToCustomClips();
                            PatchImportSettingRecycleID.Patch(serializedObject, 74, currentName, clip.name);
                        }

                        int newTakeIndex = m_AnimationClipEditor.takeIndex;
                        if (newTakeIndex != -1 && newTakeIndex != takeIndex)
                        {
                            clip.name = MakeUniqueClipName(takeNames[newTakeIndex]);
                            SetupTakeNameAndFrames(clip, importedTakeInfos[newTakeIndex]);
                            GUIUtility.keyboardControl = 0;
                            SelectClip(selectedClipIndex);

                            // actualClip has been changed by SelectClip
                            actualClip = m_AnimationClipEditor.target as AnimationClip;
                        }
                    }

                    m_AnimationClipEditor.OnInspectorGUI();

                    AvatarMaskSettings(GetSelectedClipInfo());

                    if (!actualClip.legacy)
                        GetSelectedClipInfo().ExtractFromPreviewClip(actualClip);
                }
            }

            if (EditorGUI.EndChangeCheck() || m_AnimationClipEditor.needsToGenerateClipInfo)
            {
                TransferDefaultClipsToCustomClips();
                m_AnimationClipEditor.needsToGenerateClipInfo = false;
            }
        }

        public override bool HasPreviewGUI()
        {
            return m_ImportAnimation.boolValue && m_AnimationClipEditor != null && m_AnimationClipEditor.HasPreviewGUI();
        }

        public override void OnPreviewSettings()
        {
            if (m_AnimationClipEditor != null)
                m_AnimationClipEditor.OnPreviewSettings();
        }

        bool IsDeprecatedMultiAnimationRootImport()
        {
            if (animationType == ModelImporterAnimationType.Legacy)
                return legacyGenerateAnimations == ModelImporterGenerateAnimations.InOriginalRoots || legacyGenerateAnimations == ModelImporterGenerateAnimations.InNodes;
            else
                return false;
        }

        public override void OnInteractivePreviewGUI(Rect r, GUIStyle background)
        {
            if (m_AnimationClipEditor)
                m_AnimationClipEditor.OnInteractivePreviewGUI(r, background);
        }

        AnimationClipInfoProperties GetAnimationClipInfoAtIndex(int index)
        {
            return new AnimationClipInfoProperties(m_ClipAnimations.GetArrayElementAtIndex(index));
        }

        AnimationClipInfoProperties GetSelectedClipInfo()
        {
            if (selectedClipIndex >= 0 && selectedClipIndex < m_ClipAnimations.arraySize)
                return GetAnimationClipInfoAtIndex(selectedClipIndex);
            else
                return null;
        }

        /// <summary>
        /// Removes the duplicate brackets from a name.
        /// </summary>
        /// <returns>The name without the duplicate suffix.</returns>
        /// <param name="name">Name.</param>
        /// <param name="number">Number between the brackets (-1 if no brackets were found).</param>
        string RemoveDuplicateSuffix(string name, out int number)
        {
            number = -1;

            // The smallest format is " (1)".
            int length = name.Length;
            if (length < 4 || name[length - 1] != ')')
                return name;

            // Has an opening bracket.
            int openingBracket = name.LastIndexOf('(', length - 2);
            if (openingBracket == -1 || name[openingBracket - 1] != ' ')
                return name;

            // Brackets aren't empty.
            int numberLength = length - openingBracket - 2;
            if (numberLength == 0)
                return name;

            // Only has digits between the brackets.
            int i = 0;
            while (i < numberLength && char.IsDigit(name[openingBracket + 1 + i]))
                ++i;
            if (i != numberLength)
                return name;

            // Get number.
            string numberString = name.Substring(openingBracket + 1, numberLength);
            number = int.Parse(numberString);

            // Extract base name.
            return name.Substring(0, openingBracket - 1);
        }

        /// <summary>
        /// Finds the next duplicate number.
        /// </summary>
        /// <returns>The next duplicate number (-1 if there is no other clip with the same name).</returns>
        /// <param name="baseName">Base name.</param>
        int FindNextDuplicateNumber(string baseName)
        {
            int nextNumber = -1;

            for (int i = 0; i < m_ClipAnimations.arraySize; ++i)
            {
                AnimationClipInfoProperties clip = GetAnimationClipInfoAtIndex(i);

                int clipNumber;
                string clipBaseName = RemoveDuplicateSuffix(clip.name, out clipNumber);
                if (clipBaseName == baseName)
                {
                    // Same base, so next number is at least 1.
                    if (nextNumber == -1)
                        nextNumber = 1;

                    // Next number is one more than the maximum number found.
                    if (clipNumber != -1)
                        nextNumber = Math.Max(nextNumber, clipNumber + 1);
                }
            }

            return nextNumber;
        }

        string MakeUniqueClipName(string name)
        {
            int dummy;
            string baseName = RemoveDuplicateSuffix(name, out dummy);

            int nextNumber = FindNextDuplicateNumber(baseName);
            if (nextNumber != -1)
                name = baseName + " (" + nextNumber + ")";

            return name;
        }

        void RemoveClip(int index)
        {
            m_ClipAnimations.DeleteArrayElementAtIndex(index);
            if (m_ClipAnimations.arraySize == 0)
            {
                SetupDefaultClips();
                m_ImportAnimation.boolValue = false;
            }
        }

        void SetupTakeNameAndFrames(AnimationClipInfoProperties info, TakeInfo takeInfo)
        {
            info.takeName = takeInfo.name;
            info.firstFrame = (int)Mathf.Round(takeInfo.bakeStartTime * takeInfo.sampleRate);
            info.lastFrame = (int)Mathf.Round(takeInfo.bakeStopTime * takeInfo.sampleRate);
        }

        void AddClip(TakeInfo takeInfo)
        {
            string uniqueName = MakeUniqueClipName(takeInfo.defaultClipName);

            m_ClipAnimations.InsertArrayElementAtIndex(m_ClipAnimations.arraySize);
            AnimationClipInfoProperties info = GetAnimationClipInfoAtIndex(m_ClipAnimations.arraySize - 1);

            info.name = uniqueName;
            SetupTakeNameAndFrames(info, takeInfo);
            info.wrapMode = (int)WrapMode.Default;
            info.loop = false;
            info.orientationOffsetY = 0;
            info.level = 0;
            info.cycleOffset = 0;
            info.loopTime = false;
            info.loopBlend = false;
            info.loopBlendOrientation = false;
            info.loopBlendPositionY = false;
            info.loopBlendPositionXZ = false;
            info.keepOriginalOrientation = false;
            info.keepOriginalPositionY = true;
            info.keepOriginalPositionXZ = false;
            info.heightFromFeet = false;
            info.mirror = false;
            info.maskType = ClipAnimationMaskType.None;

            SetBodyMaskDefaultValues(info);


            info.ClearEvents();
            info.ClearCurves();
        }

        private AvatarMask m_Mask = null;
        private AvatarMaskInspector m_MaskInspector = null;
        static private bool m_MaskFoldout = false;

        ////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///
        private void AvatarMaskSettings(AnimationClipInfoProperties clipInfo)
        {
            if (clipInfo != null && m_AnimationClipEditor != null)
            {
                InitMask(clipInfo);
                int prevIndent = EditorGUI.indentLevel;

                // Don't make toggling foldout cause GUI.changed to be true (shouldn't cause undoable action etc.)
                bool wasChanged = GUI.changed;
                m_MaskFoldout = EditorGUILayout.Foldout(m_MaskFoldout, styles.Mask, true);
                GUI.changed = wasChanged;

                if (clipInfo.maskType == ClipAnimationMaskType.CreateFromThisModel && !m_MaskInspector.IsMaskUpToDate() && !m_MaskInspector.IsMaskEmpty())
                {
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);
                    GUILayout.Label("Mask has a path that does not match the transform hierarchy. Animation may not import correctly.",
                        EditorStyles.wordWrappedMiniLabel);
                    GUILayout.FlexibleSpace();
                    GUILayout.BeginVertical();
                    GUILayout.Space(5);
                    if (GUILayout.Button("Update Mask"))
                    {
                        SetTransformMaskFromReference(clipInfo);
                        m_MaskInspector.FillNodeInfos();
                    }

                    GUILayout.EndVertical();
                    GUILayout.EndHorizontal();
                }
                else if (clipInfo.maskType == ClipAnimationMaskType.CopyFromOther && clipInfo.MaskNeedsUpdating())
                {
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);
                    GUILayout.Label("Source Mask has changed since last import and must be updated.",
                        EditorStyles.wordWrappedMiniLabel);
                    GUILayout.FlexibleSpace();
                    GUILayout.BeginVertical();
                    GUILayout.Space(5);
                    if (GUILayout.Button("Update Mask"))
                    {
                        clipInfo.MaskToClip(clipInfo.maskSource);
                    }
                    GUILayout.EndVertical();
                    GUILayout.EndHorizontal();
                }
                else if (clipInfo.maskType == ClipAnimationMaskType.CopyFromOther && !m_MaskInspector.IsMaskUpToDate())
                {
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);
                    GUILayout.Label("Source Mask has a path that does not match the transform hierarchy. Animation may not import correctly.",
                        EditorStyles.wordWrappedMiniLabel);
                    GUILayout.FlexibleSpace();
                    GUILayout.BeginVertical();
                    GUILayout.Space(5);
                    GUILayout.EndVertical();
                    GUILayout.EndHorizontal();
                }

                if (m_MaskFoldout)
                {
                    EditorGUI.indentLevel++;
                    m_MaskInspector.OnInspectorGUI();
                }

                EditorGUI.indentLevel = prevIndent;
            }
        }

        private void InitMask(AnimationClipInfoProperties clipInfo)
        {
            if (m_Mask == null)
            {
                AnimationClip clip = m_AnimationClipEditor.target as AnimationClip;

                m_Mask = new AvatarMask();
                m_MaskInspector = (AvatarMaskInspector)Editor.CreateEditor(m_Mask);
                m_MaskInspector.canImport = false;
                m_MaskInspector.showBody = clip.isHumanMotion;
                m_MaskInspector.clipInfo = clipInfo;
            }
        }

        private void SetTransformMaskFromReference(AnimationClipInfoProperties clipInfo)
        {
            string[] transformPaths = referenceTransformPaths;
            string[] humanTransforms = animationType == ModelImporterAnimationType.Human ?
                AvatarMaskUtility.GetAvatarHumanAndActiveExtraTransforms(serializedObject, clipInfo.transformMaskProperty, transformPaths) :
                AvatarMaskUtility.GetAvatarInactiveTransformMaskPaths(clipInfo.transformMaskProperty);

            AvatarMaskUtility.UpdateTransformMask(clipInfo.transformMaskProperty, transformPaths, humanTransforms, animationType == ModelImporterAnimationType.Human);
        }

        private void SetBodyMaskDefaultValues(AnimationClipInfoProperties clipInfo)
        {
            SerializedProperty bodyMask = clipInfo.bodyMaskProperty;
            bodyMask.ClearArray();
            for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; ++i)
            {
                bodyMask.InsertArrayElementAtIndex(i);
                bodyMask.GetArrayElementAtIndex(i).intValue = 1;
            }
        }
    }
}
