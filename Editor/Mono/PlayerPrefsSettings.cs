// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;

namespace UnityEditor
{
    internal class PlayerPrefsSettings
    {
        [MenuItem("Edit/Clear All PlayerPrefs", false, 270, false)]
        static void ClearPlayerPrefs()
        {
            if (EditorUtility.DisplayDialog("Clear All PlayerPrefs",
                "Are you sure you want to clear all PlayerPrefs? " +
                "This action cannot be undone.", "Yes", "No"))
            {
                PlayerPrefs.DeleteAll();
            }
        }
    }
}
