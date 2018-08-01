// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using System.Collections.Generic;
using Object = UnityEngine.Object;
using System.IO;
using System.Linq;
using UnityEditor.Experimental.AssetImporters;
using UnityEditor.IMGUI.Controls;

namespace UnityEditor
{
    internal class ModelImporterRigEditor : BaseAssetImporterTabUI
    {
        const float kDeleteWidth = 17;

        ModelImporter singleImporter { get { return targets[0] as ModelImporter; } }

        public int m_SelectedClipIndex = -1;

        Avatar m_Avatar;

        SerializedProperty m_AnimationType;
        SerializedProperty m_AvatarSource;
        SerializedProperty m_CopyAvatar;
        SerializedProperty m_LegacyGenerateAnimations;
        SerializedProperty m_AnimationCompression;

        SerializedProperty m_RootMotionBoneName;

        SerializedProperty m_SrcHasExtraRoot;
        SerializedProperty m_DstHasExtraRoot;

        SerializedProperty m_RigImportErrors;
        SerializedProperty m_RigImportWarnings;

        SerializedProperty m_OptimizeGameObjects;

        SerializedProperty m_HumanBoneArray;
        SerializedProperty m_Skeleton;

        private static bool importMessageFoldout = false;

        GUIContent[] m_RootMotionBoneList;

        private ExposeTransformEditor m_ExposeTransformEditor;

        private ModelImporterAnimationType animationType
        {
            get { return (ModelImporterAnimationType)m_AnimationType.intValue; }
            set { m_AnimationType.intValue = (int)value; }
        }

        bool m_AvatarCopyIsUpToDate;
        private bool m_CanMultiEditTransformList;

        public int rootIndex { get; set; }

        bool m_IsBiped = false;
        List<string> m_BipedMappingReport = null;

        bool m_ExtraExposedTransformFoldout = false;

        static class Styles
        {
            public static GUIContent AnimationType = EditorGUIUtility.TrTextContent("Animation Type", "The type of animation to support / import.");
            public static GUIContent[] AnimationTypeOpt =
            {
                EditorGUIUtility.TrTextContent("None", "No animation present."),
                EditorGUIUtility.TrTextContent("Legacy", "Legacy animation system."),
                EditorGUIUtility.TrTextContent("Generic", "Generic Mecanim animation."),
                EditorGUIUtility.TrTextContent("Humanoid", "Humanoid Mecanim animation system.")
            };

            public static GUIContent AnimLabel = EditorGUIUtility.TrTextContent("Generation", "Controls how animations are imported.");
            public static GUIContent[] AnimationsOpt =
            {
                EditorGUIUtility.TrTextContent("Don't Import", "No animation or skinning is imported."),
                EditorGUIUtility.TrTextContent("Store in Original Roots (Deprecated)", "Animations are stored in the root objects of your animation package (these might be different from the root objects in Unity)."),
                EditorGUIUtility.TrTextContent("Store in Nodes (Deprecated)", "Animations are stored together with the objects they animate. Use this when you have a complex animation setup and want full scripting control."),
                EditorGUIUtility.TrTextContent("Store in Root (Deprecated)", "Animations are stored in the scene's transform root objects. Use this when animating anything that has a hierarchy."),
                EditorGUIUtility.TrTextContent("Store in Root (New)")
            };

            public static GUIContent avatar = EditorGUIUtility.TrTextContent("Animator");
            public static GUIContent configureAvatar = EditorGUIUtility.TrTextContent("Configure...");
            public static GUIContent avatarValid = EditorGUIUtility.TrTextContent("\u2713");
            public static GUIContent avatarInvalid = EditorGUIUtility.TrTextContent("\u2715");
            public static GUIContent avatarPending = EditorGUIUtility.TrTextContent("...");


            public static GUIContent UpdateMuscleDefinitionFromSource = EditorGUIUtility.TrTextContent("Update", "Update the copy of the muscle definition from the source.");
            public static GUIContent RootNode = EditorGUIUtility.TrTextContent("Root node", "Specify the root node used to extract the animation translation.");

            public static GUIContent AvatarDefinition = EditorGUIUtility.TrTextContent("Avatar Definition", "Choose between Create From This Model or Copy From Other Avatar. The first one create an Avatar for this file and the second one use an Avatar from another file to import animation.");

            public static GUIContent[] AvatarDefinitionOpt =
            {
                EditorGUIUtility.TrTextContent("Create From This Model", "Create an Avatar based on the model from this file."),
                EditorGUIUtility.TrTextContent("Copy From Other Avatar", "Copy an Avatar from another file to import muscle clip. No avatar will be created.")
            };

            public static GUIContent UpdateReferenceClips = EditorGUIUtility.TrTextContent("Update reference clips", "Click on this button to update all the @convention file referencing this file. Should set all these files to Copy From Other Avatar, set the source Avatar to this one and reimport all these files.");

            public static GUIContent ImportMessages = EditorGUIUtility.TrTextContent("Import Messages");
            public static GUIContent ExtraExposedTransform = EditorGUIUtility.TrTextContent("Extra Transforms to Expose", "Select the list of transforms to expose in the optmized GameObject hierarchy.");
        }

        public ModelImporterRigEditor(AssetImporterEditor panelContainer)
            : base(panelContainer)
        {}

        internal override void OnEnable()
        {
            m_AnimationType = serializedObject.FindProperty("m_AnimationType");
            m_AvatarSource = serializedObject.FindProperty("m_LastHumanDescriptionAvatarSource");
            m_OptimizeGameObjects = serializedObject.FindProperty("m_OptimizeGameObjects");

            // Generic bone setup
            m_RootMotionBoneName = serializedObject.FindProperty("m_HumanDescription.m_RootMotionBoneName");

            m_ExposeTransformEditor = new ExposeTransformEditor();

            string[] transformPaths = singleImporter.transformPaths;
            m_RootMotionBoneList = new GUIContent[transformPaths.Length];
            for (int i = 0; i < transformPaths.Length; i++)
                m_RootMotionBoneList[i] = new GUIContent(transformPaths[i]);

            if (m_RootMotionBoneList.Length > 0)
                m_RootMotionBoneList[0] = EditorGUIUtility.TrTextContent("None");

            rootIndex = ArrayUtility.FindIndex(m_RootMotionBoneList, delegate(GUIContent content) { return FileUtil.GetLastPathNameComponent(content.text) == m_RootMotionBoneName.stringValue; });
            rootIndex = rootIndex < 1 ? 0 : rootIndex;

            m_SrcHasExtraRoot = serializedObject.FindProperty("m_HasExtraRoot");
            m_DstHasExtraRoot = serializedObject.FindProperty("m_HumanDescription.m_HasExtraRoot");

            // Animation
            m_CopyAvatar = serializedObject.FindProperty("m_CopyAvatar");
            m_LegacyGenerateAnimations = serializedObject.FindProperty("m_LegacyGenerateAnimations");
            m_AnimationCompression = serializedObject.FindProperty("m_AnimationCompression");

            m_RigImportErrors = serializedObject.FindProperty("m_RigImportErrors");
            m_RigImportWarnings = serializedObject.FindProperty("m_RigImportWarnings");

            m_HumanBoneArray = serializedObject.FindProperty("m_HumanDescription.m_Human");
            m_Skeleton = serializedObject.FindProperty("m_HumanDescription.m_Skeleton");

            m_ExposeTransformEditor.OnEnable(singleImporter.transformPaths, serializedObject);

            m_CanMultiEditTransformList = CanMultiEditTransformList();

            // Check if avatar definition is same as the one it's copied from
            CheckIfAvatarCopyIsUpToDate();

            m_IsBiped = false;
            m_BipedMappingReport = new List<string>();

            UpdateBipedMappingReport();

            if (m_AnimationType.intValue == (int)ModelImporterAnimationType.Human && m_Avatar == null)
            {
                ResetAvatar();
            }
        }

        private void UpdateBipedMappingReport()
        {
            if (m_AnimationType.intValue == (int)ModelImporterAnimationType.Human)
            {
                GameObject go = assetTarget as GameObject;
                if (go != null)
                {
                    m_IsBiped = AvatarBipedMapper.IsBiped(go.transform, m_BipedMappingReport);
                }
            }
        }

        private bool CanMultiEditTransformList()
        {
            string[] transformPaths = singleImporter.transformPaths;
            for (int i = 1; i < targets.Length; ++i)
            {
                ModelImporter modelImporter = targets[i] as ModelImporter;
                if (!ArrayUtility.ArrayEquals(transformPaths, modelImporter.transformPaths))
                    return false;
            }

            return true;
        }

        void CheckIfAvatarCopyIsUpToDate()
        {
            if (!(animationType == ModelImporterAnimationType.Human || animationType == ModelImporterAnimationType.Generic) || m_AvatarSource.objectReferenceValue == null)
            {
                m_AvatarCopyIsUpToDate = true;
                return;
            }

            // Get SerializedObject of this importer and the importer of the source avatar
            string path = AssetDatabase.GetAssetPath(m_AvatarSource.objectReferenceValue);
            ModelImporter sourceImporter = AssetImporter.GetAtPath(path) as ModelImporter;

            m_AvatarCopyIsUpToDate = DoesHumanDescriptionMatch(singleImporter, sourceImporter);
        }

        internal override void OnDestroy()
        {
            m_Avatar = null;
        }

        internal override void ResetValues()
        {
            base.ResetValues();
            ResetAvatar();
            m_ExposeTransformEditor.ResetExposedTransformList();
        }

        void ResetAvatar()
        {
            if (assetTarget != null)
            {
                var path = singleImporter.assetPath;
                m_Avatar = AssetDatabase.LoadAssetAtPath<Avatar>(path);
            }
        }

        void LegacyGUI()
        {
            EditorGUILayout.Popup(m_LegacyGenerateAnimations, Styles.AnimationsOpt, Styles.AnimLabel);
            // Show warning and fix button for deprecated import formats
            if (m_LegacyGenerateAnimations.intValue == 1 || m_LegacyGenerateAnimations.intValue == 2 || m_LegacyGenerateAnimations.intValue == 3)
                EditorGUILayout.HelpBox("The animation import setting \"" + Styles.AnimationsOpt[m_LegacyGenerateAnimations.intValue].text + "\" is deprecated.", MessageType.Warning);
        }

        // Show copy avatar bool as a dropdown
        void AvatarSourceGUI()
        {
            EditorGUI.BeginChangeCheck();
            int copyValue = m_CopyAvatar.boolValue ? 1 : 0;
            EditorGUI.showMixedValue = m_CopyAvatar.hasMultipleDifferentValues;
            copyValue = EditorGUILayout.Popup(Styles.AvatarDefinition, copyValue, Styles.AvatarDefinitionOpt);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
                m_CopyAvatar.boolValue = (copyValue == 1);
        }

        void GenericGUI()
        {
            AvatarSourceGUI();

            if (!m_CopyAvatar.hasMultipleDifferentValues)
            {
                if (!m_CopyAvatar.boolValue)
                {
                    // Do not allow multi edit of root node if all rigs doesn't match
                    EditorGUI.BeginChangeCheck();
                    using (new EditorGUI.DisabledScope(!m_CanMultiEditTransformList))
                    {
                        rootIndex = EditorGUILayout.Popup(Styles.RootNode, rootIndex, m_RootMotionBoneList);
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (rootIndex > 0 && rootIndex < m_RootMotionBoneList.Length)
                        {
                            m_RootMotionBoneName.stringValue =
                                FileUtil.GetLastPathNameComponent(m_RootMotionBoneList[rootIndex].text);
                        }
                        else
                        {
                            m_RootMotionBoneName.stringValue = "";
                        }
                    }
                }
                else
                    CopyAvatarGUI();
            }
        }

        void HumanoidGUI()
        {
            AvatarSourceGUI();

            if (!m_CopyAvatar.hasMultipleDifferentValues)
            {
                if (!m_CopyAvatar.boolValue)
                    ConfigureAvatarGUI();
                else
                    CopyAvatarGUI();
            }

            if (m_IsBiped)
            {
                if (m_BipedMappingReport.Count > 0)
                {
                    string report = "A Biped was detected, but cannot be configured properly because of an unsupported hierarchy. Adjust Biped settings in 3DS Max before exporting to correct this problem.\n";

                    for (int reportIter = 0; reportIter < m_BipedMappingReport.Count; reportIter++)
                    {
                        report += m_BipedMappingReport[reportIter];
                    }

                    EditorGUILayout.HelpBox(report, MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.HelpBox("A Biped was detected. Default Biped mapping and T-Pose have been configured for this avatar. Translation DoFs have been activated. Use Configure to modify default Biped setup.", MessageType.Info);
                }
            }

            EditorGUILayout.Space();
        }

        void ConfigureAvatarGUI()
        {
            if (targets.Length > 1)
            {
                GUILayout.Label("Can't configure avatar in multi-editing mode", EditorStyles.helpBox);
                return;
            }

            if (singleImporter.transformPaths.Length <= HumanTrait.RequiredBoneCount)
            {
                GUILayout.Label(string.Format("Not enough bones to create human avatar (requires {0})", HumanTrait.RequiredBoneCount, EditorStyles.helpBox));
            }

            // Validation text
            GUIContent validationContent;
            if (m_Avatar && !HasModified())
            {
                if (m_Avatar.isHuman)
                    validationContent = Styles.avatarValid;
                else
                    validationContent = Styles.avatarInvalid;
            }
            else
            {
                validationContent = Styles.avatarPending;
                GUILayout.Label("The avatar can be configured after settings have been applied.", EditorStyles.helpBox);
            }

            Rect r = EditorGUILayout.GetControlRect();
            const int buttonWidth = 75;
            GUI.Label(new Rect(r.xMax - buttonWidth - 18, r.y, 18, r.height), validationContent, EditorStyles.label);

            // Configure button
            using (new EditorGUI.DisabledScope(m_Avatar == null))
            {
                if (GUI.Button(new Rect(r.xMax - buttonWidth, r.y + 1, buttonWidth, r.height - 1), Styles.configureAvatar, EditorStyles.miniButton))
                {
                    if (!isLocked)
                    {
                        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        {
                            Selection.activeObject = m_Avatar;
                            AvatarEditor.s_EditImmediatelyOnNextOpen = true;
                        }
                        GUIUtility.ExitGUI();
                    }
                    else
                        Debug.Log("Cannot configure avatar, inspector is locked");
                }
            }
        }

        void CheckAvatar(Avatar sourceAvatar)
        {
            if (sourceAvatar != null)
            {
                if (sourceAvatar.isHuman && (animationType != ModelImporterAnimationType.Human))
                {
                    if (EditorUtility.DisplayDialog("Asigning an Humanoid Avatar on a Generic Rig",
                        "Do you want to change Animation Type to Humanoid ?", "Yes", "No"))
                    {
                        animationType = ModelImporterAnimationType.Human;
                    }
                    else
                    {
                        m_AvatarSource.objectReferenceValue = null;
                    }
                }
                else if (!sourceAvatar.isHuman && (animationType != ModelImporterAnimationType.Generic))
                {
                    if (EditorUtility.DisplayDialog("Asigning an Generic Avatar on a Humanoid Rig",
                        "Do you want to change Animation Type to Generic ?", "Yes", "No"))
                    {
                        animationType = ModelImporterAnimationType.Generic;
                    }
                    else
                    {
                        m_AvatarSource.objectReferenceValue = null;
                    }
                }
            }
        }

        void CopyAvatarGUI()
        {
            GUILayout.Label(
@"If you have already created an Avatar for another model with a rig identical to this one, you can copy its Avatar definition.
With this option, this model will not create any avatar but only import animations.", EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_AvatarSource, GUIContent.Temp("Source"));
            var sourceAvatar = m_AvatarSource.objectReferenceValue as Avatar;
            if (EditorGUI.EndChangeCheck())
            {
                CheckAvatar(sourceAvatar);

                AvatarSetupTool.ClearAll(m_HumanBoneArray, m_Skeleton);

                if (sourceAvatar != null)
                    CopyHumanDescriptionFromOtherModel(sourceAvatar);

                m_AvatarCopyIsUpToDate = true;
            }

            if (sourceAvatar != null && !m_AvatarSource.hasMultipleDifferentValues && !m_AvatarCopyIsUpToDate)
            {
                if (GUILayout.Button(Styles.UpdateMuscleDefinitionFromSource, EditorStyles.miniButton))
                {
                    AvatarSetupTool.ClearAll(m_HumanBoneArray, m_Skeleton);
                    CopyHumanDescriptionFromOtherModel(sourceAvatar);
                    m_AvatarCopyIsUpToDate = true;
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        void ShowUpdateReferenceClip()
        {
            if (targets.Length > 1 || m_CopyAvatar.boolValue || !m_Avatar || !m_Avatar.isValid)
                return;

            string[] paths = new string[0];
            ModelImporter importer = target as ModelImporter;
            if (importer.referencedClips.Length > 0)
            {
                foreach (string clipGUID in importer.referencedClips)
                    ArrayUtility.Add(ref paths, AssetDatabase.GUIDToAssetPath(clipGUID));
            }

            // Show only button if some clip reference this avatar.
            if (paths.Length > 0 && GUILayout.Button(Styles.UpdateReferenceClips, GUILayout.Width(150)))
            {
                foreach (string path in paths)
                    SetupReferencedClip(path);

                try
                {
                    AssetDatabase.StartAssetEditing();
                    foreach (string path in paths)
                        AssetDatabase.ImportAsset(path);
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                }
            }
        }

        public override void OnInspectorGUI()
        {
            string errors = m_RigImportErrors.stringValue;
            string warnings = m_RigImportWarnings.stringValue;

            if (errors.Length > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("Error(s) found while importing rig in this animation file. Open \"Import Messages\" foldout below for more details", MessageType.Error);
            }
            else
            {
                if (warnings.Length > 0)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox("Warning(s) found while importing rig in this animation file. Open \"Import Messages\" foldout below for more details", MessageType.Warning);
                }
            }


            // Animation type
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.Popup(m_AnimationType, Styles.AnimationTypeOpt, Styles.AnimationType);
            if (EditorGUI.EndChangeCheck())
            {
                m_AvatarSource.objectReferenceValue = null;

                if (animationType == ModelImporterAnimationType.Legacy)
                    m_AnimationCompression.intValue = (int)ModelImporterAnimationCompression.KeyframeReduction;
                else if (animationType == ModelImporterAnimationType.Generic || animationType == ModelImporterAnimationType.Human)
                    m_AnimationCompression.intValue = (int)ModelImporterAnimationCompression.Optimal;

                m_DstHasExtraRoot.boolValue = m_SrcHasExtraRoot.boolValue;
            }

            EditorGUILayout.Space();

            if (!m_AnimationType.hasMultipleDifferentValues)
            {
                // Show GUI depending on animation type
                if (animationType == ModelImporterAnimationType.Human)
                    HumanoidGUI();
                else if (animationType == ModelImporterAnimationType.Generic)
                    GenericGUI();
                else if (animationType == ModelImporterAnimationType.Legacy)
                    LegacyGUI();
            }

            ShowUpdateReferenceClip();

            bool canOptimizeGameObjects = true;
            if (animationType != ModelImporterAnimationType.Human && animationType != ModelImporterAnimationType.Generic)
                canOptimizeGameObjects = false;
            if (m_CopyAvatar.boolValue == true)
                // If you have already created an Avatar for another model with a rig identical to this one, you can copy its Avatar definition.
                // With this option, this model will not create any avatar but only import animations.
                canOptimizeGameObjects = false;

            if (canOptimizeGameObjects)
            {
                EditorGUILayout.PropertyField(m_OptimizeGameObjects);
                if (m_OptimizeGameObjects.boolValue &&
                    serializedObject.targetObjectsCount == 1) // SerializedProperty can't handle multiple string arrays properly.
                {
                    bool wasChanged = GUI.changed;
                    m_ExtraExposedTransformFoldout = EditorGUILayout.Foldout(m_ExtraExposedTransformFoldout, Styles.ExtraExposedTransform, true);
                    GUI.changed = wasChanged;
                    if (m_ExtraExposedTransformFoldout)
                    {
                        // Do not allow multi edit of exposed transform list if all rigs doesn't match
                        using (new EditorGUI.DisabledScope(!m_CanMultiEditTransformList))
                        using (new EditorGUI.IndentLevelScope())
                            m_ExposeTransformEditor.OnGUI();
                    }
                }
            }

            if (errors.Length > 0 || warnings.Length > 0)
            {
                EditorGUILayout.Space();

                importMessageFoldout = EditorGUILayout.Foldout(importMessageFoldout, Styles.ImportMessages, true);

                if (importMessageFoldout)
                {
                    if (errors.Length > 0)
                        EditorGUILayout.HelpBox(errors, MessageType.None);
                    else if (warnings.Length > 0)
                        EditorGUILayout.HelpBox(warnings, MessageType.None);
                }
            }
        }

        static SerializedObject GetModelImporterSerializedObject(string assetPath)
        {
            ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null)
                return null;

            return new SerializedObject(importer);
        }

        static bool DoesHumanDescriptionMatch(ModelImporter importer, ModelImporter otherImporter)
        {
            SerializedObject so = new SerializedObject(new Object[] { importer, otherImporter });

            so.maxArraySizeForMultiEditing = Math.Max(importer.transformPaths.Length, otherImporter.transformPaths.Length);
            SerializedProperty prop = so.FindProperty("m_HumanDescription");
            bool matches = !prop.hasMultipleDifferentValues;

            so.Dispose();

            return matches;
        }

        static void CopyHumanDescriptionToDestination(SerializedObject sourceObject, SerializedObject targetObject)
        {
            targetObject.CopyFromSerializedProperty(sourceObject.FindProperty("m_HumanDescription"));
        }

        private void CopyHumanDescriptionFromOtherModel(Avatar sourceAvatar)
        {
            string srcAssetPath = AssetDatabase.GetAssetPath(sourceAvatar);
            SerializedObject srcImporter = GetModelImporterSerializedObject(srcAssetPath);

            CopyHumanDescriptionToDestination(srcImporter, serializedObject);
            srcImporter.Dispose();
        }

        private void SetupReferencedClip(string otherModelImporterPath)
        {
            SerializedObject targetImporter = GetModelImporterSerializedObject(otherModelImporterPath);

            // We may receive a path that doesn't have a importer.
            if (targetImporter != null)
            {
                targetImporter.CopyFromSerializedProperty(serializedObject.FindProperty("m_AnimationType"));

                SerializedProperty copyAvatar = targetImporter.FindProperty("m_CopyAvatar");
                if (copyAvatar != null)
                    copyAvatar.boolValue = true;

                SerializedProperty avatar = targetImporter.FindProperty("m_LastHumanDescriptionAvatarSource");
                if (avatar != null)
                    avatar.objectReferenceValue = m_Avatar;

                CopyHumanDescriptionToDestination(serializedObject, targetImporter);
                targetImporter.ApplyModifiedProperties();
                targetImporter.Dispose();
            }
        }

        public bool isLocked
        {
            get
            {
                foreach (InspectorWindow i in InspectorWindow.GetAllInspectorWindows())
                {
                    ActiveEditorTracker activeEditor = i.tracker;
                    foreach (Editor e in activeEditor.activeEditors)
                    {
                        // the tab is no longer an editor, so we must always refer to the panel container
                        if (e is ModelImporterEditor && ((ModelImporterEditor)e).activeTab == this)
                        {
                            return i.isLocked;
                        }
                    }
                }
                return false;
            }
        }

        internal override void PostApply()
        {
            UpdateBipedMappingReport();
        }
    }
}
