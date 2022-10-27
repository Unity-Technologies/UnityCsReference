// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Utility to build a value editor.
    /// </summary>
    static class InlineValueEditor
    {
        public static readonly string ussClassName = "ge-inline-value-editor";

        // PF TODO:
        // - It is hard to put additional information in ConstantEditorBuilder
        // - Maybe the constant editor should be a ModelView?

        /// <summary>
        /// Creates an editor for a constants of the same type.
        /// </summary>
        /// <param name="uiContext">The view in which the constant editor will be displayed.</param>
        /// <param name="ownerModels">The graph element models that owns each constants, if any.</param>
        /// <param name="constants">The constants.</param>
        /// <param name="modelIsLocked">Whether the node owning the constant, if any, is locked.</param>
        /// <param name="label">The label to display in front of the editor.</param>
        /// <returns>A VisualElement that contains an editor for the constant.</returns>
        public static BaseModelPropertyField CreateEditorForConstants(
            RootView uiContext, IEnumerable<GraphElementModel> ownerModels, IEnumerable<Constant> constants,
            bool modelIsLocked, string label = null)
        {
            var ext = ExtensionMethodCache<ConstantEditorBuilder>.GetExtensionMethod(
                uiContext.GetType(), constants.GetType(),
                ConstantEditorBuilder.FilterMethods, ConstantEditorBuilder.KeySelector);

            if (ext != null)
            {
                var constantBuilder = new ConstantEditorBuilder(uiContext, modelIsLocked, ownerModels, label);

                var editor = (BaseModelPropertyField)ext.Invoke(null, new object[] {constantBuilder, constants});
                if (editor != null)
                {
                    editor.AddToClassList(ussClassName);
                    return editor;
                }
            }

            Debug.Log($"Could not draw Editor GUI for node of type {constants.First().Type}");
            return new MissingFieldEditor(uiContext, label ?? $"<Unknown> {constants.GetType()}");
        }
    }
}
