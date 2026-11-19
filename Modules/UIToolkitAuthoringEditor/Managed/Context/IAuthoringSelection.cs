// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// What is selected in one <see cref="AuthoringContext"/>.
/// </summary>
/// <remarks>
/// Follows the semantics of <see cref="UnityEditor.Selection"/>: setting the active object selects only it, and
/// after any change the active object is part of the selection, or the first selected object when it is not.
/// </remarks>
[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
interface IAuthoringSelection
{
    event Action Changed;

    UnityEngine.Object ActiveObject { get; set; }

    EntityId ActiveEntityId { get; set; }

    /// <summary>
    /// A copy of the selected ids, like <see cref="UnityEditor.Selection.entityIds"/>.
    /// </summary>
    EntityId[] EntityIds { get; set; }

    UnityEngine.Object[] Objects { get; }

    bool Contains(EntityId entityId);

    void Add(EntityId entityId);

    void Remove(EntityId entityId);

    void Clear();
}
