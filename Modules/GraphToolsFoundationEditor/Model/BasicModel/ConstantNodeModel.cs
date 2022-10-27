// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.GraphToolsFoundation.Editor
{
    [Serializable]
    [MovedFrom(false, "Unity.GraphToolsFoundation.Editor", "Unity.GraphTools.Foundation.Model")]
    sealed class ConstantNodeModel : NodeModel, ISingleOutputPortNodeModel
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
            set => m_Value = value;
        }

        /// <summary>
        /// The value, as an <see cref="object"/>.
        /// </summary>
        public object ObjectValue
        {
            get => m_Value.ObjectValue;
            set => m_Value.ObjectValue = value;
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
            set => m_IsLocked = value;
        }

        public ConstantNodeModel()
        {
            this.SetCapability(Editor.Capabilities.Colorable, false);
        }

        /// <summary>
        /// Initializes the node.
        /// </summary>
        /// <param name="constantTypeHandle">The type of value held by the node.</param>
        public void Initialize(TypeHandle constantTypeHandle) => m_Value.Initialize(constantTypeHandle);

        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns>A clone of this instance.</returns>
        public ConstantNodeModel Clone()
        {
            if (GetType() == typeof(ConstantNodeModel))
            {
                return new ConstantNodeModel { Value = Value.Clone() };
            }
            var clone = Activator.CreateInstance(GetType());
            EditorUtility.CopySerializedManagedFieldsOnly(this, clone);
            return (ConstantNodeModel)clone;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{GetType().Name}: {ObjectValue}";
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
    }
}
