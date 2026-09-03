// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: EditorWindowManagement not yet converted
using UnityEditor;
using UnityEditor.MPE;
using UnityEngine.UIElements;

namespace Unity.MPE
{
    internal class StandaloneWindow : EditorWindow
    {
        #pragma warning disable UAL0015 // this side effect does not outlive the current call (global trigger / lazily-loaded asset re-fetched on next access); a stale reference is harmlessly replaced
        public StandaloneWindow() { }
        #pragma warning restore UAL0015

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
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
