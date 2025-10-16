// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Utility to build a value editor.
    /// </summary>
    [UnityRestricted]
    internal static class InlineValueEditor
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
        /// <param name="label">The label to display in front of the editor.</param>
        /// <returns>A VisualElement that contains an editor for the constant.</returns>
        public static BaseModelPropertyField CreateEditorForConstants(
            RootView uiContext, IReadOnlyList<GraphElementModel> ownerModels, IReadOnlyList<Constant> constants, string label = null)
        {
            if (constants == null || constants.Count == 0)
                return null;

            var commonType = constants[0].GetType();
            for (int i = 1; i < constants.Count; i++)
            {
                if (commonType != constants[i].GetType())
                {
                    return new MixedTypeFieldEditor(uiContext, label);
                }
            }

            var constantListType = typeof(IReadOnlyList<>).MakeGenericType(commonType);
            var viewDomain = uiContext?.GetType() ?? typeof(RootView);

            object constantsToPass = constants;

            var ext = ExtensionMethodCache<ConstantEditorBuilder>.GetExtensionMethod(
                viewDomain, constantListType,
                ConstantEditorBuilder.FilterMethods, ConstantEditorBuilder.KeySelector);

            if (ext == null)
            {
                ext = ExtensionMethodCache<ConstantEditorBuilder>.GetExtensionMethod(
                    viewDomain, constants.GetType(),
                    ConstantEditorBuilder.FilterMethods, ConstantEditorBuilder.KeySelector);
            }
            else
            {
                // need some additional cooking as a method taking IReadOnlyList<DerivedConstant> will not accecpt a IReadOnlyList<Constant> even if each constant has the correct type.

                var listType = ConstantEditorBuilder.KeySelector(ext);
                var array = Array.CreateInstance(listType.GenericTypeArguments[0], constants.Count);

                for (var i = 0; i < constants.Count; ++i)
                {
                    array.SetValue(constants[i], i);
                }

                constantsToPass = array;
            }

            if (ext != null)
            {
                var constantBuilder = new ConstantEditorBuilder(uiContext, ownerModels, label);

                var editor = (BaseModelPropertyField)ext.Invoke(null, new object[] { constantBuilder, constantsToPass });
                if (editor != null)
                {
                    editor.AddToClassList(ussClassName);
                    return editor;
                }
            }

            return new MissingFieldEditor(uiContext, label ?? $"<Unknown> {constants.GetType()}");
        }
    }
}
