// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.Hierarchy
{
    /// <summary>
    /// Utility class to help with creating editor for HierarchyViewCell.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.HierarchyModule", "UnityEditor.UIToolkitAuthoringModule")]
    class HierarchyViewColumnUtility
    {
        /// <summary>
        /// USS class id for toggle using icons instead of toggle checkbox.
        /// </summary>
        public const string k_ToggleIcon = "toggle-icon";

        /// <summary>
        /// USS class for Cell using a PropertyField.
        /// </summary>
        public const string k_CellPropField = "cell-prop-field";

        /// <summary>
        /// Get the Cell corresponding to the target of a VisualElement.
        /// </summary>
        /// <param name="target">Target of a UITk event.</param>
        /// <returns>Returns the HierarchyViewCell associated to a target.</returns>
        public static HierarchyViewCell GetCellFromTarget(VisualElement target)
        {
            return (HierarchyViewCell)target.parent;
        }

        /// <summary>
        /// Get a CellValueEditor from the pool and bind it with a cell and a model.
        /// </summary>
        /// <typeparam name="TModel">Type of the value edited by TEditor</typeparam>
        /// <typeparam name="TEditor">VisualElement used to edit a Cell</typeparam>
        /// <typeparam name="TValue">Type of the value being edited in the Cell</typeparam>
        /// <param name="model">Model used to extract values to edit with the TEditor.</param>
        /// <param name="cell">Cell to bind the editor and model.</param>
        /// <param name="pool">Pool used to acquire a CellValueEditor.</param>
        /// <param name="classes">List of USS classes to apply to the editor.</param>
        /// <returns></returns>
        public static HierarchyViewCellValueEditor<TModel, TEditor, TValue> BindCellToValueEditor<TModel, TEditor, TValue>(
            TModel model, HierarchyViewCell cell,
            HierarchyViewColumnContextPool<HierarchyViewCellValueEditor<TModel, TEditor, TValue>> pool, params string[] classes) where TEditor : VisualElement, INotifyValueChanged<TValue>, new()
        {
            var editor = GetOrCreateEditor<TEditor>(cell, classes);
            var cellValueEditor = pool.Get(cell.View.GetHashCode());
            cellValueEditor.Bind(model, cell, editor);
            return cellValueEditor;
        }

        /// <summary>
        /// Unbind a Cell from its model, editor and CellValueEditor. Will use the pool to release the CellValueEditor.
        /// </summary>
        /// <typeparam name="TModel">Type of the value edited by TEditor</typeparam>
        /// <typeparam name="TEditor">VisualElement used to edit a Cell</typeparam>
        /// <typeparam name="TValue">Type of the value being edited in the Cell</typeparam>
        /// <param name="cell"></param>
        /// <param name="pool"></param>
        public static void UnbindCellFromValueEditor<TModel, TEditor, TValue>(HierarchyViewCell cell, HierarchyViewColumnContextPool<HierarchyViewCellValueEditor<TModel, TEditor, TValue>> pool) where TEditor : VisualElement, INotifyValueChanged<TValue>, new()
        {
            if (cell.userData is HierarchyViewCellValueEditor<TModel, TEditor, TValue> editor)
            {
                pool.Release(cell.View.GetHashCode(), editor);
                editor.Unbind();
            }
        }

        /// <summary>
        /// Instantiate an editor and add it to the cell. If the control already exists, it will be returned.
        /// </summary>
        /// <typeparam name="TEditor"></typeparam>
        /// <param name="cell">Cell to attach the editor to.</param>
        /// <param name="classes">List of USS classes to add to the newly created editor.</param>
        /// <returns></returns>
        public static TEditor GetOrCreateEditor<TEditor>(HierarchyViewCell cell, params string[] classes) where TEditor : VisualElement, new()
        {
            var editor = cell.Q<TEditor>();
            if (editor == null)
            {
                editor = new TEditor();
                AddToClassList(editor, classes);
                cell.Add(editor);
            }
            return editor;
        }

        internal static VisualElement AddToClassList(VisualElement element, params string[] classes)
        {
            foreach (var c in classes)
            {
                element.AddToClassList(c);
            }
            return element;
        }

        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal static HierarchyViewCellValueEditor<TModel, TEditor, TValue> CreateCellValueEditor<TModel, TEditor, TValue>(
                TModel model,
                HierarchyViewCell cell,
                Func<HierarchyViewCellValueEditor<TModel, TEditor, TValue>, TValue> getModelValue,
                Action<HierarchyViewCellValueEditor<TModel, TEditor, TValue>, TValue> setModelValue,
                Func<HierarchyViewCellValueEditor<TModel, TEditor, TValue>, TValue, bool> isDefaultValue,
                params string[] classes) where TEditor : VisualElement, INotifyValueChanged<TValue>, new()
        {
            var editor = GetOrCreateEditor<TEditor>(cell, classes);
            var cellValueEditor = new HierarchyViewCellValueEditor<TModel, TEditor, TValue>(getModelValue, setModelValue, isDefaultValue);
            cellValueEditor.Bind(model, cell, editor);
            return cellValueEditor;
        }

        internal static int GetVisibleIndex(HierarchyViewState viewState, Column c)
        {
            var colId = GetColumnId(c);
            foreach (var colState in viewState.Columns)
            {
                if (colState.ColumnId == colId)
                    return colState.Index;
            }

            return GetColumnDefaultPriority(c);
        }

        internal static string GetColumnId(Column col)
        {
            if (col is HierarchyViewColumn hc)
                return hc.Descriptor.Id;

            if (col is HierarchyViewItemColumn)
                return HierarchyViewItemColumn.k_HierarchyNameColumnName;
            return null;
        }

        internal static int GetColumnDefaultPriority(Column col)
        {
            if (col is HierarchyViewColumn hc)
                return hc.Descriptor.DefaultPriority;

            if (col is HierarchyViewItemColumn)
                return 0;
            return 1000;
        }

        internal static Column GetColumnWithId(IEnumerable<Column> columns, string id)
        {
            foreach (var col in columns)
            {
                if (GetColumnId(col) == id)
                    return col;
            }
            return null;
        }
    }
}
