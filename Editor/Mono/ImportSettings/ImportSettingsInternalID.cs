// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;
using System;

internal class ImportSettingInternalID
{
    static public void RegisterInternalID(SerializedObject serializedObject, UnityType type, long id, string name)
    {
        SerializedProperty internalIDMap = serializedObject.FindProperty("m_InternalIDToNameTable");
        internalIDMap.arraySize++;
        SerializedProperty newEntry = internalIDMap.GetArrayElementAtIndex(internalIDMap.arraySize - 1);
        SerializedProperty first = newEntry.FindPropertyRelative("first");
        SerializedProperty cid = first.FindPropertyRelative("first");
        SerializedProperty lid = first.FindPropertyRelative("second");
        cid.intValue = type.persistentTypeID;
        lid.longValue = id;
        SerializedProperty sname = newEntry.FindPropertyRelative("second");
        sname.stringValue = name;
    }

    static public bool RemoveEntryFromInternalIDTable(SerializedObject serializedObject, UnityType type, long id,
        string name)
    {
        int classID = type.persistentTypeID;
        SerializedProperty internalIDMap = serializedObject.FindProperty("m_InternalIDToNameTable");
        bool found = false;
        int index = 0;
        foreach (SerializedProperty element in internalIDMap)
        {
            SerializedProperty first = element.FindPropertyRelative("first");
            SerializedProperty cid = first.FindPropertyRelative("first");
            if (cid.intValue == classID)
            {
                SerializedProperty second = element.FindPropertyRelative("second");
                SerializedProperty lid = first.FindPropertyRelative("second");
                string foundName = second.stringValue;
                long localid = lid.longValue;

                if (foundName == name && localid == id)
                {
                    found = true;
                    internalIDMap.DeleteArrayElementAtIndex(index);
                    return found;
                }
            }

            index++;
        }
        return found;
    }

    static public long FindInternalID(SerializedObject serializedObject, UnityType type, string name)
    {
        long id = 0;
        SerializedProperty internalIDMap = serializedObject.FindProperty("m_InternalIDToNameTable");
        foreach (SerializedProperty element in internalIDMap)
        {
            SerializedProperty first = element.FindPropertyRelative("first");
            SerializedProperty cid = first.FindPropertyRelative("first");
            if (cid.intValue == type.persistentTypeID)
            {
                SerializedProperty second = element.FindPropertyRelative("second");
                if (second.stringValue == name)
                {
                    SerializedProperty lid = first.FindPropertyRelative("second");
                    id = lid.longValue;
                    return id;
                }
            }
        }

        return id;
    }

    static public long MakeInternalID(SerializedObject serializedObject, UnityType type, string name)
    {
        long id = ImportSettingInternalID.FindInternalID(serializedObject, type, name);
        if (id == 0L)
        {
            id = AssetImporter.MakeLocalFileIDWithHash(type.persistentTypeID, name, 0);
            RegisterInternalID(serializedObject, type, id, name);
        }
        return id;
    }

    static public void Rename(SerializedObject serializedObject, UnityType type, string oldName, string newName)
    {
        RenameMultiple(serializedObject, type, new string[] { oldName }, new string[] { newName });
    }

    // Rename multiple entries at once to avoid situations where swapping names of two entries would break references
    static public void RenameMultiple(SerializedObject serializedObject, UnityType type, string[] oldNames, string[] newNames)
    {
        int classID = type.persistentTypeID;

        int left = oldNames.Length;

        SerializedProperty recycleMap = serializedObject.FindProperty("m_InternalIDToNameTable");
        foreach (SerializedProperty element in recycleMap)
        {
            SerializedProperty first = element.FindPropertyRelative("first");
            SerializedProperty cid = first.FindPropertyRelative("first");
            if (cid.intValue == classID)
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
