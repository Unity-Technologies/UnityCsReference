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
    internal class AvatarSkeletonDrawer
    {
        private static bool sPoseError;

        private static Color kSkeletonColor = new Color(103.0f / 255.0f, 103.0f / 255.0f, 103.0f / 255.0f, 0.25f);
        private static Color kDummyColor = new Color(60.0f / 255.0f, 60.0f / 255.0f, 60.0f / 255.0f, 0.25f);
        private static Color kHumanColor = new Color(0, 210.0f / 255.0f, 74.0f / 255.0f, 0.25f);
        private static Color kErrorColor = new Color(1, 0, 0, 0.25f);
        private static Color kErrorMessageColor = new Color(1, 0, 0, 0.75f);
        private static Color kSelectedColor = new Color(128.0f / 255.0f, 192.0f / 255.0f, 255.0f / 255.0f, 0.15f);

        public static void DrawSkeleton(Transform reference, Dictionary<Transform, bool> actualBones)
        {
            DrawSkeleton(reference, actualBones, null);
        }

        public static void DrawSkeleton(Transform reference, Dictionary<Transform, bool> actualBones, AvatarSetupTool.BoneWrapper[] bones)
        {
            // it can happen when the avatar tool is in edit mode and the user exit the tool in an unsual way
            //  new scene
            //  delete GO
            //  press play
            if (reference == null || actualBones == null)
                return;

            sPoseError = false;

            Bounds meshBounds = new Bounds();
            Renderer[] renderers = reference.root.GetComponentsInChildren<Renderer>();
            if (renderers != null)
            {
                foreach (Renderer renderer in renderers)
                {
                    meshBounds.Encapsulate(renderer.bounds.min);
                    meshBounds.Encapsulate(renderer.bounds.max);
                }
            }

            Quaternion orientation = Quaternion.identity;
            if (bones != null)
                orientation = AvatarSetupTool.AvatarComputeOrientation(bones);

            DrawSkeletonSubTree(actualBones, bones, orientation, reference, meshBounds);

            Camera camera = Camera.current;
            if (sPoseError && camera != null)
            {
                GUIStyle style = new GUIStyle(GUI.skin.label);
                style.normal.textColor = Color.red;
                style.wordWrap = false;
                style.alignment = TextAnchor.MiddleLeft;
                style.fontSize = 20;

                GUIContent content = new GUIContent("Character is not in T pose");

                Rect rect = GUILayoutUtility.GetRect(content, style);

                rect.x = 30;
                rect.y = 30; //camera.pixelHeight;

                Handles.BeginGUI();
                GUI.Label(rect, content, style);
                Handles.EndGUI();
            }
        }

        private static bool DrawSkeletonSubTree(Dictionary<Transform, bool> actualBones, AvatarSetupTool.BoneWrapper[] bones, Quaternion orientation, Transform tr, Bounds bounds)
        {
            // if this transform is not a valid bone
            if (!actualBones.ContainsKey(tr))
                return false;

            int drawnChildren = 0;
            foreach (Transform child in tr)
                if (DrawSkeletonSubTree(actualBones, bones, orientation, child, bounds))
                    drawnChildren++;

            if (!actualBones[tr] && drawnChildren <= 1)
                return false;

            int index = -1;
            if (bones != null)
            {
                for (int i = 0; i < bones.Length; i++)
                {
                    if (bones[i].bone == tr)
                    {
                        index = i;
                        break;
                    }
                }
            }

            // There is no need to check for a pose error if the avatar is not yet valid or tools is not the active one
            bool poseError = AvatarSetupTool.GetBoneAlignmentError(bones, orientation, index) > 0;
            sPoseError |= poseError;

            if (poseError)
            {
                DrawPoseError(tr, bounds);
                Handles.color = kErrorColor;
            }
            else if (index != -1)
                Handles.color = kHumanColor;
            else if (!actualBones[tr])
                Handles.color = kDummyColor;
            else
                Handles.color = kSkeletonColor;

            Handles.DoBoneHandle(tr, actualBones);
            if (Selection.activeObject == tr)
            {
                Handles.color = kSelectedColor;
                Handles.DoBoneHandle(tr, actualBones);
            }

            return true;
        }

        private static void DrawPoseError(Transform node, Bounds bounds)
        {
            Camera camera = Camera.current;
            if (camera)
            {
                GUIStyle style = new GUIStyle(GUI.skin.label);
                style.normal.textColor = Color.red;
                style.wordWrap = false;
                style.alignment = TextAnchor.MiddleLeft;

                Vector3 start = node.position;
                Vector3 end = node.position + Vector3.up * 0.20f;
                if (node.position.x <= node.root.position.x)
                    end.x = bounds.min.x;
                else
                    end.x = bounds.max.x;

                GUIContent content = new GUIContent(node.name);
                Rect rect = HandleUtility.WorldPointToSizedRect(end, content, style);
                rect.x += 2;
                if (node.position.x > node.root.position.x)
                    rect.x -= rect.width;

                Handles.BeginGUI();
                rect.y -= style.CalcSize(content).y / 4;
                GUI.Label(rect, content, style);
                Handles.EndGUI();

                Handles.color = kErrorMessageColor;
                Handles.DrawLine(start, end);
            }
        }
    }
}
