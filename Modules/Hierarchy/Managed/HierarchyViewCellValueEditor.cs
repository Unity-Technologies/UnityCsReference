// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.Hierarchy
{
    /// <summary>
    /// Class providing utility functions to set/get Model values on a UITk Control (i.e editor) in a HierarchyViewCell.
    /// </summary>
    [VisibleToOtherModules]
    internal sealed class HierarchyViewCellValueEditor<TModel, TEditor, TValue> where TEditor : VisualElement, INotifyValueChanged<TValue>, new()
    {
        readonly Func<HierarchyViewCellValueEditor<TModel, TEditor, TValue>, TValue> m_GetModelValue;
        readonly Action<HierarchyViewCellValueEditor<TModel, TEditor, TValue>, TValue> m_SetModelValue;
        readonly Func<HierarchyViewCellValueEditor<TModel, TEditor, TValue>, TValue, bool> m_IsDefaultValue;
        readonly Action<HierarchyViewCellValueEditor<TModel, TEditor, TValue>, TValue> m_OnSetEditorValue;

        EventModifiers m_LastEventModifiers;

        /// <summary>
        /// Gets whether the Alt/Option key was pressed during the last pointer down event on this editor element.
        /// This is typically used to modify behavior, such as applying changes only to the parent object without affecting children.
        /// </summary>
        public bool AltKeyPressed => m_LastEventModifiers.HasFlag(EventModifiers.Alt);

        /// <summary>
        /// Model associated with this Editor (ex: GameObject, Scene...)
        /// </summary>
        public TModel Model { get; private set; }

        /// <summary>
        /// Cell associated with this Editor.
        /// </summary>
        public HierarchyViewCell Cell { get; private set; }

        /// <summary>
        /// Actual VisualElement used to edit the Cell value.
        /// </summary>
        public TEditor Element;

        /// <summary>
        /// Create a new CellValueEditor.
        /// A cell used to edit a Model value that is considered to be Default won't be displayed.
        /// </summary>
        /// <param name="getModelValue">Getter to access the Model value of a cell</param>
        /// <param name="setModelValue">Setter to udpate the Model value</param>
        /// <param name="isDefaultValue">Is the current Model value default. If so, the cell content won't be displayed.</param>
        /// <param name="onSetEditorValue">Optional callback that gets trigger when the Editor value is set after a model change.</param>
        public HierarchyViewCellValueEditor(
            Func<HierarchyViewCellValueEditor<TModel, TEditor, TValue>, TValue> getModelValue,
            Action<HierarchyViewCellValueEditor<TModel, TEditor, TValue>, TValue> setModelValue,
            Func<HierarchyViewCellValueEditor<TModel, TEditor, TValue>, TValue, bool> isDefaultValue,
            Action<HierarchyViewCellValueEditor<TModel, TEditor, TValue>, TValue> onSetEditorValue = null)
        {
            m_GetModelValue = getModelValue;
            m_SetModelValue = setModelValue;
            m_IsDefaultValue = isDefaultValue;
            m_OnSetEditorValue = onSetEditorValue;
        }

        /// <summary>
        /// Bind (i.e Add) the editor and the model to the Cell.
        /// </summary>
        /// <param name="model">Model being edited using an editor.</param>
        /// <param name="cell">Cell that will contain the editor.</param>
        /// <param name="editor">VisualElement used to editor the model values.</param>
        public void Bind(TModel model, HierarchyViewCell cell, TEditor editor)
        {
            Model = model;
            Cell = cell;
            Cell.userData = this;
            Element = editor;
            Element.visible = true;
            Element.RegisterCallback<PointerDownEvent>(OnPointerDown);
            Element.RegisterCallback<PointerUpEvent>(OnPointerUp);
            Element.RegisterCallback<ChangeEvent<TValue>>(SetModelValue);
            SyncEditorValueWithoutNotify();
        }

        /// <summary>
        /// Remove the UITk Editor from the Cell.
        /// </summary>
        public void Unbind()
        {
            Cell.userData = null;
            Cell = null;
            Element.visible = false;
            Element.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            Element.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            Element.UnregisterCallback<ChangeEvent<TValue>>(SetModelValue);
            Element = null;
        }

        /// <summary>
        /// Get the Model value of the Cell.
        /// </summary>
        /// <returns>Returns the Model value.</returns>
        public TValue GetModelValue()
        {
            return m_GetModelValue(this);
        }

        /// <summary>
        /// Set the Model value. In response to the VisualElement editor ValueChanged.
        /// </summary>
        /// <param name="value">New model value.</param>
        public void SetModelValue(TValue value)
        {
            if (Cell == null)
                return;

            if (!GetModelValue().Equals(value))
            {
                m_SetModelValue(this, value);
            }
            Cell.IsDefaultValue = IsModelDefaultValue();
        }

        /// <summary>
        /// Get the editor value. Should be synced with the Model value.
        /// </summary>
        /// <returns>Returns the value of the UITk editor.</returns>
        public TValue GetEditorValue()
        {
            return Element.value;
        }

        /// <summary>
        /// Callback that can be automatically connected to INotifyValueChanged that wil set the Model value.
        /// </summary>
        /// <param name="evt">On changed event.</param>
        public void SetModelValue(ChangeEvent<TValue> evt)
        {
            SetModelValue(evt.newValue);
        }

        /// <summary>
        /// Set the editor value without triggering any ValueChanged event.
        /// </summary>
        /// <param name="value">New value to apply to the editor.</param>
        public void SetEditorValueWithoutNotify(TValue value)
        {
            if (!value.Equals(Element.value))
            {
                Element.SetValueWithoutNotify(value);
            }
            m_OnSetEditorValue?.Invoke(this, value);
            Cell.IsDefaultValue = IsModelDefaultValue();
        }

        /// <summary>
        /// Sync the editor value according to the Model value.
        /// </summary>
        public void SyncEditorValueWithoutNotify()
        {
            SetEditorValueWithoutNotify(GetModelValue());
        }

        /// <summary>
        /// Is Model value a default value. By default any editor that is not displaying a default value will be hidden.
        /// </summary>
        /// <returns>Returns true if the model value is default.</returns>
        public bool IsModelDefaultValue()
        {
            return m_IsDefaultValue(this, GetModelValue());
        }

        void OnPointerDown(PointerDownEvent evt)
        {
            m_LastEventModifiers = evt.modifiers;

            // For Toggle controls with Alt key, manually trigger the value change because Alt key prevents the normal ChangeEvent from firing
            if (!evt.modifiers.HasFlag(EventModifiers.Alt) || Element is not Toggle toggle)
                return;

            evt.StopPropagation();
            var newValue = !toggle.value;
            toggle.value = newValue;
        }

        void OnPointerUp(PointerUpEvent evt)
        {
            // We stop propagation to avoid triggering selection from this code path
            if (m_LastEventModifiers.HasFlag(EventModifiers.Alt))
            {
                evt.StopPropagation();
            }
            m_LastEventModifiers = EventModifiers.None;
        }
    }
}
