// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    [Icon("UIToolkit/Icons/CustomCSharpElement.png")]
    public sealed class MainToolbarCustomElement : MainToolbarElement
    {
        readonly Func<VisualElement> m_CreateElement;

        public MainToolbarCustomElement(Func<VisualElement> createElement)
        {
            m_CreateElement = createElement;
        }

        internal override VisualElement CreateElement()
        {
            var container = new VisualElement();
            container.AddToClassList(EditorToolbar.elementClassName);
            container.Add(m_CreateElement?.Invoke());
            return container;
        }
    }
}
