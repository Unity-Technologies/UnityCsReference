// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System;
using System.Collections.Generic;

namespace UnityEditor
{
    public enum BodyPart
    {
        None = -1,
        Avatar = 0,
        Body,
        Head,
        LeftArm,
        LeftFingers,
        RightArm,
        RightFingers,
        LeftLeg,
        RightLeg,
        Last
    }

    internal class AvatarControl
    {
        class Styles
        {
            // IF you change the order of this array, please update:
            // BodyPartMapping
            // m_BodyPartHumanBone
            // BodyPart

            public GUIContent[] Silhouettes =
            {
                EditorGUIUtility.IconContent("AvatarInspector/BodySilhouette"),
                EditorGUIUtility.IconContent("AvatarInspector/HeadZoomSilhouette"),
                EditorGUIUtility.IconContent("AvatarInspector/LeftHandZoomSilhouette"),
                EditorGUIUtility.IconContent("AvatarInspector/RightHandZoomSilhouette")
            };

            public GUIContent[,] BodyPart =
            {
                {
                    null,
                    EditorGUIUtility.IconContent("AvatarInspector/Torso"),
                    EditorGUIUtility.IconContent("AvatarInspector/Head"),
                    EditorGUIUtility.IconContent("AvatarInspector/LeftArm"),
                    EditorGUIUtility.IconContent("AvatarInspector/LeftFingers"),
                    EditorGUIUtility.IconContent("AvatarInspector/RightArm"),
                    EditorGUIUtility.IconContent("AvatarInspector/RightFingers"),
                    EditorGUIUtility.IconContent("AvatarInspector/LeftLeg"),
                    EditorGUIUtility.IconContent("AvatarInspector/RightLeg")
                },
                {
                    null,
                    null,
                    EditorGUIUtility.IconContent("AvatarInspector/HeadZoom"),
                    null,
                    null,
                    null,
                    null,
                    null,
                    null
                },
                {
                    null,
                    null,
                    null,
                    null,
                    EditorGUIUtility.IconContent("AvatarInspector/LeftHandZoom"),
                    null,
                    null,
                    null,
                    null
                },
                {
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    EditorGUIUtility.IconContent("AvatarInspector/RightHandZoom"),
                    null,
                    null
                },
            };
        }

        static Styles styles { get { if (s_Styles == null) s_Styles = new Styles(); return s_Styles; } }
        static Styles s_Styles;

        public enum BodyPartColor
        {
            Off = 0x00,
            Green = 0x01 << 0,
            Red = 0x01 << 1,
            IKGreen = 0x01 << 2,
            IKRed = 0x01 << 3,
        }

        public delegate BodyPartColor BodyPartFeedback(BodyPart bodyPart);

        static public int ShowBoneMapping(int shownBodyView, BodyPartFeedback bodyPartCallback, AvatarSetupTool.BoneWrapper[] bones, SerializedObject serializedObject, AvatarMappingEditor editor)
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();

                if (styles.Silhouettes[shownBodyView].image)
                {
                    Rect rect = GUILayoutUtility.GetRect(styles.Silhouettes[shownBodyView], GUIStyle.none, GUILayout.MaxWidth(styles.Silhouettes[shownBodyView].image.width));
                    DrawBodyParts(rect, shownBodyView, bodyPartCallback);

                    for (int i = 0; i < bones.Length; i++)
                        DrawBone(shownBodyView, i, rect, bones[i], serializedObject, editor);
                }
                else
                    GUILayout.Label("texture missing,\nfix me!");

                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();

            // Body view buttons
            Rect buttonsRect = GUILayoutUtility.GetLastRect();
            const float buttonHeight = 16;
            string[] labels = new string[] { "Body", "Head", "Left Hand", "Right Hand"};
            buttonsRect.x += 5;
            buttonsRect.width = 70;
            buttonsRect.yMin = buttonsRect.yMax - (buttonHeight * 4 + 5);
            buttonsRect.height = buttonHeight;
            for (int i = 0; i < labels.Length; i++)
            {
                if (GUI.Toggle(buttonsRect, shownBodyView == i, labels[i], EditorStyles.miniButton))
                    shownBodyView = i;
                buttonsRect.y += buttonHeight;
            }

            return shownBodyView;
        }

        static public void DrawBodyParts(Rect rect, int shownBodyView, BodyPartFeedback bodyPartCallback)
        {
            GUI.color = new Color(0.2f, 0.2f, 0.2f, 1.0f);
            if (styles.Silhouettes[shownBodyView] != null)
                GUI.DrawTexture(rect, styles.Silhouettes[shownBodyView].image);
            for (int i = 1; i < (int)BodyPart.Last; i++)
                DrawBodyPart(shownBodyView, i, rect, bodyPartCallback((BodyPart)i));
        }

        static protected void DrawBodyPart(int shownBodyView, int i, Rect rect, BodyPartColor bodyPartColor)
        {
            if (styles.BodyPart[shownBodyView, i] != null && styles.BodyPart[shownBodyView, i].image != null)
            {
                if ((bodyPartColor & BodyPartColor.Green) == BodyPartColor.Green)
                    GUI.color = Color.green;
                else if ((bodyPartColor & BodyPartColor.Red) == BodyPartColor.Red)
                    GUI.color = Color.red;
                else
                    GUI.color = Color.gray;
                GUI.DrawTexture(rect, styles.BodyPart[shownBodyView, i].image);
                GUI.color = Color.white;
            }
        }

        static Vector2[,] s_BonePositions = new Vector2[4, HumanTrait.BoneCount];

        public static List<int> GetViewsThatContainBone(int bone)
        {
            List<int> views = new List<int>();

            if (bone < 0 || bone >= HumanTrait.BoneCount)
                return views;

            for (int i = 0; i < 4; i++)
            {
                if (s_BonePositions[i, bone] != Vector2.zero)
                    views.Add(i);
            }
            return views;
        }

        static AvatarControl()
        {
            // Body view
            int view = 0;
            // hips
            s_BonePositions[view,  (int)HumanBodyBones.Hips] = new Vector2(0.00f, 0.08f);

            // upper leg
            s_BonePositions[view,  (int)HumanBodyBones.LeftUpperLeg] = new Vector2(0.16f, 0.01f);
            s_BonePositions[view,  (int)HumanBodyBones.RightUpperLeg] = new Vector2(-0.16f, 0.01f);

            // lower leg
            s_BonePositions[view,  (int)HumanBodyBones.LeftLowerLeg] = new Vector2(0.21f, -0.40f);
            s_BonePositions[view,  (int)HumanBodyBones.RightLowerLeg] = new Vector2(-0.21f, -0.40f);

            // foot
            s_BonePositions[view,  (int)HumanBodyBones.LeftFoot] = new Vector2(0.23f, -0.80f);
            s_BonePositions[view,  (int)HumanBodyBones.RightFoot] = new Vector2(-0.23f, -0.80f);

            // spine - head
            s_BonePositions[view,  (int)HumanBodyBones.Spine] = new Vector2(0.00f, 0.20f);
            s_BonePositions[view,  (int)HumanBodyBones.Chest] = new Vector2(0.00f, 0.35f);
            s_BonePositions[view,  (int)HumanBodyBones.UpperChest] = new Vector2(0.00f, 0.50f);
            s_BonePositions[view, (int)HumanBodyBones.Neck] = new Vector2(0.00f, 0.66f);
            s_BonePositions[view, (int)HumanBodyBones.Head] = new Vector2(0.00f, 0.76f);

            // shoulder
            s_BonePositions[view, (int)HumanBodyBones.LeftShoulder] = new Vector2(0.14f, 0.60f);
            s_BonePositions[view, (int)HumanBodyBones.RightShoulder] = new Vector2(-0.14f, 0.60f);

            // upper arm
            s_BonePositions[view, (int)HumanBodyBones.LeftUpperArm] = new Vector2(0.30f, 0.57f);
            s_BonePositions[view, (int)HumanBodyBones.RightUpperArm] = new Vector2(-0.30f, 0.57f);

            // lower arm
            s_BonePositions[view, (int)HumanBodyBones.LeftLowerArm] = new Vector2(0.48f, 0.30f);
            s_BonePositions[view, (int)HumanBodyBones.RightLowerArm] = new Vector2(-0.48f, 0.30f);

            // hand
            s_BonePositions[view, (int)HumanBodyBones.LeftHand] = new Vector2(0.66f, 0.03f);
            s_BonePositions[view, (int)HumanBodyBones.RightHand] = new Vector2(-0.66f, 0.03f);

            // toe
            s_BonePositions[view, (int)HumanBodyBones.LeftToes] = new Vector2(0.25f, -0.89f);
            s_BonePositions[view, (int)HumanBodyBones.RightToes] = new Vector2(-0.25f, -0.89f);

            // Head view
            view = 1;
            // neck - head
            s_BonePositions[view, (int)HumanBodyBones.Neck] = new Vector2(-0.20f, -0.62f);
            s_BonePositions[view, (int)HumanBodyBones.Head] = new Vector2(-0.15f, -0.30f);
            // left, right eye
            s_BonePositions[view, (int)HumanBodyBones.LeftEye] = new Vector2(0.63f, 0.16f);
            s_BonePositions[view, (int)HumanBodyBones.RightEye] = new Vector2(0.15f, 0.16f);
            // jaw
            s_BonePositions[view, (int)HumanBodyBones.Jaw] = new Vector2(0.45f, -0.40f);

            // Left hand view
            view = 2;
            // finger bases, thumb - little
            s_BonePositions[view, (int)HumanBodyBones.LeftThumbProximal] = new Vector2(-0.35f, 0.11f);
            s_BonePositions[view, (int)HumanBodyBones.LeftIndexProximal] = new Vector2(0.19f, 0.11f);
            s_BonePositions[view, (int)HumanBodyBones.LeftMiddleProximal] = new Vector2(0.22f, 0.00f);
            s_BonePositions[view, (int)HumanBodyBones.LeftRingProximal] = new Vector2(0.16f, -0.12f);
            s_BonePositions[view, (int)HumanBodyBones.LeftLittleProximal] = new Vector2(0.09f, -0.23f);

            // finger tips, thumb - little
            s_BonePositions[view, (int)HumanBodyBones.LeftThumbDistal] = new Vector2(-0.03f, 0.33f);
            s_BonePositions[view, (int)HumanBodyBones.LeftIndexDistal] = new Vector2(0.65f, 0.16f);
            s_BonePositions[view, (int)HumanBodyBones.LeftMiddleDistal] = new Vector2(0.74f, 0.00f);
            s_BonePositions[view, (int)HumanBodyBones.LeftRingDistal] = new Vector2(0.66f, -0.14f);
            s_BonePositions[view, (int)HumanBodyBones.LeftLittleDistal] = new Vector2(0.45f, -0.25f);

            // finger middles, thumb - little
            for (int i = 0; i < 5; i++)
                s_BonePositions[view, (int)HumanBodyBones.LeftThumbIntermediate + i * 3] = Vector2.Lerp(s_BonePositions[view, (int)HumanBodyBones.LeftThumbProximal + i * 3], s_BonePositions[view, (int)HumanBodyBones.LeftThumbDistal + i * 3], 0.58f);

            // Right hand view
            view = 3;
            for (int i = 0; i < 15; i++)
                s_BonePositions[view, (int)HumanBodyBones.LeftThumbProximal + i + 15] = Vector2.Scale(s_BonePositions[view - 1, (int)HumanBodyBones.LeftThumbProximal + i], new Vector2(-1, 1));
        }

        static protected void DrawBone(int shownBodyView, int i, Rect rect, AvatarSetupTool.BoneWrapper bone, SerializedObject serializedObject, AvatarMappingEditor editor)
        {
            if (s_BonePositions[shownBodyView, i] == Vector2.zero)
                return;

            Vector2 pos = s_BonePositions[shownBodyView, i];
            pos.y *= -1; // because higher values should be up
            pos.Scale(new Vector2(rect.width * 0.5f, rect.height * 0.5f));
            pos = rect.center + pos;
            int kIconSize = AvatarSetupTool.BoneWrapper.kIconSize;
            Rect r = new Rect(pos.x - kIconSize * 0.5f, pos.y - kIconSize * 0.5f, kIconSize, kIconSize);
            bone.BoneDotGUI(r, r, i, true, true, true, serializedObject, editor);
        }
    }
}
