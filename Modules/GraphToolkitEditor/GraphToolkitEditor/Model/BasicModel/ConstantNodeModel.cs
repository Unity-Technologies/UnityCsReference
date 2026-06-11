// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
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


        CollectionOptionsHandler m_CollectionOptions;

        CollectionOptionsHandler collectionOptions
        {
            get
            {
                if (m_CollectionOptions == null)
                    m_CollectionOptions = new CollectionOptionsHandler(this);
                return m_CollectionOptions;
            }
        }

        /// <inheritdoc />
        public override string Title => string.Empty;

        /// <inheritdoc />
        #pragma warning disable UA2011 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
        public PortModel OutputPort => OutputsById.Values.FirstOrDefault();
#pragma warning restore UA2011

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

                if (m_Value.Type.IsListOrArray())
                    SetCapability(Editor.Capabilities.Collapsible, true);

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
            bool isCollection = m_Value.Type.IsListOrArray();

            if (isCollection)
                collectionOptions.OnDefineNode(scope);

            scope.AddOutputPort(isCollection? "Out" : null, Value.GetTypeHandle(), PortType.Default, k_OutputPortId);
        }

        public virtual void OnDefineSubPorts(ISubPortDefinition subPortsDefinition, PortModel port)
        {
            if (port.DataTypeHandle.Resolve().IsListOrArray())
                collectionOptions.OnDefineSubports(subPortsDefinition, port);
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

    /// <summary>
    /// Handles collection settings for a ConstantNodeModel.
    /// </summary>
    class CollectionOptionsHandler
    {
        // Collection size limits.
        const int k_MinCollectionSize = 0;
        const int k_MaxCollectionSize = 50;

        ConstantNodeModel m_Node;
        NodeOption m_Size;

        internal CollectionOptionsHandler(ConstantNodeModel node)
        {
            m_Node = node;
        }

        internal void OnDefineNode(NodeModel.NodeDefinitionScope scope)
        {
            m_Size = scope.AddNodeOption("Size",
                TypeHandle.Int,
                optionId: "collectionSizeOption",
                attributes: [new DelayedAttribute(), new RangeAttribute(k_MinCollectionSize, k_MaxCollectionSize)],
                setterAction: OnSizeOptionChanged);

            var input = scope.AddInputPort("Collection",
                TypeHandleHelpers.GenerateTypeHandle(m_Node.Value.Type),
                PortType.Default,
                portId: "collectionReference",
                options: PortModelOptions.Hidden,
                initializationCallback: InitializeSizeOption);

            input.SetExpandable(true);
            input.Capacity = PortCapacity.None;

            // Only expand if the collection actually has items
            var collection = m_Node.Value.ObjectValue as IList;
            m_Node.SetPortExpanded(input, collection != null && collection.Count > 0);
        }

        internal void OnDefineSubports(ISubPortDefinition subPortsDefinition, PortModel port)
        {
            var type = port.DataTypeHandle.Resolve();
            if (!type.IsListOrArray())
                return;

            var collection = m_Node.Value.ObjectValue as IList;
            int collectionSize = Math.Min(collection?.Count ?? 0, k_MaxCollectionSize);

            // Keep the Size option widget in sync with reality
            m_Size.PortModel.EmbeddedValue.TrySetValue<int>(collectionSize);

            Type elementType = type.IsArray ? type.GetElementType() : type.GetGenericArguments()[0];
            TypeHandle elementTH = TypeHandleHelpers.GenerateTypeHandle(elementType);

            for (int i = 0; i < collectionSize; ++i)
            {
                int index = i;
                var subPort = subPortsDefinition.AddInputSubPort($"{i}", elementTH,
                    () =>
                    {
                        var col = m_Node.Value.ObjectValue as IList;
                        return col != null && index < col.Count ? col[index] : null;
                    },
                    (o) =>
                    {
                        if (m_Node.Value.TrySetValueAt(index, o))
                            m_Node.GraphModel?.CurrentGraphChangeDescription.AddChangedModel(m_Node, ChangeHint.Data);
                    },
                    $"{i}", attributes: new[] { new DelayedAttribute() });
                subPort.Capacity = PortCapacity.None;
            }
        }

        void InitializeSizeOption(Constant constant)
        {
            m_Size.PortModel.EmbeddedValue.TrySetValue<int>((constant.ObjectValue as IList).Count);
        }

        void OnSizeOptionChanged(object size)
        {
            int asInt = (int)size;

            var newValue = Math.Clamp(asInt, k_MinCollectionSize, k_MaxCollectionSize);
            if (asInt != newValue)
            {
                // Set the new clamped value back to the option so OnDefineSubports() uses the correct size.
                m_Size.PortModel.EmbeddedValue.ObjectValue = newValue;
            }

            m_Node.Value.ResizeCollection(newValue);
            m_Node.DefineNode();

            m_Node.GraphModel.CurrentGraphChangeDescription.AddChangedModel(m_Node, ChangeHint.Data);
        }
    }
}
