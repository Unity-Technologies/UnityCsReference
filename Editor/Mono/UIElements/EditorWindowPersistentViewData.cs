// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.UIElements
{
    [LibraryFolderPath("UIElements/EditorWindows")]
    internal class EditorWindowViewData : ScriptableSingletonDictionary<
        EditorWindowViewData,
        UnityEditor.UIElements.SerializableJsonDictionary>
    {
        public static UnityEditor.UIElements.SerializableJsonDictionary GetEditorData(EditorWindow window)
        {
            string editorPrefFileName = window.GetType().ToString();
            return instance[editorPrefFileName];
        }
    }
}
