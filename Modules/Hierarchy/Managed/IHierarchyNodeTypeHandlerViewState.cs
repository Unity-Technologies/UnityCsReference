// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace Unity.Hierarchy
{
    /// <summary>
    /// State that a <see cref="HierarchyNodeTypeHandler"/> keeps for one <see cref="HierarchyView"/>.
    /// </summary>
    /// <remarks>
    /// Handlers are shared by every view bound to a hierarchy, so anything belonging to a single view is stored here
    /// instead. The view disposes the state right after it unbinds the handler, and disposes whatever is left when its
    /// source hierarchy changes.
    /// </remarks>
    [VisibleToOtherModules]
    internal interface IHierarchyNodeTypeHandlerViewState : IDisposable
    {
    }
}
