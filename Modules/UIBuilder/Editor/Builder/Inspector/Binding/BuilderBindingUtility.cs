// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Properties;
using Unity.UIToolkit.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    /// <summary>
    /// Utility class used to create, edit and find bindings.
    /// </summary>
    static class BuilderBindingUtility
    {
        private const float k_WindowWidth = 560;
        private const float k_WindowHeight = 460;
        private const float k_Spacing = 10;

        /// <summary>
        /// Finds the binding that binds the specified property of the selected VisualElement to a data source.
        /// </summary>
        /// <param name="property">The property for which we seek a related binding.</param>
        /// <param name="binding">The binding instance we seek.</param>
        /// <param name="uxmlBindingAsset">The uxml element related to the binding instance.</param>
        /// <returns>Returns true if a binding on the specified property is found; otherwise returns false.</returns>
        public static bool TryGetBinding(string property, out Binding binding, out UxmlObjectAsset uxmlBindingAsset)
        {
            var builder = Builder.ActiveWindow;
            var vta = builder.document.visualTreeAsset;
            var currentVe = builder.inspector.currentVisualElement;
            var vea = currentVe.GetVisualElementAsset();

            binding = null;
            uxmlBindingAsset = null;

            // VisualElementAsset can be null when the inspected element is coming from a template instance.
            if (vea == null)
                return false;

            if (DataBindingUtility.TryGetBinding(builder.inspector.currentVisualElement, new PropertyPath(property), out var bindingInfo))
            {
                binding = bindingInfo.binding;
                uxmlBindingAsset = vea.FindUxmlBinding(property);
            }

            return uxmlBindingAsset != null;
        }

        public static object GetBindingDataSourceOrRelativeHierarchicalDataSource(VisualElement element, BindingId id)
        {
            if (!element.TryGetBinding(id, out var binding))
                return null;

            if (binding is IDataSourceProvider {dataSource: { }} provider)
                return provider.dataSource;

            var context = element.GetHierarchicalDataSourceContext();
            var dataSource = context.dataSource;

            return PropertyContainer.TryGetValue(ref dataSource, context.dataSourcePath, out object relativeDataSource)
                ? relativeDataSource
                : dataSource;
        }

        private static void OpenBindingWindowToCreateOrEdit(string property, bool openToCreate, BuilderInspector inspector)
        {
            var fieldElement = inspector.FindFieldAtPath(property);
            var windowSize = new Vector2(k_WindowWidth, k_WindowHeight);
            var worldBound = Rect.zero;

            if (fieldElement != null)
            {
                worldBound = fieldElement.worldBound;
                // Adjust the position to align with left edge of the field
                worldBound.x -= k_WindowWidth + k_Spacing;
                worldBound.y -= (k_WindowHeight - worldBound.height) / 2;
            }

            worldBound = GUIUtility.GUIToScreenRect(worldBound);

            // Calls the active Binding window
            if (BuilderBindingWindow.activeWindow != null)
                BuilderBindingWindow.activeWindow.Close();

            BuilderBindingWindow.Open(openToCreate, property, inspector, worldBound, windowSize);
        }

        /// <summary>
        /// Opens the Binding window to create a new binding for the specified property of the selected VisualElement.
        /// </summary>
        /// <param name="property">The property to bind.</param>
        public static void OpenBindingWindowToCreate(string property, BuilderInspector inspector)
        {
            OpenBindingWindowToCreateOrEdit(property, true, inspector);
        }

        /// <summary>
        /// Opens the Binding window to edit the binding instance that binds the specified property of the selected VisualElement to a data source.
        /// </summary>
        /// <param name="property">The bound property.</param>
        public static void OpenBindingWindowToEdit(string property, BuilderInspector inspector)
        {
            OpenBindingWindowToCreateOrEdit(property, false, inspector);
        }

        /// <summary>
        /// Deletes the binding instance that binds the specified property of the selected VisualElement.
        /// </summary>
        /// <param name="context">The context in which uxml data of the document are edited</param>
        /// <param name="property">The property to unbind.</param>
        /// <param name="sourceFieldElement">The inspector field associated to the property to unbind, from where the action is invoked.</param>
        public static void DeleteBinding(BuilderUxmlAttributesEditingContext context, string property, VisualElement sourceFieldElement)
        {
            if (!TryGetBinding(property, out _, out _))
                return;

            var builder = context.builder;

            // Remove binding from SerializedData.
            RemoveBindingFromSerializedData(context, property, sourceFieldElement);

            builder.OnEnableAfterAllSerialization();

            if (property.StartsWith(BuilderConstants.StylePropertyPathPrefix))
            {
                var styleName = StyleSheetUtility.ConvertCamelToDash(property.Substring(BuilderConstants.StylePropertyPathPrefix.Length));
                builder.selection.NotifyOfStylingChange(null, new() {styleName});
            }
            // Refresh the UXML preview
            builder.selection.NotifyOfHierarchyChange();
        }

        public static void RemoveBindingFromSerializedData(BuilderUxmlAttributesEditingContext context, string property, VisualElement fieldElement)
        {
            var serializedPath = $"{context.serializedBasePath}.bindings";
            var bindingsSerializedProperty = context.rootSerializedObject.FindProperty(serializedPath);

            Undo.RegisterCompleteObjectUndo(bindingsSerializedProperty.serializedObject.targetObject, BuilderAssetUtilities.GetUndoMessage(bindingsSerializedProperty));

            for (var i = 0; i < bindingsSerializedProperty.arraySize; i++)
            {
                var item = bindingsSerializedProperty.GetArrayElementAtIndex(i);
                if (item.FindPropertyRelative("property").stringValue == property)
                {
                    if (fieldElement != null)
                        context.builder.inspector.attributeSection.SetInlineValue(fieldElement, property);
                    bindingsSerializedProperty.DeleteArrayElementAtIndex(i);
                    bindingsSerializedProperty.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                    BuilderAssetUtilities.SyncUxmlObjectChanges(context, serializedPath);
                    break;
                }
            }
        }

        /// <summary>
        /// Clear all bindings that can be found in uxml for this element.
        /// </summary>
        /// <param name="ve">The visual element to clear bindings from.</param>
        public static void ClearUxmlBindings(VisualElement ve)
        {
            using var pool = ListPool<BindingId>.Get(out var idsToRemove);
            foreach (var bindingInfo in ve.GetBindingInfos())
            {
                var bindingId = bindingInfo.binding.property;
                if (bindingId != BindingId.Invalid)
                {
                    idsToRemove.Add(bindingId);
                }
            }

            foreach (var bindingId in idsToRemove)
            {
                ve.ClearBinding(bindingId);
            }
        }

        public static bool IsInlineEditingEnabled(VisualElement field)
        {
            var inspector = Builder.ActiveWindow.inspector;
            return inspector.IsInlineEditingEnabled(field);
        }
    }
}
