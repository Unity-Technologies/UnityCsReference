// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.MPE;
using UnityEngine.UIElements;

namespace Unity.MPE
{
    internal class StandaloneWindow : EditorWindow
    {
        public void OnEnable()
        {
            titleContent.text = "Standalone Window";

            rootVisualElement.Add(new Label("Hello World!"));
            rootVisualElement.Add(new Label("From Standalone Application Window"));

            EventService.Emit("StandaloneWindowCreated");
        }

        internal void OnDisable()
        {
            EventService.Emit("StandaloneWindowDestroyed");
            EditorApplication.Exit(0);
        }
    }
}
