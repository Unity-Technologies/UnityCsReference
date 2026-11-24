// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal abstract class DropdownContent : VisualElement
    {
        public EditorWindow container { get; set; }

        public abstract Vector2 windowSize { get; }

        public abstract void OnDropdownShown();

        public abstract void OnDropdownClosed();

        protected void ShowWithNewWindowSize()
        {
            // There's no direct `resize` function for a dropdown window but setting min/max size does the same trick.
            if (container is not null)
            {
                container.minSize = windowSize;
                container.maxSize = windowSize;
                container.RepaintImmediately();
            }
            OnDropdownShown();
        }

        protected void Close()
        {
            container?.Close();
        }
    }
}
