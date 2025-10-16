// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolkit.Editor.ContextualMenuItems;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Class for a node that represents a constant value.
    /// </summary>
    [Serializable]
    [UnityRestricted]
    internal class ConstantNodeModel : NodeModel, ISingleOutputPortNodeModel, ICloneable
    {
        const string k_OutputPortId = "Output_0";

        [SerializeReference]
        Constant m_Value;

        /// <inheritdoc />
        public override string Title => string.Empty;

        /// <inheritdoc />
        public PortModel OutputPort => OutputsById.Values.FirstOrDefault();

        /// <summary>
        /// The value of the node.
        /// </summary>
        /// <remarks>Setter implementations should set the <see cref="ChangeHint.Data"/> change hints.</remarks>
        public Constant Value
        {
            get => m_Value;
            set
            {
                if (m_Value == value)
                    return;
                // Unregister ourselves as the owner of the old constant.
                if (m_Value != null)
                    m_Value.OwnerModel = null;
                m_Value = value;
                if (m_Value != null)
                    m_Value.OwnerModel = this;
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
            }
        }

        /// <summary>
        /// The <see cref="Type"/> of the value.
        /// </summary>
        /// <remarks>Setter implementations should set the <see cref="ChangeHint.Data"/> change hints.</remarks>
        public Type Type => m_Value.Type;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConstantNodeModel"/> class.
        /// </summary>
        public ConstantNodeModel()
        {
            SetCapability(Editor.Capabilities.Colorable, false);
        }

        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns>A clone of this instance.</returns>
        public Model Clone()
        {
            var clonedModel = CloneHelpers.CloneUsingScriptableObjectInstantiate(this);
            if (clonedModel.Value != null)
                clonedModel.Value.OwnerModel = clonedModel;
            return clonedModel;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{GetType().Name}: {Value.ObjectValue}";
        }

        /// <summary>
        /// Sets the value of the constant in a type-safe manner.
        /// </summary>
        /// <param name="value">The value to set.</param>
        /// <typeparam name="T">The type of the value.</typeparam>
        public void SetValue<T>(T value)
        {
            if (!(value is Enum) && Type != value.GetType() && !value.GetType().IsSubclassOf(Type))
                throw new ArgumentException($"can't set value of type {value.GetType().Name} in {Type.Name}");
            m_Value.ObjectValue = value;
        }

        /// <inheritdoc />
        protected override void OnDefineNode(NodeDefinitionScope scope)
        {
            scope.AddOutputPort(null, Value.GetTypeHandle(), PortType.Default, k_OutputPortId);
        }

        /// <inheritdoc />
        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();

            if (m_Value != null)
                m_Value.OwnerModel = this;
        }

        /// <inheritdoc />
        public override void OnBeforeCopy()
        {
            base.OnBeforeCopy();

            // ReSharper disable once SuspiciousTypeConversion.Global
            (m_Value as ICopyPasteCallbackReceiver)?.OnBeforeCopy();
        }

        /// <inheritdoc />
        public override void OnAfterCopy()
        {
            base.OnAfterCopy();

            // ReSharper disable once SuspiciousTypeConversion.Global
            (m_Value as ICopyPasteCallbackReceiver)?.OnAfterCopy();
        }

        /// <inheritdoc />
        public override void OnAfterPaste()
        {
            base.OnAfterPaste();

            // ReSharper disable once SuspiciousTypeConversion.Global
            (m_Value as ICopyPasteCallbackReceiver)?.OnAfterPaste();
        }

        /// <inheritdoc />
        public override IReadOnlyList<ContextualMenuItem> ContextualMenuItems
        {
            get
            {
                var menuItems = new List<ContextualMenuItem>(base.ContextualMenuItems);
                menuItems.AddRange(k_ContextualMenuItems);
                return menuItems;
            }
        }

        static readonly List<ContextualMenuItem> k_ContextualMenuItems =  new() {
            ContextualMenuHelpers.convertToVariableItem,
            new ContextualMenuItem(ContextualMenuHelpers.itemizeItem, 0),
        };
    }
}
