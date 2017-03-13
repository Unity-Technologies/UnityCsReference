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

            string[] humanTransforms = new string[0];
            for (int i = 0; i < humanBoneArray.arraySize; i++)
            {
                SerializedProperty transformNameP = humanBoneArray.GetArrayElementAtIndex(i).FindPropertyRelative(sBoneName);
                ArrayUtility.Add(ref humanTransforms, transformNameP.stringValue);
            }

            return TokeniseHumanTransformsPath(refTransformsPath, humanTransforms);
        }

        static public void UpdateTransformMask(AvatarMask mask, string[] refTransformsPath, string[] humanTransforms)
        {
            mask.transformCount = refTransformsPath.Length;
            for (int i = 0; i < refTransformsPath.Length; i++)
            {
                mask.SetTransformPath(i, refTransformsPath[i]);

                bool isActiveTransform = humanTransforms == null
                    ? true
                    : ArrayUtility.FindIndex(humanTransforms,
                        delegate(string s) { return refTransformsPath[i] == s; }) != -1;
                mask.SetTransformActive(i, isActiveTransform);
            }
        }

        static public void UpdateTransformMask(SerializedProperty transformMask, string[] refTransformsPath, string[] humanTransforms)
        {
            AvatarMask refMask = new AvatarMask();

            refMask.transformCount = refTransformsPath.Length;


            for (int i = 0; i < refTransformsPath.Length; i++)
            {
                bool isActiveTransform = humanTransforms == null
                    ? true
                    : ArrayUtility.FindIndex(humanTransforms,
                        delegate(string s) { return refTransformsPath[i] == s; }) != -1;

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
                if (ArrayUtility.FindIndex(humanTransforms, delegate(string s) { return path == s; }) != -1)
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
                int index1 = ArrayUtility.FindIndex(refTransformsPath, delegate(string s) { return humanTransforms[i] == FileUtil.GetLastPathNameComponent(s); });
                if (index1 != -1)
                {
                    int insertIndex = tokeniseTransformsPath.Length;

                    string path = refTransformsPath[index1];
                    while (path.Length > 0)
                    {
                        int index2 = ArrayUtility.FindIndex(tokeniseTransformsPath, delegate(string s) { return path == s; });
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
