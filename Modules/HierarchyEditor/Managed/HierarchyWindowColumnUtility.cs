// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Unity.Hierarchy.Editor
{
    /// <summary>
    /// Utility functions use to create Column handlers for the Hiearchy Window.
    /// </summary>
    class HierarchyWindowColumnUtility : HierarchyViewColumnUtility
    {
        /// <summary>
        /// If a Cell is bound to HierarchyGameObjectHandler it returns the corresponding GameObject.
        /// </summary>
        /// <param name="cell">The Cell encapsulating a GameObject</param>
        /// <returns>Returns the corresponding GameObject (if any) edited by a Cell.</returns>
        public static GameObject GetGameObject(HierarchyViewCell cell)
        {
            GameObject go = null;
            if (cell.Handler is HierarchyGameObjectHandler handler)
            {
                go = handler.GetGameObject(cell.Node);
            }
            return go;
        }

        /// <summary>
        /// Gets the GameObjects that should be affected by a column editor action.
        /// Returns current cell's GameObject and all selected GameObjects.
        /// </summary>
        /// <param name="cell">The cell containing the column editor.</param>
        /// <param name="length">The number of valid GameObjects in the returned span.</param>
        /// <returns>A rented span of GameObjects. The caller is responsible for disposing it.</returns>
        public static RentSpan<GameObject> GetTargetGameObjects(HierarchyViewCell cell, out int length)
        {
            if (cell.Handler is not HierarchyGameObjectHandler handler)
                handler = cell.View.Source.GetNodeTypeHandler<HierarchyGameObjectHandler>();

            var gameObjectNodeType = handler.GetNodeType();
            var hierarchy = cell.View.Source;
            var gameObjects = new RentSpan<GameObject>(Math.Max(cell.View.ViewModel.HasFlagsCount(HierarchyNodeFlags.Selected)+1, 1));
            length = 0;

            // First add the current cell underlying gameobject to the target objects
            var cellGo = handler.GetGameObject(cell.Node);
            if (cellGo && cellGo != null)
                gameObjects.Span[length++] = cellGo;

            foreach (ref readonly var node in cell.View.ViewModel.EnumerateNodesWithFlags(HierarchyNodeFlags.Selected))
            {
                if (node == cell.Node)
                    continue;

                if (hierarchy.GetNodeType(in node) != gameObjectNodeType)
                    continue;

                var go = handler.GetGameObject(in node);
                if (!go || go == null)
                    continue;

                gameObjects.Span[length++] = go;
            }

            return gameObjects;
        }

        /// <summary>
        /// If a Cell is bound to HierarchySceneHandler it returns the corresponding Scene.
        /// </summary>
        /// <param name="cell">The Cell encapsulating a Scene</param>
        /// <returns>Returns the corresponding Scene (if any) edited by a Cell.</returns>
        public static Scene GetScene(HierarchyViewCell cell)
        {
            if (cell.Handler is HierarchySceneHandler handler)
            {
                return handler.GetScene(cell.Node);
            }
            throw new System.Exception("Cannot convert cell to scene.");
        }

        /// <summary>
        /// Bind a Cell editor to a property path and a serialized object. This will establish a 2 way data binding between the SerializedProperty and the editor.
        /// </summary>
        /// <typeparam name="TEditor">VisualElement Type used to edit the Cell value (i.e the editor).</typeparam>
        /// <param name="editor">Editor used to edit the Cell value.</param>
        /// <param name="cell">Cell containing an editor.</param>
        /// <param name="so">SerializedObject edited by the Cell.</param>
        /// <param name="path">Property path of the SerializedProperty to modify.</param>
        public static void BindPropertyPath<TEditor>(TEditor editor, HierarchyViewCell cell, SerializedObject so, string path) where TEditor : BindableElement
        {
            editor.bindingPath = path;
            BindCellToSerializedObject(cell, so);
        }

        /// <summary>
        /// Unbind the cell from its SerialziedObject
        /// </summary>
        /// <param name="cell">Cell containing a SerialziedObjuect bound to a property path</param>
        public static void UnbindPropertyPath(HierarchyViewCell cell)
        {
            UnbindCellFromSerializedObject(cell);
        }

        /// <summary>
        /// Bind a cell to a SerializedObject and ensure the UITk Bind function is properly invoked to start the whole automated binding process.
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="so"></param>
        public static void BindCellToSerializedObject(HierarchyViewCell cell, SerializedObject so)
        {
            cell.Bind(so);
            cell.BoundObject = so;
        }

        /// <summary>
        /// Unbinds a Cell from its SerializedObject. This will also nullify the BoundObject member of the Cell.
        /// </summary>
        /// <param name="cell"></param>
        public static void UnbindCellFromSerializedObject(HierarchyViewCell cell)
        {
            cell.Unbind();
            if (cell.BoundObject is SerializedObject so)
            {
                so.Dispose();
            }
            cell.BoundObject = null;
        }

        /// <summary>
        /// Add a PropertyField to A Cell to edit a GameObject or Component.
        /// </summary>
        /// <param name="cell">HierarchyViewCell wrapping a GameObject</param>
        /// <param name="propertyPath">Property path used to find the SerializedProperty that will be edited by the PRopertyField.</param>
        /// <param name="componentType">If null, SerializedProperty will be on the GameObject. If not null try to access a property of the given Component Type if the GameObject has such a component.</param>
        /// <returns>Returns a PropertyField.</returns>
        public static PropertyField CreateGameObjectPropertyField(HierarchyViewCell cell, string propertyPath, System.Type componentType = null)
        {
            var go = GetGameObject(cell);
            SerializedObject so;
            if (componentType != null)
            {
                var comp = go.GetComponent(componentType);
                if (comp == null)
                {
                    return null;
                }
                so = new SerializedObject(comp);
            }
            else
            {
                so = new SerializedObject(go);
            }
            return CreatePropertyField(cell, so, propertyPath);
        }

        /// <summary>
        /// Add a PropertyField to A Cell to edit a SerializedObject
        /// </summary>
        /// <param name="cell">HierarchyViewCell wrapping a GameObject</param>
        /// <param name="so">SerializedObject to bind to the PropertyField.</param>
        /// <param name="propertyName">Property Name of the SerializedProperty to edit.</param>
        /// <returns>Returns a PropertyField.</returns>
        public static PropertyField CreatePropertyField(HierarchyViewCell cell, SerializedObject so, string propertyName)
        {
            var prop = so.FindProperty(propertyName);
            if (prop == null)
            {
                Debug.LogError($"Cannot find property {propertyName} for cell {cell.Descriptor.ColumnId} for Handler {cell.Descriptor.HandlerType}");
                return null;
            }

            var propField = new PropertyField(prop);
            propField.AddToClassList(k_CellPropField);
            propField.Bind(so);
            cell.IsDefaultValue = false;
            cell.Add(propField);
            return propField;
        }
    }
}
