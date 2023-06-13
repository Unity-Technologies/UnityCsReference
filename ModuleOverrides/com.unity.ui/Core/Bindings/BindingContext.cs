// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Properties;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Context object containing the necessary information to resolve a binding.
    /// </summary>
    public readonly struct BindingContext
    {
        private readonly VisualElement m_TargetElement;
        private readonly BindingId m_BindingId;
        private readonly PropertyPath m_DataSourcePath;
        private readonly object m_DataSource;

        /// <summary>
        /// The target element of the binding.
        /// </summary>
        public VisualElement targetElement => m_TargetElement;

        /// <summary>
        /// The binding ID of the element to bind.
        /// </summary>
        public BindingId bindingId => m_BindingId;

        /// <summary>
        /// The resolved path to the value in the source, including relative data source paths found in the hierarchy
        /// between the target and to the resolved source owner.
        /// </summary>
        public PropertyPath dataSourcePath => m_DataSourcePath;

        /// <summary>
        /// The data source that was resolved for a given binding.
        /// </summary>
        /// <remarks>
        /// If a <see cref="Binding"/> implements the <see cref="IDataSourceProvider"/> interface and provides its own data source, it will automatically be used as the
        /// resolved data source; otherwise the data source will be resolved to the first valid data source on the target
        /// element or its ancestors. This value can be <see langword="null"/>.
        /// </remarks>
        public object dataSource => m_DataSource;

        internal BindingContext(
            VisualElement targetElement,
            BindingId bindingId,
            PropertyPath resolvedDataSourcePath,
            object resolvedDataSource)
        {
            m_TargetElement = targetElement;
            m_BindingId = bindingId;
            m_DataSourcePath = resolvedDataSourcePath;
            m_DataSource = resolvedDataSource;
        }
    }
}

