// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEngine.Assertions;
using UnityEngine.Pool;

[assembly:GeneratePropertyBagsForTypesQualifiedWith(typeof(UnityEngine.UIElements.IDataSourceViewHashProvider))]
[assembly:GeneratePropertyBagsForTypesQualifiedWith(typeof(UnityEngine.UIElements.INotifyBindablePropertyChanged))]

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Defines a binding property that serves as an identifier for the binding system.
    /// </summary>
    public readonly struct BindingId : IEquatable<BindingId>
    {
        /// <summary>
        /// Returns an invalid binding property.
        /// </summary>
        public static readonly BindingId Invalid = default;

        private readonly PropertyPath m_PropertyPath;
        private readonly string m_Path;

        /// <summary>
        /// Instantiate a new binding property.
        /// </summary>
        /// <param name="path">The path of the property.</param>
        public BindingId(string path)
        {
            m_PropertyPath = new PropertyPath(path);
            m_Path = path;
        }

        /// <summary>
        /// Instantiate a new binding property.
        /// </summary>
        /// <param name="path">The path of the property.</param>
        public BindingId(in PropertyPath path)
        {
            m_PropertyPath = path;
            m_Path = path.ToString();
        }

        /// <summary>
        /// Converts a <see cref="BindingId"/> to a <see cref="PropertyPath"/>.
        /// </summary>
        /// <param name="vep">The property.</param>
        /// <returns>A path for the property.</returns>
        public static implicit operator PropertyPath(in BindingId vep)
        {
            return vep.m_PropertyPath;
        }

        /// <summary>
        /// Converts a <see cref="BindingId"/> to a <see cref="string"/>.
        /// </summary>
        /// <param name="vep">The property.</param>
        /// <returns>A path for the property.</returns>
        public static implicit operator string(in BindingId vep)
        {
            return vep.m_Path;
        }

        /// <summary>
        /// Converts a <see cref="string"/> to a <see cref="BindingId"/>.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <returns>The property.</returns>
        public static implicit operator BindingId(string name)
        {
            return new BindingId(name);
        }

        /// <summary>
        /// Converts a <see cref="PropertyPath"/> to a <see cref="BindingId"/>.
        /// </summary>
        /// <param name="path">The path to the property.</param>
        /// <returns>The property.</returns>
        public static implicit operator BindingId(in PropertyPath path)
        {
            return new BindingId(path);
        }

        /// <summary>
        /// Returns the binding property as a string.
        /// </summary>
        /// <returns>The property path.</returns>
        public override string ToString()
        {
            return m_Path;
        }

        /// <summary>
        /// Indicates whether two binding properties are equal.
        /// </summary>
        /// <param name="other">The object to compare with the current instance.</param>
        /// <returns><see langword="true"/> if obj and this instance are the same type and represent the same value; otherwise, <see langword="false"/>.</returns>
        public bool Equals(BindingId other)
        {
            return m_PropertyPath == other.m_PropertyPath;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is BindingId other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return m_PropertyPath.GetHashCode();
        }

        /// <undoc/>
        public static bool operator ==(in BindingId lhs, in BindingId rhs)
        {
            return lhs.m_PropertyPath == rhs.m_PropertyPath;
        }

        /// <undoc/>
        public static bool operator !=(in BindingId lhs, in BindingId rhs)
        {
            return !(lhs == rhs);
        }
    }

    /// <summary>
    /// Sends an event when a value of a property changes.
    /// </summary>
    /// <remarks>
    /// This event does not bubble up or trickle down.
    /// </remarks>
    class PropertyChangedEvent : EventBase<PropertyChangedEvent>, IChangeEvent
    {
        static PropertyChangedEvent()
        {
            SetCreateFunction(() => new PropertyChangedEvent());
        }

        /// <summary>
        /// Assigns the property of a control that has changed.
        /// </summary>
        public BindingId property { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="PropertyChangedEvent"/>.
        /// </summary>
        public PropertyChangedEvent()
        {
            bubbles = false;
            tricklesDown = false;
        }

        /// <summary>
        /// Gets an event from the event pool and initializes it with the given property. Use this function instead of
        /// creating new events. Events obtained using this method need to be released back to the pool. You can use
        /// `Dispose()` to release them.
        /// </summary>
        /// <param name="property">The property that has changed.</param>
        /// <returns>A <see cref="PropertyChangedEvent"/>.</returns>
        public static PropertyChangedEvent GetPooled(in BindingId property)
        {
            var e = GetPooled();
            e.property = property;
            return e;
        }
    }

    /// <summary>
    /// Base class for objects that are part of the UIElements visual tree.
    /// </summary>
    /// <remarks>
    /// VisualElement contains several features that are common to all controls in UIElements, such as layout, styling and event handling.
    /// Several other classes derive from it to implement custom rendering and define behaviour for controls.
    /// </remarks>
    public partial class VisualElement
    {
        private object m_DataSource;
        private PropertyPath m_DataSourcePath;

        /// <summary>
        /// Assigns a data source to this VisualElement which overrides any inherited data source. This data source is
        /// inherited by all children.
        /// </summary>
        [CreateProperty]
        public object dataSource
        {
            get => m_DataSource;
            set
            {
                if (m_DataSource == value)
                    return;

                var previous = m_DataSource;
                m_DataSource = value;
                TrackSource(previous, m_DataSource);
                IncrementVersion(VersionChangeType.DataSource);
                NotifyPropertyChanged(dataSourceProperty);
            }
        }

        /// <summary>
        /// Path from the data source to the value.
        /// </summary>
        [CreateProperty]
        public PropertyPath dataSourcePath
        {
            get => m_DataSourcePath;
            set
            {
                if (m_DataSourcePath == value)
                    return;
                m_DataSourcePath = value;
                IncrementVersion(VersionChangeType.DataSource);
                NotifyPropertyChanged(dataSourcePathProperty);
            }
        }

        internal string dataSourcePathString
        {
            get => dataSourcePath.ToString();
            set => dataSourcePath = new PropertyPath(value);
        }

        // Used for uxml serialization authoring only.
        List<Binding> m_Bindings;
        List<Binding> bindings
        {
            get => m_Bindings ??= new List<Binding>();
            set => m_Bindings = value;
        }

        /// <summary>
        /// The possible type of data source assignable to this VisualElement.
        /// <remarks>
        /// This information is only used by the UI Builder as a hint to provide some completion to the data source path field when the effective data source cannot be specified at design time.
        /// </remarks>
        /// </summary>
        internal Type dataSourceType { get; set; }

        internal string dataSourceTypeString
        {
            get => UxmlUtility.TypeToString(dataSourceType);
            set => dataSourceType = UxmlUtility.ParseType(value);
        }

        /// <summary>
        /// Assigns a binding between a target and a source.
        /// </summary>
        /// <remarks>
        /// Passing a value of <see langword="null"/> for <see cref="binding"/> removes the binding.
        /// </remarks>
        /// <param name="bindingId">The binding ID.</param>
        /// <param name="binding">The binding object.</param>
        public void SetBinding(BindingId bindingId, Binding binding)
        {
            RegisterBinding(bindingId, binding);
        }

        /// <summary>
        /// Gets the binding instance for the provided targeted property.
        /// </summary>
        /// <param name="bindingId">The binding ID.</param>
        /// <returns>The binding instance, if it exists.</returns>
        public Binding GetBinding(BindingId bindingId)
        {
            return TryGetBinding(bindingId, out var binding) ? binding : null;
        }

        /// <summary>
        /// Gets the binding instance for the provided targeted property.
        /// </summary>
        /// <param name="bindingId">The binding ID.</param>
        /// <param name="binding">When this method returns, contains the binding associated with the target property, if it exists; otherwise contains <see langword="null"/></param>
        /// <returns><see langword="true"/> if the binding exists; <see langword="false"/> otherwise.</returns>
        public bool TryGetBinding(BindingId bindingId, out Binding binding)
        {
            if (DataBindingUtility.TryGetBinding(this, bindingId, out var bindingInfo))
            {
                binding = bindingInfo.binding;
                return true;
            }

            binding = null;
            return false;
        }

        /// <summary>
        /// Gets information on all the bindings of the current element.
        /// </summary>
        /// <returns>The bindings information.</returns>
        /// <remarks>The order in which the binding information is returned is undefined.</remarks>
        public IEnumerable<BindingInfo> GetBindingInfos()
        {
            using var pool = ListPool<BindingInfo>.Get(out var  bindingInfos);
            DataBindingUtility.GetBindingsForElement(this, bindingInfos);
            foreach (var bindingInfo in bindingInfos)
            {
                yield return bindingInfo;
            }
        }

        /// <summary>
        /// Allows to know if a target property has a binding associated to it.
        /// </summary>
        /// <param name="bindingId">The binding ID.</param>
        /// <returns><see langword="true"/> if the property has a binding; <see langword="false"/> otherwise.</returns>
        public bool HasBinding(BindingId bindingId)
        {
            return TryGetBinding(bindingId, out _);
        }

        /// <summary>
        /// Removes a binding from the element.
        /// </summary>
        /// <remarks>
        /// This is equivalent to calling <see cref="SetBinding"/> with a <see langword="null"/> value./>
        /// </remarks>
        /// <param name="bindingId">The id of the binding to unbind on this element.</param>
        public void ClearBinding(BindingId bindingId)
        {
            SetBinding(bindingId, null);
            bindings?.RemoveAll(b => b.property == bindingId);
        }

        /// <summary>
        /// Removes all bindings from the element.
        /// </summary>
        public void ClearBindings()
        {
            DataBindingManager.CreateClearAllBindingsRequest(this);
            bindings?.Clear();

            if (panel != null)
                ProcessBindingRequests();
        }

        /// <summary>
        /// Queries the <see cref="dataSource"/> and <see cref="dataSourcePath"/> inherited from the hierarchy.
        /// </summary>
        /// <returns>A context object with the hierarchical data source and data source path.</returns>
        public DataSourceContext GetHierarchicalDataSourceContext()
        {
            var current = this;
            var path = default(PropertyPath);

            while (null != current)
            {
                if (!current.dataSourcePath.IsEmpty)
                    path = PropertyPath.Combine(current.dataSourcePath, path);

                if (null != current.dataSource)
                {
                    var source = current.dataSource;
                    return new DataSourceContext(source, path);
                }
                current = current.hierarchy.parent;
            }

            return new DataSourceContext(null, path);
        }

        /// <summary>
        /// Queries the <see cref="dataSource"/> and <see cref="dataSourcePath"/> of a binding object.
        /// </summary>
        /// <param name="bindingId">The binding ID to query.</param>
        /// <returns>A context object with the data source and data source path of a binding object.</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public DataSourceContext GetDataSourceContext(BindingId bindingId)
        {
            if (TryGetDataSourceContext(bindingId, out var context))
                return context;

            throw new ArgumentOutOfRangeException(nameof(bindingId), $"[UI Toolkit] could not get binding with id '{bindingId}' on the element.");
        }

        /// <summary>
        /// Queries the <see cref="dataSource"/> and <see cref="dataSourcePath"/> of a binding object.
        /// </summary>
        /// <param name="bindingId">The binding ID to query.</param>
        /// <param name="context">The resulting context object.</param>
        /// <returns>Returns <see langword="true"/> if a binding with the provided id was registered on the element; <see langword="false"/> otherwise.</returns>
        public bool TryGetDataSourceContext(BindingId bindingId, out DataSourceContext context)
        {
            var binding = GetBinding(bindingId);
            switch (binding)
            {
                case null:
                {
                    context = default;
                    return false;
                }
                case IDataSourceProvider {dataSource: { }} provider:
                {
                    context = new DataSourceContext(provider.dataSource, provider.dataSourcePath);
                    break;
                }
                case IDataSourceProvider {dataSourcePath.IsEmpty: false} provider:
                {
                    var hierarchicalContext = GetHierarchicalDataSourceContext();
                    context = new DataSourceContext(
                        hierarchicalContext.dataSource,
                        PropertyPath.Combine(hierarchicalContext.dataSourcePath, provider.dataSourcePath)
                    );
                    break;
                }
                default:
                {
                    context = GetHierarchicalDataSourceContext();
                    break;
                }
            }

            return true;
        }

        void RegisterBinding(BindingId bindingId, Binding binding)
        {
            AddBindingRequest(bindingId, binding);

            if (panel != null)
                ProcessBindingRequests();
        }

        internal void AddBindingRequest(BindingId bindingId, Binding binding)
        {
            DataBindingManager.CreateBindingRequest(this, bindingId, binding);
        }

        void ProcessBindingRequests()
        {
            Assert.IsFalse(null == elementPanel, null);
            if (DataBindingManager.AnyPendingBindingRequests(this))
                IncrementVersion(VersionChangeType.BindingRegistration);
        }

        void CreateBindingRequests()
        {
            var p = elementPanel;
            Assert.IsFalse(null == p, null);
            p.dataBindingManager.TransferBindingRequests(this);
        }

        void TrackSource(object previous, object current)
        {
            elementPanel?.dataBindingManager.TrackDataSource(previous, current);
        }
    }
}
