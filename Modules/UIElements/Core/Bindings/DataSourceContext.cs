// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using Unity.Properties;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Contains information about the data source and data source path of a binding.
    /// </summary>
    public readonly struct DataSourceContext
    {
        /// <summary>
        /// The resolved data source.
        /// </summary>
        public object dataSource { get; }

        /// <summary>
        /// The resolved data source path.
        /// </summary>
        public PropertyPath dataSourcePath { get; }

        /// <summary>
        /// Creates a new instance of a <see cref="DataSourceContext"/>.
        /// </summary>
        /// <param name="dataSource">The data source.</param>
        /// <param name="dataSourcePath">The data source path.</param>
        public DataSourceContext(object dataSource, in PropertyPath dataSourcePath)
        {
            this.dataSource = dataSource;
            this.dataSourcePath = dataSourcePath;
        }
    }

    /// <summary>
    /// Contains information passed to binding instances when the resolved data source context has changed.
    /// </summary>
    public readonly struct DataSourceContextChanged
    {
        private readonly VisualElement m_TargetElement;
        private readonly BindingId m_BindingId;
        private readonly DataSourceContext m_PreviousContext;
        private readonly DataSourceContext m_NewContext;

        /// <summary>
        /// Returns the target element of the binding.
        /// </summary>
        public VisualElement targetElement => m_TargetElement;

        /// <summary>
        /// Returns the id of the binding.
        /// </summary>
        public BindingId bindingId => m_BindingId;

        /// <summary>
        /// Returns the previous resolved data source context of the binding.
        /// </summary>
        public DataSourceContext previousContext => m_PreviousContext;

        /// <summary>
        /// Returns the newly resolved data source of the binding.
        /// </summary>
        public DataSourceContext newContext => m_NewContext;

        internal DataSourceContextChanged(VisualElement element, in BindingId bindingId, in DataSourceContext previousContext, in DataSourceContext newContext)
        {
            m_TargetElement = element;
            m_BindingId = bindingId;
            m_PreviousContext = previousContext;
            m_NewContext = newContext;
        }
    }
}
