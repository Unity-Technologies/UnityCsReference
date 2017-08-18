// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements
{
    [LibraryFolderPath("UIElements/EditorWindows")]
    internal class EditorWindowPersistentViewData : ScriptableSingletonDictionary<
            EditorWindowPersistentViewData,
            SerializableJsonDictionary>
    {
        public static SerializableJsonDictionary GetEditorData(EditorWindow window)
        {
            string editorPrefFileName = window.GetType().ToString();
            return instance[editorPrefFileName];
        }
    }
}
