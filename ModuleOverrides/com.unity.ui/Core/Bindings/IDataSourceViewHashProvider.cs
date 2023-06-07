// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Defines a component as a data source view.
    /// </summary>
    public interface IDataSourceViewHashProvider
    {
        /// <summary>
        /// Returns the hash code of the view, which can be used to notify the data binding system to refresh.
        /// </summary>
        /// <returns>The hash code of the view.</returns>
        long GetViewHashCode();
    }
}
