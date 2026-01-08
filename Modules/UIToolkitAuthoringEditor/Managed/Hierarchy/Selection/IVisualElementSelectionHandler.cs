// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal interface IVisualElementSelectionHandler
{
    void SetEditingManager(IVisualElementEditingManager manager);
    EntityId AcquireInstanceId(VisualElement element);
    void ReleaseInstanceId(VisualElement element);

    void Remap(List<VisualElementRemap> candidates);
}

internal interface IVisualElementEditingManager
{
    VisualElementEditFlags GetEditFlags(VisualElement element);
}

