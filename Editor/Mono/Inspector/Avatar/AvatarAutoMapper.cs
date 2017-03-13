// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System;
using System.Collections.Generic;

namespace UnityEditor
{
    internal class AvatarAutoMapper
    {
        private enum Side { None, Left, Right }

        private struct BoneMappingItem
        {
            public int parent;
            public int bone;
            public int minStep;
            public int maxStep;
            public float lengthRatio;
            public Vector3 dir;
            public Side side;
            public bool optional;
            public bool alwaysInclude;
            public string[] keywords;
            private int[] children;

            public BoneMappingItem(int parent, int bone, int minStep, int maxStep, float lengthRatio, Vector3 dir, Side side, bool optional, bool alwaysInclude, params string[] keywords)
            {
                this.parent = parent;
                this.bone = bone;
                this.minStep = minStep;
                this.maxStep = maxStep;
                this.lengthRatio = lengthRatio;
                this.dir = dir;
                this.side = side;
                this.optional = optional;
                this.alwaysInclude = alwaysInclude;
                this.keywords = keywords;
                this.children = null;
            }

            public BoneMappingItem(int parent, int bone, int minStep, int maxStep, float lengthRatio, Side side, bool optional, bool alwaysInclude, params string[] keywords)
                : this(parent, bone, minStep, maxStep, lengthRatio, Vector3.zero, side, optional, alwaysInclude, keywords) {}

            public BoneMappingItem(int parent, int bone, int minStep, int maxStep, float lengthRatio, Vector3 dir, Side side, params string[] keywords)
                : this(parent, bone, minStep, maxStep, lengthRatio, dir, side, false, false, keywords) {}

            public BoneMappingItem(int parent, int bone, int minStep, int maxStep, float lengthRatio, Side side, params string[] keywords)
                : this(parent, bone, minStep, maxStep, lengthRatio, Vector3.zero, side, false, false, keywords) {}

            public int[] GetChildren(BoneMappingItem[] mappingData)
            {
                if (children == null)
                {
                    List<int> list = new List<int>();
                    for (int i = 0; i < mappingData.Length; i++)
                        if (mappingData[i].parent == bone)
                            list.Add(i);
                    children = list.ToArray();
                }
                return children;
            }
        }

        private class BoneMatch : IComparable<BoneMatch>
        {
            public BoneMatch parent;
            public List<BoneMatch> children = new List<BoneMatch>();
            public bool doMap = false;
            public BoneMatch humanBoneParent
            {
                get
                {
                    BoneMatch p = parent;
                    while (p.parent != null && (p.item.bone < 0))
                        p = p.parent;
                    return p;
                }
            }
            public BoneMappingItem item;
            public Transform bone;
            public float score;
            public float siblingScore;
            public float totalSiblingScore { get { return score + siblingScore; } }
            public List<string> debugTracker = new List<string>();
            public BoneMatch(BoneMatch parent, Transform bone, BoneMappingItem item)
            {
                this.parent = parent;
                this.bone = bone;
                this.item = item;
            }

            public int CompareTo(BoneMatch other)
            {
                if (other == null)
                    return 1;
                return other.totalSiblingScore.CompareTo(totalSiblingScore);
            }
        }

        private struct QueuedBone
        {
            public Transform bone;
            public int level;
            public QueuedBone(Transform bone, int level) { this.bone = bone; this.level = level; }
        }

        private static bool kDebug = false;

        private static string[] kShoulderKeywords = {"shoulder", "collar", "clavicle"};
        private static string[] kUpperArmKeywords = {"up"};
        private static string[] kLowerArmKeywords = {"lo", "fore", "elbow"};
        private static string[] kHandKeywords = {"hand", "wrist"};

        private static string[] kUpperLegKeywords = {"up", "thigh"};
        private static string[] kLowerLegKeywords = {"lo", "calf", "knee", "shin"};
        private static string[] kFootKeywords = {"foot", "ankle"};
        private static string[] kToeKeywords = {"toe", "!end", "!top", "!nub"};

        private static string[] kNeckKeywords = {"neck"};
        private static string[] kHeadKeywords = {"head"};
        private static string[] kJawKeywords = {"jaw", "open", "!teeth", "!tongue", "!pony", "!braid", "!end", "!top", "!nub"};
        private static string[] kEyeKeywords = {"eye", "ball", "!brow", "!lid", "!pony", "!braid", "!end", "!top", "!nub"};

        private static string[] kThumbKeywords =        {"thu",          "!palm", "!wrist", "!end", "!top", "!nub"};
        private static string[] kIndexFingerKeywords =  {"ind", "point", "!palm", "!wrist", "!end", "!top", "!nub"};
        private static string[] kMiddleFingerKeywords = {"mid", "long",  "!palm", "!wrist", "!end", "!top", "!nub"};
        private static string[] kRingFingerKeywords =   {"rin",          "!palm", "!wrist", "!end", "!top", "!nub"};
        private static string[] kLittleFingerKeywords = {"lit", "pin",   "!palm", "!wrist", "!end", "!top", "!nub"};

        private static BoneMappingItem[] s_MappingDataBody = new BoneMappingItem[]
        {
            new BoneMappingItem(-1,                                (int)HumanBodyBones.Hips,           1, 3, 0.0f, Side.None),

            new BoneMappingItem((int)HumanBodyBones.Hips,          (int)HumanBodyBones.RightUpperLeg,  1, 2, 0.0f, Vector3.right, Side.Right, kUpperLegKeywords),
            new BoneMappingItem((int)HumanBodyBones.RightUpperLeg, (int)HumanBodyBones.RightLowerLeg,  1, 2, 3.0f, -Vector3.up, Side.Right, kLowerLegKeywords),
            new BoneMappingItem((int)HumanBodyBones.RightLowerLeg, (int)HumanBodyBones.RightFoot,      1, 2, 1.0f, -Vector3.up, Side.Right, kFootKeywords),
            new BoneMappingItem((int)HumanBodyBones.RightFoot,     (int)HumanBodyBones.RightToes,      1, 2, 0.5f, Vector3.forward, Side.Right, true, true, kToeKeywords),

            new BoneMappingItem((int)HumanBodyBones.Hips,          (int)HumanBodyBones.Spine,          1, 3, 0.0f, Vector3.up, Side.None),
            new BoneMappingItem((int)HumanBodyBones.Spine,         (int)HumanBodyBones.Chest,          0, 3, 1.4f, Vector3.up, Side.None, true, false),
            new BoneMappingItem((int)HumanBodyBones.Chest,         (int)HumanBodyBones.UpperChest,     0, 3, 1.4f, Vector3.up, Side.None, true, false),

            new BoneMappingItem((int)HumanBodyBones.UpperChest,    (int)HumanBodyBones.RightShoulder,  1, 3, 0.0f, Vector3.right, Side.Right, true, false, kShoulderKeywords),
            new BoneMappingItem((int)HumanBodyBones.RightShoulder, (int)HumanBodyBones.RightUpperArm,  0, 2, 0.5f, Vector3.right, Side.Right, kUpperArmKeywords),
            new BoneMappingItem((int)HumanBodyBones.RightUpperArm, (int)HumanBodyBones.RightLowerArm,  1, 2, 2.0f, Vector3.right, Side.Right, kLowerArmKeywords),
            new BoneMappingItem((int)HumanBodyBones.RightLowerArm, (int)HumanBodyBones.RightHand,      1, 2, 1.0f, Vector3.right, Side.Right, kHandKeywords),

            new BoneMappingItem((int)HumanBodyBones.UpperChest,    (int)HumanBodyBones.Neck,           1, 3, 1.8f, Vector3.up, Side.None, true, false, kNeckKeywords),
            new BoneMappingItem((int)HumanBodyBones.Neck,          (int)HumanBodyBones.Head,           0, 2, 0.3f, Vector3.up, Side.None, kHeadKeywords),

            new BoneMappingItem((int)HumanBodyBones.Head,          (int)HumanBodyBones.Jaw,            1, 2, 0.0f, Vector3.forward, Side.None, true, false, kJawKeywords),
            new BoneMappingItem((int)HumanBodyBones.Head,          (int)HumanBodyBones.RightEye,       1, 2, 0.0f, new Vector3(1, 1, 1), Side.Right, true, false, kEyeKeywords),

            new BoneMappingItem((int)HumanBodyBones.RightHand,     -2,     1, 2, 0.0f, new Vector3(1, -1, 2), Side.Right, true, false, kThumbKeywords),
            new BoneMappingItem((int)HumanBodyBones.RightHand,     -3,     1, 2, 0.0f, new Vector3(3, 0, 1), Side.Right, true, false, kIndexFingerKeywords),
        };

        private static BoneMappingItem[] s_LeftMappingDataHand = new BoneMappingItem[]
        {
            new BoneMappingItem(-2,                                        -1,                                         1, 2, 0.0f, Side.None),

            new BoneMappingItem(-1,                                        (int)HumanBodyBones.LeftThumbProximal,      1, 3, 0.0f, new Vector3(2, 0, 1), Side.None, kThumbKeywords),
            new BoneMappingItem(-1,                                        (int)HumanBodyBones.LeftIndexProximal,      1, 3, 0.0f, new Vector3(4, 0, 1), Side.None, kIndexFingerKeywords),
            new BoneMappingItem(-1,                                        (int)HumanBodyBones.LeftMiddleProximal,     1, 3, 0.0f, new Vector3(4, 0, 0), Side.None, kMiddleFingerKeywords),
            new BoneMappingItem(-1,                                        (int)HumanBodyBones.LeftRingProximal,       1, 3, 0.0f, new Vector3(4, 0, -1), Side.None, kRingFingerKeywords),
            new BoneMappingItem(-1,                                        (int)HumanBodyBones.LeftLittleProximal,     1, 3, 0.0f, new Vector3(4, 0, -2), Side.None, kLittleFingerKeywords),

            new BoneMappingItem((int)HumanBodyBones.LeftThumbProximal,     (int)HumanBodyBones.LeftThumbIntermediate,  1, 1, 0.0f, Side.None, false, true),
            new BoneMappingItem((int)HumanBodyBones.LeftIndexProximal,     (int)HumanBodyBones.LeftIndexIntermediate,  1, 1, 0.0f, Side.None, false, true),
            new BoneMappingItem((int)HumanBodyBones.LeftMiddleProximal,    (int)HumanBodyBones.LeftMiddleIntermediate, 1, 1, 0.0f, Side.None, false, true),
            new BoneMappingItem((int)HumanBodyBones.LeftRingProximal,      (int)HumanBodyBones.LeftRingIntermediate,   1, 1, 0.0f, Side.None, false, true),
            new BoneMappingItem((int)HumanBodyBones.LeftLittleProximal,    (int)HumanBodyBones.LeftLittleIntermediate, 1, 1, 0.0f, Side.None, false, true),

            new BoneMappingItem((int)HumanBodyBones.LeftThumbIntermediate, (int)HumanBodyBones.LeftThumbDistal,        1, 1, 0.0f, Side.None, false, true),
            new BoneMappingItem((int)HumanBodyBones.LeftIndexIntermediate, (int)HumanBodyBones.LeftIndexDistal,        1, 1, 0.0f, Side.None, false, true),
            new BoneMappingItem((int)HumanBodyBones.LeftMiddleIntermediate, (int)HumanBodyBones.LeftMiddleDistal,       1, 1, 0.0f, Side.None, false, true),
            new BoneMappingItem((int)HumanBodyBones.LeftRingIntermediate,  (int)HumanBodyBones.LeftRingDistal,         1, 1, 0.0f, Side.None, false, true),
            new BoneMappingItem((int)HumanBodyBones.LeftLittleIntermediate, (int)HumanBodyBones.LeftLittleDistal,       1, 1, 0.0f, Side.None, false, true),
        };

        private static BoneMappingItem[] s_RightMappingDataHand = new BoneMappingItem[]
        {
            new BoneMappingItem(-2,                                        -1,                                         1, 2, 0.0f, Side.None),

            new BoneMappingItem(-1,                                            (int)HumanBodyBones.RightThumbProximal,     1, 3, 0.0f, new Vector3(2, 0, 1), Side.None, kThumbKeywords),
            new BoneMappingItem(-1,                                            (int)HumanBodyBones.RightIndexProximal,     1, 3, 0.0f, new Vector3(4, 0, 1), Side.None, kIndexFingerKeywords),
            new BoneMappingItem(-1,                                            (int)HumanBodyBones.RightMiddleProximal,    1, 3, 0.0f, new Vector3(4, 0, 0), Side.None, kMiddleFingerKeywords),
            new BoneMappingItem(-1,                                            (int)HumanBodyBones.RightRingProximal,      1, 3, 0.0f, new Vector3(4, 0, -1), Side.None, kRingFingerKeywords),
            new BoneMappingItem(-1,                                            (int)HumanBodyBones.RightLittleProximal,    1, 3, 0.0f, new Vector3(4, 0, -2), Side.None, kLittleFingerKeywords),

            new BoneMappingItem((int)HumanBodyBones.RightThumbProximal,        (int)HumanBodyBones.RightThumbIntermediate, 1, 1, 0.0f, Side.None, false, true),
            new BoneMappingItem((int)HumanBodyBones.RightIndexProximal,        (int)HumanBodyBones.RightIndexIntermediate, 1, 1, 0.0f, Side.None, false, true),
            new BoneMappingItem((int)HumanBodyBones.RightMiddleProximal,       (int)HumanBodyBones.RightMiddleIntermediate, 1, 1, 0.0f, Side.None, false, true),
            new BoneMappingItem((int)HumanBodyBones.RightRingProximal,         (int)HumanBodyBones.RightRingIntermediate,  1, 1, 0.0f, Side.None, false, true),
            new BoneMappingItem((int)HumanBodyBones.RightLittleProximal,       (int)HumanBodyBones.RightLittleIntermediate, 1, 1, 0.0f, Side.None, false, true),

            new BoneMappingItem((int)HumanBodyBones.RightThumbIntermediate,    (int)HumanBodyBones.RightThumbDistal,       1, 1, 0.0f, Side.None, false, true),
            new BoneMappingItem((int)HumanBodyBones.RightIndexIntermediate,    (int)HumanBodyBones.RightIndexDistal,       1, 1, 0.0f, Side.None, false, true),
            new BoneMappingItem((int)HumanBodyBones.RightMiddleIntermediate,   (int)HumanBodyBones.RightMiddleDistal,      1, 1, 0.0f, Side.None, false, true),
            new BoneMappingItem((int)HumanBodyBones.RightRingIntermediate,     (int)HumanBodyBones.RightRingDistal,        1, 1, 0.0f, Side.None, false, true),
            new BoneMappingItem((int)HumanBodyBones.RightLittleIntermediate,   (int)HumanBodyBones.RightLittleDistal,      1, 1, 0.0f, Side.None, false, true),
        };

        private static bool s_DidPerformInit = false;

        private Dictionary<Transform, bool> m_ValidBones;
        private bool m_TreatDummyBonesAsReal = false;

        private Quaternion m_Orientation;
        private int m_MappingIndexOffset = 0;
        private BoneMappingItem[] m_MappingData;

        private Dictionary<string, int> m_BoneHasKeywordDict;
        private Dictionary<string, int> m_BoneHasBadKeywordDict;
        private Dictionary<int, BoneMatch> m_BoneMatchDict;

        private static int GetLeftBoneIndexFromRight(int rightIndex)
        {
            if (rightIndex < 0)
            {
                return rightIndex;
            }
            else if (rightIndex < (int)HumanBodyBones.LastBone)
            {
                string name = Enum.GetName(typeof(HumanBodyBones), rightIndex);
                name = name.Replace("Right", "Left");
                return (int)(HumanBodyBones)Enum.Parse(typeof(HumanBodyBones), name);
            }

            return -1;
        }

        public static void InitGlobalMappingData()
        {
            if (s_DidPerformInit)
                return;

            List<BoneMappingItem> mappingData = new List<BoneMappingItem>(s_MappingDataBody);

            // Add left side bones to match right side ones.
            int size = mappingData.Count;
            for (int i = 0; i < size; i++)
            {
                BoneMappingItem item = mappingData[i];
                if (item.side == Side.Right)
                {
                    // Get left HumanBodyBones that mirrors right one.
                    int bone = GetLeftBoneIndexFromRight(item.bone);
                    // Get left parent HumanBodyBones that mirrors parent of right one
                    int parentBone = GetLeftBoneIndexFromRight(item.parent);
                    // Add left BoneMappingItem that mirrors right one.
                    mappingData.Add(new BoneMappingItem(parentBone, bone, item.minStep, item.maxStep, item.lengthRatio, new Vector3(-item.dir.x, item.dir.y, item.dir.z), Side.Left, item.optional, item.alwaysInclude, item.keywords));
                }
            }

            s_MappingDataBody = mappingData.ToArray();

            // Cache children for each BoneMappingItem
            for (int i = 0; i < s_MappingDataBody.Length; i++)
                s_MappingDataBody[i].GetChildren(s_MappingDataBody);
            for (int i = 0; i < s_LeftMappingDataHand.Length; i++)
                s_LeftMappingDataHand[i].GetChildren(s_LeftMappingDataHand);
            for (int i = 0; i < s_RightMappingDataHand.Length; i++)
                s_RightMappingDataHand[i].GetChildren(s_RightMappingDataHand);

            s_DidPerformInit = true;
        }

        public static Dictionary<int, Transform> MapBones(Transform root, Dictionary<Transform, bool> validBones)
        {
            AvatarAutoMapper mapper = new AvatarAutoMapper(validBones);
            return mapper.MapBones(root);
        }

        public AvatarAutoMapper(Dictionary<Transform, bool> validBones)
        {
            m_BoneHasKeywordDict = new Dictionary<string, int>();
            m_BoneHasBadKeywordDict = new Dictionary<string, int>();
            m_BoneMatchDict = new Dictionary<int, BoneMatch>();
            m_ValidBones = validBones;
        }

        public Dictionary<int, Transform> MapBones(Transform root)
        {
            InitGlobalMappingData();

            Dictionary<int, Transform> mapping = new Dictionary<int, Transform>();

            // Perform body mapping
            {
                m_Orientation = Quaternion.identity;
                m_MappingData = s_MappingDataBody;
                m_MappingIndexOffset = 0;
                m_BoneMatchDict.Clear();
                BoneMatch rootMatch = new BoneMatch(null, root, m_MappingData[0]);

                m_TreatDummyBonesAsReal = false;
                MapBonesFromRootDown(rootMatch, mapping);
                // There are 15 required bones. If we mapped less than that, check if we can do better.
                if (mapping.Count < 15)
                {
                    m_TreatDummyBonesAsReal = true;
                    MapBonesFromRootDown(rootMatch, mapping);
                }

                // Check if character has correct alignment
                if (mapping.ContainsKey((int)HumanBodyBones.LeftUpperLeg) && mapping.ContainsKey((int)HumanBodyBones.RightUpperLeg) &&
                    mapping.ContainsKey((int)HumanBodyBones.LeftUpperArm) && mapping.ContainsKey((int)HumanBodyBones.RightUpperArm))
                {
                    m_Orientation = AvatarSetupTool.AvatarComputeOrientation(
                            mapping[(int)HumanBodyBones.LeftUpperLeg].position, mapping[(int)HumanBodyBones.RightUpperLeg].position,
                            mapping[(int)HumanBodyBones.LeftUpperArm].position, mapping[(int)HumanBodyBones.RightUpperArm].position);

                    // If not standard aligned, try to map again with correct alignment assumptions
                    if (Vector3.Angle(m_Orientation * Vector3.up, Vector3.up) > 20 || Vector3.Angle(m_Orientation * Vector3.forward, Vector3.forward) > 20)
                    {
                        if (kDebug)
                            Debug.Log("*** Mapping with new computed orientation");
                        mapping.Clear();
                        m_BoneMatchDict.Clear();
                        MapBonesFromRootDown(rootMatch, mapping);
                    }
                }

                // For models that don't have meshes, all bones are marked valid; even the root.
                // So we use this to check if this model has meshes or not.
                bool modelHasMeshes = !(m_ValidBones.ContainsKey(root) && m_ValidBones[root] == true);

                // Fix up hips to be valid bone closest to the root
                // For models with meshes, valid bones further up are mapped to part of the mesh (otherwise they wouldn't be valid).
                // For models with no meshes we don't know which transforms are valid bones and which aren't
                // so we skip this step and use the found hips bone as-is.
                if (modelHasMeshes && mapping.Count > 0 && mapping.ContainsKey((int)HumanBodyBones.Hips))
                {
                    while (true)
                    {
                        Transform parent = mapping[(int)HumanBodyBones.Hips].parent;
                        if (parent != null && parent != rootMatch.bone && m_ValidBones.ContainsKey(parent) && m_ValidBones[parent] == true)
                            mapping[(int)HumanBodyBones.Hips] = parent;
                        else
                            break;
                    }
                }

                // Move upper chest to chest if no chest was found
                if (!mapping.ContainsKey((int)HumanBodyBones.Chest) &&
                    mapping.ContainsKey((int)HumanBodyBones.UpperChest))
                {
                    mapping.Add((int)HumanBodyBones.Chest, mapping[(int)HumanBodyBones.UpperChest]);
                    mapping.Remove((int)HumanBodyBones.UpperChest);
                }
            }

            int kMinFingerBones = 3;

            Quaternion bodyOrientation = m_Orientation;

            // Perform left hand mapping
            if (mapping.ContainsKey((int)HumanBodyBones.LeftHand))
            {
                Transform lowerArm = mapping[(int)HumanBodyBones.LeftLowerArm];
                Transform hand = mapping[(int)HumanBodyBones.LeftHand];

                // Use reference orientation based on lower arm for mapping hand
                m_Orientation = Quaternion.FromToRotation(bodyOrientation * -Vector3.right, hand.position - lowerArm.position) * bodyOrientation;

                m_MappingData = s_LeftMappingDataHand;
                m_MappingIndexOffset = (int)HumanBodyBones.LeftThumbProximal;
                m_BoneMatchDict.Clear();
                BoneMatch rootMatch = new BoneMatch(null, lowerArm, m_MappingData[0]);
                m_TreatDummyBonesAsReal = true;

                int mappingCountBefore = mapping.Count;
                MapBonesFromRootDown(rootMatch, mapping);

                // If we only mapped 2 finger bones or less, then cancel mapping of fingers
                if (mapping.Count < mappingCountBefore + kMinFingerBones)
                {
                    for (int i = (int)HumanBodyBones.LeftThumbProximal; i <= (int)HumanBodyBones.LeftLittleDistal; i++)
                        mapping.Remove(i);
                }
            }

            // Perform right hand mapping
            if (mapping.ContainsKey((int)HumanBodyBones.RightHand))
            {
                Transform lowerArm = mapping[(int)HumanBodyBones.RightLowerArm];
                Transform hand = mapping[(int)HumanBodyBones.RightHand];

                // Use reference orientation based on lower arm for mapping hand
                m_Orientation = Quaternion.FromToRotation(bodyOrientation * Vector3.right, hand.position - lowerArm.position) * bodyOrientation;

                m_MappingData = s_RightMappingDataHand;
                m_MappingIndexOffset = (int)HumanBodyBones.RightThumbProximal;
                m_BoneMatchDict.Clear();
                BoneMatch rootMatch = new BoneMatch(null, lowerArm, m_MappingData[0]);
                m_TreatDummyBonesAsReal = true;

                int mappingCountBefore = mapping.Count;
                MapBonesFromRootDown(rootMatch, mapping);

                // If we only mapped 2 finger bones or less, then cancel mapping of fingers
                if (mapping.Count < mappingCountBefore + kMinFingerBones)
                {
                    for (int i = (int)HumanBodyBones.RightThumbProximal; i <= (int)HumanBodyBones.RightLittleDistal; i++)
                        mapping.Remove(i);
                }
            }

            return mapping;
        }

        private void MapBonesFromRootDown(BoneMatch rootMatch, Dictionary<int, Transform> mapping)
        {
            // Perform mapping
            List<BoneMatch> childMatches = RecursiveFindPotentialBoneMatches(rootMatch, m_MappingData[0], true);
            if (childMatches != null && childMatches.Count > 0)
            {
                if (kDebug)
                {
                    EvaluateBoneMatch(childMatches[0], true);
                }
                ApplyMapping(childMatches[0], mapping);
            }
        }

        private void ApplyMapping(BoneMatch match, Dictionary<int, Transform> mapping)
        {
            if (match.doMap)
                mapping[match.item.bone] = match.bone;
            foreach (BoneMatch child in match.children)
                ApplyMapping(child, mapping);
        }

        private string GetStrippedAndNiceBoneName(Transform bone)
        {
            string[] strings = bone.name.Split(':');
            return ObjectNames.NicifyVariableName(strings[strings.Length - 1]);
        }

        private int BoneHasBadKeyword(Transform bone, params string[] keywords)
        {
            string key = bone.GetInstanceID() + ":" + String.Concat(keywords);
            if (m_BoneHasBadKeywordDict.ContainsKey(key))
                return m_BoneHasBadKeywordDict[key];

            int score = 0;
            string boneName;

            // If parent transform has any of keywords, don't accept.
            Transform parent = bone.parent;
            while (parent.parent != null && m_ValidBones.ContainsKey(parent) && !m_ValidBones[parent])
                parent = parent.parent;
            boneName = GetStrippedAndNiceBoneName(parent).ToLower();
            foreach (string word in keywords)
                if (word[0] != '!' && boneName.Contains(word))
                {
                    score = -20;
                    m_BoneHasBadKeywordDict[key] = score;
                    return score;
                }

            // If this transform has any of the illegal keywords, don't accept.
            boneName = GetStrippedAndNiceBoneName(bone).ToLower();
            foreach (string word in keywords)
                if (word[0] == '!' && boneName.Contains(word.Substring(1)))
                {
                    score = -1000;
                    m_BoneHasBadKeywordDict[key] = score;
                    return score;
                }

            m_BoneHasBadKeywordDict[key] = score;
            return score;
        }

        private int BoneHasKeyword(Transform bone, params string[] keywords)
        {
            string key = bone.GetInstanceID() + ":" + String.Concat(keywords);
            if (m_BoneHasKeywordDict.ContainsKey(key))
                return m_BoneHasKeywordDict[key];

            int score = 0;

            // If this transform has any of the keywords, accept.
            string boneName = GetStrippedAndNiceBoneName(bone).ToLower();
            foreach (string word in keywords)
                if (word[0] != '!' && boneName.Contains(word))
                {
                    score = 20;
                    m_BoneHasKeywordDict[key] = score;
                    return score;
                }

            m_BoneHasKeywordDict[key] = score;
            return score;
        }

        private const string kLeftMatch  = @"(^|.*[ \.:_-])[lL]($|[ \.:_-].*)";
        private const string kRightMatch = @"(^|.*[ \.:_-])[rR]($|[ \.:_-].*)";
        private bool MatchesSideKeywords(string boneName, bool left)
        {
            if (boneName.ToLower().Contains(left ? "left" : "right"))
                return true;

            if (System.Text.RegularExpressions.Regex.Match(boneName, left ? kLeftMatch : kRightMatch).Length > 0)
                return true;

            return false;
        }

        private int GetBoneSideMatchPoints(BoneMatch match)
        {
            string boneName = match.bone.name;
            if (match.item.side == Side.None)
                if (MatchesSideKeywords(boneName, false) || MatchesSideKeywords(boneName, true))
                    return -1000;

            bool left = (match.item.side == Side.Left);
            if (MatchesSideKeywords(boneName, left))
                return 15;
            if (MatchesSideKeywords(boneName, !left))
                return -1000;

            return 0;
        }

        private int GetMatchKey(BoneMatch parentMatch, Transform t, BoneMappingItem goalItem)
        {
            SimpleProfiler.Begin("GetMatchKey");
            int key = goalItem.bone;
            key += t.GetInstanceID() * 1000;
            if (parentMatch != null)
            {
                key += parentMatch.bone.GetInstanceID() * 1000000;
                if (parentMatch.parent != null)
                    key += parentMatch.parent.bone.GetInstanceID() * 1000000000;
            }
            SimpleProfiler.End();
            return key;
        }

        // Returns possible matches sorted with best-scoring ones first in the list
        private List<BoneMatch> RecursiveFindPotentialBoneMatches(BoneMatch parentMatch, BoneMappingItem goalItem, bool confirmedChoice)
        {
            List<BoneMatch> matches = new List<BoneMatch>();

            // We want to search with breadh first search so we have to use a queue
            Queue<QueuedBone> queue = new Queue<QueuedBone>();

            // Find matches
            queue.Enqueue(new QueuedBone(parentMatch.bone, 0));
            while (queue.Count > 0)
            {
                QueuedBone current = queue.Dequeue();
                Transform t = current.bone;
                if (current.level >= goalItem.minStep && (m_TreatDummyBonesAsReal || m_ValidBones == null || (m_ValidBones.ContainsKey(t) && m_ValidBones[t])))
                {
                    BoneMatch match;
                    var key = GetMatchKey(parentMatch, t, goalItem);
                    if (m_BoneMatchDict.ContainsKey(key))
                    {
                        match = m_BoneMatchDict[key];
                    }
                    else
                    {
                        match = new BoneMatch(parentMatch, t, goalItem);

                        // RECURSIVE CALL
                        EvaluateBoneMatch(match, false);
                        m_BoneMatchDict[key] = match;
                    }

                    if (match.score > 0 || kDebug)
                        matches.Add(match);
                }
                SimpleProfiler.Begin("Queue");
                if (current.level < goalItem.maxStep)
                {
                    foreach (Transform child in t)
                        if (m_ValidBones == null || m_ValidBones.ContainsKey(child))
                            if (!m_TreatDummyBonesAsReal && m_ValidBones != null && !m_ValidBones[child])
                                queue.Enqueue(new QueuedBone(child, current.level));
                            else
                                queue.Enqueue(new QueuedBone(child, current.level + 1));
                }
                SimpleProfiler.End();
            }

            if (matches.Count == 0)
                return null;

            // Sort by match score with best matches first
            SimpleProfiler.Begin("SortAndTrim");
            matches.Sort();
            if (matches[0].score <= 0)
                return null;

            if (kDebug && confirmedChoice)
                DebugMatchChoice(matches);

            // Keep top 3 priorities only for optimization
            while (matches.Count > 3)
                matches.RemoveAt(matches.Count - 1);
            matches.TrimExcess();
            SimpleProfiler.End();

            return matches;
        }

        private string GetNameOfBone(int boneIndex)
        {
            if (boneIndex < 0)
                return "" + boneIndex;

            return "" + (HumanBodyBones)boneIndex;
        }

        private string GetMatchString(BoneMatch match)
        {
            return GetNameOfBone(match.item.bone) + ":" + (match.bone == null ? "null" : match.bone.name);
        }

        private void DebugMatchChoice(List<BoneMatch> matches)
        {
            string str = GetNameOfBone(matches[0].item.bone) + " preferred order: ";
            for (int i = 0; i < matches.Count; i++)
                str += matches[i].bone.name + " (" + matches[i].score.ToString("0.0") + " / " + matches[i].totalSiblingScore.ToString("0.0") + "), ";
            foreach (BoneMatch m in matches)
            {
                str += "\n   Match " + m.bone.name + " (" + m.score.ToString("0.0") + " / " + m.totalSiblingScore.ToString("0.0") + "):";
                foreach (string s in m.debugTracker)
                    str += "\n    - " + s;
            }
            Debug.Log(str);
        }

        private bool IsParentOfOther(Transform knownCommonParent, Transform potentialParent, Transform potentialChild)
        {
            Transform t = potentialChild;
            while (t != knownCommonParent)
            {
                if (t == potentialParent)
                    return true;
                if (t == knownCommonParent)
                    return false;
                t = t.parent;
            }
            return false;
        }

        private bool ShareTransformPath(Transform commonParent, Transform childA, Transform childB)
        {
            return IsParentOfOther(commonParent, childA, childB) || IsParentOfOther(commonParent, childB, childA);
        }

        private List<BoneMatch> GetBestChildMatches(BoneMatch parentMatch, List<List<BoneMatch>> childMatchesLists)
        {
            List<BoneMatch> bestMatches = new List<BoneMatch>();
            if (childMatchesLists.Count == 1)
            {
                bestMatches.Add(childMatchesLists[0][0]);
                return bestMatches;
            }

            int[] choices = new int[childMatchesLists.Count];
            float dummyScore;
            choices = GetBestChildMatchChoices(parentMatch, childMatchesLists, choices, out dummyScore);
            for (int i = 0; i < choices.Length; i++)
            {
                if (choices[i] >= 0)
                    bestMatches.Add(childMatchesLists[i][choices[i]]);
            }
            return bestMatches;
        }

        private int[] GetBestChildMatchChoices(BoneMatch parentMatch, List<List<BoneMatch>> childMatchesLists, int[] choices, out float score)
        {
            // See if any choices share the same transform path.
            List<int> sharedIndices = new List<int>();
            for (int i = 0; i < choices.Length; i++)
            {
                if (choices[i] < 0)
                    continue;
                sharedIndices.Clear();
                sharedIndices.Add(i);
                for (int j = i + 1; j < choices.Length; j++)
                {
                    if (choices[j] < 0)
                        continue;
                    if (ShareTransformPath(parentMatch.bone, childMatchesLists[i][choices[i]].bone, childMatchesLists[j][choices[j]].bone))
                        sharedIndices.Add(j);
                }

                if (sharedIndices.Count > 1)
                    break;
            }

            if (sharedIndices.Count <= 1)
            {
                // If no shared transform paths, calculate score and return choices.
                score = 0;
                for (int i = 0; i < choices.Length; i++)
                    if (choices[i] >= 0)
                        score += childMatchesLists[i][choices[i]].totalSiblingScore;
                return choices;
            }
            else
            {
                // If transform paths are shared by multiple choices, call function recursively.
                float bestScore = 0;
                int[] bestChoices = choices;
                for (int i = 0; i < sharedIndices.Count; i++)
                {
                    int[] altChoices = new int[choices.Length];
                    Array.Copy(choices, altChoices, choices.Length);
                    // In each call, one of the choices retains their priority while the remaining are bumped one priority down.
                    for (int j = 0; j < sharedIndices.Count; j++)
                    {
                        if (i != j)
                        {
                            if (sharedIndices[j] >= altChoices.Length)
                                Debug.LogError("sharedIndices[j] (" + sharedIndices[j] + ") >= altChoices.Length (" + altChoices.Length + ")");
                            if (sharedIndices[j] >= childMatchesLists.Count)
                                Debug.LogError("sharedIndices[j] (" + sharedIndices[j] + ") >= childMatchesLists.Count (" + childMatchesLists.Count + ")");
                            if (altChoices[sharedIndices[j]] < childMatchesLists[sharedIndices[j]].Count - 1)
                                altChoices[sharedIndices[j]]++;
                            else
                                altChoices[sharedIndices[j]] = -1;
                        }
                    }
                    float altScore;
                    altChoices = GetBestChildMatchChoices(parentMatch, childMatchesLists, altChoices, out altScore);
                    if (altScore > bestScore)
                    {
                        bestScore = altScore;
                        bestChoices = altChoices;
                    }
                }
                // Return the choices with the best score
                score = bestScore;
                return bestChoices;
            }
        }

        private void EvaluateBoneMatch(BoneMatch match, bool confirmedChoice)
        {
            match.score = 0;
            match.siblingScore = 0;

            // Things to copy from identical match: score, siblingScore, children, doMap

            // Iterate child BoneMappingitems
            List<List<BoneMatch>> childMatchesLists = new List<List<BoneMatch>>();
            int intendedChildCount = 0;
            foreach (int c in match.item.GetChildren(m_MappingData))
            {
                BoneMappingItem i = m_MappingData[c];
                if (i.parent == match.item.bone)
                {
                    intendedChildCount++;
                    // RECURSIVE CALL
                    List<BoneMatch> childMatches = RecursiveFindPotentialBoneMatches(match, i, confirmedChoice);
                    if (childMatches == null || childMatches.Count == 0)
                        continue;
                    childMatchesLists.Add(childMatches);
                }
            }

            // Best best child matches
            bool sameAsParentOrChild = (match.bone == match.humanBoneParent.bone);
            int childCount = 0;
            if (childMatchesLists.Count > 0)
            {
                SimpleProfiler.Begin("GetBestChildMatches");
                match.children = GetBestChildMatches(match, childMatchesLists);
                SimpleProfiler.End();

                // Handle child matches
                foreach (BoneMatch childMatch in match.children)
                {
                    // RECURSIVE CALL for debugging purposes
                    if (kDebug && confirmedChoice)
                        EvaluateBoneMatch(childMatch, confirmedChoice);

                    // Transfer info from best child match to parent
                    childCount++;
                    match.score += childMatch.score;
                    if (kDebug)
                        match.debugTracker.AddRange(childMatch.debugTracker);
                    if (childMatch.bone == match.bone && childMatch.item.bone >= 0)
                        sameAsParentOrChild = true;
                }
            }

            SimpleProfiler.Begin("ScoreBoneMatch");
            // Keyword score the bone if it's not optional or if it's different from both parent bone and all child bones
            if (!match.item.optional || !sameAsParentOrChild)
                ScoreBoneMatch(match);
            SimpleProfiler.End();

            // Rate bone according to how well it matches goal direction
            SimpleProfiler.Begin("MatchesDir");
            if (match.item.dir != Vector3.zero)
            {
                Vector3 goalDir = match.item.dir;
                if (m_MappingIndexOffset >= (int)HumanBodyBones.LeftThumbProximal && m_MappingIndexOffset < (int)HumanBodyBones.RightThumbProximal)
                    goalDir.x *= -1;
                Vector3 dir = (match.bone.position - match.humanBoneParent.bone.position).normalized;
                dir = Quaternion.Inverse(m_Orientation) * dir;
                float dirMatchingScore = Vector3.Dot(dir, goalDir) * (match.item.optional ? 5 : 10);

                match.siblingScore += dirMatchingScore;
                if (kDebug)
                    match.debugTracker.Add("* " + dirMatchingScore + ": " + GetMatchString(match) + " matched dir (" + (match.bone.position - match.humanBoneParent.bone.position).normalized + " , " + goalDir + ")");

                if (dirMatchingScore > 0)
                {
                    match.score += 10;
                    if (kDebug)
                        match.debugTracker.Add(10 + ": " + GetMatchString(match) + " matched dir (" + (match.bone.position - match.humanBoneParent.bone.position).normalized + " , " + goalDir + ")");
                }
            }
            SimpleProfiler.End();

            // Give small score if bone matches side it belongs to.
            SimpleProfiler.Begin("MatchesSide");
            if (m_MappingIndexOffset == 0)
            {
                int sideMatchingScore = GetBoneSideMatchPoints(match);
                if (match.parent.item.side == Side.None || sideMatchingScore < 0)
                {
                    match.siblingScore += sideMatchingScore;
                    if (kDebug)
                        match.debugTracker.Add("* " + sideMatchingScore + ": " + GetMatchString(match) + " matched side");
                }
            }
            SimpleProfiler.End();

            // These criteria can not push a bone above the threshold, but they can help to break ties.
            if (match.score > 0)
            {
                // Reward optional bones being included
                if (match.item.optional && !sameAsParentOrChild)
                {
                    match.score += 5;
                    if (kDebug)
                        match.debugTracker.Add(5 + ": " + GetMatchString(match) + " optional bone is included");
                }

                // Handle end bones
                if (intendedChildCount == 0 && match.bone.childCount > 0)
                {
                    // Reward end bones having a dummy child transform
                    match.score += 1;
                    if (kDebug)
                        match.debugTracker.Add(1 + ": " + GetMatchString(match) + " has dummy child bone");
                }

                // Give score to bones length ratio according to match with goal ratio.
                SimpleProfiler.Begin("LengthRatio");
                if (match.item.lengthRatio != 0)
                {
                    float parentLength = Vector3.Distance(match.bone.position, match.humanBoneParent.bone.position);
                    if (parentLength == 0 && match.bone != match.humanBoneParent.bone)
                    {
                        match.score -= 1000;
                        if (kDebug)
                            match.debugTracker.Add((-1000) + ": " + GetMatchString(match.humanBoneParent) + " has zero length");
                    }

                    float grandParentLength = Vector3.Distance(match.humanBoneParent.bone.position, match.humanBoneParent.humanBoneParent.bone.position);
                    if (grandParentLength > 0)
                    {
                        float logRatio = Mathf.Log(parentLength / grandParentLength, 2);
                        float logGoalRatio = Mathf.Log(match.item.lengthRatio, 2);
                        float ratioScore = 10 * Mathf.Clamp(1 - 0.6f * Mathf.Abs(logRatio - logGoalRatio), 0, 1);
                        match.score += ratioScore;
                        if (kDebug)
                            match.debugTracker.Add(ratioScore + ": parent " + GetMatchString(match.humanBoneParent) + " matched lengthRatio - " + parentLength + " / " + grandParentLength + " = " + (parentLength / grandParentLength) + " (" + logRatio + ") goal: " + match.item.lengthRatio + " (" + logGoalRatio + ")");
                    }
                }
                SimpleProfiler.End();
            }

            // Only map optional bones if they're not the same as the parent or child.
            if (match.item.bone >= 0 && (!match.item.optional || !sameAsParentOrChild))
                match.doMap = true;
        }

        private void ScoreBoneMatch(BoneMatch match)
        {
            int badKeywordScore = BoneHasBadKeyword(match.bone, match.item.keywords);
            match.score += badKeywordScore;
            if (kDebug && badKeywordScore != 0)
                match.debugTracker.Add(badKeywordScore + ": " + GetMatchString(match) + " matched bad keywords");
            if (badKeywordScore < 0)
                return;

            int keywordScore = BoneHasKeyword(match.bone, match.item.keywords);
            match.score += keywordScore;
            if (kDebug && keywordScore != 0)
                match.debugTracker.Add(keywordScore + ": " + GetMatchString(match) + " matched keywords");

            // If child bone with no required keywords, give a minimal score so it can still be included in mapping
            if (match.item.keywords.Length == 0 && match.item.alwaysInclude)
            {
                match.score += 1;
                if (kDebug)
                    match.debugTracker.Add(1 + ": " + GetMatchString(match) + " always-include point");
            }
        }
    }
}
