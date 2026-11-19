// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    public class EditorToolbarDropdown : EditorToolbarButton
    {
        internal new static readonly string ussClassName = "unity-editor-toolbar-dropdown";
        internal new static readonly UniqueStyleString ussClassNameUnique = new(ussClassName);
        internal const string arrowClassName = "unity-icon-arrow";

        public EditorToolbarDropdown() : this(string.Empty, null, null)
        {
        }

        public EditorToolbarDropdown(Action clickEvent) : this(string.Empty, null, clickEvent)
        {
        }

        public EditorToolbarDropdown(string text, Action clickEvent) : this(text, null, clickEvent)
        {
        }

        public EditorToolbarDropdown(Texture2D icon, Action clickEvent) : this(string.Empty, icon, clickEvent)
        {
        }

        public EditorToolbarDropdown(string text, Texture2D icon, Action clickEvent) : base(text, icon, clickEvent)
        {
            AddToClassList(ussClassNameUnique);
            var arrow = new VisualElement();
            arrow.AddToClassList(arrowClassName);
            Add(arrow);
        }
    }
}
