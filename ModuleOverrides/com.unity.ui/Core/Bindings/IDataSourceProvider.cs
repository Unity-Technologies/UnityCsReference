// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Properties;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Binding types deriving from <see cref="Binding"/> can implement this interface to gain
    /// an additional layer of data source and data source path. These will be used by the binding system
    /// to compute the final <see cref="BindingContext.dataSource"/> and <see cref="BindingContext.dataSourcePath"/> that
    /// will be passed to the <see cref="BindingContext"/> during the binding update.
    /// </summary>
    /// <remarks>
    /// This <see cref="dataSource"/> and <see cref="dataSourcePath"/> will only affect the binding itself and not the
    /// hierarchy.
    /// </remarks>
    public interface IDataSourceProvider
    {
        /// <summary>
        /// Data source object that is local to the binding object.
        /// </summary>
        public object dataSource { get; }

        /// <summary>
        /// Data source path that is local to the binding object.
        /// </summary>
        public PropertyPath dataSourcePath { get; }
    }
}
