// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.ItemLibrary.Editor;
using UnityEditor;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Opens an item library to pick the blackboard variable compared by a <see cref="VariableConditionModel"/>.
    /// </summary>
    internal static class VariableConditionPicker
    {
        static bool CanUse(GraphModel graphModel, VariableDeclarationModelBase declaration)
        {
            if (graphModel == null || declaration == null)
                return false;

            var dataType = declaration.DataType;
            return !dataType.IsCustomTypeHandle() && graphModel.GetConstantType(dataType) != null;
        }

        /// <summary>
        /// The variables a variable condition can compare: those whose type can produce a constant value.
        /// </summary>
        /// <param name="graphModel">The graph whose variables are filtered.</param>
        /// <returns>The selectable variable declarations.</returns>
        public static IReadOnlyList<VariableDeclarationModelBase> GetSelectableVariables(GraphModel graphModel)
        {
            var result = new List<VariableDeclarationModelBase>();
            if (graphModel == null)
                return result;

            foreach (var declaration in graphModel.VariableDeclarations)
            {
                if (CanUse(graphModel, declaration))
                    result.Add(declaration);
            }

            return result;
        }

        /// <summary>
        /// Shows the variable picker.
        /// </summary>
        /// <param name="view">The view on which to display the window.</param>
        /// <param name="graphModel">The graph whose variables are offered.</param>
        /// <param name="position">The screen position of the window.</param>
        /// <param name="onSelected">Called with the chosen variable.</param>
        public static void Show(RootView view, GraphModel graphModel, Vector2 position, Action<VariableDeclarationModelBase> onSelected)
        {
            var items = new List<ItemLibraryItem>();
            var declarationsByItem = new Dictionary<ItemLibraryItem, VariableDeclarationModelBase>();
            foreach (var declaration in GetSelectableVariables(graphModel))
            {
                var item = new GraphNodeModelLibraryItem(
                    declaration.Title,
                    new TypeItemLibraryData(declaration.DataType, graphModel),
                    _ => null);
                items.Add(item);
                declarationsByItem[item] = declaration;
            }

            var database = new ItemLibraryDatabase(items);
            var library = new ItemLibraryLibrary(database, new SimpleLibraryAdapter("Pick a Variable"), context: "VariableCondition");

            var window = library.Show(EditorWindow.focusedWindow, position, view.TypeHandleInfos);
            window.itemChosen += item =>
            {
                if (item != null && declarationsByItem.TryGetValue(item, out var declaration))
                    onSelected(declaration);
            };
        }
    }
}
