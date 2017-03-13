// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;
using System.Linq;

using Object = UnityEngine.Object;

namespace UnityEditor
{
    internal class AvatarBipedMapper
    {
        private struct BipedBone
        {
            public string name;
            public int index;

            public BipedBone(string name, int index)
            {
                this.name = name;
                this.index = index;
            }
        }

        private static BipedBone[] s_BipedBones = new BipedBone[]
        {
            // body
            new BipedBone("Pelvis",        (int)HumanBodyBones.Hips),
            new BipedBone("L Thigh",       (int)HumanBodyBones.LeftUpperLeg),
            new BipedBone("R Thigh",       (int)HumanBodyBones.RightUpperLeg),
            new BipedBone("L Calf",        (int)HumanBodyBones.LeftLowerLeg),
            new BipedBone("R Calf",        (int)HumanBodyBones.RightLowerLeg),
            new BipedBone("L Foot",        (int)HumanBodyBones.LeftFoot),
            new BipedBone("R Foot",        (int)HumanBodyBones.RightFoot),
            new BipedBone("Spine",         (int)HumanBodyBones.Spine),
            new BipedBone("Spine1",        (int)HumanBodyBones.Chest),
            new BipedBone("Spine2",        (int)HumanBodyBones.UpperChest),
            new BipedBone("Neck",          (int)HumanBodyBones.Neck),
            new BipedBone("Head",          (int)HumanBodyBones.Head),
            new BipedBone("L Clavicle",    (int)HumanBodyBones.LeftShoulder),
            new BipedBone("R Clavicle",    (int)HumanBodyBones.RightShoulder),
            new BipedBone("L UpperArm",    (int)HumanBodyBones.LeftUpperArm),
            new BipedBone("R UpperArm",    (int)HumanBodyBones.RightUpperArm),
            new BipedBone("L Forearm",     (int)HumanBodyBones.LeftLowerArm),
            new BipedBone("R Forearm",     (int)HumanBodyBones.RightLowerArm),
            new BipedBone("L Hand",        (int)HumanBodyBones.LeftHand),
            new BipedBone("R Hand",        (int)HumanBodyBones.RightHand),
            new BipedBone("L Toe0",        (int)HumanBodyBones.LeftToes),
            new BipedBone("R Toe0",        (int)HumanBodyBones.RightToes),
            // Left Hand
            new BipedBone("L Finger0",     (int)HumanBodyBones.LeftThumbProximal),
            new BipedBone("L Finger01",    (int)HumanBodyBones.LeftThumbIntermediate),
            new BipedBone("L Finger02",    (int)HumanBodyBones.LeftThumbDistal),
            new BipedBone("L Finger1",     (int)HumanBodyBones.LeftIndexProximal),
            new BipedBone("L Finger11",    (int)HumanBodyBones.LeftIndexIntermediate),
            new BipedBone("L Finger12",    (int)HumanBodyBones.LeftIndexDistal),
            new BipedBone("L Finger2",     (int)HumanBodyBones.LeftMiddleProximal),
            new BipedBone("L Finger21",    (int)HumanBodyBones.LeftMiddleIntermediate),
            new BipedBone("L Finger22",    (int)HumanBodyBones.LeftMiddleDistal),
            new BipedBone("L Finger3",     (int)HumanBodyBones.LeftRingProximal),
            new BipedBone("L Finger31",    (int)HumanBodyBones.LeftRingIntermediate),
            new BipedBone("L Finger32",    (int)HumanBodyBones.LeftRingDistal),
            new BipedBone("L Finger4",     (int)HumanBodyBones.LeftLittleProximal),
            new BipedBone("L Finger41",    (int)HumanBodyBones.LeftLittleIntermediate),
            new BipedBone("L Finger42",    (int)HumanBodyBones.LeftLittleDistal),
            // Right Hand
            new BipedBone("R Finger0",     (int)HumanBodyBones.RightThumbProximal),
            new BipedBone("R Finger01",    (int)HumanBodyBones.RightThumbIntermediate),
            new BipedBone("R Finger02",    (int)HumanBodyBones.RightThumbDistal),
            new BipedBone("R Finger1",     (int)HumanBodyBones.RightIndexProximal),
            new BipedBone("R Finger11",    (int)HumanBodyBones.RightIndexIntermediate),
            new BipedBone("R Finger12",    (int)HumanBodyBones.RightIndexDistal),
            new BipedBone("R Finger2",     (int)HumanBodyBones.RightMiddleProximal),
            new BipedBone("R Finger21",    (int)HumanBodyBones.RightMiddleIntermediate),
            new BipedBone("R Finger22",    (int)HumanBodyBones.RightMiddleDistal),
            new BipedBone("R Finger3",     (int)HumanBodyBones.RightRingProximal),
            new BipedBone("R Finger31",    (int)HumanBodyBones.RightRingIntermediate),
            new BipedBone("R Finger32",    (int)HumanBodyBones.RightRingDistal),
            new BipedBone("R Finger4",     (int)HumanBodyBones.RightLittleProximal),
            new BipedBone("R Finger41",    (int)HumanBodyBones.RightLittleIntermediate),
            new BipedBone("R Finger42",    (int)HumanBodyBones.RightLittleDistal)
        };

        public static bool IsBiped(Transform root, List<string> report)
        {
            if (report != null)
            {
                report.Clear();
            }

            Transform[] humanToTransform = new Transform[HumanTrait.BoneCount];
            return MapBipedBones(root, ref humanToTransform, report);
        }

        public static Dictionary<int, Transform> MapBones(Transform root)
        {
            Dictionary<int, Transform> ret = new Dictionary<int, Transform>();

            Transform[] humanToTransform = new Transform[HumanTrait.BoneCount];

            if (MapBipedBones(root, ref humanToTransform, null))
            {
                for (int boneIter = 0; boneIter < HumanTrait.BoneCount; boneIter++)
                {
                    if (humanToTransform[boneIter] != null)
                    {
                        ret.Add(boneIter, humanToTransform[boneIter]);
                    }
                }
            }

            // Move upper chest to chest if no chest was found
            if (!ret.ContainsKey((int)HumanBodyBones.Chest) &&
                ret.ContainsKey((int)HumanBodyBones.UpperChest))
            {
                ret.Add((int)HumanBodyBones.Chest, ret[(int)HumanBodyBones.UpperChest]);
                ret.Remove((int)HumanBodyBones.UpperChest);
            }

            return ret;
        }

        private static bool MapBipedBones(Transform root, ref Transform[] humanToTransform, List<string> report)
        {
            for (int bipedBoneIter = 0; bipedBoneIter < s_BipedBones.Length; bipedBoneIter++)
            {
                int boneIndex = s_BipedBones[bipedBoneIter].index;

                int parentIndex = HumanTrait.GetParentBone(boneIndex);

                bool required = HumanTrait.RequiredBone(boneIndex);
                bool parentRequired = parentIndex != -1 ? HumanTrait.RequiredBone(parentIndex) : true;

                Transform parentTransform = parentIndex != -1 ? humanToTransform[parentIndex] : root;

                if (parentTransform == null && !parentRequired)
                {
                    parentIndex = HumanTrait.GetParentBone(parentIndex);
                    parentRequired = parentIndex != -1 ? HumanTrait.RequiredBone(parentIndex) : true;
                    parentTransform = parentIndex != -1 ? humanToTransform[parentIndex] : null;

                    if (parentTransform == null && !parentRequired)
                    {
                        parentIndex = HumanTrait.GetParentBone(parentIndex);
                        parentTransform = parentIndex != -1 ? humanToTransform[parentIndex] : null;
                    }
                }

                humanToTransform[boneIndex] = MapBipedBone(bipedBoneIter, parentTransform, parentTransform, report);

                if (humanToTransform[boneIndex] == null && required)
                {
                    return false;
                }
            }

            return true;
        }

        private static Transform MapBipedBone(int bipedBoneIndex, Transform transform, Transform parentTransform, List<string> report)
        {
            Transform ret = null;

            if (transform != null)
            {
                int childCount = transform.childCount;

                for (int childIter = 0; ret == null && childIter < childCount; childIter++)
                {
                    string boneName = s_BipedBones[bipedBoneIndex].name;
                    int boneIndex = s_BipedBones[bipedBoneIndex].index;

                    if (transform.GetChild(childIter).name.EndsWith(boneName))
                    {
                        ret = transform.GetChild(childIter);

                        if (ret != null && report != null && boneIndex != (int)HumanBodyBones.Hips && transform != parentTransform)
                        {
                            string current = "- Invalid parent for " + ret.name + ". Expected " + parentTransform.name + ", but found " + transform.name + ".";

                            if (boneIndex == (int)HumanBodyBones.LeftUpperLeg || boneIndex == (int)HumanBodyBones.RightUpperLeg)
                            {
                                current += " Disable Triangle Pelvis";
                            }
                            else if (boneIndex == (int)HumanBodyBones.LeftShoulder || boneIndex == (int)HumanBodyBones.RightShoulder)
                            {
                                current += " Enable Triangle Neck";
                            }
                            else if (boneIndex == (int)HumanBodyBones.Neck)
                            {
                                current += " Preferred is three Spine Links";
                            }
                            else if (boneIndex == (int)HumanBodyBones.Head)
                            {
                                current += " Preferred is one Neck Links";
                            }

                            current += "\n";

                            report.Add(current);
                        }
                    }
                }

                for (int childIter = 0; ret == null && childIter < childCount; childIter++)
                {
                    ret = MapBipedBone(bipedBoneIndex, transform.GetChild(childIter), parentTransform, report);
                }
            }

            return ret;
        }

        internal static void BipedPose(GameObject go, AvatarSetupTool.BoneWrapper[] bones)
        {
            BipedPose(go.transform, true);

            // Orient Biped
            Quaternion rot = AvatarSetupTool.AvatarComputeOrientation(bones);
            go.transform.rotation = Quaternion.Inverse(rot) * go.transform.rotation;

            // Move Biped feet to ground plane
            AvatarSetupTool.MakeCharacterPositionValid(bones);
        }

        private static void BipedPose(Transform t, bool ignore)
        {
            if (t.name.EndsWith("Pelvis"))
            {
                t.localRotation = Quaternion.Euler(270, 90, 0);
                ignore = false;
            }
            else if (t.name.EndsWith("Thigh"))
            {
                t.localRotation = Quaternion.Euler(0, 180, 0);
            }
            else if (t.name.EndsWith("Toe0"))
            {
                t.localRotation = Quaternion.Euler(0, 0, 270);
            }
            else if (t.name.EndsWith("L Clavicle"))
            {
                t.localRotation = Quaternion.Euler(0, 270, 180);
            }
            else if (t.name.EndsWith("R Clavicle"))
            {
                t.localRotation = Quaternion.Euler(0, 90, 180);
            }
            else if (t.name.EndsWith("L Hand"))
            {
                t.localRotation = Quaternion.Euler(270, 0, 0);
            }
            else if (t.name.EndsWith("R Hand"))
            {
                t.localRotation = Quaternion.Euler(90, 0, 0);
            }
            else if (t.name.EndsWith("L Finger0"))
            {
                t.localRotation = Quaternion.Euler(0, 315, 0);
            }
            else if (t.name.EndsWith("R Finger0"))
            {
                t.localRotation = Quaternion.Euler(0, 45, 0);
            }
            else if (!ignore)
            {
                t.localRotation = Quaternion.identity;
            }

            foreach (Transform child in t)
            {
                BipedPose(child, ignore);
            }
        }
    }
}
