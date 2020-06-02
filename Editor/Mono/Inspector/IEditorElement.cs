// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor
{
    internal interface IEditorElement
    {
        Editor editor { get; }
        IEnumerable<Editor> Editors { get; }

        void Reinit(int editorIndex);

        void AddPrefabComponent(VisualElement comp);

        // From VisualElement
        void RemoveFromHierarchy();
        string name { get; set; }
    }

    internal static class EditorElementHelper
    {
        internal static Func<int, IPropertyView, string, IEditorElement> CreateFunctor;

        internal static IEditorElement CreateEditorElement(int editorIndex, IPropertyView iw, string title)
        {
            return CreateFunctor.Invoke(editorIndex, iw, title);
        }
    }
}
