// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor
{
    internal class UndoSerializationWindow : EditorWindow
    {

        [MenuItem("Window/Internal/Undo Serialization", false, 2013, true)]
        static void CreateUndoSerializationWindow()
        {
            UndoSerializationWindow window = EditorWindow.GetWindow<UndoSerializationWindow>();
            window.titleContent = EditorGUIUtility.TrTextContent("Undo Serialization");
            window.Show();
        }

        void OnEnable()
        {
            VisualElement root = rootVisualElement;

            Label label = new Label("Available only in Developer Mode");
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            root.Add(label);

            Button convertorButton = new Button();
            convertorButton.name = "convertorButton";
            convertorButton.text = "Convert Serialized Data To Readable Format";
            rootVisualElement.Add(convertorButton);

            convertorButton.RegisterCallback<ClickEvent>(ConvertSerializedData);
        }

        private void ConvertSerializedData(ClickEvent evt)
        {
            bool success = Undo.ConvertSerializedData();
            if (success)
            EditorUtility.DisplayDialog("Undo Serialization", "Undo stack has been converted and available in project's Library folder", "OK");
        }

        void OnDisable()
        {

        }
    }
}
