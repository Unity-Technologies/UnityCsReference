// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

using UnityEditorInternal;

namespace UnityEditor
{
    internal class AvatarMaskUtility
    {
        private static string sHuman = "m_HumanDescription.m_Human";
        private static string sBoneName = "m_BoneName";

        static public string[] GetAvatarHumanTransform(SerializedObject so, string[] refTransformsPath)
        {
            SerializedProperty humanBoneArray = so.FindProperty(sHuman);
            if (humanBoneArray == null || !humanBoneArray.isArray)
                return null;

            List<string> humanTransforms = new List<string>();
            for (int i = 0; i < humanBoneArray.arraySize; i++)
            {
                SerializedProperty transformNameP = humanBoneArray.GetArrayElementAtIndex(i).FindPropertyRelative(sBoneName);
                humanTransforms.Add(transformNameP.stringValue);
            }

            return TokeniseHumanTransformsPath(refTransformsPath, humanTransforms.ToArray());
        }

        static public string[] GetAvatarHumanAndActiveExtraTransforms(SerializedObject so, SerializedProperty transformMaskProperty, string[] refTransformsPath)
        {
            SerializedProperty humanBoneArray = so.FindProperty(sHuman);
            if (humanBoneArray == null || !humanBoneArray.isArray)
                return null;

            List<string> humanTransforms = new List<string>();
            for (int i = 0; i < humanBoneArray.arraySize; i++)
            {
                SerializedProperty transformNameP = humanBoneArray.GetArrayElementAtIndex(i).FindPropertyRelative(sBoneName);
                humanTransforms.Add(transformNameP.stringValue);
            }

            List<string> values = new List<string>(TokeniseHumanTransformsPath(refTransformsPath, humanTransforms.ToArray()));

            for (int i = 0; i < transformMaskProperty.arraySize; i++)
            {
                float weight = transformMaskProperty.GetArrayElementAtIndex(i).FindPropertyRelative("m_Weight").floatValue;
                string transformName = transformMaskProperty.GetArrayElementAtIndex(i).FindPropertyRelative("m_Path").stringValue;

                if (weight > 0.0f &&  !values.Contains(transformName))
                {
                    values.Add(transformName);
                }
            }

            return values.ToArray();
        }

        static public string[] GetAvatarInactiveTransformMaskPaths(SerializedProperty transformMaskProperty)
        {
            if (transformMaskProperty == null || !transformMaskProperty.isArray)
                return null;

            List<string> transformPaths = new List<string>();
            for (int i = 0; i < transformMaskProperty.arraySize; i++)
            {
                SerializedProperty weight = transformMaskProperty.GetArrayElementAtIndex(i).FindPropertyRelative("m_Weight");
                if (weight.floatValue < 0.5f)
                {
                    SerializedProperty transformNameP = transformMaskProperty.GetArrayElementAtIndex(i).FindPropertyRelative("m_Path");
                    transformPaths.Add(transformNameP.stringValue);
                }
            }

            return transformPaths.ToArray();
        }

        static public void UpdateTransformMask(AvatarMask mask, string[] refTransformsPath, string[] humanTransforms)
        {
            mask.transformCount = refTransformsPath.Length;
            for (int i = 0; i < refTransformsPath.Length; i++)
            {
                mask.SetTransformPath(i, refTransformsPath[i]);

                bool isActiveTransform = humanTransforms == null
                    ? true
                    : ArrayUtility.FindIndex(humanTransforms, s => refTransformsPath[i] == s) != -1;
                mask.SetTransformActive(i, isActiveTransform);
            }
        }

        static public void UpdateTransformMask(SerializedProperty transformMask, string[] refTransformsPath, string[] currentPaths, bool areActivePaths = true)
        {
            // if areActivePaths=true, currentPaths is treated as the list of active transform paths
            // else, currentPaths is treated as the list of inactive transform paths
            AvatarMask refMask = new AvatarMask();

            refMask.transformCount = refTransformsPath.Length;

            for (int i = 0; i < refTransformsPath.Length; i++)
            {
                bool isActiveTransform;
                if (currentPaths == null)
                    isActiveTransform = true;
                else if (areActivePaths)
                    isActiveTransform = ArrayUtility.FindIndex(currentPaths, s => refTransformsPath[i] == s) != -1;
                else
                    isActiveTransform = ArrayUtility.FindIndex(currentPaths, s => refTransformsPath[i] == s) == -1;

                refMask.SetTransformPath(i, refTransformsPath[i]);
                refMask.SetTransformActive(i, isActiveTransform);
            }
            ModelImporter.UpdateTransformMask(refMask, transformMask);
        }

        static public void SetActiveHumanTransforms(AvatarMask mask, string[] humanTransforms)
        {
            for (int i = 0; i < mask.transformCount; i++)
            {
                string path = mask.GetTransformPath(i);
                if (ArrayUtility.FindIndex(humanTransforms, s => path == s) != -1)
                    mask.SetTransformActive(i, true);
            }
        }

        static private string[] TokeniseHumanTransformsPath(string[] refTransformsPath, string[] humanTransforms)
        {
            if (humanTransforms == null)
                return null;

            // all list must always include the string "" which is the root game object
            string[] tokeniseTransformsPath = new string[] {""};

            for (int i = 0; i < humanTransforms.Length; i++)
            {
                int index1 = ArrayUtility.FindIndex(refTransformsPath, s => humanTransforms[i] == FileUtil.GetLastPathNameComponent(s));
                if (index1 != -1)
                {
                    int insertIndex = tokeniseTransformsPath.Length;

                    string path = refTransformsPath[index1];
                    while (path.Length > 0)
                    {
                        int index2 = ArrayUtility.FindIndex(tokeniseTransformsPath, s => path == s);
                        if (index2 == -1)
                            ArrayUtility.Insert(ref tokeniseTransformsPath, insertIndex, path);

                        int lastIndex = path.LastIndexOf('/');
                        path = path.Substring(0, lastIndex != -1 ? lastIndex : 0);
                    }
                }
            }

            return tokeniseTransformsPath;
        }
    }
}
