// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// Create decorators (additional <see cref="VisualElement"/>s) for the Inspector window.
    /// </summary>
    /// <remarks>
    /// Unity automatically calls the methods in this interface for instances added to <see cref="EditorElement.AddDecorator(IEditorElementDecorator)"/>.
    /// </remarks>
    /// <seealso cref="EditorElement.RemoveDecorator(IEditorElementDecorator)"/>
    internal interface IEditorElementDecorator
    {
        /// <summary>
        /// Invoked once when the hierarchy element is constructed.
        /// You can create and return a <see cref="VisualElement"/> that appears after the given editor in the InspectorWindow.
        /// </summary>
        /// <param name="editor">The editor to create the footer decorator.</param>
        /// <returns>A footer <see cref="VisualElement"/> for the editor or null if you don't want to create a decorator.</returns>
        VisualElement OnCreateFooter(Editor editor);
    }
}
