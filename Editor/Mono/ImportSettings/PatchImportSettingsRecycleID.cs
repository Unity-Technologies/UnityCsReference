// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;
using System;

internal class PatchImportSettingRecycleID
{
    static public void Patch(SerializedObject serializedObject, int classID, string oldName, string newName)
    {
        PatchMultiple(serializedObject, classID, new string[] { oldName }, new string[] { newName });
    }

    // Patches multiple entries at once to avoid situations where swapping names of two entries would break references
    static public void PatchMultiple(SerializedObject serializedObject, int classID, string[] oldNames, string[] newNames)
    {
        int left = oldNames.Length;

        SerializedProperty recycleMap = serializedObject.FindProperty("m_FileIDToRecycleName");
        foreach (SerializedProperty element in recycleMap)
        {
            SerializedProperty first = element.FindPropertyRelative("first");
            if (AssetImporter.LocalFileIDToClassID(first.longValue) == classID)
            {
                SerializedProperty second = element.FindPropertyRelative("second");
                int idx = Array.IndexOf(oldNames, second.stringValue);
                if (idx >= 0)
                {
                    second.stringValue = newNames[idx];
                    if (--left == 0)
                        break;
                }
            }
        }
    }
}
