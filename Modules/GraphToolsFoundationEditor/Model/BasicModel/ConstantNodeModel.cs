// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.GraphToolsFoundation.Editor
{
    [Serializable]
    [MovedFrom(false, "Unity.GraphToolsFoundation.Editor", "Unity.GraphTools.Foundation.Model")]
    sealed class ConstantNodeModel : NodeModel, ISingleOutputPortNodeModel, ICloneable
    {
        const string k_OutputPortId = "Output_0";

        [SerializeField]
        bool m_IsLocked;

        [SerializeReference]
        Constant m_Value;

        /// <inheritdoc />
        public override string Title => string.Empty;

        /// <inheritdoc />
        public PortModel OutputPort => OutputsById.Values.FirstOrDefault();

        /// <summary>
        /// The value of the node.
        /// </summary>
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
                GraphModel?.CurrentGraphChangeDescription?.AddChangedModel(this, ChangeHint.Data);
            }
        }

        /// <summary>
        /// The <see cref="Type"/> of the value.
        /// </summary>
        public Type Type => m_Value.Type;

        /// <summary>
        /// Whether the constant is locked or not.
        /// </summary>
        public bool IsLocked
        {
            get => m_IsLocked;
            set
            {
                if (m_IsLocked == value)
                    return;
                m_IsLocked = value;
                GraphModel?.CurrentGraphChangeDescription?.AddChangedModel(this, ChangeHint.Data);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConstantNodeModel"/> class.
        /// </summary>
        public ConstantNodeModel()
        {
            this.SetCapability(Editor.Capabilities.Colorable, false);
        }

        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns>A clone of this instance.</returns>
        public GraphElementModel Clone()
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
        protected override void OnDefineNode()
        {
            this.AddDataOutputPort(null, Value.GetTypeHandle(), k_OutputPortId);
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();

            if (m_Value != null)
                m_Value.OwnerModel = this;
        }

        /// <inheritdoc />
        public override void OnBeforeCopy()
        {
            (m_Value as ICopyPasteCallbackReceiver)?.OnBeforeCopy();
        }

        /// <inheritdoc />
        public override void OnAfterPaste()
        {
            (m_Value as ICopyPasteCallbackReceiver)?.OnAfterPaste();
        }
    }
}
