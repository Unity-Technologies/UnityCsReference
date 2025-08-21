// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Editor
{
    static class VisualElementExtensions
    {
        public static void AddToClassList(this VisualElement visualElement, params string[] classNames)
        {
            foreach (var className in classNames)
            {
                visualElement.AddToClassList(className);
            }
        }

        public static void RemoveFromClassList(this VisualElement visualElement, params string[] classNames)
        {
            foreach (var className in classNames)
            {
                visualElement.RemoveFromClassList(className);
            }
        }

        public static void AddEventLifecycle(
            this VisualElement visualElement,
            EventCallback<AttachToPanelEvent> onAttach,
            EventCallback<DetachFromPanelEvent> onDetach)
        {
            visualElement.RegisterCallback(onAttach);
            visualElement.RegisterCallback(onDetach);
        }
    }
}
