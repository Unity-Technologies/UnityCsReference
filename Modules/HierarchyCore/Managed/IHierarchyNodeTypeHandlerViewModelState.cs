// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Hierarchy
{
    /// <summary>
    /// State that a <see cref="HierarchyNodeTypeHandlerBase"/> keeps for one <see cref="HierarchyViewModel"/>.
    /// </summary>
    /// <remarks>
    /// Handlers are shared by every view model on a hierarchy, so anything belonging to a single one is stored here
    /// instead. The view model disposes whatever is left when it is disposed; every other lifetime decision, including
    /// when to release individual fields and when to invalidate them, belongs to the handler.
    /// </remarks>
    public interface IHierarchyNodeTypeHandlerViewModelState : IDisposable
    {
    }
}
