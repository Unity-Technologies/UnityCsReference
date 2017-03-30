// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

using Object = UnityEngine.Object;

namespace UnityEditor
{
    internal static class AvatarSetupTool
    {
        [System.Serializable]
        internal class BoneWrapper
        {
            private static string sHumanName = "m_HumanName";
            private static string sBoneName = "m_BoneName";

            private string m_HumanBoneName;
            public string humanBoneName { get { return m_HumanBoneName; } }
            public string error = string.Empty;
            public Transform bone;
            public BoneState state;

            public string messageName
            {
                get
                {
                    return ObjectNames.NicifyVariableName(m_HumanBoneName) + " Transform '" + (bone ? bone.name : "None") + "'";
                }
            }

            public BoneWrapper(string humanBoneName, SerializedObject serializedObject, Dictionary<Transform, bool> bones)
            {
                m_HumanBoneName = humanBoneName;
                Reset(serializedObject, bones);
            }

            public void Reset(SerializedObject serializedObject, Dictionary<Transform, bool> bones)
            {
                bone = null;
                SerializedProperty property = GetSerializedProperty(serializedObject, false);
                if (property != null)
                {
                    string boneName = property.FindPropertyRelative(sBoneName).stringValue;
                    bone = bones.Keys.FirstOrDefault(b => (b != null && b.name == boneName));
                }
                state = BoneState.Valid;
            }

            public void Serialize(SerializedObject serializedObject)
            {
                if (bone == null)
                {
                    DeleteSerializedProperty(serializedObject);
                }
                else
                {
                    SerializedProperty property = GetSerializedProperty(serializedObject, true);
                    if (property != null)
                        property.FindPropertyRelative(sBoneName).stringValue = bone.name;
                }
            }

            protected void DeleteSerializedProperty(SerializedObject serializedObject)
            {
                SerializedProperty humanBoneArray = serializedObject.FindProperty(sHuman);
                if (humanBoneArray == null || !humanBoneArray.isArray)
                    return;

                for (int i = 0; i < humanBoneArray.arraySize; i++)
                {
                    SerializedProperty humanNameP = humanBoneArray.GetArrayElementAtIndex(i).FindPropertyRelative(sHumanName);
                    if (humanNameP.stringValue == humanBoneName)
                    {
                        humanBoneArray.DeleteArrayElementAtIndex(i);
                        break;
                    }
                }
            }

            public SerializedProperty GetSerializedProperty(SerializedObject serializedObject, bool createIfMissing)
            {
                SerializedProperty humanBoneArray = serializedObject.FindProperty(sHuman);
                if (humanBoneArray == null || !humanBoneArray.isArray)
                    return null;

                for (int i = 0; i < humanBoneArray.arraySize; i++)
                {
                    SerializedProperty humanNameP = humanBoneArray.GetArrayElementAtIndex(i).FindPropertyRelative(sHumanName);
                    if (humanNameP.stringValue == humanBoneName)
                        return humanBoneArray.GetArrayElementAtIndex(i);
                }

                if (createIfMissing)
                {
                    humanBoneArray.arraySize++;
                    SerializedProperty bone = humanBoneArray.GetArrayElementAtIndex(humanBoneArray.arraySize - 1);
                    if (bone != null)
                    {
                        bone.FindPropertyRelative(sHumanName).stringValue = humanBoneName;
                        return bone;
                    }
                }

                return null;
            }

            public const int kIconSize = 19;

            static Color kBoneValid = new Color(0, 0.75f, 0, 1.0f);
            static Color kBoneInvalid = new Color(1.0f, 0.3f, 0.25f, 1.0f);
            static Color kBoneInactive = Color.gray;
            static Color kBoneSelected = new Color(0.4f, 0.7f, 1.0f, 1.0f);
            static Color kBoneDrop = new Color(0.1f, 0.7f, 1.0f, 1.0f);
            public void BoneDotGUI(Rect rect, Rect selectRect, int boneIndex, bool doClickSelect, bool doDragDrop, bool doDeleteKey, SerializedObject serializedObject, AvatarMappingEditor editor)
            {
                int id = GUIUtility.GetControlID(FocusType.Passive, rect);
                int keyboardID = GUIUtility.GetControlID(FocusType.Keyboard, selectRect);

                if (doClickSelect)
                    HandleClickSelection(keyboardID, selectRect, boneIndex);

                if (doDeleteKey)
                    HandleDeleteSelection(keyboardID, serializedObject, editor);

                if (doDragDrop)
                    HandleDragDrop(rect, boneIndex, id, serializedObject, editor);

                Color old = GUI.color;

                // Selection
                if (AvatarMappingEditor.s_SelectedBoneIndex == boneIndex)
                {
                    GUI.color = kBoneSelected;
                    GUI.DrawTexture(rect, AvatarMappingEditor.styles.dotSelection.image);
                }

                // State color
                if (DragAndDrop.activeControlID == id)
                    GUI.color = kBoneDrop;
                else if (state == BoneState.Valid)
                    GUI.color = kBoneValid;
                else if (state == BoneState.None)
                    GUI.color = kBoneInactive;
                else
                    GUI.color = kBoneInvalid;

                // Frame
                Texture tex;
                if (HumanTrait.RequiredBone(boneIndex))
                    tex = AvatarMappingEditor.styles.dotFrame.image;
                else
                    tex = AvatarMappingEditor.styles.dotFrameDotted.image;
                GUI.DrawTexture(rect, tex);

                // Fill
                if (bone != null || DragAndDrop.activeControlID == id)
                    GUI.DrawTexture(rect, AvatarMappingEditor.styles.dotFill.image);

                GUI.color = old;
            }

            public void HandleClickSelection(int keyboardID, Rect selectRect, int boneIndex)
            {
                Event evt = Event.current;
                if (evt.type == EventType.MouseDown && selectRect.Contains(evt.mousePosition))
                {
                    AvatarMappingEditor.s_SelectedBoneIndex = boneIndex;
                    AvatarMappingEditor.s_DirtySelection = true;
                    // case 837655.  Late update GUIUtility.keyboardControl to avoid it being overriden during scene selection.
                    AvatarMappingEditor.s_KeyboardControl = keyboardID;
                    Selection.activeTransform = bone;
                    if (bone != null)
                        EditorGUIUtility.PingObject(bone);
                    evt.Use();
                }
            }

            public void HandleDeleteSelection(int keyboardID, SerializedObject serializedObject, AvatarMappingEditor editor)
            {
                Event evt = Event.current;
                if (evt.type == EventType.KeyDown)
                {
                    if (GUIUtility.keyboardControl == keyboardID)
                    {
                        if ((evt.keyCode == KeyCode.Backspace) || (evt.keyCode == KeyCode.Delete))
                        {
                            Undo.RegisterCompleteObjectUndo(editor, "Avatar mapping modified");

                            //  Unreference transform component in selected bone.
                            bone = null;
                            state = BoneState.None;
                            Serialize(serializedObject);

                            //  Clear scene selection.
                            Selection.activeTransform = null;

                            GUI.changed = true;
                            evt.Use();
                        }
                    }
                }
            }

            private void HandleDragDrop(Rect dropRect, int boneIndex, int id, SerializedObject serializedObject, AvatarMappingEditor editor)
            {
                EventType eventType = Event.current.type;
                switch (eventType)
                {
                    case EventType.DragExited:
                        if (GUI.enabled)
                            HandleUtility.Repaint();
                        break;
                    case EventType.DragUpdated:
                    case EventType.DragPerform:
                        if (dropRect.Contains(Event.current.mousePosition) && GUI.enabled)
                        {
                            Object[] references = DragAndDrop.objectReferences;
                            Object validatedObject = references.Length == 1 ? references[0] : null;
                            if (validatedObject != null)
                            {
                                if (!(validatedObject is Transform || validatedObject is GameObject) || EditorUtility.IsPersistent(validatedObject))
                                    validatedObject = null;
                            }
                            if (validatedObject != null)
                            {
                                DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                                if (eventType == EventType.DragPerform)
                                {
                                    Undo.RegisterCompleteObjectUndo(editor, "Avatar mapping modified");

                                    if (validatedObject is GameObject)
                                        bone = (validatedObject as GameObject).transform;
                                    else
                                        bone = validatedObject as Transform;

                                    Serialize(serializedObject);

                                    GUI.changed = true;
                                    DragAndDrop.AcceptDrag();
                                    DragAndDrop.activeControlID = 0;
                                }
                                else
                                {
                                    DragAndDrop.activeControlID = id;
                                }
                                Event.current.Use();
                            }
                        }
                        break;
                }
            }
        }

        private class BonePoseData
        {
            public Vector3 direction = Vector3.zero;
            public bool compareInGlobalSpace = false;
            public float maxAngle;
            public int[] childIndices = null;
            public Vector3 planeNormal = Vector3.zero;
            public BonePoseData(Vector3 dir, bool globalSpace, float maxAngleDiff)
            {
                direction = (dir == Vector3.zero ? dir : dir.normalized);
                compareInGlobalSpace = globalSpace;
                maxAngle = maxAngleDiff;
            }

            public BonePoseData(Vector3 dir, bool globalSpace, float maxAngleDiff, int[] children) : this(dir, globalSpace, maxAngleDiff)
            {
                childIndices = children;
            }

            public BonePoseData(Vector3 dir, bool globalSpace, float maxAngleDiff, Vector3 planeNormal, int[] children) : this(dir, globalSpace, maxAngleDiff, children)
            {
                this.planeNormal = planeNormal;
            }
        }

        private static string sHuman = "m_HumanDescription.m_Human";
        private static string sHasTranslationDoF = "m_HumanDescription.m_HasTranslationDoF";

        internal static string sSkeleton = "m_HumanDescription.m_Skeleton";
        internal static string sName = "m_Name";
        internal static string sParentName = "m_ParentName";
        internal static string sPosition = "m_Position";
        internal static string sRotation = "m_Rotation";
        internal static string sScale = "m_Scale";

        private static BonePoseData[] sBonePoses = new BonePoseData[]
        {
            new BonePoseData(Vector3.up, true, 15),  // Hips,
            new BonePoseData(new Vector3(-0.05f, -1, 0),      true, 15),   // LeftUpperLeg,
            new BonePoseData(new Vector3(0.05f, -1, 0),      true, 15),    // RightUpperLeg,
            new BonePoseData(new Vector3(-0.05f, -1, -0.15f), true, 20),   // LeftLowerLeg,
            new BonePoseData(new Vector3(0.05f, -1, -0.15f), true, 20),    // RightLowerLeg,
            new BonePoseData(new Vector3(-0.05f, 0, 1),       true, 20, Vector3.up, null),   // LeftFoot,
            new BonePoseData(new Vector3(0.05f, 0, 1),       true, 20, Vector3.up, null),    // RightFoot,
            new BonePoseData(Vector3.up, true, 30, new int[] {(int)HumanBodyBones.Chest, (int)HumanBodyBones.UpperChest, (int)HumanBodyBones.Neck, (int)HumanBodyBones.Head}),  // Spine,
            new BonePoseData(Vector3.up, true, 30, new int[] {(int)HumanBodyBones.UpperChest, (int)HumanBodyBones.Neck, (int)HumanBodyBones.Head}),  // Chest,
            new BonePoseData(Vector3.up, true, 30),  // Neck,
            null, // Head,
            new BonePoseData(-Vector3.right, true, 20),  // LeftShoulder,
            new BonePoseData(Vector3.right, true, 20),   // RightShoulder,
            new BonePoseData(-Vector3.right, true, 05),  // LeftArm,
            new BonePoseData(Vector3.right, true, 05),   // RightArm,
            new BonePoseData(-Vector3.right, true, 05),  // LeftForeArm,
            new BonePoseData(Vector3.right, true, 05),   // RightForeArm,
            new BonePoseData(-Vector3.right, false, 10, Vector3.forward, new int[] {(int)HumanBodyBones.LeftMiddleProximal}),  // LeftHand,
            new BonePoseData(Vector3.right, false, 10, Vector3.forward, new int[] {(int)HumanBodyBones.RightMiddleProximal}),   // RightHand,
            null, // LeftToes,
            null, // RightToes,
            null, // LeftEye,
            null, // RightEye,
            null, // Jaw,
            new BonePoseData(new Vector3(-1, 0, 1), false, 10), // Left Thumb
            new BonePoseData(new Vector3(-1, 0, 1), false, 05),
            new BonePoseData(new Vector3(-1, 0, 1), false, 05),
            new BonePoseData(-Vector3.right, false, 10),  // Left Index
            new BonePoseData(-Vector3.right, false, 05),
            new BonePoseData(-Vector3.right, false, 05),
            new BonePoseData(-Vector3.right, false, 10),  // Left Middle
            new BonePoseData(-Vector3.right, false, 05),
            new BonePoseData(-Vector3.right, false, 05),
            new BonePoseData(-Vector3.right, false, 10),  // Left Ring
            new BonePoseData(-Vector3.right, false, 05),
            new BonePoseData(-Vector3.right, false, 05),
            new BonePoseData(-Vector3.right, false, 10),  // Left Little
            new BonePoseData(-Vector3.right, false, 05),
            new BonePoseData(-Vector3.right, false, 05),
            new BonePoseData(new Vector3(1, 0, 1), false, 10),  // Right Thumb
            new BonePoseData(new Vector3(1, 0, 1), false, 05),
            new BonePoseData(new Vector3(1, 0, 1), false, 05),
            new BonePoseData(Vector3.right, false, 10),   // Right Index
            new BonePoseData(Vector3.right, false, 05),
            new BonePoseData(Vector3.right, false, 05),
            new BonePoseData(Vector3.right, false, 10),   // Right Middle
            new BonePoseData(Vector3.right, false, 05),
            new BonePoseData(Vector3.right, false, 05),
            new BonePoseData(Vector3.right, false, 10),   // Right Ring
            new BonePoseData(Vector3.right, false, 05),
            new BonePoseData(Vector3.right, false, 05),
            new BonePoseData(Vector3.right, false, 10),   // Right Little
            new BonePoseData(Vector3.right, false, 05),
            new BonePoseData(Vector3.right, false, 05),
            new BonePoseData(Vector3.up, true, 30, new int[] {(int)HumanBodyBones.Neck, (int)HumanBodyBones.Head}),  // UpperChest,
        };

        public static Dictionary<Transform, bool> GetModelBones(Transform root, bool includeAll, BoneWrapper[] humanBones)
        {
            if (root == null)
                return null;

            // Find out which transforms are actual bones and which are parents of actual bones
            Dictionary<Transform, bool> bones = new Dictionary<Transform, bool>();
            List<Transform> skinnedBones = new List<Transform>();

            if (!includeAll)
            {
                // Find out in advance which bones are used by SkinnedMeshRenderers
                SkinnedMeshRenderer[] skinnedMeshRenderers = root.GetComponentsInChildren<SkinnedMeshRenderer>();

                foreach (SkinnedMeshRenderer rend in skinnedMeshRenderers)
                {
                    Transform[] meshBones = rend.bones;
                    bool[] meshBonesUsed = new bool[meshBones.Length];
                    BoneWeight[] weights = rend.sharedMesh.boneWeights;
                    foreach (BoneWeight w in weights)
                    {
                        if (w.weight0 != 0)
                            meshBonesUsed[w.boneIndex0] = true;
                        if (w.weight1 != 0)
                            meshBonesUsed[w.boneIndex1] = true;
                        if (w.weight2 != 0)
                            meshBonesUsed[w.boneIndex2] = true;
                        if (w.weight3 != 0)
                            meshBonesUsed[w.boneIndex3] = true;
                    }
                    for (int i = 0; i < meshBones.Length; i++)
                    {
                        if (meshBonesUsed[i])
                            if (!skinnedBones.Contains(meshBones[i]))
                                skinnedBones.Add(meshBones[i]);
                    }
                }

                // Recursive call
                DetermineIsActualBone(root, bones, skinnedBones, false, humanBones);
            }

            // If not enough bones were found, fallback to treating all transforms as bones
            if (bones.Count < HumanTrait.RequiredBoneCount)
            {
                bones.Clear();
                skinnedBones.Clear();
                DetermineIsActualBone(root, bones, skinnedBones, true, humanBones);
            }

            return bones;
        }

        private static bool DetermineIsActualBone(Transform tr, Dictionary<Transform, bool> bones, List<Transform> skinnedBones, bool includeAll, BoneWrapper[] humanBones)
        {
            bool actualBone = includeAll;
            bool boneParent = false;
            bool boneChild = false;

            // Actual bone parent if any of children are bones
            int childBones = 0;
            foreach (Transform child in tr)
                if (DetermineIsActualBone(child, bones, skinnedBones, includeAll, humanBones))
                    childBones++;

            if (childBones > 0)
                boneParent = true;
            if (childBones > 1)
                actualBone = true;

            // Actual bone if used by skinned mesh
            if (!actualBone)
                if (skinnedBones.Contains(tr))
                    actualBone = true;

            // Actual bone if contains component other than transform
            if (!actualBone)
            {
                Component[] components = tr.GetComponents<Component>();
                if (components.Length > 1)
                {
                    foreach (Component comp in components)
                    {
                        if ((comp is Renderer) && !(comp is SkinnedMeshRenderer))
                        {
                            Bounds bounds = (comp as Renderer).bounds;

                            // Double size of bounds in order to still make bone valid
                            // if its pivot is just slightly outside of renderer bounds.
                            bounds.extents = bounds.size;

                            // If the parent is inside the bounds, this transform is probably
                            // just a geometry dummy for the parent bone
                            if (tr.childCount == 0 && tr.parent && bounds.Contains(tr.parent.position))
                            {
                                if (tr.parent.GetComponent<Renderer>() != null)
                                    actualBone = true;
                                else
                                    boneChild = true;
                            }
                            // if not, give transform itself a chance.
                            // If pivot is way outside of bounds, it's not an actual bone.
                            else if (bounds.Contains(tr.position))
                            {
                                actualBone = true;
                            }
                        }
                    }
                }
            }

            // Actual bone if the bone is define in human definition.
            if (!actualBone && humanBones != null)
            {
                foreach (var boneWrapper in humanBones)
                {
                    if (tr == boneWrapper.bone)
                    {
                        actualBone = true;
                        break;
                    }
                }
            }

            if (actualBone)
                bones[tr] = true;
            else if (boneParent)
            {
                if (!bones.ContainsKey(tr))
                    bones[tr] = false;
            }
            else if (boneChild)
                bones[tr.parent] = true;

            return bones.ContainsKey(tr);
        }

        public static int GetFirstHumanBoneAncestor(BoneWrapper[] bones, int boneIndex)
        {
            boneIndex = HumanTrait.GetParentBone(boneIndex);
            while (boneIndex > 0 && bones[boneIndex].bone == null)
                boneIndex = HumanTrait.GetParentBone(boneIndex);
            return boneIndex;
        }

        public static int GetHumanBoneChild(BoneWrapper[] bones, int boneIndex)
        {
            for (int i = 0; i < HumanTrait.BoneCount; i++)
                if (HumanTrait.GetParentBone(i) == boneIndex)
                    return i;
            return -1;
        }

        public static BoneWrapper[] GetHumanBones(SerializedObject serializedObject, Dictionary<Transform, bool> actualBones)
        {
            string[] humanBoneNames = HumanTrait.BoneName;
            BoneWrapper[] bones = new BoneWrapper[humanBoneNames.Length];
            for (int i = 0; i < humanBoneNames.Length; i++)
                bones[i] = new BoneWrapper(humanBoneNames[i], serializedObject, actualBones);
            return bones;
        }

        public static void ClearAll(SerializedObject serializedObject)
        {
            ClearHumanBoneArray(serializedObject);
            ClearSkeletonBoneArray(serializedObject);
        }

        public static void ClearHumanBoneArray(SerializedObject serializedObject)
        {
            SerializedProperty humanBody = serializedObject.FindProperty(sHuman);
            if (humanBody != null && humanBody.isArray)
                humanBody.ClearArray();
        }

        public static void ClearSkeletonBoneArray(SerializedObject serializedObject)
        {
            SerializedProperty skeleton = serializedObject.FindProperty(sSkeleton);
            if (skeleton != null && skeleton.isArray)
                skeleton.ClearArray();
        }

        public static void AutoSetupOnInstance(GameObject modelPrefab, SerializedObject modelImporterSerializedObject)
        {
            GameObject instance = GameObject.Instantiate(modelPrefab) as GameObject;
            instance.hideFlags = HideFlags.HideAndDontSave;
            AvatarSetupTool.AutoSetup(modelPrefab, instance, modelImporterSerializedObject);
            GameObject.DestroyImmediate(instance);
        }

        public static bool IsPoseValidOnInstance(GameObject modelPrefab, SerializedObject modelImporterSerializedObject)
        {
            GameObject instance = GameObject.Instantiate(modelPrefab) as GameObject;
            instance.hideFlags = HideFlags.HideAndDontSave;

            Dictionary<Transform, bool> modelBones = GetModelBones(instance.transform, false, null);
            BoneWrapper[] humanBones = GetHumanBones(modelImporterSerializedObject, modelBones);

            TransferDescriptionToPose(modelImporterSerializedObject, instance.transform);
            bool valid = IsPoseValid(humanBones);

            GameObject.DestroyImmediate(instance);
            return valid;
        }

        public static void AutoSetup(GameObject modelPrefab, GameObject modelInstance, SerializedObject modelImporterSerializedObject)
        {
            SerializedProperty humanBoneArray = modelImporterSerializedObject.FindProperty(sHuman);
            SerializedProperty hasTranslationDoF = modelImporterSerializedObject.FindProperty(sHasTranslationDoF);

            if (humanBoneArray == null || !humanBoneArray.isArray)
                return;

            SimpleProfiler.Begin("AutoSetup Total");

            SimpleProfiler.Begin("ClearHumanBoneArray");
            ClearHumanBoneArray(modelImporterSerializedObject);
            SimpleProfiler.End();

            SimpleProfiler.Begin("IsBiped");
            bool isBiped = AvatarBipedMapper.IsBiped(modelInstance.transform, null);
            SimpleProfiler.End();

            SimpleProfiler.Begin("SampleBindPose");
            SampleBindPose(modelInstance);
            SimpleProfiler.End();

            // Perform auto-mapping and get back mapping
            SimpleProfiler.Begin("GetModelBones");
            Dictionary<Transform, bool> modelBones = GetModelBones(modelInstance.transform, false, null);
            SimpleProfiler.End();

            Dictionary<int, Transform> mapping = null;

            if (isBiped)
            {
                SimpleProfiler.Begin("MapBipedBones");
                mapping = AvatarBipedMapper.MapBones(modelInstance.transform);
                SimpleProfiler.End();
            }
            else
            {
                SimpleProfiler.Begin("MapBones");
                mapping = AvatarAutoMapper.MapBones(modelInstance.transform, modelBones);
                SimpleProfiler.End();
            }

            // Apply mapping to SerializedObject
            SimpleProfiler.Begin("ApplyMapping");
            BoneWrapper[] humanBones = GetHumanBones(modelImporterSerializedObject, modelBones);
            for (int i = 0; i < humanBones.Length; i++)
            {
                BoneWrapper bone = humanBones[i];
                if (mapping.ContainsKey(i))
                    bone.bone = mapping[i];
                else
                    bone.bone = null;
                bone.Serialize(modelImporterSerializedObject);
            }
            SimpleProfiler.End();

            if (!isBiped)
            {
                // Check error of current pose (bind pose)
                float bindPoseError = GetPoseError(humanBones);

                // Check error of reset pose
                CopyPose(modelInstance, modelPrefab);
                float resetPoseError = GetPoseError(humanBones);

                // If the bind pose was better, sample the bind pose again to use that as a starting point for the T-pose.
                // Otherwise use the reset pose as starting point.
                if (bindPoseError < resetPoseError)
                    SampleBindPose(modelInstance);

                // Move bones into valid T-pose
                SimpleProfiler.Begin("MakePoseValid");
                MakePoseValid(humanBones);
                SimpleProfiler.End();
            }
            else
            {
                SimpleProfiler.Begin("SetBipedPose");
                AvatarBipedMapper.BipedPose(modelInstance, humanBones);
                SimpleProfiler.End();

                hasTranslationDoF.boolValue = true;
            }

            // Apply pose to SerializedObject
            SimpleProfiler.Begin("TransferPose");
            TransferPoseToDescription(modelImporterSerializedObject, modelInstance.transform);
            SimpleProfiler.End();

            SimpleProfiler.End();
            SimpleProfiler.PrintTimes();
        }

        public static bool TestAndValidateAutoSetup(GameObject modelAsset)
        {
            // Sanity check
            if (modelAsset == null)
            {
                Debug.LogError("GameObject is null");
                return false;
            }
            if (PrefabUtility.GetPrefabType(modelAsset) != PrefabType.ModelPrefab)
            {
                Debug.LogError(modelAsset.name + ": GameObject is not a ModelPrefab", modelAsset);
                return false;
            }
            if (modelAsset.transform.parent != null)
            {
                Debug.LogError(modelAsset.name + ": GameObject is not the root", modelAsset);
                return false;
            }

            // Get importer
            string path = AssetDatabase.GetAssetPath(modelAsset);
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError(modelAsset.name + ": Could not load ModelImporter (path:" + path + ")", modelAsset);
                return false;
            }

            // Get CreateAvatar property
            SerializedObject serializedObject = new SerializedObject(importer);
            SerializedProperty animationType = serializedObject.FindProperty("m_AnimationType");
            if (animationType == null)
            {
                Debug.LogError(modelAsset.name + ": Could not find property m_AnimationType on ModelImporter", modelAsset);
                return false;
            }

            // Clear avatar import settings and reimport
            animationType.intValue = 2;
            ClearAll(serializedObject);
            serializedObject.ApplyModifiedProperties();
            AssetDatabase.ImportAsset(path);

            // Setup avatar import settings and reimport
            animationType.intValue = 3;
            AutoSetupOnInstance(modelAsset, serializedObject);
            serializedObject.ApplyModifiedProperties();
            AssetDatabase.ImportAsset(path);

            // Check if Avatar is valid
            Avatar avatar = AssetDatabase.LoadAssetAtPath(path, typeof(Avatar)) as Avatar;
            if (avatar == null)
            {
                Debug.LogError(modelAsset.name + ": Did not find Avatar after reimport with CreateAvatar enabled", modelAsset);
                return false;
            }
            if (!avatar.isHuman)
            {
                Debug.LogError(modelAsset.name + ": Avatar is not valid after reimport", modelAsset);
                return false;
            }
            if (!IsPoseValidOnInstance(modelAsset, serializedObject))
            {
                Debug.LogError(modelAsset.name + ": Avatar has invalid pose after reimport", modelAsset);
                return false;
            }

            // Check that mapping matches reference mapping
            {
                string pathWithoutExtension = path.Substring(0, path.Length - System.IO.Path.GetExtension(path).Length);

                // Get reference template
                string templatePath = pathWithoutExtension + ".ht";
                HumanTemplate humanTemplate = AssetDatabase.LoadMainAssetAtPath(templatePath) as HumanTemplate;
                if (humanTemplate == null)
                    Debug.LogWarning(modelAsset.name + ": Didn't find template at path " + templatePath);
                else
                {
                    // Get file with bones to ignore, if present
                    List<string> ignoreBones = null;
                    string ignorePath = pathWithoutExtension + ".ignore";
                    if (System.IO.File.Exists(ignorePath))
                        ignoreBones = new List<string>(System.IO.File.ReadAllLines(ignorePath));

                    // Create instance
                    GameObject instance = GameObject.Instantiate(modelAsset) as GameObject;
                    instance.hideFlags = HideFlags.HideAndDontSave;

                    // Get BoneWrapper array
                    Dictionary<Transform, bool> modelBones = GetModelBones(instance.transform, false, null);
                    BoneWrapper[] humanBones = GetHumanBones(serializedObject, modelBones);

                    // Test that bone mapping match the one in the reference
                    bool mismatch = false;
                    for (int i = 0; i < humanBones.Length; i++)
                    {
                        // Ignore bones in the ignore file
                        if (ignoreBones != null && ignoreBones.Contains(humanBones[i].humanBoneName))
                            continue;

                        string referenceBoneName = humanTemplate.Find(humanBones[i].humanBoneName);
                        string actualBoneName = (humanBones[i].bone == null ? "" : humanBones[i].bone.name);
                        if (!AvatarMappingEditor.MatchName(actualBoneName, referenceBoneName))
                        {
                            mismatch = true;
                            Debug.LogError(modelAsset.name + ": Avatar has bone " + humanBones[i].humanBoneName + " mapped to \"" + actualBoneName + "\" but expected \"" + referenceBoneName + "\"", modelAsset);
                        }
                    }

                    GameObject.DestroyImmediate(instance);

                    if (mismatch)
                        return false;
                }
            }

            return true;
        }

        public static void DebugTransformTree(Transform tr, Dictionary<Transform, bool> bones, int level)
        {
            string pre = "  ";
            if (bones.ContainsKey(tr))
                if (bones[tr])
                    pre = "* ";
                else
                    pre = ". ";
            Debug.Log("                                             ".Substring(0, level * 2) + pre + tr.name + "\n\n");
            foreach (Transform child in tr)
                DebugTransformTree(child, bones, level + 1);
        }

        public static SerializedProperty FindSkeletonBone(SerializedObject serializedObject, Transform t, bool createMissing, bool isRoot)
        {
            SerializedProperty skeletonBoneArray = serializedObject.FindProperty(sSkeleton);
            if (skeletonBoneArray == null || !skeletonBoneArray.isArray)
                return null;
            return FindSkeletonBone(skeletonBoneArray, t, createMissing, isRoot);
        }

        public static SerializedProperty FindSkeletonBone(SerializedProperty skeletonBoneArray, Transform t, bool createMissing, bool isRoot)
        {
            if (isRoot && skeletonBoneArray.arraySize > 0)
            {
                SerializedProperty boneP = skeletonBoneArray.GetArrayElementAtIndex(0);
                SerializedProperty boneNameP = boneP.FindPropertyRelative(sName);
                if (boneNameP.stringValue == t.name)
                    return boneP;
            }
            else
            {
                // So root object name is not unique among all his child
                // so we always need to skip the first element which is the root
                for (int i = 1; i < skeletonBoneArray.arraySize; i++)
                {
                    SerializedProperty boneP = skeletonBoneArray.GetArrayElementAtIndex(i);
                    SerializedProperty boneNameP = boneP.FindPropertyRelative(sName);
                    if (boneNameP.stringValue == t.name)
                        return boneP;
                }
            }

            if (createMissing)
            {
                skeletonBoneArray.arraySize++;
                SerializedProperty bone = skeletonBoneArray.GetArrayElementAtIndex(skeletonBoneArray.arraySize - 1);
                if (bone != null)
                {
                    bone.FindPropertyRelative(sName).stringValue = t.name;
                    bone.FindPropertyRelative(sParentName).stringValue = isRoot ? "" : t.parent.name;

                    bone.FindPropertyRelative(sPosition).vector3Value = t.localPosition;
                    bone.FindPropertyRelative(sRotation).quaternionValue = t.localRotation;
                    bone.FindPropertyRelative(sScale).vector3Value = t.localScale;
                    return bone;
                }
            }
            return null;
        }

        public static void CopyPose(GameObject go, GameObject source)
        {
            GameObject instance = GameObject.Instantiate(source) as GameObject;
            instance.hideFlags = HideFlags.HideAndDontSave;
            AnimatorUtility.DeoptimizeTransformHierarchy(instance);
            CopyPose(go.transform, instance.transform);
            GameObject.DestroyImmediate(instance);
        }

        private static void CopyPose(Transform t, Transform source)
        {
            t.localPosition = source.localPosition;
            t.localRotation = source.localRotation;
            t.localScale = source.localScale;
            foreach (Transform child in t)
            {
                Transform sourceChild = source.Find(child.name);
                if (sourceChild)
                    CopyPose(child, sourceChild);
            }
        }

        public static void GetBindPoseBonePositionRotation(Matrix4x4 skinMatrix, Matrix4x4 boneMatrix, Transform bone, out Vector3 position, out Quaternion rotation)
        {
            // Get global matrix for bone
            Matrix4x4 bindMatrixGlobal = skinMatrix * boneMatrix.inverse;

            // Get local X, Y, Z, and position of matrix
            Vector3 mX = new Vector3(bindMatrixGlobal.m00, bindMatrixGlobal.m10, bindMatrixGlobal.m20);
            Vector3 mY = new Vector3(bindMatrixGlobal.m01, bindMatrixGlobal.m11, bindMatrixGlobal.m21);
            Vector3 mZ = new Vector3(bindMatrixGlobal.m02, bindMatrixGlobal.m12, bindMatrixGlobal.m22);
            Vector3 mP = new Vector3(bindMatrixGlobal.m03, bindMatrixGlobal.m13, bindMatrixGlobal.m23);

            // Set position
            // Adjust scale of matrix to compensate for difference in binding scale and model scale
            float bindScale = mZ.magnitude;
            float modelScale = Mathf.Abs(bone.lossyScale.z);
            position = mP * (modelScale / bindScale);

            // Set rotation
            // Check if scaling is negative and handle accordingly
            if (Vector3.Dot(Vector3.Cross(mX, mY), mZ) >= 0)
                rotation = Quaternion.LookRotation(mZ, mY);
            else
                rotation = Quaternion.LookRotation(-mZ, -mY);
        }

        public static void SampleBindPose(GameObject go)
        {
            List<SkinnedMeshRenderer> skins = new List<SkinnedMeshRenderer>(go.GetComponentsInChildren<SkinnedMeshRenderer>());
            skins.Sort(new SkinTransformHierarchySorter());
            foreach (SkinnedMeshRenderer skin in skins)
            {
                //Debug.Log ("Sampling skinning of SkinnedMeshRenderer "+skin.name);
                Matrix4x4 goMatrix = skin.transform.localToWorldMatrix;
                List<Transform> bones = new List<Transform>(skin.bones);
                Vector3[] backupLocalPosition = new Vector3[bones.Count];

                // backup local position of bones. Only use rotation given by bind pose
                for (int i = 0; i < bones.Count; i++)
                {
                    backupLocalPosition[i] = bones[i].localPosition;
                }

                // Set all parents to be null to be able to set global alignments of bones without affecting their children.
                Dictionary<Transform, Transform> parents = new Dictionary<Transform, Transform>();
                foreach (Transform bone in bones)
                {
                    parents[bone] = bone.parent;
                    bone.parent = null;
                }

                // Set global space position and rotation of each bone
                for (int i = 0; i < bones.Count; i++)
                {
                    Vector3 position;
                    Quaternion rotation;
                    GetBindPoseBonePositionRotation(goMatrix, skin.sharedMesh.bindposes[i], bones[i], out position, out rotation);
                    bones[i].position = position;
                    bones[i].rotation = rotation;
                }

                // Reconnect bones in their original hierarchy
                foreach (Transform bone in bones)
                    bone.parent = parents[bone];

                // put back local postion of bones
                for (int i = 0; i < bones.Count; i++)
                {
                    bones[i].localPosition = backupLocalPosition[i];
                }
            }
        }

        public static void ShowBindPose(SkinnedMeshRenderer skin)
        {
            Matrix4x4 goMatrix = skin.transform.localToWorldMatrix;
            for (int i = 0; i < skin.bones.Length; i++)
            {
                Vector3 position;
                Quaternion rotation;
                GetBindPoseBonePositionRotation(goMatrix, skin.sharedMesh.bindposes[i], skin.bones[i], out position, out rotation);
                float size = HandleUtility.GetHandleSize(position);
                Handles.color = Handles.xAxisColor;
                Handles.DrawLine(position, position + rotation * Vector3.right * 0.3f * size);
                Handles.color = Handles.yAxisColor;
                Handles.DrawLine(position, position + rotation * Vector3.up * 0.3f * size);
                Handles.color = Handles.zAxisColor;
                Handles.DrawLine(position, position + rotation * Vector3.forward * 0.3f * size);
            }
        }

        private class SkinTransformHierarchySorter : IComparer<SkinnedMeshRenderer>
        {
            public int Compare(SkinnedMeshRenderer skinA, SkinnedMeshRenderer skinB)
            {
                Transform a = skinA.transform;
                Transform b = skinB.transform;
                while (true)
                {
                    if (a == null)
                        if (b == null)
                            return 0;
                        else
                            return -1;
                    if (b == null)
                        return 1;
                    a = a.parent;
                    b = b.parent;
                }
            }
        }

        public static void TransferPoseToDescription(SerializedObject serializedObject, Transform root)
        {
            SkeletonBone[] skeletonBones = new SkeletonBone[0];
            if (root)
                TransferPoseToDescription(serializedObject, root, true, ref skeletonBones);

            SerializedProperty skeletonBoneArray = serializedObject.FindProperty(sSkeleton);
            ModelImporter.UpdateSkeletonPose(skeletonBones, skeletonBoneArray);
        }

        private static void TransferPoseToDescription(SerializedObject serializedObject, Transform transform, bool isRoot, ref SkeletonBone[] skeletonBones)
        {
            SkeletonBone skeletonBone = new SkeletonBone();

            skeletonBone.name = transform.name;
            skeletonBone.parentName = isRoot ? "" : transform.parent.name;
            skeletonBone.position = transform.localPosition;
            skeletonBone.rotation = transform.localRotation;
            skeletonBone.scale = transform.localScale;

            ArrayUtility.Add(ref skeletonBones, skeletonBone);

            foreach (Transform child in transform)
                TransferPoseToDescription(serializedObject, child, false, ref skeletonBones);
        }

        public static void TransferDescriptionToPose(SerializedObject serializedObject, Transform root)
        {
            if (root != null)
                TransferDescriptionToPose(serializedObject, root, true);
        }

        private static void TransferDescriptionToPose(SerializedObject serializedObject, Transform transform, bool isRoot)
        {
            SerializedProperty bone = FindSkeletonBone(serializedObject, transform, false, isRoot);
            if (bone != null)
            {
                SerializedProperty positionP = bone.FindPropertyRelative(sPosition);
                SerializedProperty rotationP = bone.FindPropertyRelative(sRotation);
                SerializedProperty scaleP = bone.FindPropertyRelative(sScale);
                transform.localPosition = positionP.vector3Value;
                transform.localRotation = rotationP.quaternionValue;
                transform.localScale = scaleP.vector3Value;
            }

            foreach (Transform child in transform)
                TransferDescriptionToPose(serializedObject, child, false);
        }

        // BONE ALIGNMENT HANDLING

        public static bool IsPoseValid(BoneWrapper[] bones)
        {
            return (GetPoseError(bones) == 0);
        }

        public static float GetPoseError(BoneWrapper[] bones)
        {
            Quaternion orientation = AvatarComputeOrientation(bones);
            float error = 0;

            for (int i = 0; i < sBonePoses.Length; i++)
                error += GetBoneAlignmentError(bones, orientation, i);

            error += GetCharacterPositionError(bones);

            return error;
        }

        // Enforces TPose T-Pose
        public static void MakePoseValid(BoneWrapper[] bones)
        {
            Quaternion orientation = AvatarComputeOrientation(bones);
            for (int i = 0; i < sBonePoses.Length; i++)
            {
                MakeBoneAlignmentValid(bones, orientation, i);
                // Recalculate orientation after handling hips since they may have changed it
                if (i == (int)HumanBodyBones.Hips)
                    orientation = AvatarComputeOrientation(bones);
            }

            // Move feet to ground plane
            MakeCharacterPositionValid(bones);
        }

        public static float GetBoneAlignmentError(BoneWrapper[] bones, Quaternion avatarOrientation, int boneIndex)
        {
            if (boneIndex < 0 || boneIndex >= sBonePoses.Length)
                return 0;

            BoneWrapper bone = bones[boneIndex];
            BonePoseData pose = sBonePoses[boneIndex];
            if (bone.bone == null || pose == null)
                return 0;

            if (boneIndex == (int)HumanBodyBones.Hips)
            {
                float angleX = Vector3.Angle(avatarOrientation * Vector3.right, Vector3.right);
                float angleY = Vector3.Angle(avatarOrientation * Vector3.up, Vector3.up);
                float angleZ = Vector3.Angle(avatarOrientation * Vector3.forward, Vector3.forward);
                return Mathf.Max(0, Mathf.Max(angleX, angleY, angleZ) - pose.maxAngle);
            }

            Vector3 dir = GetBoneAlignmentDirection(bones, avatarOrientation, boneIndex);
            if (dir == Vector3.zero)
                return 0;
            Quaternion space = GetRotationSpace(bones, avatarOrientation, boneIndex);
            Vector3 goalDir = space * pose.direction;
            if (pose.planeNormal != Vector3.zero)
                dir = Vector3.ProjectOnPlane(dir, space * pose.planeNormal);

            // Check if the bone direction is not close enough to the target direction
            return Mathf.Max(0, Vector3.Angle(dir, goalDir) - pose.maxAngle);
        }

        public static void MakeBoneAlignmentValid(BoneWrapper[] bones, Quaternion avatarOrientation, int boneIndex)
        {
            if (boneIndex < 0 || boneIndex >= sBonePoses.Length || boneIndex >= bones.Length)
                return;

            BoneWrapper bone = bones[boneIndex];
            BonePoseData pose = sBonePoses[boneIndex];
            if (bone.bone == null || pose == null)
                return;

            if (boneIndex == (int)HumanBodyBones.Hips)
            {
                float angleX = Vector3.Angle(avatarOrientation * Vector3.right, Vector3.right);
                float angleY = Vector3.Angle(avatarOrientation * Vector3.up, Vector3.up);
                float angleZ = Vector3.Angle(avatarOrientation * Vector3.forward, Vector3.forward);
                if (angleX > pose.maxAngle || angleY > pose.maxAngle || angleZ > pose.maxAngle)
                    bone.bone.rotation = Quaternion.Inverse(avatarOrientation) * bone.bone.rotation;
                return;
            }

            Vector3 dir = GetBoneAlignmentDirection(bones, avatarOrientation, boneIndex);
            if (dir == Vector3.zero)
                return;
            Quaternion space = GetRotationSpace(bones, avatarOrientation, boneIndex);
            Vector3 goalDir = space * pose.direction;
            if (pose.planeNormal != Vector3.zero)
                dir = Vector3.ProjectOnPlane(dir, space * pose.planeNormal);

            // If the bone direction is not close enough to the target direction,
            // rotate it so it matches the target direction.
            float deltaAngle = Vector3.Angle(dir, goalDir);
            if (deltaAngle > pose.maxAngle * 0.99f)
            {
                Quaternion adjust = Quaternion.FromToRotation(dir, goalDir);

                // If this bone is hip or knee, remember global foor rotation and apply it after this adjustment
                Transform footBone = null;
                Quaternion footRot = Quaternion.identity;
                if (boneIndex == (int)HumanBodyBones.LeftUpperLeg || boneIndex == (int)HumanBodyBones.LeftLowerLeg)
                    footBone = bones[(int)HumanBodyBones.LeftFoot].bone;
                if (boneIndex == (int)HumanBodyBones.RightUpperLeg || boneIndex == (int)HumanBodyBones.RightLowerLeg)
                    footBone = bones[(int)HumanBodyBones.RightFoot].bone;
                if (footBone != null)
                    footRot = footBone.rotation;

                // Adjust only enough to fall within maxAngle
                float adjustAmount = Mathf.Clamp01(1.05f - (pose.maxAngle / deltaAngle));
                adjust = Quaternion.Slerp(Quaternion.identity, adjust, adjustAmount);

                bone.bone.rotation = adjust * bone.bone.rotation;

                // Revert foot rotation to what it was
                if (footBone != null)
                    footBone.rotation = footRot;
            }
        }

        private static Quaternion GetRotationSpace(BoneWrapper[] bones, Quaternion avatarOrientation, int boneIndex)
        {
            Quaternion parentDelta = Quaternion.identity;
            BonePoseData pose = sBonePoses[boneIndex];
            if (!pose.compareInGlobalSpace)
            {
                int parentIndex = HumanTrait.GetParentBone(boneIndex);

                if (parentIndex > 0)
                {
                    BonePoseData parentPose = sBonePoses[parentIndex];
                    if (bones[parentIndex].bone != null && parentPose != null)
                    {
                        Vector3 parentDir = GetBoneAlignmentDirection(bones, avatarOrientation, parentIndex);
                        if (parentDir != Vector3.zero)
                        {
                            Vector3 parentPoseDir = avatarOrientation * parentPose.direction;
                            parentDelta = Quaternion.FromToRotation(parentPoseDir, parentDir);
                        }
                    }
                }
            }

            return parentDelta * avatarOrientation;
        }

        private static Vector3 GetBoneAlignmentDirection(BoneWrapper[] bones, Quaternion avatarOrientation, int boneIndex)
        {
            if (sBonePoses[boneIndex] == null)
                return Vector3.zero;

            BoneWrapper bone = bones[boneIndex];
            Vector3 dir;

            // Get the child bone
            BonePoseData pose = sBonePoses[boneIndex];
            int childBoneIndex = -1;
            if (pose.childIndices != null)
            {
                foreach (int i in pose.childIndices)
                {
                    if (bones[i].bone != null)
                    {
                        childBoneIndex = i;
                        break;
                    }
                }
            }
            else
            {
                childBoneIndex = GetHumanBoneChild(bones, boneIndex);
            }

            // TODO@MECANIM Something si wrong with the indexes
            //if (boneIndex == (int)HumanBodyBones.LeftHand)
            //  Debug.Log ("Child bone for left hand: "+childBoneIndex);

            if (childBoneIndex >= 0 && bones[childBoneIndex] != null && bones[childBoneIndex].bone != null)
            {
                // Get direction from bone to child
                BoneWrapper childBone = bones[childBoneIndex];
                dir = childBone.bone.position - bone.bone.position;

                // TODO@MECANIM Something si wrong with the indexes
                //if (boneIndex == (int)HumanBodyBones.LeftHand)
                //  Debug.Log (" - "+childBone.humanBoneName + " - " +childBone.bone.name);
            }
            else
            {
                if (bone.bone.childCount != 1)
                    return Vector3.zero;

                dir = Vector3.zero;
                // Get direction from bone to child
                foreach (Transform child in bone.bone)
                {
                    dir = child.position - bone.bone.position;
                    break;
                }
            }

            return dir.normalized;
        }

        public static Quaternion AvatarComputeOrientation(BoneWrapper[] bones)
        {
            Transform leftUpLeg = bones[(int)HumanBodyBones.LeftUpperLeg].bone;
            Transform rightUpLeg = bones[(int)HumanBodyBones.RightUpperLeg].bone;
            Transform leftArm = bones[(int)HumanBodyBones.LeftUpperArm].bone;
            Transform rightArm = bones[(int)HumanBodyBones.RightUpperArm].bone;
            if (leftUpLeg != null && rightUpLeg != null && leftArm != null && rightArm != null)
                return AvatarComputeOrientation(leftUpLeg.position, rightUpLeg.position, leftArm.position, rightArm.position);
            else
                return Quaternion.identity;
        }

        public static Quaternion AvatarComputeOrientation(Vector3 leftUpLeg, Vector3 rightUpLeg, Vector3 leftArm, Vector3 rightArm)
        {
            Vector3 legsRightDir = Vector3.Normalize(rightUpLeg - leftUpLeg);
            Vector3 armsRightDir = Vector3.Normalize(rightArm - leftArm);
            Vector3 torsoRightDir = Vector3.Normalize(legsRightDir + armsRightDir);

            // Find out if torso right dir seems sensible or completely arbitrary.
            // It's sensible if it's aligned along some axis.
            bool sensibleOrientation =
                Mathf.Abs(torsoRightDir.x * torsoRightDir.y) < 0.05f &&
                Mathf.Abs(torsoRightDir.y * torsoRightDir.z) < 0.05f &&
                Mathf.Abs(torsoRightDir.z * torsoRightDir.x) < 0.05f;

            Vector3 legsAvgPos = (leftUpLeg + rightUpLeg) * 0.5f;
            Vector3 armsAvgPos = (leftArm + rightArm) * 0.5f;
            Vector3 torsoUpDir = Vector3.Normalize(armsAvgPos - legsAvgPos);

            // If the orientation is sensible, assume character up vector is aligned along x, y, or z axis, so fix it to closest axis
            if (sensibleOrientation)
            {
                int axisIndex = 0;
                for (int i = 1; i < 3; i++)
                    if (Mathf.Abs(torsoUpDir[i]) > Mathf.Abs(torsoUpDir[axisIndex]))
                        axisIndex = i;
                float sign = Mathf.Sign(torsoUpDir[axisIndex]);
                torsoUpDir = Vector3.zero;
                torsoUpDir[axisIndex] = sign;
            }

            Vector3 torsoForwardDir = Vector3.Cross(torsoRightDir, torsoUpDir);

            if (torsoForwardDir == Vector3.zero || torsoUpDir == Vector3.zero)
                return Quaternion.identity;

            return Quaternion.LookRotation(torsoForwardDir, torsoUpDir);
        }

        private static float GetCharacterPositionError(BoneWrapper[] bones)
        {
            float error;
            GetCharacterPositionAdjustVector(bones, out error);
            return error;
        }

        internal static void MakeCharacterPositionValid(BoneWrapper[] bones)
        {
            float error;
            Vector3 adjustVector = GetCharacterPositionAdjustVector(bones, out error);
            if (adjustVector != Vector3.zero)
                bones[(int)HumanBodyBones.Hips].bone.position += adjustVector;
        }

        private static Vector3 GetCharacterPositionAdjustVector(BoneWrapper[] bones, out float error)
        {
            error = 0;

            // Get hip bones
            Transform leftUpLeg = bones[(int)HumanBodyBones.LeftUpperLeg].bone;
            Transform rightUpLeg = bones[(int)HumanBodyBones.RightUpperLeg].bone;
            if (leftUpLeg == null || rightUpLeg == null)
                return Vector3.zero;
            Vector3 avgHipPos = (leftUpLeg.position + rightUpLeg.position) * 0.5f;

            // Get foot bones
            // Prefer toe bones but use foot bones if toes are not mapped
            bool usingToes = true;
            Transform leftFoot = bones[(int)HumanBodyBones.LeftToes].bone;
            Transform rightFoot = bones[(int)HumanBodyBones.RightToes].bone;
            if (leftFoot == null || rightFoot == null)
            {
                usingToes = false;
                leftFoot = bones[(int)HumanBodyBones.LeftFoot].bone;
                rightFoot = bones[(int)HumanBodyBones.RightFoot].bone;
            }
            if (leftFoot == null || rightFoot == null)
                return Vector3.zero;
            Vector3 avgFootPos = (leftFoot.position + rightFoot.position) * 0.5f;

            // Get approximate length of legs
            float hipsHeight = avgHipPos.y - avgFootPos.y;
            if (hipsHeight <= 0)
                return Vector3.zero;

            Vector3 adjustVector = Vector3.zero;

            // We can force the feet to be at an approximate good height.
            // But the feet might be at a perfect height from the start if the bind pose is good.
            // So only do it if the feet look like they're not at a good position from the beginning.
            // Check if feet are already at height that looks about right.
            if (avgFootPos.y < 0 || avgFootPos.y > hipsHeight * (usingToes ? 0.1f : 0.3f))
            {
                // Current height is not good, so adjust it using best guess based on human anatomy.
                float estimatedFootBottomHeight = avgHipPos.y - hipsHeight * (usingToes ? 1.03f : 1.13f);
                adjustVector.y = -estimatedFootBottomHeight;
            }

            // Move the avg hip pos to the center on the left-right axis if it's not already there.
            if (Mathf.Abs(avgHipPos.x) > 0.01f * hipsHeight)
                adjustVector.x = -avgHipPos.x;

            // Move the avg hip pos to the center on the front-back axis if it's not already approximately there.
            if (Mathf.Abs(avgHipPos.z) > 0.2f * hipsHeight)
                adjustVector.z = -avgHipPos.z;

            error = adjustVector.magnitude * 100 / hipsHeight;
            return adjustVector;
        }
    }
}
