// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor.Implementation;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    public abstract partial class Node
    {
        [NonSerialized]
        internal NodeModel m_Implementation;

        NodeModel INode.NodeModel => GetImplementation();


        internal NodeModel GetImplementation()
        {
            if (m_Implementation == null)
            {
                CreateImplementation();
            }

            return m_Implementation;
        }

        internal virtual void CreateImplementation()
        {
            (new UserNodeModelImp()).InitCustomNode(this);
        }

        internal class PortDefinitionContext : IPortDefinitionContext
        {
            class PortBuilder :
                IOutputPortBuilder,
                IInputPortBuilder,
                ITypedInputPortBuilder,
                ITypedOutputPortBuilder
            {
                PortDefinitionContext m_PortsDefinitionContext;

                string m_PortName;
                PortDirection m_Direction;

                string m_DisplayName;
                string m_Tooltip;
                PortOrientation m_Orientation;
                PortConnectorUI? m_ConnectorUI;
                List<Attribute> m_Attributes = new();

                internal Type m_PortType;
                internal object m_DefaultValue;

                internal object m_TypedBuilder;

                internal void Reset()
                {
                    m_PortName = null;
                    m_Direction = PortDirection.None;

                    m_DisplayName = null;
                    m_Tooltip = null;
                    m_PortType = null;
                    m_DefaultValue = null;
                    m_Orientation = PortOrientation.Horizontal;
                    m_ConnectorUI = null;
                    m_Attributes.Clear();
                    m_TypedBuilder = null;
                }

                internal IInputPortBuilder AddInputPort(PortDefinitionContext portsDefinition, string portName)
                {
                    m_PortsDefinitionContext = portsDefinition;
                    m_PortName = portName;
                    m_Direction = PortDirection.Input;
                    return this;
                }

                internal IOutputPortBuilder AddOutputPort(PortDefinitionContext portsDefinition, string portName)
                {
                    m_PortsDefinitionContext = portsDefinition;
                    m_PortName = portName;
                    m_Direction = PortDirection.Output;
                    return this;
                }

                IOutputPortBuilder IPortBuilder<IOutputPortBuilder>.WithDisplayName(string displayName) => WithDisplayName(displayName);
                ITypedOutputPortBuilder IPortBuilder<ITypedOutputPortBuilder>.WithDisplayName(string displayName) => WithDisplayName(displayName);
                IInputPortBuilder IPortBuilder<IInputPortBuilder>.WithDisplayName(string displayName) => WithDisplayName(displayName);
                ITypedInputPortBuilder IPortBuilder<ITypedInputPortBuilder>.WithDisplayName(string displayName) => WithDisplayName(displayName);

                IOutputPortBuilder IPortBuilder<IOutputPortBuilder>.WithTooltip(string tooltip) => WithTooltip(tooltip);
                ITypedOutputPortBuilder IPortBuilder<ITypedOutputPortBuilder>.WithTooltip(string tooltip) => WithTooltip(tooltip);
                IInputPortBuilder IPortBuilder<IInputPortBuilder>.WithTooltip(string tooltip) => WithTooltip(tooltip);
                ITypedInputPortBuilder IPortBuilder<ITypedInputPortBuilder>.WithTooltip(string tooltip) => WithTooltip(tooltip);

                IOutputPortBuilder IPortBuilder<IOutputPortBuilder>.WithConnectorUI(PortConnectorUI connectorUI) => WithConnectorUI(connectorUI);
                ITypedOutputPortBuilder IPortBuilder<ITypedOutputPortBuilder>.WithConnectorUI(PortConnectorUI connectorUI) => WithConnectorUI(connectorUI);
                IInputPortBuilder IPortBuilder<IInputPortBuilder>.WithConnectorUI(PortConnectorUI connectorUI) => WithConnectorUI(connectorUI);
                ITypedInputPortBuilder IPortBuilder<ITypedInputPortBuilder>.WithConnectorUI(PortConnectorUI connectorUI) => WithConnectorUI(connectorUI);

                IInputPortBuilder IInputBasePortBuilder<IInputPortBuilder>.Delayed() => Delayed();
                ITypedInputPortBuilder IInputBasePortBuilder<ITypedInputPortBuilder>.Delayed() => Delayed();
                IInputPortBuilder IInputBasePortBuilder<IInputPortBuilder>.AsTextArea(int minLines, int maxLines) => AsTextArea(minLines, maxLines);
                ITypedInputPortBuilder IInputBasePortBuilder<ITypedInputPortBuilder>.AsTextArea(int minLines, int maxLines) => AsTextArea(minLines, maxLines);

                IOutputPortBuilder IOutputPortBuilder.WithDataType(Type portType) => WithDataType(portType);
                IOutputPortBuilder<T> IOutputPortBuilder.WithDataType<T>() => WithDataType<T>();

                ITypedInputPortBuilder IInputPortBuilder.WithDataType(Type portType) => WithDataType(portType);

                IInputPortBuilder<T> IInputPortBuilder.WithDataType<T>() => WithDataType<T>();

                ITypedInputPortBuilder ITypedInputPortBuilder.WithDefaultValue(object defaultValue) => WithDefaultValue(defaultValue);

                IOutputPortBuilder IPortBuilder<IOutputPortBuilder>.AsVertical() => AsVertical();
                ITypedOutputPortBuilder IPortBuilder<ITypedOutputPortBuilder>.AsVertical() => AsVertical();
                IInputPortBuilder IPortBuilder<IInputPortBuilder>.AsVertical() => AsVertical();
                ITypedInputPortBuilder IPortBuilder<ITypedInputPortBuilder>.AsVertical() => AsVertical();

                public PortBuilder WithDisplayName(string displayName)
                {
                    this.m_DisplayName = displayName;
                    return this;
                }

                public PortBuilder WithTooltip(string tooltip)
                {
                    this.m_Tooltip = tooltip;
                    return this;
                }

                PortBuilder WithDataType(Type portType)
                {
                    this.m_PortType = portType;

                    return this;
                }

                PortBuilder<T> WithDataType<T>()
                {
                    WithDataType(typeof(T));
                    return m_PortsDefinitionContext.GetFreeTypedBuilder<T>(this);
                }

                PortBuilder WithDefaultValue(object defaultValue)
                {
                    if (m_PortType != null && defaultValue != null && !m_PortType.IsAssignableFrom(defaultValue.GetType()))
                    {
                        throw new ArgumentException($"Default value type {defaultValue} is not assignable to port type {m_PortType}");
                    }
                    this.m_DefaultValue = defaultValue;
                    return this;
                }

                public PortBuilder AsVertical()
                {
                    m_Orientation = PortOrientation.Vertical;
                    return this;
                }

                public PortBuilder WithConnectorUI(PortConnectorUI connectorUI)
                {
                    m_ConnectorUI = connectorUI;
                    return this;
                }

                public PortBuilder Delayed()
                {
                    if (!m_Attributes.HasAny(t => t is DelayedAttribute))
                        m_Attributes.Add(new DelayedAttribute());
                    return this;
                }

                public PortBuilder AsTextArea(int minLines, int maxLines)
                {
                    if (!m_Attributes.HasAny(t => t is TextAreaAttribute))
                        m_Attributes.Add(new TextAreaAttribute(minLines, maxLines));
                    return this;
                }

                public IPort Build()
                {
                    IPort result;

                    var attributesArray = m_Attributes.Count > 0 ? m_Attributes.ToArray() : null;
                    if (m_Direction == PortDirection.Input)
                        result = m_PortsDefinitionContext.portsDefinition.AddInputPort(m_DisplayName ?? m_PortName, m_PortType, m_PortName, m_Orientation, attributesArray, m_DefaultValue);
                    else
                        result = m_PortsDefinitionContext.portsDefinition.AddOutputPort(m_DisplayName ?? m_PortName, m_PortType, m_PortName, m_Orientation, attributesArray);

                    if (result is PortModelImp portModel)
                    {
                        if (m_Tooltip != null)
                            portModel.ToolTip = m_Tooltip;

                        // Use the arrowhead UI for untyped ports by default
                        if (m_ConnectorUI != null)
                            portModel.ConnectorUI = m_ConnectorUI.Value;
                        else
                            portModel.ConnectorUI = m_PortType == null ? PortConnectorUI.Arrowhead : PortConnectorUI.Circle;
                    }
                    m_PortsDefinitionContext.ReleaseBuilder(this);

                    return result;
                }
            }

            class PortBuilder<TData> : IOutputPortBuilder<TData>, IInputPortBuilder<TData>
            {
                public PortBuilder parent;

                IOutputPortBuilder<TData> IPortBuilder<IOutputPortBuilder<TData>>.WithDisplayName(string displayName) => WithDisplayName(displayName);
                IInputPortBuilder<TData> IPortBuilder<IInputPortBuilder<TData>>.WithDisplayName(string displayName) => WithDisplayName(displayName);

                IOutputPortBuilder<TData> IPortBuilder<IOutputPortBuilder<TData>>.WithTooltip(string tooltip) => WithTooltip(tooltip);
                IInputPortBuilder<TData> IPortBuilder<IInputPortBuilder<TData>>.WithTooltip(string tooltip) => WithTooltip(tooltip);

                IOutputPortBuilder<TData> IPortBuilder<IOutputPortBuilder<TData>>.WithConnectorUI(PortConnectorUI connectorUI) => WithConnectorUI(connectorUI);
                IInputPortBuilder<TData> IPortBuilder<IInputPortBuilder<TData>>.WithConnectorUI(PortConnectorUI connectorUI) => WithConnectorUI(connectorUI);

                IInputPortBuilder<TData> IInputBasePortBuilder<IInputPortBuilder<TData>>.Delayed() => Delayed();
                IInputPortBuilder<TData> IInputBasePortBuilder<IInputPortBuilder<TData>>.AsTextArea(int minLines, int maxLines) => AsTextArea(minLines, maxLines);

                IInputPortBuilder<TData> IInputPortBuilder<TData>.WithDefaultValue(TData defaultValue) => WithDefaultValue(defaultValue);

                IOutputPortBuilder<TData> IPortBuilder<IOutputPortBuilder<TData>>.AsVertical() => AsVertical();
                IInputPortBuilder<TData> IPortBuilder<IInputPortBuilder<TData>>.AsVertical() => AsVertical();

                PortBuilder<TData> WithDisplayName(string displayName)
                {
                    parent.WithDisplayName(displayName);
                    return this;
                }

                PortBuilder<TData> WithTooltip(string tooltip)
                {
                    parent.WithTooltip(tooltip);
                    return this;
                }

                PortBuilder<TData> AsVertical()
                {
                    parent.AsVertical();
                    return this;
                }

                PortBuilder<TData> WithConnectorUI(PortConnectorUI connectorUI)
                {
                    parent.WithConnectorUI(connectorUI);
                    return this;
                }

                PortBuilder<TData> Delayed()
                {
                    parent.Delayed();
                    return this;
                }

                PortBuilder<TData> AsTextArea(int minLines, int maxLines)
                {
                    parent.AsTextArea(minLines, maxLines);
                    return this;
                }


                PortBuilder<TData> WithDefaultValue(TData defaultValue)
                {
                    parent.m_DefaultValue = defaultValue;
                    return this;
                }

                public IPort Build()
                {
                    return parent.Build();
                }
            }

            public IPortsDefinition portsDefinition;

            List<PortBuilder> m_PortBuilderPool = new();

            Dictionary<Type, List<object>> m_TypedPortBuilderPools = new();

            List<PortBuilder> m_UsedPortBuilder = new();

            PortBuilder GetFreeBuilder()
            {
                PortBuilder builder;
                if (m_PortBuilderPool.Count > 0)
                {
                    builder = m_PortBuilderPool[^1];
                    m_PortBuilderPool.RemoveAt(m_PortBuilderPool.Count - 1);
                }
                else
                {
                    builder = new PortBuilder(); //TODO : pool
                }
                m_UsedPortBuilder.Add(builder);
                return builder;
            }

            void ReleaseBuilder(PortBuilder builder)
            {
                if (builder == null)
                    return;

                if (builder.m_TypedBuilder != null)
                {
                    ReleaseTypedBuilder(builder.m_PortType, builder.m_TypedBuilder);
                }

                if (m_UsedPortBuilder.Remove(builder))
                {
                    m_PortBuilderPool.Add(builder);
                    builder.Reset();
                }
            }

            PortBuilder<T> GetFreeTypedBuilder<T>(PortBuilder parent)
            {
                PortBuilder<T> result;
                if (!m_TypedPortBuilderPools.TryGetValue(typeof(T), out var builderPool) || builderPool.Count == 0)
                {
                    builderPool = new List<object>();
                    m_TypedPortBuilderPools[typeof(T)] = builderPool;

                    result = new PortBuilder<T>();
                }
                else
                {
                    result = (PortBuilder<T>)builderPool[^1];
                    builderPool.RemoveAt(builderPool.Count - 1);
                }
                parent.m_TypedBuilder = result;
                result.parent = parent;
                return result;
            }

            void ReleaseTypedBuilder(Type type, object typedBuilder)
            {
                m_TypedPortBuilderPools[type].Add(typedBuilder);
            }

            public IInputPortBuilder AddInputPort(string portName)
            {
                return GetFreeBuilder().AddInputPort(this, portName);
            }
            public IOutputPortBuilder AddOutputPort(string portName)
            {
                return GetFreeBuilder().AddOutputPort(this, portName);
            }

            public void Finish()
            {
                while (m_UsedPortBuilder.Count > 0)
                {
                    m_UsedPortBuilder[0].Build();
                }
            }
        }

        internal class OptionDefinitionContext : IOptionDefinitionContext
        {
            class OptionBuilder : IOptionBuilder
            {
                OptionDefinitionContext m_OptionsDefinitionContext;

                string m_OptionName;
                string m_DisplayName;
                string m_Tooltip;
                int m_Order;
                List<Attribute> m_Attributes = new();
                bool m_ShowInInspectorOnly;
                object m_DefaultValue;

                internal Type m_OptionType;
                internal object m_TypedBuilder;

                internal void Reset()
                {
                    m_OptionName = null;
                    m_DisplayName = null;
                    m_Tooltip = null;
                    m_Order = 0;
                    m_Attributes.Clear();
                    m_OptionType = null;
                    m_ShowInInspectorOnly = false;
                    m_DefaultValue = null;
                    m_TypedBuilder = null;
                }

                internal IOptionBuilder AddOption(OptionDefinitionContext optionsDefinition, string optionName, Type dataType)
                {
                    m_OptionsDefinitionContext = optionsDefinition;
                    m_OptionName = optionName;
                    m_OptionType = dataType;
                    return this;
                }

                public IOptionBuilder WithDisplayName(string displayName)
                {
                    this.m_DisplayName = displayName;
                    return this;
                }

                public IOptionBuilder WithTooltip(string tooltip)
                {
                    this.m_Tooltip = tooltip;
                    return this;
                }

                public IOptionBuilder WithDefaultValue(object defaultValue)
                {
                    if (defaultValue != null && !m_OptionType.IsInstanceOfType(defaultValue))
                    {
                        throw new ArgumentException($"Default value type {defaultValue} is not assignable to option type {m_OptionType}");
                    }
                    this.m_DefaultValue = defaultValue;
                    return this;
                }

                public IOptionBuilder Delayed()
                {
                    if (!m_Attributes.HasAny(t => t is DelayedAttribute))
                        m_Attributes.Add(new DelayedAttribute());
                    return this;
                }

                public IOptionBuilder AsTextArea(int minLines, int maxLines)
                {
                    if (!m_Attributes.HasAny(t => t is TextAreaAttribute))
                        m_Attributes.Add(new TextAreaAttribute(minLines, maxLines));
                    return this;
                }

                public IOptionBuilder ShowInInspectorOnly()
                {
                    m_ShowInInspectorOnly = true;
                    return this;
                }

                public INodeOption Build()
                {
                    var attributesArray = m_Attributes.Count > 0 ? m_Attributes.ToArray() : null;
                    var result = m_OptionsDefinitionContext.OptionsDefinition.AddNodeOption(m_OptionName, m_OptionType,
                        m_DisplayName, m_Tooltip, m_ShowInInspectorOnly, m_Order, attributesArray, m_DefaultValue);
                    m_OptionsDefinitionContext.ReleaseBuilder(this);

                    return result;
                }
            }

            class OptionBuilder<TData> : IOptionBuilder<TData>
            {
                public OptionBuilder parent;

                internal void Reset() => parent.Reset();

                internal IOptionBuilder<TData> AddOption(OptionDefinitionContext optionsDefinition, string optionName)
                {
                    parent.AddOption(optionsDefinition, optionName, typeof(TData));
                    return this;
                }

                public IOptionBuilder<TData> WithDisplayName(string displayName)
                {
                    parent.WithDisplayName(displayName);
                    return this;
                }

                public IOptionBuilder<TData> WithTooltip(string tooltip)
                {
                    parent.WithTooltip(tooltip);
                    return this;
                }

                public IOptionBuilder<TData> WithDefaultValue(TData defaultValue)
                {
                    parent.WithDefaultValue(defaultValue);
                    return this;
                }

                public IOptionBuilder<TData> Delayed()
                {
                    parent.Delayed();
                    return this;
                }

                public IOptionBuilder<TData> AsTextArea(int minLines, int maxLines)
                {
                    parent.AsTextArea(minLines, maxLines);
                    return this;
                }

                public IOptionBuilder<TData> ShowInInspectorOnly()
                {
                    parent.ShowInInspectorOnly();
                    return this;
                }

                public INodeOption Build() => parent.Build();
            }

            public IOptionsDefinition OptionsDefinition;

            List<OptionBuilder> m_Pool = new();
            List<OptionBuilder> m_Used = new();
            Dictionary<Type, List<object>> m_TypedBuilderPools = new();

            OptionBuilder GetFreeBuilder()
            {
                OptionBuilder builder;
                if (m_Pool.Count > 0)
                {
                    builder = m_Pool[^1];
                    m_Pool.RemoveAt(m_Pool.Count - 1);
                }
                else
                {
                    builder = new OptionBuilder(); //TODO : pool
                }
                m_Used.Add(builder);
                return builder;
            }

            void ReleaseBuilder(OptionBuilder builder)
            {
                if (builder == null)
                    return;

                if (builder.m_TypedBuilder != null)
                {
                    ReleaseTypedBuilder(builder.m_OptionType, builder.m_TypedBuilder);
                }

                if (m_Used.Remove(builder))
                {
                    m_Pool.Add(builder);
                    builder.Reset();
                }
            }

            OptionBuilder<T> GetFreeTypedBuilder<T>(OptionBuilder parent)
            {
                OptionBuilder<T> result;
                if (!m_TypedBuilderPools.TryGetValue(typeof(T), out var builderPool) || builderPool.Count == 0)
                {
                    builderPool = new List<object>();
                    m_TypedBuilderPools[typeof(T)] = builderPool;

                    result = new OptionBuilder<T>();
                }
                else
                {
                    result = (OptionBuilder<T>)builderPool[^1];
                    builderPool.RemoveAt(builderPool.Count - 1);
                }
                parent.m_TypedBuilder = result;
                result.parent = parent;
                return result;
            }

            void ReleaseTypedBuilder(Type type, object typedBuilder)
            {
                m_TypedBuilderPools[type].Add(typedBuilder);
            }

            public IOptionBuilder AddOption(string name, Type dataType)
            {
                return GetFreeBuilder().AddOption(this, name, dataType);
            }

            public IOptionBuilder<T> AddOption<T>(string name)
            {
                return GetFreeTypedBuilder<T>(GetFreeBuilder()).AddOption(this, name);
            }

            public void Finish()
            {
                while (m_Used.Count > 0)
                {
                    m_Used[0].Build();
                }
            }
        }

        static PortDefinitionContext s_PortDefinitionContext = new();
        static OptionDefinitionContext s_OptionDefinitionContext = new();

        internal void CallOnDefineNode(IPortsDefinition context)
        {
            s_PortDefinitionContext.portsDefinition = context;
            OnDefinePorts(s_PortDefinitionContext);
            s_PortDefinitionContext.Finish();
        }

        internal void CallOnDefineOptions(IOptionsDefinition context)
        {
            s_OptionDefinitionContext.OptionsDefinition = context;
            OnDefineOptions(s_OptionDefinitionContext);
            s_OptionDefinitionContext.Finish();
        }

        internal void SetImplementation(NodeModel implementation)
        {
            m_Implementation = implementation;
        }
    }
}
