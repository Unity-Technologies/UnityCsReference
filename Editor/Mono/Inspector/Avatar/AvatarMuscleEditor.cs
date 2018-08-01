// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;

namespace UnityEditor
{
    [System.Serializable]
    internal class AvatarMuscleEditor : AvatarSubEditor
    {
        class Styles
        {
            public GUIContent[] muscleBodyGroup =
            {
                EditorGUIUtility.TrTextContent("Body"),
                EditorGUIUtility.TrTextContent("Head"),
                EditorGUIUtility.TrTextContent("Left Arm"),
                EditorGUIUtility.TrTextContent("Left Fingers"),
                EditorGUIUtility.TrTextContent("Right Arm"),
                EditorGUIUtility.TrTextContent("Right Fingers"),
                EditorGUIUtility.TrTextContent("Left Leg"),
                EditorGUIUtility.TrTextContent("Right Leg")
            };

            public GUIContent[] muscleTypeGroup =
            {
                EditorGUIUtility.TrTextContent("Open Close"),
                EditorGUIUtility.TrTextContent("Left Right"),
                EditorGUIUtility.TrTextContent("Roll Left Right"),
                EditorGUIUtility.TrTextContent("In Out"),
                EditorGUIUtility.TrTextContent("Roll In Out"),
                EditorGUIUtility.TrTextContent("Finger Open Close"),
                EditorGUIUtility.TrTextContent("Finger In Out")
            };

            public GUIContent armTwist = EditorGUIUtility.TrTextContent("Upper Arm Twist");
            public GUIContent foreArmTwist = EditorGUIUtility.TrTextContent("Lower Arm Twist");
            public GUIContent upperLegTwist = EditorGUIUtility.TrTextContent("Upper Leg Twist");
            public GUIContent legTwist = EditorGUIUtility.TrTextContent("Lower Leg Twist");
            public GUIContent armStretch = EditorGUIUtility.TrTextContent("Arm Stretch");
            public GUIContent legStretch = EditorGUIUtility.TrTextContent("Leg Stretch");
            public GUIContent feetSpacing = EditorGUIUtility.TrTextContent("Feet Spacing");
            public GUIContent hasTranslationDoF = EditorGUIUtility.TrTextContent("Translation DoF");

            public GUIStyle box = "OL box noexpand";
            public GUIStyle title = "OL TITLE";

            public GUIStyle toolbar = "TE Toolbar";
            public GUIStyle toolbarDropDown = "TE ToolbarDropDown";

            public GUIContent muscle = EditorGUIUtility.TrTextContent("Muscles");
            public GUIContent resetMuscle = EditorGUIUtility.TrTextContent("Reset");
        }

        static Styles styles { get { if (s_Styles == null) s_Styles = new Styles(); return s_Styles; } }
        static Styles s_Styles;

        // This list containt the mecanim's musle id for each muscle group
        protected int[][] m_Muscles =
        {
            new int[] { (int)Dof.BodyDofStart + (int)BodyDof.SpineFrontBack,
                        (int)Dof.BodyDofStart + (int)BodyDof.SpineLeftRight,
                        (int)Dof.BodyDofStart + (int)BodyDof.SpineRollLeftRight,
                        (int)Dof.BodyDofStart + (int)BodyDof.ChestFrontBack,
                        (int)Dof.BodyDofStart + (int)BodyDof.ChestLeftRight,
                        (int)Dof.BodyDofStart + (int)BodyDof.ChestRollLeftRight,
                        (int)Dof.BodyDofStart + (int)BodyDof.UpperChestFrontBack,
                        (int)Dof.BodyDofStart + (int)BodyDof.UpperChestLeftRight,
                        (int)Dof.BodyDofStart + (int)BodyDof.UpperChestRollLeftRight},

            new int[] { (int)Dof.HeadDofStart + (int)HeadDof.NeckFrontBack,
                        (int)Dof.HeadDofStart + (int)HeadDof.NeckLeftRight,
                        (int)Dof.HeadDofStart + (int)HeadDof.NeckRollLeftRight,
                        (int)Dof.HeadDofStart + (int)HeadDof.HeadFrontBack,
                        (int)Dof.HeadDofStart + (int)HeadDof.HeadLeftRight,
                        (int)Dof.HeadDofStart + (int)HeadDof.HeadRollLeftRight,
                        (int)Dof.HeadDofStart + (int)HeadDof.LeftEyeDownUp,
                        (int)Dof.HeadDofStart + (int)HeadDof.LeftEyeInOut,
                        (int)Dof.HeadDofStart + (int)HeadDof.RightEyeDownUp,
                        (int)Dof.HeadDofStart + (int)HeadDof.RightEyeInOut,
                        (int)Dof.HeadDofStart + (int)HeadDof.JawDownUp,
                        (int)Dof.HeadDofStart + (int)HeadDof.JawLeftRight},

            new int[] { (int)Dof.LeftArmDofStart + (int)ArmDof.ShoulderDownUp,
                        (int)Dof.LeftArmDofStart + (int)ArmDof.ShoulderFrontBack,
                        (int)Dof.LeftArmDofStart + (int)ArmDof.ArmDownUp,
                        (int)Dof.LeftArmDofStart + (int)ArmDof.ArmFrontBack,
                        (int)Dof.LeftArmDofStart + (int)ArmDof.ArmRollInOut,
                        (int)Dof.LeftArmDofStart + (int)ArmDof.ForeArmCloseOpen,
                        (int)Dof.LeftArmDofStart + (int)ArmDof.ForeArmRollInOut,
                        (int)Dof.LeftArmDofStart + (int)ArmDof.HandDownUp,
                        (int)Dof.LeftArmDofStart + (int)ArmDof.HandInOut},

            new int[]
            {
                (int)Dof.LeftThumbDofStart + (int)FingerDof.ProximalDownUp,
                (int)Dof.LeftThumbDofStart + (int)FingerDof.ProximalInOut,
                (int)Dof.LeftThumbDofStart + (int)FingerDof.IntermediateCloseOpen,
                (int)Dof.LeftThumbDofStart + (int)FingerDof.DistalCloseOpen,

                (int)Dof.LeftIndexDofStart + (int)FingerDof.ProximalDownUp,
                (int)Dof.LeftIndexDofStart + (int)FingerDof.ProximalInOut,
                (int)Dof.LeftIndexDofStart + (int)FingerDof.IntermediateCloseOpen,
                (int)Dof.LeftIndexDofStart + (int)FingerDof.DistalCloseOpen,

                (int)Dof.LeftMiddleDofStart + (int)FingerDof.ProximalDownUp,
                (int)Dof.LeftMiddleDofStart + (int)FingerDof.ProximalInOut,
                (int)Dof.LeftMiddleDofStart + (int)FingerDof.IntermediateCloseOpen,
                (int)Dof.LeftMiddleDofStart + (int)FingerDof.DistalCloseOpen,

                (int)Dof.LeftRingDofStart + (int)FingerDof.ProximalDownUp,
                (int)Dof.LeftRingDofStart + (int)FingerDof.ProximalInOut,
                (int)Dof.LeftRingDofStart + (int)FingerDof.IntermediateCloseOpen,
                (int)Dof.LeftRingDofStart + (int)FingerDof.DistalCloseOpen,

                (int)Dof.LeftLittleDofStart + (int)FingerDof.ProximalDownUp,
                (int)Dof.LeftLittleDofStart + (int)FingerDof.ProximalInOut,
                (int)Dof.LeftLittleDofStart + (int)FingerDof.IntermediateCloseOpen,
                (int)Dof.LeftLittleDofStart + (int)FingerDof.DistalCloseOpen
            },

            new int[] { (int)Dof.RightArmDofStart + (int)ArmDof.ShoulderDownUp,
                        (int)Dof.RightArmDofStart + (int)ArmDof.ShoulderFrontBack,
                        (int)Dof.RightArmDofStart + (int)ArmDof.ArmDownUp,
                        (int)Dof.RightArmDofStart + (int)ArmDof.ArmFrontBack,
                        (int)Dof.RightArmDofStart + (int)ArmDof.ArmRollInOut,
                        (int)Dof.RightArmDofStart + (int)ArmDof.ForeArmCloseOpen,
                        (int)Dof.RightArmDofStart + (int)ArmDof.ForeArmRollInOut,
                        (int)Dof.RightArmDofStart + (int)ArmDof.HandDownUp,
                        (int)Dof.RightArmDofStart + (int)ArmDof.HandInOut},

            new int[]
            {
                (int)Dof.RightThumbDofStart + (int)FingerDof.ProximalDownUp,
                (int)Dof.RightThumbDofStart + (int)FingerDof.ProximalInOut,
                (int)Dof.RightThumbDofStart + (int)FingerDof.IntermediateCloseOpen,
                (int)Dof.RightThumbDofStart + (int)FingerDof.DistalCloseOpen,

                (int)Dof.RightIndexDofStart + (int)FingerDof.ProximalDownUp,
                (int)Dof.RightIndexDofStart + (int)FingerDof.ProximalInOut,
                (int)Dof.RightIndexDofStart + (int)FingerDof.IntermediateCloseOpen,
                (int)Dof.RightIndexDofStart + (int)FingerDof.DistalCloseOpen,

                (int)Dof.RightMiddleDofStart + (int)FingerDof.ProximalDownUp,
                (int)Dof.RightMiddleDofStart + (int)FingerDof.ProximalInOut,
                (int)Dof.RightMiddleDofStart + (int)FingerDof.IntermediateCloseOpen,
                (int)Dof.RightMiddleDofStart + (int)FingerDof.DistalCloseOpen,

                (int)Dof.RightRingDofStart + (int)FingerDof.ProximalDownUp,
                (int)Dof.RightRingDofStart + (int)FingerDof.ProximalInOut,
                (int)Dof.RightRingDofStart + (int)FingerDof.IntermediateCloseOpen,
                (int)Dof.RightRingDofStart + (int)FingerDof.DistalCloseOpen,

                (int)Dof.RightLittleDofStart + (int)FingerDof.ProximalDownUp,
                (int)Dof.RightLittleDofStart + (int)FingerDof.ProximalInOut,
                (int)Dof.RightLittleDofStart + (int)FingerDof.IntermediateCloseOpen,
                (int)Dof.RightLittleDofStart + (int)FingerDof.DistalCloseOpen
            },

            new int[]
            {
                (int)Dof.LeftLegDofStart + (int)LegDof.UpperLegFrontBack,
                (int)Dof.LeftLegDofStart + (int)LegDof.UpperLegInOut,
                (int)Dof.LeftLegDofStart + (int)LegDof.UpperLegRollInOut,
                (int)Dof.LeftLegDofStart + (int)LegDof.LegCloseOpen,
                (int)Dof.LeftLegDofStart + (int)LegDof.LegRollInOut,
                (int)Dof.LeftLegDofStart + (int)LegDof.FootCloseOpen,
                (int)Dof.LeftLegDofStart + (int)LegDof.FootInOut,
                (int)Dof.LeftLegDofStart + (int)LegDof.ToesUpDown,
            },

            new int[]
            {
                (int)Dof.RightLegDofStart + (int)LegDof.UpperLegFrontBack,
                (int)Dof.RightLegDofStart + (int)LegDof.UpperLegInOut,
                (int)Dof.RightLegDofStart + (int)LegDof.UpperLegRollInOut,
                (int)Dof.RightLegDofStart + (int)LegDof.LegCloseOpen,
                (int)Dof.RightLegDofStart + (int)LegDof.LegRollInOut,
                (int)Dof.RightLegDofStart + (int)LegDof.FootCloseOpen,
                (int)Dof.RightLegDofStart + (int)LegDof.FootInOut,
                (int)Dof.RightLegDofStart + (int)LegDof.ToesUpDown,
            }
        };

        protected int[][] m_MasterMuscle =
        {
            // Body open close
            new int[]
            {
                (int)Dof.BodyDofStart + (int)BodyDof.SpineFrontBack,
                (int)Dof.BodyDofStart + (int)BodyDof.ChestFrontBack,
                (int)Dof.BodyDofStart + (int)BodyDof.UpperChestFrontBack,
                (int)Dof.HeadDofStart + (int)HeadDof.NeckFrontBack,
                (int)Dof.HeadDofStart + (int)HeadDof.HeadFrontBack,

                (int)Dof.LeftLegDofStart + (int)LegDof.UpperLegFrontBack,
                (int)Dof.LeftLegDofStart + (int)LegDof.LegCloseOpen,
                (int)Dof.LeftLegDofStart + (int)LegDof.FootCloseOpen,
                (int)Dof.RightLegDofStart + (int)LegDof.UpperLegFrontBack,
                (int)Dof.RightLegDofStart + (int)LegDof.LegCloseOpen,
                (int)Dof.RightLegDofStart + (int)LegDof.FootCloseOpen,

                (int)Dof.LeftArmDofStart + (int)ArmDof.ShoulderDownUp,
                (int)Dof.LeftArmDofStart + (int)ArmDof.ArmDownUp,
                (int)Dof.LeftArmDofStart + (int)ArmDof.ForeArmCloseOpen,
                (int)Dof.LeftArmDofStart + (int)ArmDof.HandDownUp,

                (int)Dof.RightArmDofStart + (int)ArmDof.ShoulderDownUp,
                (int)Dof.RightArmDofStart + (int)ArmDof.ArmDownUp,
                (int)Dof.RightArmDofStart + (int)ArmDof.ForeArmCloseOpen,
                (int)Dof.RightArmDofStart + (int)ArmDof.HandDownUp
            },

            // Body Left Right
            new int[]
            {
                (int)Dof.BodyDofStart + (int)BodyDof.SpineLeftRight,
                (int)Dof.BodyDofStart + (int)BodyDof.ChestLeftRight,
                (int)Dof.BodyDofStart + (int)BodyDof.UpperChestLeftRight,
                (int)Dof.HeadDofStart + (int)HeadDof.NeckLeftRight,
                (int)Dof.HeadDofStart + (int)HeadDof.HeadLeftRight,
            },

            // Roll Left Right
            new int[]
            {
                (int)Dof.BodyDofStart + (int)BodyDof.SpineRollLeftRight,
                (int)Dof.BodyDofStart + (int)BodyDof.ChestRollLeftRight,
                (int)Dof.BodyDofStart + (int)BodyDof.UpperChestRollLeftRight,
                (int)Dof.HeadDofStart + (int)HeadDof.NeckRollLeftRight,
                (int)Dof.HeadDofStart + (int)HeadDof.HeadRollLeftRight,
            },

            // In Out
            new int[]
            {
                (int)Dof.LeftLegDofStart + (int)LegDof.UpperLegInOut,
                (int)Dof.LeftLegDofStart + (int)LegDof.FootInOut,
                (int)Dof.RightLegDofStart + (int)LegDof.UpperLegInOut,
                (int)Dof.RightLegDofStart + (int)LegDof.FootInOut,
                (int)Dof.LeftArmDofStart + (int)ArmDof.ShoulderFrontBack,
                (int)Dof.LeftArmDofStart + (int)ArmDof.ArmFrontBack,
                (int)Dof.LeftArmDofStart + (int)ArmDof.HandInOut,
                (int)Dof.RightArmDofStart + (int)ArmDof.ShoulderFrontBack,
                (int)Dof.RightArmDofStart + (int)ArmDof.ArmFrontBack,
                (int)Dof.RightArmDofStart + (int)ArmDof.HandInOut
            },

            // Roll In Out
            new int[]
            {
                (int)Dof.LeftLegDofStart + (int)LegDof.UpperLegRollInOut,
                (int)Dof.LeftLegDofStart + (int)LegDof.LegRollInOut,
                (int)Dof.RightLegDofStart + (int)LegDof.UpperLegRollInOut,
                (int)Dof.RightLegDofStart + (int)LegDof.LegRollInOut,
                (int)Dof.LeftArmDofStart + (int)ArmDof.ArmRollInOut,
                (int)Dof.LeftArmDofStart + (int)ArmDof.ForeArmRollInOut,
                (int)Dof.RightArmDofStart + (int)ArmDof.ArmRollInOut,
                (int)Dof.RightArmDofStart + (int)ArmDof.ForeArmRollInOut
            },

            // Finger open close
            new int[]
            {
                (int)Dof.LeftThumbDofStart + (int)FingerDof.ProximalDownUp,
                (int)Dof.LeftThumbDofStart + (int)FingerDof.IntermediateCloseOpen,
                (int)Dof.LeftThumbDofStart + (int)FingerDof.DistalCloseOpen,

                (int)Dof.LeftIndexDofStart + (int)FingerDof.ProximalDownUp,
                (int)Dof.LeftIndexDofStart + (int)FingerDof.IntermediateCloseOpen,
                (int)Dof.LeftIndexDofStart + (int)FingerDof.DistalCloseOpen,

                (int)Dof.LeftMiddleDofStart + (int)FingerDof.ProximalDownUp,
                (int)Dof.LeftMiddleDofStart + (int)FingerDof.IntermediateCloseOpen,
                (int)Dof.LeftMiddleDofStart + (int)FingerDof.DistalCloseOpen,

                (int)Dof.LeftRingDofStart + (int)FingerDof.ProximalDownUp,
                (int)Dof.LeftRingDofStart + (int)FingerDof.IntermediateCloseOpen,
                (int)Dof.LeftRingDofStart + (int)FingerDof.DistalCloseOpen,

                (int)Dof.LeftLittleDofStart + (int)FingerDof.ProximalDownUp,
                (int)Dof.LeftLittleDofStart + (int)FingerDof.IntermediateCloseOpen,
                (int)Dof.LeftLittleDofStart + (int)FingerDof.DistalCloseOpen,

                (int)Dof.RightThumbDofStart + (int)FingerDof.ProximalDownUp,
                (int)Dof.RightThumbDofStart + (int)FingerDof.IntermediateCloseOpen,
                (int)Dof.RightThumbDofStart + (int)FingerDof.DistalCloseOpen,

                (int)Dof.RightIndexDofStart + (int)FingerDof.ProximalDownUp,
                (int)Dof.RightIndexDofStart + (int)FingerDof.IntermediateCloseOpen,
                (int)Dof.RightIndexDofStart + (int)FingerDof.DistalCloseOpen,

                (int)Dof.RightMiddleDofStart + (int)FingerDof.ProximalDownUp,
                (int)Dof.RightMiddleDofStart + (int)FingerDof.IntermediateCloseOpen,
                (int)Dof.RightMiddleDofStart + (int)FingerDof.DistalCloseOpen,

                (int)Dof.RightRingDofStart + (int)FingerDof.ProximalDownUp,
                (int)Dof.RightRingDofStart + (int)FingerDof.IntermediateCloseOpen,
                (int)Dof.RightRingDofStart + (int)FingerDof.DistalCloseOpen,

                (int)Dof.RightLittleDofStart + (int)FingerDof.ProximalDownUp,
                (int)Dof.RightLittleDofStart + (int)FingerDof.IntermediateCloseOpen,
                (int)Dof.RightLittleDofStart + (int)FingerDof.DistalCloseOpen
            },

            // Finger In Out
            new int[]
            {
                (int)Dof.LeftThumbDofStart + (int)FingerDof.ProximalInOut,
                (int)Dof.LeftIndexDofStart + (int)FingerDof.ProximalInOut,
                (int)Dof.LeftMiddleDofStart + (int)FingerDof.ProximalInOut,
                (int)Dof.LeftRingDofStart + (int)FingerDof.ProximalInOut,
                (int)Dof.LeftLittleDofStart + (int)FingerDof.ProximalInOut,
                (int)Dof.RightThumbDofStart + (int)FingerDof.ProximalInOut,
                (int)Dof.RightIndexDofStart + (int)FingerDof.ProximalInOut,
                (int)Dof.RightMiddleDofStart + (int)FingerDof.ProximalInOut,
                (int)Dof.RightRingDofStart + (int)FingerDof.ProximalInOut,
                (int)Dof.RightLittleDofStart + (int)FingerDof.ProximalInOut,
            }
        };


        bool[] m_MuscleBodyGroupToggle;
        bool[] m_MuscleToggle;

        int m_FocusedMuscle;

        [SerializeField]
        float[] m_MuscleValue = null;

        [SerializeField]
        float[] m_MuscleMasterValue = null;

        [SerializeField]
        protected float m_ArmTwistFactor;

        [SerializeField]
        protected float m_ForeArmTwistFactor;

        [SerializeField]
        protected float m_UpperLegTwistFactor;

        [SerializeField]
        protected float m_LegTwistFactor;

        [SerializeField]
        protected float m_ArmStretchFactor;

        [SerializeField]
        protected float m_LegStretchFactor;

        [SerializeField]
        protected float m_FeetSpacingFactor;

        [SerializeField]
        protected bool m_HasTranslationDoF;

        string[] m_MuscleName = null;
        int m_MuscleCount = 0;

        SerializedProperty[] m_MuscleMin = null;
        SerializedProperty[] m_MuscleMax = null;

        [SerializeField]
        float[] m_MuscleMinEdit = null;

        [SerializeField]
        float[] m_MuscleMaxEdit = null;

        SerializedProperty[] m_Modified = null;

        // These member are used when the avatar is part of an asset
        SerializedProperty m_ArmTwistProperty;
        SerializedProperty m_ForeArmTwistProperty;
        SerializedProperty m_UpperLegTwistProperty;
        SerializedProperty m_LegTwistProperty;

        SerializedProperty m_ArmStretchProperty;
        SerializedProperty m_LegStretchProperty;

        SerializedProperty m_FeetSpacingProperty;

        SerializedProperty m_HasTranslationDoFProperty;
        SerializedProperty m_HumanBoneArrayProperty;

        const string sMinX = "m_Limit.m_Min.x";
        const string sMinY = "m_Limit.m_Min.y";
        const string sMinZ = "m_Limit.m_Min.z";

        const string sMaxX = "m_Limit.m_Max.x";
        const string sMaxY = "m_Limit.m_Max.y";
        const string sMaxZ = "m_Limit.m_Max.z";
        const string sModified = "m_Limit.m_Modified";

        const string sArmTwist = "m_HumanDescription.m_ArmTwist";
        const string sForeArmTwist = "m_HumanDescription.m_ForeArmTwist";
        const string sUpperLegTwist = "m_HumanDescription.m_UpperLegTwist";
        const string sLegTwist = "m_HumanDescription.m_LegTwist";

        const string sArmStretch = "m_HumanDescription.m_ArmStretch";
        const string sLegStretch = "m_HumanDescription.m_LegStretch";

        const string sFeetSpacing = "m_HumanDescription.m_FeetSpacing";

        const string sHasTranslationDoF = "m_HumanDescription.m_HasTranslationDoF";

        const string sHumanBoneArray = "m_HumanDescription.m_Human";

        const float sMuscleMin = -180.0f;
        const float sMuscleMax = 180.0f;

        const float kPreviewWidth = 80;
        const float kNumberWidth = 38;
        const float kLineHeight = 16;

        private readonly Handles.BoneRenderer m_BoneRenderer = new Handles.BoneRenderer();

        static Rect GetSettingsRect(Rect wholeWidthRect)
        {
            wholeWidthRect.xMin += (kPreviewWidth + 3);
            wholeWidthRect.width -= 4;
            return wholeWidthRect;
        }

        static Rect GetSettingsRect()
        {
            return GetSettingsRect(GUILayoutUtility.GetRect(10, kLineHeight));
        }

        static Rect GetPreviewRect(Rect wholeWidthRect)
        {
            wholeWidthRect.width = kPreviewWidth - 9;
            wholeWidthRect.x += 5;
            wholeWidthRect.height = kLineHeight;
            return wholeWidthRect;
        }

        void HeaderGUI(string h1, string h2)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(h1, styles.title, GUILayout.Width(kPreviewWidth));
            GUILayout.Label(h2, styles.title, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
        }

        static float PreviewSlider(Rect position, float val)
        {
            val = GUI.HorizontalSlider(GetPreviewRect(position), val, -1, 1);
            if (val > -0.1f && val < 0.1f)
                val = 0;
            return val;
        }

        protected AvatarSetupTool.BoneWrapper[] m_Bones;

        internal void ResetValuesFromProperties()
        {
            m_ArmTwistFactor = m_ArmTwistProperty.floatValue;
            m_ForeArmTwistFactor = m_ForeArmTwistProperty.floatValue;
            m_UpperLegTwistFactor = m_UpperLegTwistProperty.floatValue;
            m_LegTwistFactor = m_LegTwistProperty.floatValue;
            m_ArmStretchFactor = m_ArmStretchProperty.floatValue;
            m_LegStretchFactor = m_LegStretchProperty.floatValue;
            m_FeetSpacingFactor = m_FeetSpacingProperty.floatValue;
            m_HasTranslationDoF = m_HasTranslationDoFProperty.boolValue;

            // limit is a special case, because they are added dynamicly by the editor
            // all the default value are wrong, we must explictly query mecanim to get the default value when
            // m_Modified is set to false.
            for (int i = 0; i < m_Bones.Length; i++)
            {
                if (m_Modified[i] != null)
                {
                    bool modified = m_Modified[i].boolValue;

                    int dx = HumanTrait.MuscleFromBone(i, 0);
                    int dy = HumanTrait.MuscleFromBone(i, 1);
                    int dz = HumanTrait.MuscleFromBone(i, 2);
                    if (dx != -1)
                    {
                        m_MuscleMinEdit[dx] = modified ? m_MuscleMin[dx].floatValue : HumanTrait.GetMuscleDefaultMin(dx);
                        m_MuscleMaxEdit[dx] = modified ? m_MuscleMax[dx].floatValue : HumanTrait.GetMuscleDefaultMax(dx);
                    }

                    if (dy != -1)
                    {
                        m_MuscleMinEdit[dy] = modified ? m_MuscleMin[dy].floatValue : HumanTrait.GetMuscleDefaultMin(dy);
                        m_MuscleMaxEdit[dy] = modified ? m_MuscleMax[dy].floatValue : HumanTrait.GetMuscleDefaultMax(dy);
                    }

                    if (dz != -1)
                    {
                        m_MuscleMinEdit[dz] = modified ? m_MuscleMin[dz].floatValue : HumanTrait.GetMuscleDefaultMin(dz);
                        m_MuscleMaxEdit[dz] = modified ? m_MuscleMax[dz].floatValue : HumanTrait.GetMuscleDefaultMax(dz);
                    }
                }
            }
        }

        internal void InitializeProperties()
        {
            m_ArmTwistProperty = serializedObject.FindProperty(sArmTwist);
            m_ForeArmTwistProperty = serializedObject.FindProperty(sForeArmTwist);
            m_UpperLegTwistProperty = serializedObject.FindProperty(sUpperLegTwist);
            m_LegTwistProperty = serializedObject.FindProperty(sLegTwist);
            m_ArmStretchProperty = serializedObject.FindProperty(sArmStretch);
            m_LegStretchProperty = serializedObject.FindProperty(sLegStretch);
            m_FeetSpacingProperty = serializedObject.FindProperty(sFeetSpacing);
            m_HasTranslationDoFProperty = serializedObject.FindProperty(sHasTranslationDoF);
            m_HumanBoneArrayProperty = serializedObject.FindProperty(sHumanBoneArray);

            for (int i = 0; i < m_Bones.Length; i++)
            {
                SerializedProperty bone = m_Bones[i].GetSerializedProperty(m_HumanBoneArrayProperty, false);
                if (bone != null)
                {
                    m_Modified[i] = bone.FindPropertyRelative(sModified);

                    int dx = HumanTrait.MuscleFromBone(i, 0);
                    int dy = HumanTrait.MuscleFromBone(i, 1);
                    int dz = HumanTrait.MuscleFromBone(i, 2);


                    if (dx != -1)
                    {
                        m_MuscleMin[dx] = bone.FindPropertyRelative(sMinX);
                        m_MuscleMax[dx] = bone.FindPropertyRelative(sMaxX);
                    }

                    if (dy != -1)
                    {
                        m_MuscleMin[dy] = bone.FindPropertyRelative(sMinY);
                        m_MuscleMax[dy] = bone.FindPropertyRelative(sMaxY);
                    }

                    if (dz != -1)
                    {
                        m_MuscleMin[dz] = bone.FindPropertyRelative(sMinZ);
                        m_MuscleMax[dz] = bone.FindPropertyRelative(sMaxZ);
                    }
                }
            }
        }

        internal void Initialize()
        {
            // Handle human bones
            if (m_Bones == null)
                m_Bones = AvatarSetupTool.GetHumanBones(m_HumanBoneArrayProperty, modelBones);

            m_FocusedMuscle = -1;

            m_MuscleBodyGroupToggle = new bool[m_Muscles.Length];
            for (int i = 0; i < m_Muscles.Length; i++)
            {
                m_MuscleBodyGroupToggle[i] = false;
            }

            m_MuscleName = HumanTrait.MuscleName;
            for (int i = 0; i < m_MuscleName.Length; i++)
            {
                if (m_MuscleName[i].StartsWith("Right"))
                    m_MuscleName[i] = m_MuscleName[i].Substring(5).Trim();
                if (m_MuscleName[i].StartsWith("Left"))
                    m_MuscleName[i] = m_MuscleName[i].Substring(4).Trim();
            }
            m_MuscleCount = HumanTrait.MuscleCount;

            m_MuscleToggle = new bool[m_MuscleCount];
            m_MuscleValue = new float[m_MuscleCount];
            m_MuscleMin = new SerializedProperty[m_MuscleCount];
            m_MuscleMax = new SerializedProperty[m_MuscleCount];

            m_MuscleMinEdit = new float[m_MuscleCount];
            m_MuscleMaxEdit = new float[m_MuscleCount];

            for (int i = 0; i < m_MuscleCount; i++)
            {
                m_MuscleToggle[i] = false;
                m_MuscleValue[i] = 0;
                m_MuscleMin[i] = null;
                m_MuscleMin[i] = null;
            }

            m_Modified = new SerializedProperty[m_Bones.Length];
            for (int i = 0; i < m_Bones.Length; i++)
            {
                m_Modified[i] = null;
            }

            InitializeProperties();
            ResetValuesFromProperties();

            m_MuscleMasterValue = new float[m_MasterMuscle.Length];
            for (int i = 0; i < m_MasterMuscle.Length; i++)
            {
                m_MuscleMasterValue[i] = 0;
            }
        }

        public override void Enable(AvatarEditor inspector)
        {
            base.Enable(inspector);

            Initialize();

            WritePose();
        }

        public override void OnInspectorGUI()
        {
            if (Event.current.type == EventType.ValidateCommand && Event.current.commandName == EventCommandNames.UndoRedoPerformed)
            {
                WritePose();
            }

            if (avatarAsset == null)
            {
                return;
            }

            using (new EditorGUI.DisabledScope(!avatarAsset.isHuman))
            {
                EditorGUIUtility.labelWidth = 110;
                EditorGUIUtility.fieldWidth = 40;

                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical();

                MuscleGroupGUI();
                EditorGUILayout.Space();
                MuscleGUI();
                EditorGUILayout.Space();
                PropertiesGUI();

                GUILayout.EndVertical();
                GUILayout.Space(1);
                GUILayout.EndHorizontal();

                DisplayMuscleButtons();

                ApplyRevertGUI();
            }
        }

        protected void DisplayMuscleButtons()
        {
            GUILayout.BeginHorizontal("", styles.toolbar, GUILayout.ExpandWidth(true));
            {
                Rect r;

                // Muscle
                r = GUILayoutUtility.GetRect(styles.muscle, styles.toolbarDropDown);
                if (GUI.Button(r, styles.muscle, styles.toolbarDropDown))
                {
                    GenericMenu menu = new GenericMenu();
                    menu.AddItem(styles.resetMuscle, false, ResetMuscleToDefault);
                    menu.DropDown(r);
                }

                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();
        }

        protected override void ResetValues()
        {
            serializedObject.Update();
            ResetValuesFromProperties();
        }

        protected void ResetMuscleToDefault()
        {
            Avatar avatar = null;
            // For live update, update instanciate avatar to adjust pose
            if (gameObject != null)
            {
                Animator animator = gameObject.GetComponent(typeof(Animator)) as Animator;
                avatar = animator.avatar;
            }

            for (int i = 0; i < m_MuscleCount; i++)
            {
                float min = HumanTrait.GetMuscleDefaultMin(i);
                float max = HumanTrait.GetMuscleDefaultMax(i);

                if (m_MuscleMin[i] != null && m_MuscleMax[i] != null)
                {
                    m_MuscleMin[i].floatValue = m_MuscleMinEdit[i] = min;
                    m_MuscleMax[i].floatValue = m_MuscleMaxEdit[i] = max;
                }

                int humanId = HumanTrait.BoneFromMuscle(i);
                if (m_Modified[humanId] != null && humanId != -1)
                    m_Modified[humanId].boolValue = false;

                if (avatar != null)
                    avatar.SetMuscleMinMax(i, min, max);
            }

            WritePose();
        }

        protected void UpdateAvatarParameter(HumanParameter parameterId, float value)
        {
            // For live update, update instanciate avatar to adjust pose
            if (gameObject != null)
            {
                Animator animator = gameObject.GetComponent(typeof(Animator)) as Animator;
                Avatar avatar = animator.avatar;
                avatar.SetParameter((int)parameterId, value);
            }
        }

        protected bool UpdateMuscle(int muscleId, float min, float max)
        {
            Undo.RegisterCompleteObjectUndo(this, "Updated muscle range");
            m_MuscleMin[muscleId].floatValue = min;
            m_MuscleMax[muscleId].floatValue = max;

            int humanId = HumanTrait.BoneFromMuscle(muscleId);
            if (humanId != -1)
            {
                if (!m_Modified[humanId].boolValue)
                {
                    int mx = HumanTrait.MuscleFromBone(humanId, 0);
                    int my = HumanTrait.MuscleFromBone(humanId, 1);
                    int mz = HumanTrait.MuscleFromBone(humanId, 2);

                    if (mx != -1 && mx != muscleId)
                    {
                        m_MuscleMin[mx].floatValue = HumanTrait.GetMuscleDefaultMin(mx);
                        m_MuscleMax[mx].floatValue = HumanTrait.GetMuscleDefaultMax(mx);
                    }

                    if (my != -1 && my != muscleId)
                    {
                        m_MuscleMin[my].floatValue = HumanTrait.GetMuscleDefaultMin(my);
                        m_MuscleMax[my].floatValue = HumanTrait.GetMuscleDefaultMax(my);
                    }

                    if (mz != -1 && mz != muscleId)
                    {
                        m_MuscleMin[mz].floatValue = HumanTrait.GetMuscleDefaultMin(mz);
                        m_MuscleMax[mz].floatValue = HumanTrait.GetMuscleDefaultMax(mz);
                    }
                }

                m_Modified[humanId].boolValue = true;
            }

            // OnSceneGUI need focused muscle to know which one to draw
            m_FocusedMuscle = muscleId;

            // For live update, update instanciate avatar to adjust pose
            if (gameObject != null)
            {
                Animator animator = gameObject.GetComponent(typeof(Animator)) as Animator;
                Avatar avatar = animator.avatar;
                avatar.SetMuscleMinMax(muscleId, min, max);
            }

            // Need to repaint scene to update muscle range handle
            SceneView.RepaintAll();

            return gameObject != null;
        }

        protected void MuscleGroupGUI()
        {
            bool recomputePose = false;

            HeaderGUI("Preview", "Muscle Group Preview");
            GUILayout.BeginVertical(styles.box);
            {
                {
                    Rect r = GUILayoutUtility.GetRect(10, kLineHeight);
                    Rect settingsRect = GetSettingsRect(r);
                    Rect previewRect = GetPreviewRect(r);
                    if (GUI.Button(previewRect, "Reset All", EditorStyles.miniButton))
                    {
                        for (int i = 0; i < m_MuscleMasterValue.Length; i++)
                            m_MuscleMasterValue[i] = 0;
                        for (int i = 0; i < m_MuscleValue.Length; i++)
                            m_MuscleValue[i] = 0;
                        recomputePose = true;
                    }

                    GUI.Label(settingsRect, "Reset All Preview Values", EditorStyles.label);
                }

                for (int i = 0; i < m_MasterMuscle.Length; i++)
                {
                    Rect r = GUILayoutUtility.GetRect(10, kLineHeight);
                    Rect settingsRect = GetSettingsRect(r);

                    float oldValue = m_MuscleMasterValue[i];
                    m_MuscleMasterValue[i] = PreviewSlider(r, m_MuscleMasterValue[i]);
                    if (m_MuscleMasterValue[i] != oldValue)
                    {
                        Undo.RegisterCompleteObjectUndo(this, "Muscle preview");
                        for (int j = 0; j < m_MasterMuscle[i].Length; j++)
                        {
                            if (m_MasterMuscle[i][j] != -1)
                            {
                                m_MuscleValue[m_MasterMuscle[i][j]] = m_MuscleMasterValue[i];
                            }
                        }
                    }
                    // Muscle value changed and we do have a game object to update
                    recomputePose |= m_MuscleMasterValue[i] != oldValue && gameObject != null;

                    GUI.Label(settingsRect, styles.muscleTypeGroup[i], EditorStyles.label);
                }
            }
            GUILayout.EndVertical();

            if (recomputePose)
                WritePose();
        }

        protected void MuscleGUI()
        {
            bool recomputePose = false;

            HeaderGUI("Preview", "Per-Muscle Settings");
            GUILayout.BeginVertical(styles.box);
            {
                Rect r, settingsRect;
                const int indentPerLevel = 15;
                for (int i = 0; i < m_MuscleBodyGroupToggle.Length; i++)
                {
                    r = GUILayoutUtility.GetRect(10, kLineHeight);
                    settingsRect = GetSettingsRect(r);
                    m_MuscleBodyGroupToggle[i] = GUI.Toggle(settingsRect, m_MuscleBodyGroupToggle[i], styles.muscleBodyGroup[i], EditorStyles.foldout);
                    if (m_MuscleBodyGroupToggle[i])
                    {
                        for (int j = 0; j < m_Muscles[i].Length; j++)
                        {
                            int muscleId = m_Muscles[i][j];
                            // Some muscle can be optionnal like Toes, if this bone is not characterized you can't edit this muscle
                            if (muscleId != -1 && m_MuscleMin[muscleId] != null && m_MuscleMax[muscleId] != null)
                            {
                                bool expanded = m_MuscleToggle[muscleId];

                                r = GUILayoutUtility.GetRect(10, expanded ? kLineHeight * 2 : kLineHeight);
                                settingsRect = GetSettingsRect(r);
                                settingsRect.xMin += indentPerLevel;

                                // Foldout
                                settingsRect.height = kLineHeight;
                                m_MuscleToggle[muscleId] = GUI.Toggle(settingsRect, m_MuscleToggle[muscleId], m_MuscleName[muscleId], EditorStyles.foldout);

                                // Preview slider
                                float value = PreviewSlider(r, m_MuscleValue[muscleId]);
                                // OnSceneGUI need focused muscle to know which one to draw
                                if (m_MuscleValue[muscleId] != value)
                                {
                                    Undo.RegisterCompleteObjectUndo(this, "Muscle preview");
                                    m_FocusedMuscle = muscleId;
                                    m_MuscleValue[muscleId] = value;
                                    recomputePose |= (gameObject != null);
                                }

                                if (expanded)
                                {
                                    bool muscleChanged = false;

                                    settingsRect.xMin += indentPerLevel;
                                    settingsRect.y += kLineHeight;

                                    Rect sliderRect = settingsRect;

                                    if (settingsRect.width > 160)
                                    {
                                        Rect numberRect = settingsRect;
                                        numberRect.width = kNumberWidth;

                                        EditorGUI.BeginChangeCheck();
                                        m_MuscleMinEdit[muscleId] = EditorGUI.FloatField(numberRect, m_MuscleMinEdit[muscleId]);
                                        muscleChanged |= EditorGUI.EndChangeCheck();

                                        numberRect.x = settingsRect.xMax - kNumberWidth;

                                        EditorGUI.BeginChangeCheck();
                                        m_MuscleMaxEdit[muscleId] = EditorGUI.FloatField(numberRect, m_MuscleMaxEdit[muscleId]);
                                        muscleChanged |= EditorGUI.EndChangeCheck();

                                        sliderRect.xMin += (kNumberWidth + 5);
                                        sliderRect.xMax -= (kNumberWidth + 5);
                                    }

                                    EditorGUI.BeginChangeCheck();
                                    EditorGUI.MinMaxSlider(sliderRect, ref m_MuscleMinEdit[muscleId], ref m_MuscleMaxEdit[muscleId], sMuscleMin, sMuscleMax);
                                    muscleChanged |= EditorGUI.EndChangeCheck();

                                    if (muscleChanged)
                                    {
                                        m_MuscleMinEdit[muscleId] = Mathf.Clamp(m_MuscleMinEdit[muscleId], sMuscleMin, 0);
                                        m_MuscleMaxEdit[muscleId] = Mathf.Clamp(m_MuscleMaxEdit[muscleId], 0, sMuscleMax);
                                        recomputePose |= UpdateMuscle(muscleId, m_MuscleMinEdit[muscleId], m_MuscleMaxEdit[muscleId]);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            GUILayout.EndVertical();

            if (recomputePose)
                WritePose();
        }

        protected void PropertiesGUI()
        {
            bool recomputePose = false;

            HeaderGUI("", "Additional Settings");
            GUILayout.BeginVertical(styles.box);
            {
                m_ArmTwistFactor = EditorGUI.Slider(GetSettingsRect(), styles.armTwist, m_ArmTwistFactor, 0, 1);
                if (m_ArmTwistProperty.floatValue != m_ArmTwistFactor)
                {
                    Undo.RegisterCompleteObjectUndo(this, "Upper arm twist");
                    m_ArmTwistProperty.floatValue = m_ArmTwistFactor;
                    UpdateAvatarParameter(HumanParameter.UpperArmTwist, m_ArmTwistFactor);
                    recomputePose = true;
                }

                m_ForeArmTwistFactor = EditorGUI.Slider(GetSettingsRect(), styles.foreArmTwist, m_ForeArmTwistFactor, 0, 1);
                if (m_ForeArmTwistProperty.floatValue != m_ForeArmTwistFactor)
                {
                    Undo.RegisterCompleteObjectUndo(this, "Lower arm twist");
                    m_ForeArmTwistProperty.floatValue = m_ForeArmTwistFactor;
                    UpdateAvatarParameter(HumanParameter.LowerArmTwist, m_ForeArmTwistFactor);
                    recomputePose = true;
                }

                m_UpperLegTwistFactor = EditorGUI.Slider(GetSettingsRect(), styles.upperLegTwist, m_UpperLegTwistFactor, 0, 1);
                if (m_UpperLegTwistProperty.floatValue != m_UpperLegTwistFactor)
                {
                    Undo.RegisterCompleteObjectUndo(this, "Upper leg twist");
                    m_UpperLegTwistProperty.floatValue = m_UpperLegTwistFactor;
                    UpdateAvatarParameter(HumanParameter.UpperLegTwist, m_UpperLegTwistFactor);
                    recomputePose = true;
                }

                m_LegTwistFactor = EditorGUI.Slider(GetSettingsRect(), styles.legTwist, m_LegTwistFactor, 0, 1);
                if (m_LegTwistProperty.floatValue != m_LegTwistFactor)
                {
                    Undo.RegisterCompleteObjectUndo(this, "Lower leg twist");
                    m_LegTwistProperty.floatValue = m_LegTwistFactor;
                    UpdateAvatarParameter(HumanParameter.LowerLegTwist, m_LegTwistFactor);
                    recomputePose = true;
                }

                m_ArmStretchFactor = EditorGUI.Slider(GetSettingsRect(), styles.armStretch, m_ArmStretchFactor, 0, 1);
                if (m_ArmStretchProperty.floatValue != m_ArmStretchFactor)
                {
                    Undo.RegisterCompleteObjectUndo(this, "Arm stretch");
                    m_ArmStretchProperty.floatValue = m_ArmStretchFactor;
                    UpdateAvatarParameter(HumanParameter.ArmStretch, m_ArmStretchFactor);
                    recomputePose = true;
                }

                m_LegStretchFactor = EditorGUI.Slider(GetSettingsRect(), styles.legStretch, m_LegStretchFactor, 0, 1);
                if (m_LegStretchProperty.floatValue != m_LegStretchFactor)
                {
                    Undo.RegisterCompleteObjectUndo(this, "Leg stretch");
                    m_LegStretchProperty.floatValue = m_LegStretchFactor;
                    UpdateAvatarParameter(HumanParameter.LegStretch, m_LegStretchFactor);
                    recomputePose = true;
                }

                m_FeetSpacingFactor = EditorGUI.Slider(GetSettingsRect(), styles.feetSpacing, m_FeetSpacingFactor, 0, 1);
                if (m_FeetSpacingProperty.floatValue != m_FeetSpacingFactor)
                {
                    Undo.RegisterCompleteObjectUndo(this, "Feet spacing");
                    m_FeetSpacingProperty.floatValue = m_FeetSpacingFactor;
                    UpdateAvatarParameter(HumanParameter.FeetSpacing, m_FeetSpacingFactor);
                    recomputePose = true;
                }

                m_HasTranslationDoF = EditorGUI.Toggle(GetSettingsRect(), styles.hasTranslationDoF, m_HasTranslationDoF);
                if (m_HasTranslationDoFProperty.boolValue != m_HasTranslationDoF)
                {
                    Undo.RegisterCompleteObjectUndo(this, "Translation DoF");
                    m_HasTranslationDoFProperty.boolValue = m_HasTranslationDoF;
                }
            }
            GUILayout.EndVertical();

            if (recomputePose)
                WritePose();
        }

        protected void WritePose()
        {
            if (gameObject)
            {
                Animator animator = gameObject.GetComponent(typeof(Animator)) as Animator;
                if (animator != null)
                {
                    Avatar avatar = animator.avatar;
                    if (avatar != null && avatar.isValid && avatar.isHuman)
                    {
                        AvatarUtility.SetHumanPose(animator, m_MuscleValue);
                        SceneView.RepaintAll();
                    }
                }
            }
        }

        public void DrawMuscleHandle(Transform t, int humanId)
        {
            Animator animator = gameObject.GetComponent(typeof(Animator)) as Animator;
            Avatar avatar = animator.avatar;

            int mx = HumanTrait.MuscleFromBone(humanId, 0);
            int my = HumanTrait.MuscleFromBone(humanId, 1);
            int mz = HumanTrait.MuscleFromBone(humanId, 2);

            float axisLen = avatar.GetAxisLength(humanId);
            Quaternion preQ = avatar.GetPreRotation(humanId);
            Quaternion postQ = avatar.GetPostRotation(humanId);

            preQ = t.parent.rotation * preQ;
            postQ = t.rotation * postQ;

            Vector3 normal;
            Vector3 from;

            Color alpha = new Color(1, 1, 1, 0.5f);
            Quaternion zyRoll = avatar.GetZYRoll(humanId, Vector3.zero);
            Vector3 sign = avatar.GetLimitSign(humanId);

            // Draw axis
            normal = postQ * Vector3.right;
            Vector3 axisEnd = t.position + (normal * axisLen);
            Handles.color = Color.white;
            Handles.DrawLine(t.position, axisEnd);

            if (mx != -1)
            {
                Quaternion zyPostQ = avatar.GetZYPostQ(humanId, t.parent.rotation, t.rotation);

                float minx = m_MuscleMinEdit[mx];
                float maxx = m_MuscleMaxEdit[mx];

                normal = postQ * Vector3.right;
                from = zyPostQ * Vector3.forward;

                Handles.color = Color.black;
                //Handles.DrawLine (t.position, t.position + (from * axisLen * 0.75f));

                Vector3 xDoF = t.position + (normal * axisLen * 0.75f);

                normal = postQ * Vector3.right * sign.x;
                Quaternion q = Quaternion.AngleAxis(minx, normal);
                from = q * from;

                Handles.color = Color.yellow;
                //Handles.DrawLine (t.position, t.position + (from * axisLen * 0.75f));

                // Draw Muscle range
                Handles.color = Handles.xAxisColor * alpha;
                Handles.DrawSolidArc(xDoF, normal, from, maxx - minx, axisLen * 0.25f);

                from = postQ * Vector3.forward;
                Handles.color = Handles.centerColor;
                Handles.DrawLine(xDoF, xDoF + (from * axisLen * 0.25f));
            }

            if (my != -1)
            {
                float miny = m_MuscleMinEdit[my];
                float maxy = m_MuscleMaxEdit[my];

                normal = preQ * Vector3.up * sign.y;
                from = preQ * zyRoll * Vector3.right;

                Handles.color = Color.black;
                //Handles.DrawLine (t.position, t.position + (from * axisLen * 0.75f));

                Quaternion q = Quaternion.AngleAxis(miny, normal);
                from = q * from;

                Handles.color = Color.yellow;
                //Handles.DrawLine (t.position, t.position + (from * axisLen * 0.75f));

                // Draw Muscle range
                Handles.color = Handles.yAxisColor * alpha;
                Handles.DrawSolidArc(t.position, normal, from, maxy - miny, axisLen * 0.25f);
            }
            if (mz != -1)
            {
                float minz = m_MuscleMinEdit[mz];
                float maxz = m_MuscleMaxEdit[mz];

                normal = preQ * Vector3.forward * sign.z;
                from = preQ * zyRoll * Vector3.right;

                Handles.color = Color.black;
                //Handles.DrawLine (t.position, t.position + (from * axisLen * 0.75f));

                Quaternion q = Quaternion.AngleAxis(minz, normal);
                from = q * from;

                Handles.color = Color.yellow;
                //Handles.DrawLine (t.position, t.position + (from * axisLen * 0.75f));

                // Draw Muscle range
                Handles.color = Handles.zAxisColor * alpha;
                Handles.DrawSolidArc(t.position, normal, from, maxz - minz, axisLen * 0.25f);
            }
        }

        public override void OnSceneGUI()
        {
            AvatarSkeletonDrawer.DrawSkeleton(root, modelBones, m_BoneRenderer);

            if (gameObject == null)
                return;

            Animator animator = gameObject.GetComponent(typeof(Animator)) as Animator;
            if (m_FocusedMuscle == -1 || animator == null)
                return;

            int humanId = HumanTrait.BoneFromMuscle(m_FocusedMuscle);
            if (humanId != -1)
            {
                DrawMuscleHandle(m_Bones[humanId].bone, humanId);
            }
        }
    }
}
