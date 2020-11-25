// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal abstract class DropdownContent : VisualElement
    {
        internal EditorWindow container { get; set; }

        internal Rect position { get; set; }

        internal abstract Vector2 windowSize { get; }

        internal abstract void OnDropdownShown();

        internal abstract void OnDropdownClosed();

        protected void Close()
        {
            container?.Close();
        }
    }
}
